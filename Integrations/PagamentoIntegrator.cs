using PDVStore.Models;
using System;
using System.Threading.Tasks;

namespace PDVStore.Integrations
{
    public class PagamentoIntegrator
    {
        // Mock para integração bancária; substitua por SDK real (ex: PagSeguro, Mercado Pago)
        public bool ProcessarPagamento(decimal valor, string formaPagamento)
        {
            Console.WriteLine($"[Pagamento] Processando {formaPagamento} de {valor:C2}...");
            IntegrarContaBancaria(valor);
            return true; // Sempre aprova em mock
        }

        /// <summary>
        /// Simula o processamento do pagamento de forma assíncrona.
        /// Para PIX gera um identificador de transação (TxId).
        /// </summary>
        internal async Task<bool> ProcessarPagamentoAsync(Venda venda)
        {
            if (venda == null || venda.ValorTotal < 0)
                return false;

            // Simula latência de uma chamada bancária
            await Task.Delay(200);

            if (venda.FormaPagamento.Equals("PIX", StringComparison.OrdinalIgnoreCase))
            {
                venda.PixTxId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            }

            Console.WriteLine($"[Pagamento] {venda.FormaPagamento} de {venda.ValorTotal:C2} aprovado." +
                              (venda.PixTxId != null ? $" TxId={venda.PixTxId}" : string.Empty));

            IntegrarContaBancaria(venda.ValorTotal);
            return true; // Sempre aprova em mock
        }

        private void IntegrarContaBancaria(decimal valor)
        {
            // Mock depósito em conta bancária
            Console.WriteLine($"[Pagamento] Depósito de {valor:C2} na conta bancária.");
        }
    }
}