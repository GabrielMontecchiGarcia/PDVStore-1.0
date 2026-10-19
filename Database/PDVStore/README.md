# Banco de Dados — PDVStore Web (SQL Server)

Scripts versionados, schema compatível com o banco do **PDVStore Windows** e
procedures opcionais de relatório. O provider usado pela API é o
`Microsoft.EntityFrameworkCore.SqlServer`.

## Ordem de execução

| Script | Quando usar | Conteúdo |
|---|---|---|
| `Scripts/001_schema_e_seed.sql` | Banco **novo** | Cria todas as tabelas, PKs, FKs, índices, histórico de migrations e seeds (Admin e Caixa 1). Gerado a partir da migration EF `InitialCreate` (idempotente). |
| `Scripts/002_procedures_relatorios.sql` | Opcional | `sp_RelatorioItensMaisVendidos`, `sp_FecharCaixa`, `sp_ResumoPagamentosPorForma`. |
| `Scripts/003_procedures_dashboard.sql` | Opcional | `sp_MovimentacoesPorDia`, `sp_AlertasEstoqueMinimo`, `sp_ResumoDashboard`. |
| `Scripts/004_baseline_banco_existente.sql` | Banco **já existente** do sistema Windows | Apenas registra a migration inicial e cria índices adicionais. **Não** recria tabelas. |
| `Scripts/005_dados_exemplo.sql` | Avaliação/demonstração | Produtos, clientes e fornecedor de exemplo (idempotente). Não usar em produção. |

Exemplo com `sqlcmd`:

```bash
sqlcmd -S localhost,1433 -U sa -P "SuaSenha" -C -Q "IF DB_ID('PDVStore') IS NULL CREATE DATABASE PDVStore"
sqlcmd -S localhost,1433 -U sa -P "SuaSenha" -C -d PDVStore -i Scripts/001_schema_e_seed.sql
sqlcmd -S localhost,1433 -U sa -P "SuaSenha" -C -d PDVStore -i Scripts/002_procedures_relatorios.sql
sqlcmd -S localhost,1433 -U sa -P "SuaSenha" -C -d PDVStore -i Scripts/003_procedures_dashboard.sql
```

## Schema (compatível com o sistema Windows)

| Tabela | PK | FKs / comportamento |
|---|---|---|
| `Usuarios` | `Id` identity | — |
| `Produtos` | `Id` identity | — |
| `Clientes` | `Id` identity | — |
| `Fornecedores` | `Id` identity | — |
| `Caixas` | `Id` identity | `UsuarioCaixaId → Usuarios` (Restrict) |
| `Vendas` | `Id` identity | `UsuarioCaixaId → Usuarios` (Restrict), `CaixaId → Caixas` (Restrict), `ClienteId → Clientes` (SetNull) |
| `ItensVendas` | `Id` identity | `VendaId → Vendas` (Cascade), `ProdutoId → Produtos` (Restrict) |
| `Compras` | `Id` identity | `FornecedorId → Fornecedores` (Restrict), `UsuarioCaixaId → Usuarios` (Restrict) |
| `ItensCompras` | `Id` identity | `CompraId → Compras` (Cascade), `ProdutoId → Produtos` (Restrict) |
| `MovimentacoesEstoque` | `Id` identity | `ProdutoId → Produtos` (Restrict), `UsuarioId → Usuarios` (Restrict, nullable) |

Tipos: valores monetários `decimal(18,2)`; datas `datetime2` (**UTC**);
booleanos `bit`; textos `nvarchar(max)` (mesmo padrão do EF no projeto original).

### Constraints e índices

- PKs identity em todas as tabelas; FKs conforme tabela acima.
- Índices automáticos nas FKs.
- Índices adicionais **não únicos** para relatórios por período:
  `Vendas(DataVenda)`, `Compras(DataCompra)`,
  `MovimentacoesEstoque(DataMovimentacao)` e
  `MovimentacoesEstoque(ProdutoId, DataMovimentacao)`.
- **Por que não indexar Nome/CPF/CNPJ/Código de barras?** Essas colunas são
  `nvarchar(max)`, exatamente como no banco original. O SQL Server não indexa
  `nvarchar(max)` diretamente e as buscas usam `LIKE '%termo%'`, que não se
  beneficia de um índice B-tree comum. Uma otimização futura seria Full-Text
  Search, sem alterar as regras atuais.

## Seeds

| Registro | Valores |
|---|---|
| `Usuarios` Id=1 | `Admin`, perfil Administrador, hash BCrypt da senha `admin123`, ativo |
| `Caixas` Id=1 | caixa fechado de compatibilidade para vendas legadas com `CaixaId=1` |

> Troque a senha do Admin após o primeiro acesso. O seed é o mesmo do projeto
> Windows, portanto o hash é interoperável entre as duas aplicações.

## Migrations (EF Core)

A migration inicial fica em `Web/Backend/Repository/Migrations`:

- `20261019203046_InitialCreate` — cria o schema acima.
- `PDVStoreDbContextModelSnapshot` — snapshot do modelo.

Comandos (a partir de `Web/Backend`):

```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=PDVStore;User Id=sa;Password=SuaSenha;TrustServerCertificate=True;"
dotnet ef database update \
  --project Repository/PDVStore.Repository.csproj \
  --startup-project API/PDVStore.API.csproj

# nova migration após mudança de modelo:
dotnet ef migrations add NomeDaMigration \
  --project Repository/PDVStore.Repository.csproj \
  --startup-project API/PDVStore.API.csproj \
  --output-dir Migrations
```

A API também pode aplicar migrations no startup com
`Database:ApplyMigrationsOnStartup=true` (equivalente ao passo 3 da splash
original).

## Banco existente do sistema Windows

1. Faça backup do banco.
2. Aponte `ConnectionStrings:DefaultConnection` para o banco existente
   (ex.: `PDV_StoreDB`).
3. Execute `004_baseline_banco_existente.sql` **no banco existente**.
4. Inicie a API com `Database:ApplyMigrationsOnStartup=false`.

Não há alteração de colunas, tipos ou dados em relação ao modelo original;
apenas índices de data são acrescentados.

## Procedures

As procedures são **opcionais** e reproduzem consultas já implementadas na API
com EF Core (parâmetros tipados, sem concatenação de SQL). Servem para BI,
integrações ou consultas manuais. A aplicação não depende delas.

## Backup e retenção

Recomenda-se backup completo diário e backup de log a cada 15 minutos em
produção, com retenção definida pela política da loja. O sistema não executa
exclusões físicas de cadastros (soft delete), reduzindo risco de perda lógica.
