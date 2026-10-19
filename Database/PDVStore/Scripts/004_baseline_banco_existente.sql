/* ============================================================================
   PDVStore Web — Baseline para banco já existente (sistema Windows)
   ----------------------------------------------------------------------------
   Cenário: você vai apontar a API para o MESMO banco já usado pelo PDVStore
   Windows (ex.: PDV_StoreDB). Nesse caso NÃO execute o script 001, pois as
   tabelas já existem. Execute somente este script no banco existente para
   registrar a migration inicial no histórico do EF Core.

   Depois, a API pode iniciar com Database:ApplyMigrationsOnStartup=false (o
   schema já está compatível) ou rodar `dotnet ef database update`, que não
   tentará recriar nada.

   IMPORTANTE: faça backup antes de qualquer alteração em produção.
   ============================================================================ */

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019203046_InitialCreate', N'9.0.17');
END;
GO

/* ----------------------------------------------------------------------------
   Índices adicionais de consulta (opcionais, seguros em base existente).
   São idênticos aos criados pela migration inicial e NÃO alteram dados/regras.
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Vendas_DataVenda' AND object_id = OBJECT_ID('Vendas'))
    CREATE INDEX [IX_Vendas_DataVenda] ON [Vendas] ([DataVenda]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Compras_DataCompra' AND object_id = OBJECT_ID('Compras'))
    CREATE INDEX [IX_Compras_DataCompra] ON [Compras] ([DataCompra]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MovimentacoesEstoque_DataMovimentacao' AND object_id = OBJECT_ID('MovimentacoesEstoque'))
    CREATE INDEX [IX_MovimentacoesEstoque_DataMovimentacao] ON [MovimentacoesEstoque] ([DataMovimentacao]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MovimentacoesEstoque_ProdutoId_DataMovimentacao' AND object_id = OBJECT_ID('MovimentacoesEstoque'))
    CREATE INDEX [IX_MovimentacoesEstoque_ProdutoId_DataMovimentacao]
        ON [MovimentacoesEstoque] ([ProdutoId], [DataMovimentacao]);
GO
