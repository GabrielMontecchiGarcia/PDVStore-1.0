using Microsoft.EntityFrameworkCore;
using PDVStore.Domain.Entities;
using PDVStore.Repository.Context;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Repository.Repositories;

/// <summary>
/// Repositório de usuários. Consultas de leitura usam AsNoTracking (mesmo padrão
/// do UsuarioService original); quando o serviço precisa alterar o registro,
/// solicita rastreamento para que o Unit of Work persista as mudanças.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly PDVStoreDbContext _context;

    public UsuarioRepository(PDVStoreDbContext context) => _context = context;

    public async Task<UsuarioCaixa?> ObterPorIdAsync(int id, bool rastrear = false, CancellationToken ct = default)
    {
        IQueryable<UsuarioCaixa> query = _context.Usuarios;
        if (!rastrear) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UsuarioCaixa?> ObterPorNomeAsync(string nome, CancellationToken ct = default) =>
        await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Nome == nome, ct);

    public async Task<List<UsuarioCaixa>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default)
    {
        var query = _context.Usuarios.AsNoTracking();

        if (apenasAtivos)
            query = query.Where(u => u.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro))
            query = query.Where(u => u.Nome.Contains(filtro));

        return await query.OrderBy(u => u.Nome).ToListAsync(ct);
    }

    public Task<bool> ExisteNomeAsync(string nome, CancellationToken ct = default) =>
        _context.Usuarios.AnyAsync(u => u.Nome == nome, ct);

    public async Task AdicionarAsync(UsuarioCaixa usuario, CancellationToken ct = default) =>
        await _context.Usuarios.AddAsync(usuario, ct);
}

/// <summary>Repositório de produtos, com busca por nome/código de barras.</summary>
public class ProdutoRepository : IProdutoRepository
{
    private readonly PDVStoreDbContext _context;

    public ProdutoRepository(PDVStoreDbContext context) => _context = context;

    public async Task<List<Produto>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default)
    {
        var query = _context.Produtos.AsNoTracking();

        if (apenasAtivos)
            query = query.Where(p => p.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var termo = filtro.Trim();
            query = query.Where(p => p.Nome.Contains(termo) ||
                                    (p.CodigoBarras != null && p.CodigoBarras.Contains(termo)));
        }

        return await query.OrderBy(p => p.Nome).ToListAsync(ct);
    }

    public async Task<Produto?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default)
    {
        IQueryable<Produto> query = _context.Produtos;
        if (!rastrear) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default) =>
        await _context.Produtos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CodigoBarras == codigoBarras && p.Ativo, ct);

    public async Task AdicionarAsync(Produto produto, CancellationToken ct = default) =>
        await _context.Produtos.AddAsync(produto, ct);
}

/// <summary>Repositório de clientes, com filtro por nome ou CPF/CNPJ.</summary>
public class ClienteRepository : IClienteRepository
{
    private readonly PDVStoreDbContext _context;

    public ClienteRepository(PDVStoreDbContext context) => _context = context;

    public async Task<List<Cliente>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default)
    {
        var query = _context.Clientes.AsNoTracking();

        if (apenasAtivos)
            query = query.Where(c => c.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro))
            query = query.Where(c => c.Nome.Contains(filtro) ||
                                     (c.CpfCnpj != null && c.CpfCnpj.Contains(filtro)));

        return await query.OrderBy(c => c.Nome).ToListAsync(ct);
    }

    public async Task<Cliente?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default)
    {
        IQueryable<Cliente> query = _context.Clientes;
        if (!rastrear) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task AdicionarAsync(Cliente cliente, CancellationToken ct = default) =>
        await _context.Clientes.AddAsync(cliente, ct);
}

/// <summary>Repositório de fornecedores, com filtro por nome ou CNPJ.</summary>
public class FornecedorRepository : IFornecedorRepository
{
    private readonly PDVStoreDbContext _context;

    public FornecedorRepository(PDVStoreDbContext context) => _context = context;

    public async Task<List<Fornecedor>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default)
    {
        var query = _context.Fornecedores.AsNoTracking();

        if (apenasAtivos)
            query = query.Where(f => f.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro))
            query = query.Where(f => f.Nome.Contains(filtro) ||
                                     (f.Cnpj != null && f.Cnpj.Contains(filtro)));

        return await query.OrderBy(f => f.Nome).ToListAsync(ct);
    }

    public async Task<Fornecedor?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default)
    {
        IQueryable<Fornecedor> query = _context.Fornecedores;
        if (!rastrear) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task AdicionarAsync(Fornecedor fornecedor, CancellationToken ct = default) =>
        await _context.Fornecedores.AddAsync(fornecedor, ct);
}
