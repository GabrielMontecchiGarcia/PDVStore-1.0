# PDVStore Web

Versão web do **PDVStore** (sistema de Ponto de Venda originalmente em Windows Forms),
construída em **React + TypeScript** (frontend), **C# / .NET 8** (API REST) e
**SQL Server**. O projeto Windows existente **não foi alterado**: todo o código novo
está em `/Web` e `/Database`.

> A fonte oficial da verdade é o sistema Windows. As regras de negócio, mensagens,
nomenclatura e schema foram mapeados antes da implementação em
[`Docs/01-analise-sistema-referencia.md`](Docs/01-analise-sistema-referencia.md).

---

## Sumário

- [Stack](#stack)
- [Estrutura](#estrutura)
- [Como executar](#como-executar)
- [Acesso inicial e perfis](#acesso-inicial-e-perfis)
- [Endpoints da API](#endpoints-da-api)
- [Regras de negócio preservadas](#regras-de-negócio-preservadas)
- [Segurança](#segurança)
- [Testes](#testes)
- [Documentação técnica](#documentação-técnica)

---

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | React 18, TypeScript, Vite, React Router, Axios |
| Backend | C# / .NET 8, ASP.NET Core Web API, FluentValidation, Serilog |
| Persistência | Entity Framework Core 9, SQL Server, Repository Pattern + Unit of Work |
| Segurança | JWT Bearer, BCrypt, autorização por policy/perfil, rate limit no login |
| Relatórios | ClosedXML (Excel) e QuestPDF (PDF) |
| Testes | xUnit + EF Core InMemory (72 testes) |

---

## Estrutura

```
Web/
├── Backend/
│   ├── API/              Controllers, Program, JWT, middleware, appsettings
│   ├── Application/      Interfaces de serviços, DTO mappings, validators
│   ├── Domain/           Entidades, enums, exceções, máscaras/validações
│   ├── DTOs/             Contratos de entrada/saída (DTO Pattern)
│   ├── Infrastructure/   Integrações externas: mock de pagamento e exportação PDF/Excel
│   ├── Middleware/       Tratamento global de exceções
│   ├── Repository/       DbContext, migrations, repositórios, Unit of Work
│   ├── Services/         Regras de negócio, integração de pagamento, exportação
│   └── Tests/            Testes das regras de negócio e validações
└── Frontend/
    └── src/
        ├── components/   Layout, modais, tabelas, gráficos, formulários
        ├── pages/        Login, Dashboard, PDV, Produtos, Estoque, Clientes,
        │                 Fornecedores, Compras, Caixa, Usuários, Sobre
        ├── services/     Cliente HTTP e serviços por domínio
        ├── hooks/        useApi, useDebounce, useCarrinho
        ├── contexts/     AuthContext (sessão JWT) e ToastContext
        ├── routes/       Rotas protegidas por perfil
        ├── models/       Tipos/DTOs compartilhados
        ├── utils/        Formatação, máscaras, validação, download
        └── styles/       Tokens, componentes e layout responsivo
```

> A composição de infraestrutura da aplicação (DI, Serilog, JWT, CORS) está em
> `API/Configuration` e `API/Security`; as integrações externas (mock de pagamento
> e exportação PDF/Excel) estão em `Infrastructure`; e o acesso a dados em
> `Repository`. Detalhes em `Docs/02-arquitetura-e-decisoes.md`.

---

## Como executar

### Pré-requisitos

- .NET SDK 8.0+
- Node.js 18+ e npm
- SQL Server 2019+ (LocalDB, Express, Developer ou container)
- `dotnet-ef` (opcional, para migrations): `dotnet tool install --global dotnet-ef --version 9.0.17`

### 1. Banco de dados (SQL Server)

Opção A — banco novo, usando os scripts versionados:

```bash
sqlcmd -S localhost,1433 -U sa -P "SuaSenha" -C -Q "IF DB_ID('PDVStore') IS NULL CREATE DATABASE PDVStore"
sqlcmd -S localhost,1433 -U sa -P "SuaSenha" -C -d PDVStore -i ../Database/PDVStore/Scripts/001_schema_e_seed.sql
```

Opção B — banco novo, usando migrations:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=PDVStore;User Id=sa;Password=SuaSenha;TrustServerCertificate=True;"
dotnet ef database update --project Backend/Repository/PDVStore.Repository.csproj --startup-project Backend/API/PDVStore.API.csproj
```

Opção C — apontar para o banco **já existente** do sistema Windows:
execute `Database/PDVStore/Scripts/004_baseline_banco_existente.sql` no banco e
configure a connection string. Veja `Database/PDVStore/README.md`.

### 2. Backend

```bash
cd Backend/API
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=PDVStore;User Id=sa;Password=SuaSenha;TrustServerCertificate=True;"
export Jwt__Key="uma-chave-secreta-com-no-minimo-32-caracteres"
dotnet run
```

- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Health check: `http://localhost:5080/health`

> Em `Development`, a chave JWT de exemplo está em `appsettings.Development.json`.
> Em produção, use `Jwt__Key` como secret/variável de ambiente (a API recusa
> chaves vazias ou com menos de 32 caracteres).

#### Modo demonstração (sem SQL Server)

Para avaliar a aplicação completa sem instalar banco, use o provider InMemory com
dados de exemplo (produtos, clientes e fornecedor). **Não usar em produção.**

```bash
cd Backend/API
Database__Provider=InMemory Database__SeedDemoData=true dotnet run
```

### 3. Frontend

```bash
cd Frontend
npm install
npm run dev
```

Acesse `http://localhost:5173`. O Vite faz proxy de `/api` para
`http://localhost:5080` (configurável em `vite.config.ts`). Para produção, defina
`VITE_API_URL` e rode `npm run build` (saída em `dist/`).

---

## Acesso inicial e perfis

| Usuário | Senha | Perfil |
|---|---|---|
| `Admin` | `admin123` | Administrador |

Troque a senha no primeiro acesso (ícone de cadeado na barra superior).

| Funcionalidade | Administrador | Operador (Caixa) | Estoquista |
|---|---|---|---|
| PDV / vendas | ✔ | ✔ | ✘ |
| Dashboard / relatórios | ✔ | ✘ | ✘ |
| Produtos | ✔ | ✘ | ✔ |
| Estoque (ajustes/histórico) | ✔ | ✘ | ✘ |
| Clientes / fornecedores / compras | ✔ | ✘ | ✘ |
| Caixa | ✔ | ✘ | ✘ |
| Usuários | ✔ | ✘ | ✘ |
| Sobre / alterar a própria senha | ✔ | ✔ | ✔ |

---

## Endpoints da API

Todas as rotas exigem `Authorization: Bearer <token>`, exceto login e health.

| Método | Rota | Perfil | Descrição |
|---|---|---|---|
| POST | `/api/auth/login` | público | Login (rate limit de 10/min por IP) |
| GET | `/api/auth/me` | autenticado | Usuário da sessão |
| POST | `/api/auth/alterar-senha` | autenticado | Troca da própria senha |
| GET/POST/PUT/DELETE | `/api/usuarios[...]` | Administrador | CRUD com soft delete |
| GET | `/api/produtos`, `/api/produtos/{id}`, `/api/produtos/codigo-barras/{codigo}` | autenticado | Consulta (PDV precisa) |
| POST/PUT/DELETE | `/api/produtos[...]` | Admin/Estoquista | Cadastro e soft delete |
| GET | `/api/produtos/movimentacoes` | Administrador | Histórico de estoque |
| POST | `/api/produtos/{id}/estoque/entrada` e `/saida` | Administrador | Ajuste manual |
| GET | `/api/clientes`, `/api/clientes/{id}` | autenticado | Consulta (PDV precisa) |
| POST/PUT/DELETE | `/api/clientes[...]` | Administrador | Cadastro, status e soft delete |
| POST | `/api/clientes/{id}/receber-fiado` | Administrador | Baixa de fiado |
| GET/POST/PUT/DELETE | `/api/fornecedores[...]` | Administrador | CRUD com soft delete |
| GET | `/api/caixas/aberto` | autenticado | Caixa aberto ou 204 |
| GET | `/api/caixas` | Administrador | Histórico |
| POST | `/api/caixas/abrir`, `/{id}/sangria`, `/{id}/fechar` | Administrador | Operações de caixa |
| POST | `/api/vendas` | Admin/Operador | Registra a venda |
| GET | `/api/vendas/{id}` | Admin/dono | Detalhe da venda |
| GET | `/api/vendas` | Administrador | Vendas do período |
| POST | `/api/vendas/{id}/cancelar` | Administrador | Estorno de estoque/fiado |
| GET/POST | `/api/compras[...]` | Administrador | Compras e cancelamento |
| GET | `/api/dashboard` | Administrador | Indicadores consolidados |
| GET | `/api/relatorios/itens-mais-vendidos`, `/estoque-minimo`, `/total-vendas` | Administrador | Relatórios |
| GET | `/api/relatorios/exportar/{tipo}/{excel\|pdf}` | Admin (produtos: também Estoquista) | Exportação |
| GET | `/api/sobre` | autenticado | Informações do sistema |
| GET | `/health` | público | Health check |

Datas de filtro (`inicio`, `fim`) usam o formato `yyyy-MM-dd`, são interpretadas no
fuso `App:TimeZone` (padrão `America/Sao_Paulo`) e convertidas para UTC.

---

## Regras de negócio preservadas

- **Venda** exige caixa aberto, ao menos 1 item, estoque suficiente e valor total
  não negativo; em dinheiro, o valor recebido precisa cobrir o total (troco ≥ 0).
- **Fiado** exige cliente ativo; bloqueia quando `saldo devedor + venda > limite`,
  apenas se `limite > 0` (limite 0 = sem bloqueio).
- **Venda** grava movimentações “Saída” com custo, usuário e referência; PIX recebe
  `PixTxId` (mock). O cancelamento devolve estoque, registra “Entrada” de estorno e
  reduz o saldo devedor com `Math.Max(0, ...)`.
- **Caixa**: um único aberto; fechamento = `valor inicial + vendas concluídas - sangrias`;
  sangria exige valor > 0.
- **Compra** exige fornecedor, ao menos 1 item, quantidade > 0 e custo ≥ 0; dá entrada
  no estoque, atualiza o custo quando > 0 e registra movimentação. Cancelamento reduz
  o estoque sem gerar movimentação (comportamento original).
- **Cadastros** usam soft delete; CPF/CNPJ/IE, telefone, e-mail, endereço e limite
  seguem as validações das telas WinForms (ver `Docs/01-analise-sistema-referencia.md`).
- **Datas** são armazenadas em UTC; o dashboard agrupa por dia no fuso configurado.

---

## Segurança

- **JWT** assinado com HMAC-SHA256; claims de Id, nome e perfil. Token em
  `sessionStorage` (não em cookie), enviado via header `Authorization` — CSRF não se
  aplica a esse modelo.
- **Senhas** com BCrypt (mesmo hash/seed do sistema Windows). Nunca são retornadas
  pela API nem gravadas em log.
- **Autorização por perfil** com policies (`Administrador`, `Pdv`, `Produtos`),
  espelhando o menu original. O operador da venda vem do token, nunca do corpo.
- **SQL Injection**: EF Core com consultas parametrizadas; nenhuma concatenação de
  SQL com entrada do usuário.
- **XSS**: React escapa o conteúdo por padrão; não há `dangerouslySetInnerHTML`; a
  API responde apenas JSON e envia headers `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy` e `Permissions-Policy`.
- **Força bruta**: rate limit de 10 tentativas de login por minuto por IP.
- **Erros**: middleware global devolve mensagens de negócio em PT-BR, sem stack trace;
  logs estruturados no Serilog (console + arquivo diário).
- **Produção**: use HTTPS/HSTS, `Jwt__Key` e `ConnectionStrings__DefaultConnection`
  por secret/variável de ambiente, e restrinja `Cors:Origins` ao domínio do frontend.

---

## Testes

```bash
cd Backend
dotnet test Tests/PDVStore.Tests.csproj
```

Cobrem: venda (caixa aberto, estoque, fiado/limite, troco, PIX), cancelamento,
caixa/fechamento/sangria, compras e estorno, produtos/estoque, clientes/fiado,
usuários/BCrypt, relatórios/dashboard, validadores FluentValidation e máscaras
(CPF, CNPJ, IE, e-mail, decimal).

```bash
cd Frontend
npm run build     # typecheck + build de produção
```

---

## Documentação técnica

| Documento | Conteúdo |
|---|---|
| [`Docs/01-analise-sistema-referencia.md`](Docs/01-analise-sistema-referencia.md) | Arquitetura, entidades, relações, fluxos, validações e riscos mapeados |
| [`Docs/02-arquitetura-e-decisoes.md`](Docs/02-arquitetura-e-decisoes.md) | Camadas, SOLID, dependências, decisões e divergências justificadas |
| [`Docs/03-funcionalidades-implementadas.md`](Docs/03-funcionalidades-implementadas.md) | Por funcionalidade: objetivo, arquivos, regras, endpoints e impactos |
| [`../Database/PDVStore/README.md`](../Database/PDVStore/README.md) | Scripts, schema, índices, procedures, migrations e baseline |

---

## Observações

- O projeto Windows original (`/Forms`, `/Services`, `/Models`, `/Data`, ...) não foi
  modificado. A pasta `Web` é autônoma e pode ser publicada separadamente.
- A integração de pagamento é um **mock didático** (PIX gera `TxId`, sem adquirente).
- O modo InMemory existe apenas para demonstração/desenvolvimento; o modo oficial é
  SQL Server, com migrations e scripts versionados.
