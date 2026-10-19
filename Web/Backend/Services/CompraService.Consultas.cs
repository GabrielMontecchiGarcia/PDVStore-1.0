using Microsoft.Extensions.Logging;
using PDVStore.Domain.Entities;
using PDVStore.Domain.Exceptions;

namespace PDVStore.Services;

/// <summary>Consultas e cancelamento de compras (parte complementar do serviço).</summary>
public partial class CompraService
{
    public Task<Compra?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _compras.ObterPorIdAsync(id, rastrear: false, ct);

    public Task<List<Compra>> ListarAsync(DateTime? inicio, DateTime? fim, CancellationToken ct = default) =>
        _compras.ListarAsync(inicio, fim, ct);

    /// <summary>
    /// Cancela a compra reduzindo o estoque dos itens (nunca abaixo de zero).
    /// Retorna false se não existir ou já estiver cancelada. Não gera
    /// movimentação de estorno, preservando o comportamento do sistema original.
    /// </summary>
    public async Task<bool> CancelarCompraAsync(int id, CancellationToken ct = default)
    {
        var compra = await _compras.ObterPorIdAsync(id, rastrear: true, ct);

        if (compra == null || compra.Status == StatusCompra.Cancelada)
            return false;

        await using var transacao = await _unitOfWork.IniciarTransacaoAsync(ct);

        try
        {
            foreach (var item in compra.Itens)
            {
                var produto = await _produtos.ObterPorIdAsync(item.ProdutoId, rastrear: true, ct);
                if (produto != null)
                    produto.Estoque = Math.Max(0, produto.Estoque - item.Quantidade);
            }

            compra.Status = StatusCompra.Cancelada;
            await _unitOfWork.SalvarAsync(ct);
            await transacao.CommitAsync(ct);

            _logger.LogInformation("Compra {CompraId} cancelada.", compra.Id);
            return true;
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync(ct);
            _logger.LogError(ex, "Falha ao cancelar a compra {CompraId}", id);
            return false;
        }
    }
}
