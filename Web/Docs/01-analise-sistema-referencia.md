# Análise do Sistema de Referência — PDVStore (WinForms)

> Documento produzido **antes** de qualquer implementação. O projeto Windows original
> (`/workspace/Models`, `/workspace/Services`, `/workspace/Forms`, `/workspace/Data`, ...)
> **não foi alterado**. Toda a nova aplicação vive em `/workspace/Web` e
> `/workspace/Database`.

## 1. Arquitetura identificada

O PDVStore original é uma aplicação desktop **C# / .NET 8 / Windows Forms**, com:

| Camada | Pasta original | Responsabilidade |
|---|---|---|
| Apresentação | `Forms/` | 13 telas WinForms montadas em código (login, splash, menu, PDV, cadastros, caixa, dashboard, sobre) |
| ViewModels | `ViewModels/` | `PDVViewModel` (carrinho/total) e `DashboardViewModel` (orquestra relatórios) |
| Serviços de negócio | `Services/` | Venda, Caixa, Estoque, Cliente, Fornecedor, Compra, Usuário, Relatório, Exportação e Verificação do sistema |
| Domínio | `Models/` | Entidades POCO + enum de permissão + métodos de senha/permissão |
| Persistência | `Data/` + `Migrations/` | `PDVContext` (EF Core 9 / SQL Server), Fluent API, seeds e 7 migrations |
| Integrações | `Integrations/` | `PagamentoIntegrator` (mock PIX/cartão) |
| Infra transversal | `Helpers/` | Connection string, máscaras/validações, tema, gráficos, prompt |
| Testes | `PDVStore.Tests/` | 165 testes NUnit (serviços, viewmodels, modelos, máscaras, banco) |

Composição: `Program.cs` usa `Microsoft.Extensions.Hosting` + DI com `AddTransient` para
serviços e formulários; cada login abre um escopo próprio de `PDVContext`; o usuário
autenticado fica em `Session.CurrentUser` (estático). O fluxo pré-login (`frmSplash`) usa
`VerificacaoSistemaService` para checar configuração, conexão, migrations e administrador.

> **Conclusão arquitetural:** não existe Repository/Unit of Work formal; os serviços
> acessam o `PDVContext` diretamente. A versão web preserva as regras, mas aplica
> Clean Architecture + Repository + UoW + DTO + DI, sem alterar comportamento.

## 2. Entidades encontradas

| Entidade | Tabela | Campos relevantes | Regras |
|---|---|---|---|
| `UsuarioCaixa` | `Usuarios` | Nome (login), SenhaHash (BCrypt), Permissao (1=Operador, 2=Administrador, 3=Estoquista), FotoPath, Ativo, CreatedAt | Nome obrigatório; senha ≥ 6 (tela); nome único (tela); soft delete |
| `Produto` | `Produtos` | Nome, CodigoBarras, Preco, PrecoCusto, Estoque, EstoqueMinimo, Categoria, Descricao, Ativo | Todos os campos obrigatórios na tela; preços/estoques ≥ 0; soft delete; `EstoqueAtual` é `[NotMapped]` |
| `Cliente` | `Clientes` | Nome, CpfCnpj, Telefone, Email, Endereco, LimiteCredito, SaldoDevedor, Ativo, CadastradoEm | Nome obrigatório; CPF/CNPJ válido; telefone ≥ 10 dígitos; e-mail válido; endereço obrigatório; limite ≥ 10 (tela); soft delete |
| `Fornecedor` | `Fornecedores` | Nome, Cnpj, InscricaoEstadual, Telefone, Email, Ativo | Nome, CNPJ válido, IE válida (14), telefone ≥ 10, e-mail válido; soft delete |
| `Caixa` | `Caixas` | UsuarioCaixaId, Abertura, Fechamento, ValorInicial, ValorFinal, Sangria, Status (`Aberto`/`Fechado`) | Um único caixa aberto; fechamento = inicial + vendas concluídas − sangria; sangria > 0 |
| `Venda` | `Vendas` | UsuarioCaixaId, CaixaId, ClienteId?, DataVenda, ValorTotal, Desconto, FormaPagamento, PixTxId, Status (`Concluida`/`Cancelada`/`Pendente`) | Caixa aberto; ≥ 1 item; estoque; total ≥ 0; fiado com cliente ativo e limite; dinheiro com valor recebido ≥ total (tela) |
| `ItemVenda` | `ItensVendas` | VendaId, ProdutoId, Quantidade, PrecoUnitario; `Subtotal` computado | Quantidade/Preço conforme produto |
| `Compra` | `Compras` | FornecedorId, UsuarioCaixaId, DataCompra, NumeroNota?, ValorTotal, Status (`Concluida`/`Cancelada`) | ≥ 1 item; qtd > 0; custo ≥ 0 (serviço) |
| `ItemCompra` | `ItensCompras` | CompraId, ProdutoId, Quantidade, PrecoCusto; `Subtotal` computado | idem |
| `MovimentacaoEstoque` | `MovimentacoesEstoque` | ProdutoId, Tipo (`Entrada`/`Saída`), Quantidade, PrecoUnitario?, DataMovimentacao, UsuarioId?, Motivo, ReferenciaVendaId? | Histórico de entradas/saídas |
| `FormaPagamento` | — | Entidade existe, mas a tabela foi removida; pagamento virou texto em `Vendas` | Formas fixas: Dinheiro, PIX, Cartão Crédito, Cartão Débito, Fiado |

