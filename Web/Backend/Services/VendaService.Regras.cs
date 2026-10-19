using PDVStore.Domain.Common;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;
using PDVStore.DTOs;

namespace PDVStore.Services;

/// <summary>
/// Validações e efeitos auxiliares do registro de venda, extraídos para manter
/// o fluxo principal legível (Clean Code) sem alterar a ordem das regras.
/// </summary>
public partial class VendaService
{
    /// <summary>
    /// Regras do fiado: cliente obrigatório e ativo; quando o limite de crédito é
    /// maior que zero, saldo devedor + venda não pode ultrapassar o limite.
    /// Limite zero significa "sem limite configurado" (não bloqueia).
    /// </summary>
    private async Task<Cliente> ValidarFiadoAsync(int? clienteId, decimal total, CancellationToken ct)
    {
        if (!clienteId.HasValue)
            throw new RegraDeNegocioException("Venda fiada exige um cliente selecionado.");

        var cliente = await _clientes.ObterPorIdAsync(clienteId.Value, rastrear: true, ct);
        if (cliente == null || !cliente.Ativo)
            throw new RegraDeNegocioException("Cliente informado não está ativo.");

        if (cliente.LimiteCredito > 0 &&
            cliente.SaldoDevedor + total > cliente.LimiteCredito)
        {
            throw new RegraDeNegocioException(
                $"Limite de crédito do cliente excedido. Saldo atual: {Formatacao.Moeda(cliente.SaldoDevedor)}.");
        }

        return cliente;
    }

    /// <summary>
    /// Total não pode ser negativo e, para dinheiro, o valor recebido precisa
    /// cobrir o total (troco &gt;= 0), como na tela do PDV.
    /// </summary>
    private static void ValidarTotalEValorRecebido(VendaRequest request, decimal total)
    {
        if (total < 0)
            throw new RegraDeNegocioException("O valor total da venda não pode ser negativo.");

        if (FormasPagamento.Eh(request.FormaPagamento, FormasPagamento.Dinheiro) &&
            (request.ValorRecebido ?? 0) < total)
        {
            throw new RegraDeNegocioException("Valor recebido é insuficiente para completar a venda.");
        }
    }

    /// <summary>
    /// Valida existência e saldo de todos os produtos antes de gravar a venda.
    /// Retorna os produtos rastreados para a baixa posterior.
    /// </summary>
    private async Task<Dictionary<int, Produto>> ValidarEstoqueAsync(List<ItemVenda> itens, CancellationToken ct)
    {
        var produtos = new Dictionary<int, Produto>();

        foreach (var item in itens)
        {
            var produto = await _produtos.ObterPorIdAsync(item.ProdutoId, rastrear: true, ct);
            if (produto == null)
                throw new RegraDeNegocioException($"Produto ID {item.ProdutoId} não encontrado.");

            if (produto.Estoque < item.Quantidade)
                throw new RegraDeNegocioException($"Estoque insuficiente para o produto '{produto.Nome}'.");

            produtos[item.ProdutoId] = produto;
        }

        return produtos;
    }

    /// <summary>
    /// Reduz o estoque e grava a movimentação "Saída" com o custo do produto,
    /// usuário responsável e referência da venda (mesmos dados do original).
    /// </summary>
    private async Task BaixarEstoqueERegistrarMovimentacoesAsync(
        Venda venda, Dictionary<int, Produto> produtos, int usuarioId, CancellationToken ct)
    {
        foreach (var item in venda.Itens)
        {
            var produto = produtos[item.ProdutoId];
            produto.Estoque -= item.Quantidade;

            await _movimentacoes.AdicionarAsync(new MovimentacaoEstoque
            {
                ProdutoId = item.ProdutoId,
                Tipo = TiposMovimentacao.Saida,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.PrecoCusto,
                DataMovimentacao = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Motivo = $"Venda #{venda.Id}",
                ReferenciaVendaId = venda.Id
            }, ct);
        }
    }
}
