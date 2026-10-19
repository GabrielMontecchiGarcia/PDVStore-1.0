using Microsoft.Extensions.Logging;
using PDVStore.Application.Interfaces;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;
using PDVStore.DTOs;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Serviço de compras de mercadoria. Preserva as regras do CompraService:
/// ao menos um item, quantidade &gt; 0, custo &gt;= 0, produtos existentes;
/// grava a compra e dá entrada no estoque em UMA transação, atualizando o custo
/// quando positivo e registrando movimentações "Entrada".
/// </summary>
public partial class CompraService : ICompraService
{
    private readonly ICompraRepository _compras;
    private readonly IProdutoRepository _produtos;
    private readonly IFornecedorRepository _fornecedores;
    private readonly IMovimentacaoEstoqueRepository _movimentacoes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompraService> _logger;

    public CompraService(
        ICompraRepository compras,
        IProdutoRepository produtos,
        IFornecedorRepository fornecedores,
        IMovimentacaoEstoqueRepository movimentacoes,
        IUnitOfWork unitOfWork,
        ILogger<CompraService> logger)
    {
        _compras = compras;
        _produtos = produtos;
        _fornecedores = fornecedores;
        _movimentacoes = movimentacoes;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>Registra a compra e dá entrada automática no estoque.</summary>
    public async Task<Compra> RegistrarCompraAsync(CompraRequest request, int usuarioId, CancellationToken ct = default)
    {
        if (request.Itens == null || request.Itens.Count == 0)
            throw new RegraDeNegocioException("A compra deve conter pelo menos um item.");

        foreach (var item in request.Itens)
        {
            if (item.Quantidade <= 0)
                throw new RegraDeNegocioException("Quantidade dos itens deve ser maior que zero.");
            if (item.PrecoCusto < 0)
                throw new RegraDeNegocioException("Preço de custo não pode ser negativo.");
        }

        // O fornecedor é exigido pela tela; validar aqui evita erro de FK (500).
        var fornecedor = await _fornecedores.ObterPorIdAsync(request.FornecedorId, rastrear: false, ct);
        if (fornecedor == null)
            throw new RegraDeNegocioException("Fornecedor não encontrado.");

        await using var transacao = await _unitOfWork.IniciarTransacaoAsync(ct);

        try
        {
            // Valida todos os produtos antes de gravar qualquer coisa.
            var produtos = new Dictionary<int, Produto>();
            foreach (var item in request.Itens)
            {
                var produto = await _produtos.ObterPorIdAsync(item.ProdutoId, rastrear: true, ct);
                if (produto == null)
                    throw new RegraDeNegocioException($"Produto ID {item.ProdutoId} não encontrado.");

                produtos[item.ProdutoId] = produto;
            }

            var compra = new Compra
            {
                FornecedorId = request.FornecedorId,
                UsuarioCaixaId = usuarioId,
                NumeroNota = string.IsNullOrWhiteSpace(request.NumeroNota) ? null : request.NumeroNota.Trim(),
                DataCompra = DateTime.UtcNow,
                ValorTotal = request.Itens.Sum(i => i.Quantidade * i.PrecoCusto),
                Status = StatusCompra.Concluida,
                Itens = request.Itens.Select(i => new ItemCompra
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade,
                    PrecoCusto = i.PrecoCusto
                }).ToList()
            };

            await _compras.AdicionarAsync(compra, ct);
            await _unitOfWork.SalvarAsync(ct);

            foreach (var item in compra.Itens)
            {
                var produto = produtos[item.ProdutoId];

                produto.Estoque += item.Quantidade;
                if (item.PrecoCusto > 0)
                    produto.PrecoCusto = item.PrecoCusto;

                await _movimentacoes.AdicionarAsync(new MovimentacaoEstoque
                {
                    ProdutoId = produto.Id,
                    Tipo = TiposMovimentacao.Entrada,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoCusto,
                    DataMovimentacao = DateTime.UtcNow,
                    UsuarioId = usuarioId,
                    Motivo = $"Compra #{compra.Id} - {compra.NumeroNota ?? "sem nota"}"
                }, ct);
            }

            await _unitOfWork.SalvarAsync(ct);
            await transacao.CommitAsync(ct);

            _logger.LogInformation(
                "Compra {CompraId} registrada por usuário {UsuarioId}. Fornecedor: {FornecedorId}. Total: {Total}",
                compra.Id, usuarioId, compra.FornecedorId, compra.ValorTotal);

            return compra;
        }
        catch
        {
            await transacao.RollbackAsync(ct);
            throw;
        }
    }
}