## 3. Relação entre tabelas

```
Usuarios 1─N Caixas            (FK Caixas.UsuarioCaixaId, Restrict)
Usuarios 1─N Vendas            (FK Vendas.UsuarioCaixaId, Restrict)
Usuarios 1─N Compras           (FK Compras.UsuarioCaixaId, Restrict)
Usuarios 1─N Movimentacoes     (FK MovimentacoesEstoque.UsuarioId, Restrict, nullable)

Caixas   1─N Vendas            (FK Vendas.CaixaId, Restrict; CaixaId default 1)
Clientes 1─N Vendas            (FK Vendas.ClienteId, SetNull — venda sem cliente permitida)

Vendas   1─N ItensVendas       (FK ItensVendas.VendaId, Cascade)
Produtos 1─N ItensVendas       (FK ItensVendas.ProdutoId, Restrict)

Fornecedores 1─N Compras       (FK Compras.FornecedorId, Restrict)
Compras      1─N ItensCompras  (FK ItensCompras.CompraId, Cascade)
Produtos     1─N ItensCompras  (FK ItensCompras.ProdutoId, Restrict)

Produtos 1─N Movimentacoes     (FK MovimentacoesEstoque.ProdutoId, Restrict)
```

Chaves primárias `Id` identity (SQL Server `int IDENTITY(1,1)`); valores monetários
`decimal(18,2)`; datas `datetime2` em **UTC**; strings `nvarchar(max)` (padrão do EF);
booleanos `bit`. Índices originais: todas as FKs. Seeds: `Caixa Id=1` fechado e
`Usuario Id=1` Admin (hash BCrypt de `admin123`, perfil Administrador).

## 4. Serviços e fluxos principais

### 4.1 Autenticação
`frmLogin` → `UsuarioService.AutenticarAsync(nome, senha)` → busca por nome (AsNoTracking)
→ `UsuarioCaixa.Autenticar` (BCrypt.Verify). Credencial errada: “Credenciais inválidas!”.
Usuário inativo: “Usuário inativo. Contate o administrador.”. O menu exibe atalhos conforme
`TipoPermissao` (Admin: tudo; Estoquista: Produtos; Operador: PDV).

### 4.2 Venda (`VendaService.RegistrarVendaAsync`) — transação única
1. Exige ao menos 1 item e forma de pagamento.
2. Exige **caixa aberto** (`CaixaService.ObterCaixaAbertoAsync`), senão
   “Nenhum caixa aberto. Abra o caixa antes de registrar vendas.”.
3. Fiado: exige cliente, cliente ativo, e `SaldoDevedor + total <= LimiteCredito`
   somente quando `LimiteCredito > 0` (limite 0 = sem bloqueio).
4. `DataVenda = UTC`; `ValorTotal = Σ(quantidade × preço) − desconto`; negativo bloqueado.
5. Valida estoque de cada produto (mensagem “Estoque insuficiente para o produto 'X'.”).
6. Grava venda + itens; baixa estoque; grava movimentações “Saída” com motivo
   `Venda #id` e `ReferenciaVendaId`.
7. Fiado aumenta `SaldoDevedor`.
8. `PagamentoIntegrator` (mock) sempre aprova, gera `PixTxId` (GUID) para PIX; falha
   desfaz a transação.
9. Commit; qualquer exceção → rollback e relança.

Regras adicionais da tela: valor recebido em dinheiro deve cobrir o total (troco ≥ 0);
o desconto nunca é negativo; o preço unitário vem do produto carregado no PDV.

### 4.3 Cancelamento de venda
Devolve estoque item a item, grava movimentação “Entrada” motivo `Estorno - Venda #id`,
reduz `SaldoDevedor` do cliente em vendas fiadas com proteção `Math.Max(0, ...)` e marca
`Status = "Cancelada"`. Motivo recebido não é persistido (não existe coluna).

