using PDVStore.Domain.Entities;
using PDVStore.DTOs;

namespace PDVStore.Application.Interfaces;

/// <summary>
/// Serviço de usuários: cadastro, autenticação, alteração de senha e permissões.
/// As regras são as mesmas do UsuarioService do projeto Windows.
/// </summary>
public interface IUsuarioService
{
    Task<UsuarioCaixa> CriarAsync(CriarUsuarioRequest request, CancellationToken ct = default);
    Task<UsuarioCaixa> AtualizarAsync(int id, AtualizarUsuarioRequest request, CancellationToken ct = default);
    Task DesativarAsync(int id, CancellationToken ct = default);
    Task<List<UsuarioCaixa>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<UsuarioCaixa?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<UsuarioCaixa?> AutenticarAsync(string nome, string senha, CancellationToken ct = default);
    Task<bool> AlterarSenhaAsync(int id, string senhaAtual, string novaSenha, CancellationToken ct = default);
    Task<bool> EhAdministradorAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Serviço de produtos/estoque. Mantém as regras de soft delete, busca por
/// nome/código de barras e ajustes manuais com movimentação registrada.
/// </summary>
public interface IProdutoService
{
    Task<List<Produto>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);
    Task<Produto> CriarAsync(ProdutoRequest request, CancellationToken ct = default);
    Task<Produto> AtualizarAsync(int id, ProdutoRequest request, CancellationToken ct = default);
    Task DesativarAsync(int id, CancellationToken ct = default);
    Task<List<MovimentacaoEstoque>> ObterHistoricoAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default);
    Task<bool> AdicionarEstoqueAsync(int produtoId, int quantidade, string? motivo, int? usuarioId, CancellationToken ct = default);
    Task<bool> BaixarEstoqueAsync(int produtoId, int quantidade, string? motivo, int? usuarioId, CancellationToken ct = default);
}

/// <summary>Serviço de clientes, incluindo controle de fiado (limite/saldo).</summary>
public interface IClienteService
{
    Task<List<Cliente>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<Cliente?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Cliente> CriarAsync(ClienteRequest request, CancellationToken ct = default);
    Task<Cliente> AtualizarAsync(int id, ClienteRequest request, CancellationToken ct = default);
    Task<bool> AtualizarStatusAsync(int id, bool ativo, CancellationToken ct = default);
    Task<bool> ReceberFiadoAsync(int clienteId, decimal valor, CancellationToken ct = default);
}

/// <summary>Serviço de fornecedores (upsert e soft delete).</summary>
public interface IFornecedorService
{
    Task<List<Fornecedor>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default);
    Task<Fornecedor?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Fornecedor> CriarAsync(FornecedorRequest request, CancellationToken ct = default);
    Task<Fornecedor> AtualizarAsync(int id, FornecedorRequest request, CancellationToken ct = default);
    Task<bool> AtualizarStatusAsync(int id, bool ativo, CancellationToken ct = default);
}

/// <summary>
/// Serviço de caixa: abertura, fechamento, sangria e consulta.
/// Regra central: somente um caixa aberto por vez.
/// </summary>
public interface ICaixaService
{
    Task<Caixa> AbrirCaixaAsync(decimal valorInicial, int usuarioCaixaId, CancellationToken ct = default);
    Task<Caixa?> ObterCaixaAbertoAsync(CancellationToken ct = default);
    Task<bool> FecharCaixaAsync(int caixaId, CancellationToken ct = default);
    Task<bool> RegistrarSangriaAsync(int caixaId, decimal valor, CancellationToken ct = default);
    Task<List<Caixa>> ListarAsync(CancellationToken ct = default);
}

/// <summary>
/// Serviço de vendas: registra a venda com todas as validações de negócio em
/// uma única transação, consulta, lista e cancela (estorno de estoque/fiado).
/// </summary>
public interface IVendaService
{
    Task<Venda> RegistrarVendaAsync(VendaRequest request, int usuarioId, CancellationToken ct = default);
    Task<Venda?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<List<Venda>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, int? caixaId, CancellationToken ct = default);
    Task<bool> CancelarVendaAsync(int vendaId, string? motivo, int usuarioId, CancellationToken ct = default);
}

/// <summary>Serviço de compras: entrada de mercadoria e cancelamento com estorno.</summary>
public interface ICompraService
{
    Task<Compra> RegistrarCompraAsync(CompraRequest request, int usuarioId, CancellationToken ct = default);
    Task<Compra?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<List<Compra>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default);
    Task<bool> CancelarCompraAsync(int id, CancellationToken ct = default);
}

/// <summary>Serviço de relatórios/dashboard e exportações.</summary>
public interface IRelatorioService
{
    Task<List<ItemRelatorioDto>> ItensMaisVendidosAsync(DateTime inicio, DateTime fim, int topN, CancellationToken ct = default);
    Task<List<Produto>> EstoqueMinimoAsync(CancellationToken ct = default);
    Task<DashboardDto> ObterDashboardAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<int> TotalVendasAsync(CancellationToken ct = default);
}

/// <summary>Integração de pagamento (mock PIX/cartão), preservada do projeto original.</summary>
public interface IPagamentoIntegrator
{
    Task<bool> ProcessarPagamentoAsync(Venda venda, CancellationToken ct = default);
}
