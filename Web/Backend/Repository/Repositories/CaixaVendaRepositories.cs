using Microsoft.EntityFrameworkCore;
using PDVStore.Domain.Entities;
using PDVStore.Repository.Context;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Repository.Repositories;

/// <summary>Repositório de caixas, com Include do usuário responsável no histórico.</summary>
public class CaixaRepository : ICaixaRepository
{
    private readonly PDVStoreDbContext _context;

    public CaixaRepository(PDVStoreDbContext context) => _context = context;

    public async Task<Caixa?> ObterAbertoAsync(bool rastrear = false, CancellationToken ct = default)
    {
        IQueryable<Caixa> query = _context.Caixas;
        if (!rastrear) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(c => c.Status == StatusCaixa.Aberto, ct);
    }

    public async Task<Caixa?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default)
    {
        IQueryable<Caixa> query = _context.Caixas;
        if (!rastrear) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<List<Caixa>> ListarAsync(CancellationToken ct = default) =>
        await _context.Caixas
            .Include(c => c.UsuarioCaixa)
            .AsNoTracking()
            .OrderByDescending(c => c.Abertura)
            .ToListAsync(ct);

    public async Task AdicionarAsync(Caixa caixa, CancellationToken ct = default) =>
        await _context.Caixas.AddAsync(caixa, ct);
}

/// <summary>
/// Repositório de vendas. As consultas de período incluem itens/usuário/cliente
/// para exibição, como no VendaService original.
/// </summary>
public class VendaRepository : IVendaRepository
{
    private readonly PDVStoreDbContext _context;

    public VendaRepository(PDVStoreDbContext context) => _context = context;

    public async Task AdicionarAsync(Venda venda, CancellationToken ct = default) =>
        await _context.Vendas.AddAsync(venda, ct);

    public async Task<Venda?> ObterPorIdAsync(int id, bool rastrear = false, CancellationToken ct = default)
    {
        IQueryable<Venda> query = _context.Vendas
            .Include(v => v.Itens)
            .ThenInclude(i => i.Produto)
            .Include(v => v.UsuarioCaixa)
            .Include(v => v.Cliente);

        if (!rastrear) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<List<Venda>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, int? caixaId, CancellationToken ct = default)
    {
        var query = _context.Vendas
            .Include(v => v.Itens)
            .Include(v => v.UsuarioCaixa)
            .Include(v => v.Cliente)
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == StatusVenda.Concluida);

        if (caixaId.HasValue)
            query = query.Where(v => v.CaixaId == caixaId.Value);

        return await query.OrderByDescending(v => v.DataVenda).ToListAsync(ct);
    }

    public async Task<decimal> CalcularTotalAsync(DateTime inicio, DateTime fim, int? caixaId, CancellationToken ct = default)
    {
        var query = _context.Vendas
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == StatusVenda.Concluida);

        if (caixaId.HasValue)
            query = query.Where(v => v.CaixaId == caixaId.Value);

        return await query.SumAsync(v => v.ValorTotal, ct);
    }

    public async Task<decimal> SomarVendasDoCaixaAsync(int caixaId, CancellationToken ct = default) =>
        await _context.Vendas
            .Where(v => v.CaixaId == caixaId && v.Status == StatusVenda.Concluida)
            .SumAsync(v => v.ValorTotal, ct);

    public Task<int> ContarAsync(DateTime inicio, DateTime fim, CancellationToken ct = default) =>
        _context.Vendas.CountAsync(
            v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == StatusVenda.Concluida, ct);

    public Task<int> ContarTodasAsync(CancellationToken ct = default) =>
        _context.Vendas.CountAsync(ct);

    public async Task<List<Venda>> ListarConcluidasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default) =>
        await _context.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == StatusVenda.Concluida)
            .ToListAsync(ct);
}