### 4.4 Caixa
- Abrir: falha se já existe caixa `Aberto`; grava valor inicial, usuário e data UTC.
- Fechar: só se `Aberto`; `ValorFinal = ValorInicial + Σ(vendas concluídas) − Sangria`.
- Sangria: valor > 0; acumula em `Sangria`; só em caixa aberto.
- Listar: todos, mais recentes primeiro, com usuário.

### 4.5 Estoque e produtos
CRUD com soft delete (`Ativo=false`); busca por nome/código de barras apenas ativos;
entrada/saída manual > 0; saída bloqueada se saldo insuficiente; movimentação registrada
com usuário logado.

### 4.6 Compras
≥ 1 item; qtd > 0; custo ≥ 0; todos os produtos existem; `ValorTotal = Σ subtotais`;
aumenta estoque, atualiza `PrecoCusto` quando > 0 e grava movimentação “Entrada” com motivo
`Compra #id - nota`. Cancelamento reduz estoque (`Math.Max(0, ...)`) e marca `Cancelada`
(sem movimentação de estorno — comportamento original preservado).

### 4.7 Clientes / fornecedores
Upsert com nome obrigatório (serviço); demais validações na tela; desativação lógica;
recebimento de fiado > 0 com `Math.Max(0, ...)`. Não há reativação na UI, mas o serviço
suporta `AtualizarStatusAsync(id, true)`.

### 4.8 Dashboard e relatórios
Período padrão: 1º dia do mês até hoje. Indicadores: total e quantidade de vendas
concluídas; agrupamento por forma de pagamento; ranking top 10 de itens vendidos;
alertas `Estoque <= EstoqueMinimo` para ativos; movimentações por dia (entradas × saídas);
vendas por dia. Exportação PDF (iText) e Excel (EPPlus) do que está na grade.
Datas UTC convertidas para o fuso local (`ToLocalTime().Date`) nos agrupamentos.

### 4.9 Verificação pré-login
4 passos: connection string contém `Database=`; `CanConnectAsync`; aplica migrations
pendentes; garante administrador ativo (cria `Admn`/`admin123` se não houver nenhum e
zera a senha do admin para `admin123` quando diferente). **Esse comportamento de
“resetar senha do admin” é um risco de segurança e não será replicado cegamente na web**
(ver seção 7).

## 5. Integrações e dependências

| Dependência | Uso | Equivalente web |
|---|---|---|
| EF Core 9 + SQL Server | Persistência | Mesmo provider, DbContext equivalente |
| BCrypt.Net-Next 4.2.0 | Hash de senha | Mantido (mesmo hash/seed, interoperável) |
| Serilog (arquivo diário, 14 dias) | Log | Serilog console + arquivo rotativo |
| iText 9 | PDF | QuestPDF (licença Community, MIT-like para uso do projeto) |
| EPPlus 8 | Excel | ClosedXML (MIT) |
| `PagamentoIntegrator` | Mock PIX/cartão | Mesmo mock, com `ILogger` |
| `Session` estático | Usuário logado | Claim `NameIdentifier` no JWT |
| `Microsoft.Extensions.Hosting` | DI | DI nativa do ASP.NET Core |

## 6. Validações mapeadas (fonte: telas + serviços + helpers)

- **CPF**: 11 dígitos, não todos iguais, dois dígitos verificadores (módulo 11).
- **CNPJ**: 14 dígitos, não todos iguais, pesos 5..2 / 6..2.
- **Inscrição Estadual (SP)**: 14 dígitos, pesos 6..2 sobre 13, DV com regra 10→1 e 11→0.
- **E-mail**: regex `^[^@\s]+@[^@\s]+\.[^@\s]+$`.
- **Telefone**: mínimo 10 dígitos (máscara progressiva).
- **Decimal**: aceita vírgula ou ponto; nunca negativo; último separador é o decimal.
- **Usuário**: nome obrigatório; senha ≥ 6; confirmação igual; nome único.
- **Cliente**: ver tabela da seção 2 (inclusive limite ≥ 10 e endereço obrigatório).
- **Produto**: barcode, nome, preço, custo, estoque, mínimo, categoria e descrição.
- **Fornecedor**: nome, CNPJ, telefone, e-mail, IE.
- **Venda/Compra/Caixa**: mensagens da seção 4.


## 7. Riscos da migração para web e decisões

