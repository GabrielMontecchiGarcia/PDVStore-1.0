using Microsoft.Extensions.Logging;
using PDVStore.Application.Interfaces;
using PDVStore.Domain.Entities;

namespace PDVStore.Services.Integrations;

/// <summary>
/// Mock de integração bancária (PIX/cartão), preservado do projeto Windows.
/// Regras: valida venda/valor, simula a latência de 200 ms de uma chamada real,
/// gera PixTxId para PIX e sempre aprova (não há adquirente conectado).
/// O retorno false faz a venda sofrer rollback no VendaService.
/// </summary>
public class PagamentoIntegrator : IPagamentoIntegrator
{
    private readonly ILogger<PagamentoIntegrator> _logger;

    public PagamentoIntegrator(ILogger<PagamentoIntegrator> logger) => _logger = logger;

    public async Task<bool> ProcessarPagamentoAsync(Venda venda, CancellationToken ct = default)
    {
        if (venda == null || venda.ValorTotal < 0)
            return false;

        // Simula latência de uma chamada bancária (mesmo delay do original).
        await Task.Delay(200, ct);

        if (FormasPagamento.Eh(venda.FormaPagamento, FormasPagamento.Pix))
            venda.PixTxId = Guid.NewGuid().ToString("N").ToUpperInvariant();

        // Log estruturado substitui o Console.WriteLine do mock original.
        _logger.LogInformation(
            "Pagamento {FormaPagamento} de {Valor} aprovado. TxId={PixTxId}",
            venda.FormaPagamento, venda.ValorTotal, venda.PixTxId ?? "-");

        return true; // Sempre aprova em mock
    }
}
