using PDVStore.Application.Interfaces;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;
using PDVStore.DTOs;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de produtos e estoque. Preserva as regras do EstoqueService:
/// - Listagens padrão apenas de produtos ativos;
/// - Exclusão lógica (Ativo = false);
/// - Ajustes de estoque exigem quantidade &gt; 0 e geram movimentação;
/// - Saída manual não pode deixar o saldo negativo.
/// </summary>
public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _produtos;
    private readonly IMovimentacaoEstoqueRepository _movimentacoes;
    private readonly IUnitOfWork _unitOfWork;

    public ProdutoService(
        IProdutoRepository produtos,
        IMovimentacaoEstoqueRepository movimentacoes,
        IUnitOfWork unitOfWork)
    {
        _produtos = produtos;
        _movimentacoes = movimentacoes;
        _unitOfWork = unitOfWork;
    }

    public Task<List<Produto>> ListarAsync(bool apenasAtivos, string? filtro, CancellationToken ct = default) =>
        _produtos.ListarAsync(apenasAtivos, filtro?.Trim(), ct);

    public Task<Produto?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _produtos.ObterPorIdAsync(id, rastrear: false, ct);

    public Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default) =>
        _produtos.ObterPorCodigoBarrasAsync(codigoBarras, ct);

    /// <summary>Cadastra produto ativo com os dados informados.</summary>
    public async Task<Produto> CriarAsync(ProdutoRequest request, CancellationToken ct = default)
    {
        var produto = new Produto { Ativo = true };
        Aplicar(request, produto);

        await _produtos.AdicionarAsync(produto, ct);
        await _unitOfWork.SalvarAsync(ct);
        return produto;
    }

    /// <summary>
    /// Atualiza o cadastro do produto. Não altera movimentações nem histórico;
    /// o estoque aqui é o saldo informado na tela (edição de cadastro).
    /// </summary>
    public async Task<Produto> AtualizarAsync(int id, ProdutoRequest request, CancellationToken ct = default)
    {
        var produto = await _produtos.ObterPorIdAsync(id, rastrear: true, ct)
            ?? throw new NaoEncontradoException("Produto não encontrado.");

        Aplicar(request, produto);
        await _unitOfWork.SalvarAsync(ct);
        return produto;
    }

    /// <summary>Exclusão lógica do produto; não remove vendas/compras históricas.</summary>
    public async Task DesativarAsync(int id, CancellationToken ct = default)
    {
        var produto = await _produtos.ObterPorIdAsync(id, rastrear: true, ct);
        if (produto == null) return;

        produto.Ativo = false;
        await _unitOfWork.SalvarAsync(ct);
    }

    public Task<List<MovimentacaoEstoque>> ObterHistoricoAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default) =>
        _movimentacoes.ListarAsync(inicio, fim, ct);

    /// <summary>
    /// Entrada manual de estoque. Quantidade inválida gera erro de negócio e o
    /// produto inexistente retorna false, mantendo o contrato do serviço original.
    /// </summary>
    public async Task<bool> AdicionarEstoqueAsync(int produtoId, int quantidade, string? motivo, int? usuarioId, CancellationToken ct = default)
    {
        if (quantidade <= 0)
            throw new RegraDeNegocioException("Quantidade deve ser maior que zero.");

        var produto = await _produtos.ObterPorIdAsync(produtoId, rastrear: true, ct);
        if (produto == null) return false;

        produto.Estoque += quantidade;
        await RegistrarMovimentacaoAsync(produtoId, TiposMovimentacao.Entrada, quantidade, null, usuarioId, motivo, ct);
        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    /// <summary>
    /// Saída manual de estoque. Bloqueia quando não há saldo suficiente (retorna
    /// false, sem lançar exceção), exatamente como o serviço original.
    /// </summary>
    public async Task<bool> BaixarEstoqueAsync(int produtoId, int quantidade, string? motivo, int? usuarioId, CancellationToken ct = default)
    {
        if (quantidade <= 0)
            throw new RegraDeNegocioException("Quantidade deve ser maior que zero.");

        var produto = await _produtos.ObterPorIdAsync(produtoId, rastrear: true, ct);
        if (produto == null) return false;

        if (produto.Estoque < quantidade)
            return false;

        produto.Estoque -= quantidade;
        await RegistrarMovimentacaoAsync(produtoId, TiposMovimentacao.Saida, quantidade, null, usuarioId, motivo, ct);
        await _unitOfWork.SalvarAsync(ct);
        return true;
    }

    private Task RegistrarMovimentacaoAsync(
        int produtoId, string tipo, int quantidade, decimal? precoUnitario,
        int? usuarioId, string? motivo, CancellationToken ct) =>
        _movimentacoes.AdicionarAsync(new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = tipo,
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            DataMovimentacao = DateTime.UtcNow,
            UsuarioId = usuarioId,
            Motivo = motivo
        }, ct);

    /// <summary>Copia os campos do DTO para a entidade, normalizando textos e valores.</summary>
    private static void Aplicar(ProdutoRequest request, Produto produto)
    {
        produto.CodigoBarras = request.CodigoBarras?.Trim();
        produto.Nome = request.Nome.Trim();
        produto.Preco = request.Preco;
        produto.PrecoCusto = Math.Max(0, request.PrecoCusto);
        produto.Estoque = Math.Max(0, request.Estoque);
        produto.EstoqueMinimo = Math.Max(0, request.EstoqueMinimo);
        produto.Categoria = request.Categoria?.Trim();
        produto.Descricao = request.Descricao?.Trim();
    }
}