| # | Risco / dúvida | Decisão documentada |
|---|---|---|
| 1 | `Session` estático e `DbContext` por login não existem na web | JWT com `NameIdentifier`, `Name` e `Role`; `DbContext` **scoped** por requisição; usuário vem do token, nunca do corpo |
| 2 | Serviço só valida nome em cliente/fornecedor, mas a tela valida CPF/CNPJ/telefone/e-mail/endereço/limite | A API valida **todas** as regras da tela (fonte oficial inclui a UI). Cadastros legados inválidos passam a ser rejeitados em edição — documentado como adaptação |
| 3 | Preço unitário da venda vem do cliente | Mantido (fidelidade), com validação `>= 0` e `quantidade > 0`; risco de adulteração por cliente web é registrado. Não foi criada regra nova de recálculo |
| 4 | Concorrência de estoque: checagem + baixa em transação Read Committed | Mantido o comportamento original. Recomendação futura: `rowversion`/UPDLOCK (não implementado para não alterar schema) |
| 5 | `ValorRecebido`/troco não são persistidos | A API recebe `valorRecebido` e valida `>= total` para Dinheiro (regra da tela); o valor não é gravado, igual ao original |
| 6 | Fechamento de caixa soma todas as vendas concluídas do caixa, inclusive PIX/cartão/fiado | Mantido fielmente (regra existente), embora contabilmente discutível |
| 7 | Cancelamento de compra não gera movimentação de estorno | Mantido fielmente; apenas o saldo é reduzido |
| 8 | Tela de compras exige “Nº da nota” numérico > 0, mas o domínio/manual diz opcional | Mantido **opcional em texto** (serviço + manual). A obrigatoriedade numérica da tela é tratada como defeito de UI e registrada; não replicada |
| 9 | Verificação pré-login reseta a senha do admin para `admin123` | **Não replicado** por risco crítico de segurança. O seed Admin é criado apenas quando o banco está vazio; troca de senha é permitida. Documentado como desvio justificado |
| 10 | Datas agrupadas com `ToLocalTime()` (fuso da máquina) | Servidor usa fuso configurável `App:TimeZone` (padrão `America/Sao_Paulo`), documentado; armazenamento continua UTC |
| 11 | Sem unicidade de nome de usuário/código de barras no banco | Serviço valida nome de usuário duplicado (como a tela). Código de barras continua sem unicidade (não há regra no original) |
| 12 | Exclusões físicas proibidas | Soft delete preservado em produtos, clientes, fornecedores e usuários |
| 13 | JSON trafega CPF e dados pessoais | HTTPS obrigatório em produção, JWT, autorização por perfil e logs sem dados sensíveis. Criptografia de campos alteraria o schema e quebraria compatibilidade — decisão registrada |
| 14 | CSRF | JWT enviado no header `Authorization` (não em cookie), portanto CSRF não se aplica; CORS restrito às origens configuradas |
| 15 | XSS | React escapa conteúdo por padrão; proibido `dangerouslySetInnerHTML`; API não devolve HTML; headers de segurança |
| 16 | Arredondamento monetário | Nenhum arredondamento adicional (igual ao original); SQL `decimal(18,2)` |
| 17 | `Compra.UsuarioCaixaId` obrigatório | Usuário autenticado do token; compras não podem ser anônimas |
| 18 | Upload de foto do usuário (WinForms) | Web recebe `fotoPath` (string) e mantém o campo; upload de arquivo não foi implementado para não criar armazenamento novo sem requisito |
| 19 | Migrations EF da web | Migration inicial própria + scripts SQL versionados equivalentes ao snapshot original |
| 20 | Relatórios síncronos | Consultas assíncronas equivalentes; exportações PDF/Excel mantidas |

## 8. Fluxos que serão implementados na versão web

1. Login/logout, sessão JWT, troca de senha, autorização por perfil.
2. PDV completo (busca, carrinho, desconto, 5 formas de pagamento, fiado, troco, recibo).
3. Caixa (abrir, sangria, fechar, histórico).
4. Produtos (CRUD, soft delete) e Estoque (entrada/saída + histórico).
5. Clientes (CRUD, limite/saldo, receber fiado).
6. Fornecedores (CRUD, soft delete).
7. Compras (registrar com itens, listar, cancelar).
8. Dashboard + relatórios e exportação PDF/Excel.
9. Usuários (CRUD, perfis, ativação/desativação).
10. Sobre (informações do sistema).

## 9. Matriz de autorização (espelha o menu original)

| Funcionalidade | Administrador | Operador (Caixa) | Estoquista |
|---|---|---|---|
| PDV / vendas | ✔ | ✔ | ✘ |
| Dashboard / relatórios | ✔ | ✘ | ✘ |
| Produtos (cadastro) | ✔ | ✘ | ✔ |
| Estoque (ajustes/histórico) | ✔ | ✘ | ✘ |
| Clientes / fornecedores / compras | ✔ | ✘ | ✘ |
| Caixa | ✔ | ✘ | ✘ |
| Usuários | ✔ | ✘ | ✘ |
| Sobre / próprio perfil | ✔ | ✔ | ✔ |

