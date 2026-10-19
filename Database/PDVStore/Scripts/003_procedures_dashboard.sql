/* ============================================================================
   PDVStore Web — Procedures de apoio ao dashboard (parte 2)
   Executar depois de 001_schema_e_seed.sql.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   Movimentações agregadas por dia (entradas x saídas).
   Agrupamento em UTC; a API converte para o fuso App:TimeZone.
   ---------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.sp_MovimentacoesPorDia
    @DataInicio datetime2,
    @DataFim    datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(m.DataMovimentacao AS date) AS Dia,
           SUM(CASE WHEN m.Tipo = 'Entrada' THEN m.Quantidade ELSE 0 END) AS Entradas,
           SUM(CASE WHEN m.Tipo = 'Saída'   THEN m.Quantidade ELSE 0 END) AS Saidas
    FROM MovimentacoesEstoque m
    WHERE m.DataMovimentacao >= @DataInicio
      AND m.DataMovimentacao <= @DataFim
    GROUP BY CAST(m.DataMovimentacao AS date)
    ORDER BY Dia;
END;
GO

/* ----------------------------------------------------------------------------
   Produtos ativos com estoque igual ou abaixo do mínimo (alertas).
   ---------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.sp_AlertasEstoqueMinimo
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.Id, p.Nome, p.CodigoBarras, p.Estoque, p.EstoqueMinimo, p.Categoria
    FROM Produtos p
    WHERE p.Ativo = 1
      AND p.Estoque <= p.EstoqueMinimo
    ORDER BY p.Nome;
END;
GO

/* ----------------------------------------------------------------------------
   Resumo geral do período: quantidade, faturamento e ticket médio.
   Mantém a mesma definição de venda concluída usada na aplicação.
   ---------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.sp_ResumoDashboard
    @DataInicio datetime2,
    @DataFim    datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)                     AS QuantidadeVendas,
           ISNULL(SUM(v.ValorTotal), 0) AS TotalPeriodo,
           CASE WHEN COUNT(*) = 0 THEN 0
                ELSE SUM(v.ValorTotal) / COUNT(*) END AS TicketMedio
    FROM Vendas v
    WHERE v.Status = 'Concluida'
      AND v.DataVenda >= @DataInicio
      AND v.DataVenda <= @DataFim;
END;
GO
