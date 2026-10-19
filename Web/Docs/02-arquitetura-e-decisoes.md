# Arquitetura e Decisões Técnicas — PDVStore Web

Documento de decisões tomadas **antes/durante** a implementação, complementando a
análise do sistema de referência (`01-analise-sistema-referencia.md`).

## 1. Visão de camadas

```
┌──────────────────────────────┐
│ Frontend React (SPA)         │  pages/components/services/hooks/contexts
└──────────────┬───────────────┘
               │ HTTP/JSON + JWT
┌──────────────▼───────────────┐
│ API (ASP.NET Core)           │  Controllers, Program, JWT, policies, Swagger
├──────────────────────────────┤
│ Middleware                   │  GlobalExceptionMiddleware (erros padronizados)
├──────────────────────────────┤
│ Application                  │  Interfaces, validators, DTO mappings
├──────────────────────────────┤
│ Services                     │  Regras de negócio (venda, caixa, compras...)
├──────────────────────────────┤
│ Infrastructure               │  Integrações externas: mock de pagamento, PDF/Excel
├──────────────────────────────┤
│ Repository                   │  DbContext, repositórios, Unit of Work, migrations
├──────────────────────────────┤
│ Domain                       │  Entidades, enums, exceções, máscaras/validações
└──────────────┬───────────────┘
               │ EF Core (parametrizado)
        SQL Server / InMemory (demo)
```

**Direção das dependências**

- `Domain` não depende de nenhum outro projeto.
- `DTOs` também é independente (contratos puros).
- `Repository` depende de `Domain` e `DTOs` (projeções de relatório).
- `Application` depende de `Domain`, `DTOs` e `Repository`; contém as **interfaces**
  dos serviços e os validators.
- `Services` depende de `Application` (implementa as interfaces), `Repository`,
  `DTOs` e `Domain`.
- `Infrastructure` depende de `Application`/`Domain`/`DTOs` e hospeda as
  integrações externas (mock de pagamento bancário e exportação PDF/Excel).
- `Middleware` depende de `Domain`/`DTOs` e do framework ASP.NET Core.
- `API` compõe tudo (DI, JWT, CORS, Serilog, Swagger).
- `Tests` instancia repositórios/serviços reais sobre EF Core InMemory.

> Essa direção evita ciclos: os contratos ficam em `Application`, as implementações
> em `Services`, e a composição em `API`.

## 2. Fluxo de uma requisição

1. **React** envia JSON com `Authorization: Bearer <JWT>`.
2. **API** valida o token (assinatura, emissor, audiência, expiração).
3. **Policy** verifica o perfil (`Administrador`, `Pdv`, `Produtos`).
4. **FluentValidation** valida o DTO (mensagens PT-BR).
5. **Controller** extrai o usuário do token e chama a interface do serviço.
6. **Service** executa a regra de negócio, usando repositórios e `IUnitOfWork`.
7. **Repository** traduz LINQ para SQL parametrizado.
8. **Unit of Work** confirma a transação (vendas/compras/caixa/cancelamentos).
9. **Application.Mappings** converte entidades em DTOs (sem expor persistência).
10. **Middleware** converte exceções em `ErroResposta` JSON.

## 3. Repository + Unit of Work

O sistema Windows acessava o `PDVContext` diretamente nos serviços. A versão web
introduz o **Repository Pattern** e o **Unit of Work** sem mudar o comportamento:

- `PDVStoreDbContext` é **Scoped** por requisição (equivalente ao escopo por login).
- Repositórios compartilham o mesmo contexto; `IUnitOfWork.SalvarAsync` persiste tudo.
- `IUnitOfWork.IniciarTransacaoAsync` é usado por venda, compra, cancelamento e caixa,
  com `Commit`/`Rollback` explícitos.
- Consultas de leitura usam `AsNoTracking`; operações de escrita solicitam rastreamento.
- Os repositórios não contêm regra de negócio: apenas consultas e persistência.

## 4. DTO Pattern e validação em duas camadas

- **DTOs** de entrada (`*Request`) e saída (`*Dto`) isolam o contrato HTTP das entidades.
- **FluentValidation** aplica as mensagens das telas WinForms no servidor. Exemplos:
  “Informe um CPF ou CNPJ válido.”, “Senha deve ter no mínimo 6 caracteres!”.
- **Serviços** mantêm as validações de domínio (caixa aberto, estoque, limite, etc.),
  independentemente da camada HTTP.
- O **frontend** repete as validações para feedback imediato, mas nunca é a única
  barreira: a API valida novamente.

## 5. Ciclo de vida e DI

| Serviço | Lifetime | Motivo |
|---|---|---|
| `PDVStoreDbContext` | Scoped | Uma unidade de trabalho por requisição |
| Repositórios / `IUnitOfWork` | Scoped | Compartilham o mesmo contexto |
| Serviços de negócio | Scoped | Dependem do contexto/UoW |
| `PagamentoIntegrator` (Infrastructure) | Scoped | Dependência da venda |
| `ExportacaoService` (Infrastructure) | Scoped | Sem estado entre requisições |
| `JwtTokenService` / `PeriodoService` / `TimeZoneInfo` | Singleton | Configuração imutável |

## 6. SOLID aplicado

- **S**RP: controllers só orquestram HTTP; serviços concentram regras; repositórios
  só acessam dados; validators só validam.
- **O**CP: novas formas de pagamento/relatórios podem ser adicionadas sem alterar os
  contratos existentes (ex.: `IPagamentoIntegrator`, `IExportacaoService`).
