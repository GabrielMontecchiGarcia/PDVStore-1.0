using PDVStore.Models;
using System;
using System.Threading.Tasks;

namespace PDVStore.Integrations
{
    public class PagamentoIntegrator
    {
        // Mock para integração bancária; substitua por SDK real (ex: PagSeguro, Mercado Pago)
        // Processa um pagamento de forma SÍNCRONA (mock). Regra do mock: sempre
        // aprova, sem chamar um gateway real — serve para demonstrar o ponto de
        // integração bancária onde entraria um SDK (PagSeguro/Mercado Pago,
        // como indica o comentário acima). Registra a ação no console e chama
        // IntegrarContaBancaria para simular o depósito. Retorna sempre true.
        public bool ProcessarPagamento(decimal valor, string formaPagamento)
        {
            Console.WriteLine($"[Pagamento] Processando {formaPagamento} de {valor:C2}...");
            IntegrarContaBancaria(valor);
            return true; // Sempre aprova em mock
        }

        // Versão ASSÍNCRONA do processamento de pagamento, chamada por
        // VendaService.RegistrarVendaAsync (dependência crítica da venda).
        // Valida venda/ValorTotal (retorna false se inválido), simula a latência
        // de uma chamada bancária (Task.Delay(200)), e para pagamento PIX gera um
        // identificador de transação (PixTxId). Sempre aprova no mock e chama
        // IntegrarContaBancaria. O resultado false faz a venda ser DESFEITA via
        // rollback — por isso retorna bool em vez de lançar exceção.
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

        // Simula o crédito do valor na conta bancária da loja (mock). Aqui entraria a
        // chamada real ao banco/gateway de conciliação; no momento apenas registra
        // a ação no console. É chamado por ProcessarPagamento e
        // ProcessarPagamentoAsync após a "aprovação" do pagamento, mantendo a
        // separação entre aprovar (cartão/PIX) e conciliar o dinheiro (conta).
        private void IntegrarContaBancaria(decimal valor)
        {
            // Mock depósito em conta bancária
            Console.WriteLine($"[Pagamento] Depósito de {valor:C2} na conta bancária.");
        }
    }
}