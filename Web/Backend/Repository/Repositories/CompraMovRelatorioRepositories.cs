using Microsoft.EntityFrameworkCore;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Repository.Context;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Repository.Repositories;

/// <summary>Repositório de compras (listagem inclui fornecedor para exibição).</summary>
public class CompraRepository : ICompraRepository
{
    private readonly PDVStoreDbContext _context;

    public CompraRepository(PDVStoreDbContext context) => _context = context;

    public async Task AdicionarAsync(Compra compra, CancellationToken ct = default) =>
        await _context.Compras.AddAsync(compra, ct);

    public async Task<Compra?> ObterPorIdAsync(int id, bool rastrear = false, CancellationToken ct = default)
    {
        IQueryable<Compra> query = _context.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.UsuarioCaixa)
            .Include(c => c.Itens)
            .ThenInclude(i => i.Produto);

        if (!rastrear) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Compra>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default)
    {
        var query = _context.Compras
            .Include(c => c.Fornecedor)
            .AsNoTracking();

        if (inicio.HasValue) query = query.Where(c => c.DataCompra >= inicio.Value);
        if (fim.HasValue) query = query.Where(c => c.DataCompra <= fim.Value);

        return await query.OrderByDescending(c => c.DataCompra).ToListAsync(ct);
    }
}

/// <summary>Repositório do histórico de movimentações de estoque.</summary>
public class MovimentacaoEstoqueRepository : IMovimentacaoEstoqueRepository
{
    private readonly PDVStoreDbContext _context;

    public MovimentacaoEstoqueRepository(PDVStoreDbContext context) => _context = context;

    public async Task AdicionarAsync(MovimentacaoEstoque movimentacao, CancellationToken ct = default) =>
        await _context.MovimentacoesEstoque.AddAsync(movimentacao, ct);

    public async Task<List<MovimentacaoEstoque>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default)
    {
        var query = _context.MovimentacoesEstoque
            .Include(m => m.Produto)
            .Include(m => m.Usuario)
            .AsNoTracking()
            .AsQueryable();

        if (inicio.HasValue) query = query.Where(m => m.DataMovimentacao >= inicio.Value);
        if (fim.HasValue) query = query.Where(m => m.DataMovimentacao <= fim.Value);

        return await query.OrderByDescending(m => m.DataMovimentacao).ToListAsync(ct);
    }
}

/// <summary>
/// Consultas de relatório. O ranking replica o join/agrupamento do
/// RelatorioService.GerarRelatorioItensMaisVendidos, considerando apenas vendas
/// concluídas no período e ordenando pela quantidade vendida.
/// </summary>
public class RelatorioRepository : IRelatorioRepository
{
    private readonly PDVStoreDbContext _context;

    public RelatorioRepository(PDVStoreDbContext context) => _context = context;

    public async Task<List<ItemRelatorioDto>> ItensMaisVendidosAsync(DateTime inicio, DateTime fim, int topN, CancellationToken ct = default)
    {
        return await (from iv in _context.ItensVendas
                      join p in _context.Produtos on iv.ProdutoId equals p.Id
                      join v in _context.Vendas on iv.VendaId equals v.Id
                      where v.Status == StatusVenda.Concluida && v.DataVenda >= inicio && v.DataVenda <= fim
                      group new { iv, p } by new { p.Id, p.Nome, p.CodigoBarras, p.Estoque, p.EstoqueMinimo } into g
                      select new ItemRelatorioDto
                      {
                          NomeProduto = g.Key.Nome,
                          CodigoBarras = g.Key.CodigoBarras,
                          TotalVendido = g.Sum(x => x.iv.Quantidade),
                          ValorTotalVendido = g.Sum(x => x.iv.Quantidade * x.iv.PrecoUnitario),
                          EstoqueAtual = g.Key.Estoque,
                          EstoqueMinimo = g.Key.EstoqueMinimo,
                          StatusMinimo = g.Key.Estoque <= g.Key.EstoqueMinimo ? "Baixo" : "OK"
                      })
            .OrderByDescending(ir => ir.TotalVendido)
            .Take(topN)
            .ToListAsync(ct);
    }

    public async Task<List<Produto>> EstoqueMinimoAsync(CancellationToken ct = default) =>
        await _context.Produtos
            .AsNoTracking()
            .Where(p => p.Ativo && p.Estoque <= p.EstoqueMinimo)
            .OrderBy(p => p.Nome)
            .ToListAsync(ct);
}
