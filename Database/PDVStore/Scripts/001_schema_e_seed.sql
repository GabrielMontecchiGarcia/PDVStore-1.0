IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [Clientes] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [CpfCnpj] nvarchar(max) NULL,
        [Telefone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Endereco] nvarchar(max) NULL,
        [CadastradoEm] datetime2 NOT NULL,
        [Ativo] bit NOT NULL,
        [LimiteCredito] decimal(18,2) NOT NULL,
        [SaldoDevedor] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [Fornecedores] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [Cnpj] nvarchar(max) NULL,
        [InscricaoEstadual] nvarchar(max) NULL,
        [Telefone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Ativo] bit NOT NULL,
        CONSTRAINT [PK_Fornecedores] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [Produtos] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [CodigoBarras] nvarchar(max) NULL,
        [Preco] decimal(18,2) NOT NULL,
        [PrecoCusto] decimal(18,2) NOT NULL,
        [Estoque] int NOT NULL,
        [EstoqueMinimo] int NOT NULL,
        [Categoria] nvarchar(max) NULL,
        [Descricao] nvarchar(max) NULL,
        [Ativo] bit NOT NULL,
        CONSTRAINT [PK_Produtos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [Nome] nvarchar(max) NOT NULL,
        [SenhaHash] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [FotoPath] nvarchar(max) NULL,
        [Ativo] bit NOT NULL,
        [Permissao] int NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [Caixas] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioCaixaId] int NOT NULL,
        [Abertura] datetime2 NOT NULL,
        [Fechamento] datetime2 NULL,
        [ValorInicial] decimal(18,2) NOT NULL,
        [ValorFinal] decimal(18,2) NULL,
        [Sangria] decimal(18,2) NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Caixas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Caixas_Usuarios_UsuarioCaixaId] FOREIGN KEY ([UsuarioCaixaId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [Compras] (
        [Id] int NOT NULL IDENTITY,
        [FornecedorId] int NOT NULL,
        [UsuarioCaixaId] int NOT NULL,
        [DataCompra] datetime2 NOT NULL,
        [NumeroNota] nvarchar(max) NULL,
        [ValorTotal] decimal(18,2) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Compras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Compras_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [Fornecedores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Compras_Usuarios_UsuarioCaixaId] FOREIGN KEY ([UsuarioCaixaId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [MovimentacoesEstoque] (
        [Id] int NOT NULL IDENTITY,
        [ProdutoId] int NOT NULL,
        [Tipo] nvarchar(max) NOT NULL,
        [Quantidade] int NOT NULL,
        [PrecoUnitario] decimal(18,2) NULL,
        [DataMovimentacao] datetime2 NOT NULL,
        [UsuarioId] int NULL,
        [Motivo] nvarchar(max) NULL,
        [ReferenciaVendaId] int NULL,
        CONSTRAINT [PK_MovimentacoesEstoque] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MovimentacoesEstoque_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [Produtos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MovimentacoesEstoque_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [Vendas] (
        [Id] int NOT NULL IDENTITY,
        [UsuarioCaixaId] int NOT NULL,
        [DataVenda] datetime2 NOT NULL,
        [ValorTotal] decimal(18,2) NOT NULL,
        [Desconto] decimal(18,2) NOT NULL,
        [FormaPagamento] nvarchar(max) NOT NULL,
        [PixTxId] nvarchar(max) NULL,
        [Status] nvarchar(max) NULL,
        [CaixaId] int NOT NULL,
        [ClienteId] int NULL,
        CONSTRAINT [PK_Vendas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vendas_Caixas_CaixaId] FOREIGN KEY ([CaixaId]) REFERENCES [Caixas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Vendas_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Vendas_Usuarios_UsuarioCaixaId] FOREIGN KEY ([UsuarioCaixaId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [ItensCompras] (
        [Id] int NOT NULL IDENTITY,
        [CompraId] int NOT NULL,
        [ProdutoId] int NOT NULL,
        [Quantidade] int NOT NULL,
        [PrecoCusto] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ItensCompras] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ItensCompras_Compras_CompraId] FOREIGN KEY ([CompraId]) REFERENCES [Compras] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ItensCompras_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [Produtos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE TABLE [ItensVendas] (
        [Id] int NOT NULL IDENTITY,
        [VendaId] int NOT NULL,
        [ProdutoId] int NOT NULL,
        [Quantidade] int NOT NULL,
        [PrecoUnitario] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ItensVendas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ItensVendas_Produtos_ProdutoId] FOREIGN KEY ([ProdutoId]) REFERENCES [Produtos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ItensVendas_Vendas_VendaId] FOREIGN KEY ([VendaId]) REFERENCES [Vendas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Ativo', N'CreatedAt', N'FotoPath', N'Nome', N'Permissao', N'SenhaHash') AND [object_id] = OBJECT_ID(N'[Usuarios]'))
        SET IDENTITY_INSERT [Usuarios] ON;
    EXEC(N'INSERT INTO [Usuarios] ([Id], [Ativo], [CreatedAt], [FotoPath], [Nome], [Permissao], [SenhaHash])
    VALUES (1, CAST(1 AS bit), ''2026-07-03T00:00:00.0000000Z'', NULL, N''Admin'', 2, N''$2a$11$ZBR/L6.DiCjgBWjvnJOsMeVfeHtigReLCpU15E8R8FK/Kgn8n1nZa'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Ativo', N'CreatedAt', N'FotoPath', N'Nome', N'Permissao', N'SenhaHash') AND [object_id] = OBJECT_ID(N'[Usuarios]'))
        SET IDENTITY_INSERT [Usuarios] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Abertura', N'Fechamento', N'Sangria', N'Status', N'UsuarioCaixaId', N'ValorFinal', N'ValorInicial') AND [object_id] = OBJECT_ID(N'[Caixas]'))
        SET IDENTITY_INSERT [Caixas] ON;
    EXEC(N'INSERT INTO [Caixas] ([Id], [Abertura], [Fechamento], [Sangria], [Status], [UsuarioCaixaId], [ValorFinal], [ValorInicial])
    VALUES (1, ''2026-07-03T00:00:00.0000000Z'', NULL, NULL, N''Fechado'', 1, NULL, 0.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Abertura', N'Fechamento', N'Sangria', N'Status', N'UsuarioCaixaId', N'ValorFinal', N'ValorInicial') AND [object_id] = OBJECT_ID(N'[Caixas]'))
        SET IDENTITY_INSERT [Caixas] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Caixas_UsuarioCaixaId] ON [Caixas] ([UsuarioCaixaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_DataCompra] ON [Compras] ([DataCompra]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_FornecedorId] ON [Compras] ([FornecedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Compras_UsuarioCaixaId] ON [Compras] ([UsuarioCaixaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ItensCompras_CompraId] ON [ItensCompras] ([CompraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ItensCompras_ProdutoId] ON [ItensCompras] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ItensVendas_ProdutoId] ON [ItensVendas] ([ProdutoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ItensVendas_VendaId] ON [ItensVendas] ([VendaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MovimentacoesEstoque_DataMovimentacao] ON [MovimentacoesEstoque] ([DataMovimentacao]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MovimentacoesEstoque_ProdutoId_DataMovimentacao] ON [MovimentacoesEstoque] ([ProdutoId], [DataMovimentacao]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MovimentacoesEstoque_UsuarioId] ON [MovimentacoesEstoque] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vendas_CaixaId] ON [Vendas] ([CaixaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vendas_ClienteId] ON [Vendas] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vendas_DataVenda] ON [Vendas] ([DataVenda]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vendas_UsuarioCaixaId] ON [Vendas] ([UsuarioCaixaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261019203046_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261019203046_InitialCreate', N'9.0.17');
END;

COMMIT;
GO

