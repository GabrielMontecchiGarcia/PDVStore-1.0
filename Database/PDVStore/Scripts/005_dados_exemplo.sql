/* ============================================================================
   PDVStore Web — Dados de demonstração (OPCIONAL)
   ----------------------------------------------------------------------------
   Popula o banco com produtos, clientes e um fornecedor de exemplo, os mesmos
   usados no modo de demonstração (provider InMemory). Útil para avaliar a
   aplicação com SQL Server real. O script é idempotente: nada é duplicado.

   ATENÇÃO: em produção, NÃO execute este script.
   ============================================================================ */

SET NOCOUNT ON;
GO

/* --------------------------------- Produtos -------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Produtos] WHERE [CodigoBarras] = '7891000100101')
    INSERT INTO [Produtos] ([Nome], [CodigoBarras], [Preco], [PrecoCusto], [Estoque], [EstoqueMinimo], [Categoria], [Descricao], [Ativo])
    VALUES (N'Café Torrado 500g', '7891000100101', 18.90, 12.50, 20, 5, N'Mercearia', N'Café torrado e moído, embalagem de 500g', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [Produtos] WHERE [CodigoBarras] = '7891000100102')
    INSERT INTO [Produtos] ([Nome], [CodigoBarras], [Preco], [PrecoCusto], [Estoque], [EstoqueMinimo], [Categoria], [Descricao], [Ativo])
    VALUES (N'Arroz Branco 5kg', '7891000100102', 28.50, 21.00, 15, 4, N'Mercearia', N'Arroz branco tipo 1, pacote de 5kg', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [Produtos] WHERE [CodigoBarras] = '7891000100103')
    INSERT INTO [Produtos] ([Nome], [CodigoBarras], [Preco], [PrecoCusto], [Estoque], [EstoqueMinimo], [Categoria], [Descricao], [Ativo])
    VALUES (N'Leite Integral 1L', '7891000100103', 6.49, 4.80, 30, 10, N'Laticínios', N'Leite integral UHT, caixa de 1 litro', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [Produtos] WHERE [CodigoBarras] = '7891000100104')
    INSERT INTO [Produtos] ([Nome], [CodigoBarras], [Preco], [PrecoCusto], [Estoque], [EstoqueMinimo], [Categoria], [Descricao], [Ativo])
    VALUES (N'Sabão em Pó 1kg', '7891000100104', 14.90, 9.90, 3, 5, N'Limpeza', N'Sabão em pó para lavagem de roupas, 1kg', 1);
GO

IF NOT EXISTS (SELECT 1 FROM [Produtos] WHERE [CodigoBarras] = '7891000100105')
    INSERT INTO [Produtos] ([Nome], [CodigoBarras], [Preco], [PrecoCusto], [Estoque], [EstoqueMinimo], [Categoria], [Descricao], [Ativo])
    VALUES (N'Refrigerante 2L', '7891000100105', 9.99, 6.50, 12, 6, N'Bebidas', N'Refrigerante de cola, garrafa de 2 litros', 1);
GO

/* -------------------------------- Clientes --------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Clientes] WHERE [CpfCnpj] = '529.982.247-25')
    INSERT INTO [Clientes] ([Nome], [CpfCnpj], [Telefone], [Email], [Endereco], [CadastradoEm], [Ativo], [LimiteCredito], [SaldoDevedor])
    VALUES (N'Maria Silva', '529.982.247-25', '(69) 99999-1111', 'maria.silva@exemplo.com', N'Rua das Flores, 100 - Ji-Paraná/RO', SYSUTCDATETIME(), 1, 300.00, 0.00);
GO

IF NOT EXISTS (SELECT 1 FROM [Clientes] WHERE [CpfCnpj] = '11.222.333/0001-81')
    INSERT INTO [Clientes] ([Nome], [CpfCnpj], [Telefone], [Email], [Endereco], [CadastradoEm], [Ativo], [LimiteCredito], [SaldoDevedor])
    VALUES (N'Comércio Bom Preço', '11.222.333/0001-81', '(69) 3333-2222', 'contato@bompreco.com', N'Av. Brasil, 500 - Ji-Paraná/RO', SYSUTCDATETIME(), 1, 1000.00, 0.00);
GO

/* ------------------------------- Fornecedor -------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [Fornecedores] WHERE [Cnpj] = '11.222.333/0001-81')
    INSERT INTO [Fornecedores] ([Nome], [Cnpj], [InscricaoEstadual], [Telefone], [Email], [Ativo])
    VALUES (N'Distribuidora Central', '11.222.333/0001-81', '11004249011401', '(69) 3333-4444', 'compras@central.com', 1);
GO

PRINT 'Dados de demonstração processados.';
GO
