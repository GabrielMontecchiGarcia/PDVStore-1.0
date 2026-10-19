/* ============================================================================
   PDVStore Web — Procedures de apoio a relatórios (parte 1)
   ----------------------------------------------------------------------------
   As procedures são OPCIONAIS: a API usa EF Core/LINQ (mesmas regras) para
   manter portabilidade e segurança (parâmetros tipados, sem concatenação SQL).
   Executar depois de 001_schema_e_seed.sql.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ----------------------------------------------------------------------------
   Ranking dos produtos mais vendidos no período.
   Regra idêntica ao RelatorioService.GerarRelatorioItensMaisVendidos:
   somente vendas 'Concluida'; agrupa por produto; ordena por quantidade;
   classifica estoque como 'Baixo' quando Estoque <= EstoqueMinimo.
   ---------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.sp_RelatorioItensMaisVendidos
    @DataInicio datetime2,
    @DataFim    datetime2,
    @TopN       int = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@TopN)
        p.Nome                                   AS NomeProduto,
        p.CodigoBarras,
        SUM(iv.Quantidade)                       AS TotalVendido,
        SUM(iv.Quantidade * iv.PrecoUnitario)    AS ValorTotalVendido,
        p.Estoque                                AS EstoqueAtual,
        p.EstoqueMinimo,
        CASE WHEN p.Estoque <= p.EstoqueMinimo THEN 'Baixo' ELSE 'OK' END AS StatusMinimo
    FROM ItensVendas iv
    INNER JOIN Produtos p ON p.Id = iv.ProdutoId
    INNER JOIN Vendas   v ON v.Id = iv.VendaId
    WHERE v.Status = 'Concluida'
      AND v.DataVenda >= @DataInicio
      AND v.DataVenda <= @DataFim
    GROUP BY p.Id, p.Nome, p.CodigoBarras, p.Estoque, p.EstoqueMinimo
    ORDER BY SUM(iv.Quantidade) DESC;
END;
GO

/* ----------------------------------------------------------------------------
   Fechamento de caixa (mesma fórmula do CaixaService):
   ValorFinal = ValorInicial + SUM(vendas concluídas do caixa) - Sangria.
   Só atua em caixa 'Aberto'; 0 linhas afetadas quando inexistente/já fechado.
   ---------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.sp_FecharCaixa
    @CaixaId int
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE c
       SET c.ValorFinal = c.ValorInicial
                        + ISNULL((SELECT SUM(v.ValorTotal)
                                    FROM Vendas v
                                   WHERE v.CaixaId = c.Id
                                     AND v.Status = 'Concluida'), 0)
                        - ISNULL(c.Sangria, 0),
           c.Fechamento = SYSUTCDATETIME(),
           c.Status     = 'Fechado'
     FROM Caixas c
    WHERE c.Id = @CaixaId
      AND c.Status = 'Aberto';
END;
GO

/* ----------------------------------------------------------------------------
   Resumo de vendas por forma de pagamento no período (Dashboard).
   ---------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.sp_ResumoPagamentosPorForma
    @DataInicio datetime2,
    @DataFim    datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT v.FormaPagamento AS Forma,
           COUNT(*)         AS Quantidade,
           SUM(v.ValorTotal) AS Total
    FROM Vendas v
    WHERE v.Status = 'Concluida'
      AND v.DataVenda >= @DataInicio
      AND v.DataVenda <= @DataFim
    GROUP BY v.FormaPagamento
    ORDER BY v.FormaPagamento;
END;
GO