- **L**SP: implementações de repositório/serviço respeitam os contratos de `Application`.
- **I**SP: interfaces específicas por agregado (`IProdutoRepository`, `IVendaService`...).
- **D**IP: serviços dependem de interfaces; a API injeta as implementações.

## 7. Tratamento de erros e logs

- `GlobalExceptionMiddleware` mapeia:
  - regra de negócio/argumento/invalid operation → **400**;
  - não encontrado → **404**; acesso negado → **403**;
  - `DbUpdateException` → **409**; inesperado → **500** sem detalhes internos.
- Validação de modelo (`ApiBehaviorOptions`) devolve o mesmo formato `ErroResposta`,
  com todos os detalhes em PT-BR.
- Serilog grava console + `logs/pdvstore-web-AAAAMMDD.log`, retenção de 14 dias.
- Vendas/compras/cancelamentos geram logs estruturados com ids e totais, sem senhas.

## 7.1 Upload de arquivos (foto do usuário)

- `POST /api/usuarios/foto` (policy `Administrador`) recebe `multipart/form-data`.
- Validações: tamanho ≤ 2 MB, extensão em {jpg, jpeg, png, bmp} e **magic bytes**
  do arquivo (a extensão não é confiável).
- O nome do arquivo é gerado pelo servidor (`user_{data}_{guid}.png`), evitando
  path traversal e colisão; a pasta é `wwwroot/uploads/usuarios`.
- A API serve os arquivos com `app.UseStaticFiles()`; no frontend, o Vite faz
  proxy de `/uploads` e, em produção, a URL é montada com a origem da API
  (`utils/download.ts → urlDeArquivo`).

## 8. Segurança — decisões

| Tema | Decisão |
|---|---|
| Sessão | JWT HMAC-SHA256 com Id/nome/perfil; `sessionStorage` no frontend |
| Senhas | BCrypt (mesmo seed do sistema Windows) |
| Autorização | Policies por perfil + verificação de dono da venda para Operador |
| CSRF | Não aplicável: token no header `Authorization`, não em cookie |
| XSS | React escapa por padrão; sem HTML dinâmico; headers de segurança |
| SQL Injection | EF Core parametrizado; nenhuma concatenação de entrada |
| Força bruta | Rate limit fixo de 10/min por IP no login |
| Dados sensíveis | CPF/CNPJ não são criptografados no banco para preservar schema e buscas; acesso restrito por perfil, HTTPS obrigatório e logs sem PII |
| Segredos | `Jwt__Key` e `ConnectionStrings__DefaultConnection` por ambiente/secret |

## 9. Datas e fuso horário

- O banco grava **UTC** (`datetime2`), como no sistema original.
- A API recebe datas `yyyy-MM-dd` (data local), converte as bordas para UTC usando
  `App:TimeZone` (padrão `America/Sao_Paulo`) e consulta o banco.
- O dashboard agrupa por dia convertendo UTC → fuso configurado, substituindo o
  `ToLocalTime()` dependente da máquina do operador.

## 10. Divergências justificadas (resumo)

| Tema | Sistema Windows | Versão web | Justificativa |
|---|---|---|---|
| Reset automático da senha do Admin na splash | Redefine para `admin123` | **Não replicado** | Risco crítico de segurança; seed já garante o primeiro acesso |
| Nº da nota de compra | Tela exige numérico > 0 (bug), manual diz opcional | Texto opcional | Fidelidade ao domínio/manual; defeito de UI registrado |
| Preço unitário da venda | Enviado pela tela | Mantido, com validação ≥ 0 | Fidelidade; risco de adulteração documentado |
| Transações em testes | SQL Server real | EF InMemory ignora transações | Apenas nos testes; produção usa SQL Server |
| Upload de foto de usuário | Copia arquivo local | Campo `fotoPath` preservado, sem upload | Evita criar armazenamento sem requisito |
| Modo InMemory | Não existia | Opcional para demo/desenvolvimento | Facilita avaliação sem SQL Server; desativado por padrão |

Demais riscos e decisões: ver seção 7 de `01-analise-sistema-referencia.md`.

## 10.1 Testes

| Tipo | Onde | Como roda |
|---|---|---|
| Unitários (regras, validadores, máscaras) | `Web/Backend/Tests` | `dotnet test` — 74 testes, provider EF InMemory |
| Integração da API (HTTP real) | `Web/Backend/Tests/Integration` | `WebApplicationFactory` com banco InMemory isolado por teste — 8 cenários (login, 401, 403 por perfil, venda com/sem caixa, validações em PT-BR, exportação, upload) |

A execução é sequencial (`CollectionBehavior(DisableTestParallelization)`) porque
os testes de integração configuram a API por variáveis de ambiente.

## 11. Publicação

- **API**: `dotnet publish Backend/API/PDVStore.API.csproj -c Release`; configurar
  variáveis de ambiente, HTTPS e um proxy reverso (IIS/Nginx).
- **Frontend**: `npm run build`; publicar `dist/` e configurar `VITE_API_URL` para a
  URL da API. O CORS da API deve listar apenas a origem do frontend.
- **Banco**: aplicar migrations/scripts com backup prévio; nunca usar o modo InMemory
  em produção.
- **CI**: `.github/workflows/ci-web.yml` compila/testa a solução .NET (ubuntu) e faz
  typecheck + build do frontend React; o workflow do projeto Windows (`ci.yml`)
  permanece intacto.
- **Ambiente local**: `Web/docker-compose.yml` sobe apenas o SQL Server 2022
  (usuário `sa`, senha `Your_password123`, porta 1433) para desenvolvimento.
