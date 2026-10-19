using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.Repository.Interfaces;

/// <summary>
/// Repositório de usuários: consultas e inclusão. Alterações são persistidas
/// pelo Unit of Work, mantendo o mesmo padrão transacional em toda a aplicação.
/// </summary>
public interface IUsuarioRepository
{
    Task<UsuarioCaixa?> ObterPorIdAsync(int id, bool rastrear = false, CancellationToken ct = default);
    Task<UsuarioCaixa?> ObterPorNomeAsync(string nome, CancellationToken ct = default);
    Task<List<UsuarioCaixa>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<bool> ExisteNomeAsync(string nome, CancellationToken ct = default);
    Task AdicionarAsync(UsuarioCaixa usuario, CancellationToken ct = default);
}

/// <summary>Repositório de produtos (cadastro/estoque).</summary>
public interface IProdutoRepository
{
    Task<List<Produto>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<Produto?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default);
    Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task AdicionarAsync(Produto produto, CancellationToken ct = default);
}

/// <summary>Repositório de clientes (inclui consulta de fiado).</summary>
public interface IClienteRepository
{
    Task<List<Cliente>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<Cliente?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default);
    Task AdicionarAsync(Cliente cliente, CancellationToken ct = default);
}

/// <summary>Repositório de fornecedores.</summary>
public interface IFornecedorRepository
{
    Task<List<Fornecedor>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<Fornecedor?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default);
    Task AdicionarAsync(Fornecedor fornecedor, CancellationToken ct = default);
}

/// <summary>Repositório de caixas; a regra de caixa único aberto fica no serviço.</summary>
public interface ICaixaRepository
{
    Task<Caixa?> ObterAbertoAsync(bool rastrear = false, CancellationToken ct = default);
    Task<Caixa?> ObterPorIdAsync(int id, bool rastrear = true, CancellationToken ct = default);
    Task<List<Caixa>> ListarAsync(CancellationToken ct = default);
    Task AdicionarAsync(Caixa caixa, CancellationToken ct = default);
}

/// <summary>Repositório de vendas e agregações de faturamento.</summary>
public interface IVendaRepository
{
    Task AdicionarAsync(Venda venda, CancellationToken ct = default);
    Task<Venda?> ObterPorIdAsync(int id, bool rastrear = false, CancellationToken ct = default);
    Task<List<Venda>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, int? caixaId, CancellationToken ct = default);
    Task<decimal> CalcularTotalAsync(DateTime inicio, DateTime fim, int? caixaId, CancellationToken ct = default);
    Task<decimal> SomarVendasDoCaixaAsync(int caixaId, CancellationToken ct = default);
    Task<int> ContarAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<int> ContarTodasAsync(CancellationToken ct = default);
    Task<List<Venda>> ListarConcluidasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
}

/// <summary>Repositório de compras.</summary>
public interface ICompraRepository
{
    Task AdicionarAsync(Compra compra, CancellationToken ct = default);
    Task<Compra?> ObterPorIdAsync(int id, bool rastrear = false, CancellationToken ct = default);
    Task<List<Compra>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default);
}

/// <summary>Repositório do histórico de movimentações de estoque.</summary>
public interface IMovimentacaoEstoqueRepository
{
    Task AdicionarAsync(MovimentacaoEstoque movimentacao, CancellationToken ct = default);
    Task<List<MovimentacaoEstoque>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default);
}

/// <summary>
/// Consultas de relatório (projeções diretas no banco).
/// ItensMaisVendidos replica o join ItensVendas/Produtos/Vendas do RelatorioService.
/// </summary>
public interface IRelatorioRepository
{
    Task<List<ItemRelatorioDto>> ItensMaisVendidosAsync(DateTime inicio, DateTime fim, int topN, CancellationToken ct = default);
    Task<List<Produto>> EstoqueMinimoAsync(CancellationToken ct = default);
}
