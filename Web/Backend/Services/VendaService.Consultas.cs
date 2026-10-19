using Microsoft.Extensions.Logging;
using PDVStore.Domain.Entities;
using PDVStore.Repository.Interfaces;

namespace PDVStore.Services;

/// <summary>
/// Consultas e cancelamento de vendas (parte complementar do VendaService).
/// </summary>
public partial class VendaService
{
    /// <summary>Carrega a venda completa (itens/produtos/usuário/cliente) para detalhe.</summary>
    public Task<Venda?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        _vendas.ObterPorIdAsync(id, rastrear: false, ct);

    /// <summary>
    /// Lista as vendas concluídas do período, opcionalmente filtrando por caixa.
    /// Mesma consulta usada pelos relatórios do sistema original.
    /// </summary>
    public Task<List<Venda>> ListarPorPeriodoAsync(DateTime inicio, DateTime fim, int? caixaId, CancellationToken ct = default) =>
        _vendas.ListarPorPeriodoAsync(inicio, fim, caixaId, ct);

    /// <summary>
    /// Cancela uma venda desfazendo os efeitos em uma transação:
    /// devolve o estoque, registra movimentações de estorno, reduz o saldo devedor
    /// (se fiado, com Math.Max(0,...)) e marca o status como "Cancelada".
    /// Retorna false se a venda não existir ou já estiver cancelada.
    /// </summary>
    public async Task<bool> CancelarVendaAsync(int vendaId, string? motivo, int usuarioId, CancellationToken ct = default)
    {
        var venda = await _vendas.ObterPorIdAsync(vendaId, rastrear: true, ct);

        if (venda == null || venda.Status == StatusVenda.Cancelada)
            return false;

        await using var transacao = await _unitOfWork.IniciarTransacaoAsync(ct);

        try
        {
            foreach (var item in venda.Itens)
            {
                var produto = await _produtos.ObterPorIdAsync(item.ProdutoId, rastrear: true, ct);
                if (produto != null)
                    produto.Estoque += item.Quantidade;

                await _movimentacoes.AdicionarAsync(new MovimentacaoEstoque
                {
                    ProdutoId = item.ProdutoId,
                    Tipo = TiposMovimentacao.Entrada,
                    Quantidade = item.Quantidade,
                    DataMovimentacao = DateTime.UtcNow,
                    UsuarioId = usuarioId,
                    Motivo = $"Estorno - Venda #{venda.Id}",
                    ReferenciaVendaId = venda.Id
                }, ct);
            }

            // Devolve o saldo devedor se a venda era fiada.
            if (venda.ClienteId.HasValue && FormasPagamento.Eh(venda.FormaPagamento, FormasPagamento.Fiado))
            {
                var cliente = await _clientes.ObterPorIdAsync(venda.ClienteId.Value, rastrear: true, ct);
                if (cliente != null)
                    cliente.SaldoDevedor = Math.Max(0, cliente.SaldoDevedor - venda.ValorTotal);
            }

            venda.Status = StatusVenda.Cancelada;
            await _unitOfWork.SalvarAsync(ct);
            await transacao.CommitAsync(ct);

            _logger.LogInformation(
                "Venda {VendaId} cancelada por usuário {UsuarioId}. Motivo: {Motivo}",
                venda.Id, usuarioId, string.IsNullOrWhiteSpace(motivo) ? "não informado" : motivo);

            return true;
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync(ct);
            _logger.LogError(ex, "Falha ao cancelar a venda {VendaId}", vendaId);
            return false;
        }
    }
}
