using PDVStore.Models;
using System.Collections.Generic;
using System.Linq;

namespace PDVStore.ViewModels
{
    public class PDVViewModel
    {
        public List<ItemVenda> Itens { get; } = new List<ItemVenda>();
        public decimal Desconto { get; set; }

        // PROPRIEDADE COMPUTADA: calcula o total do cupom em tempo real. Fórmula:
        // soma dos subtotais dos itens (Itens.Sum(i => i.Subtotal)) MENOS o
        // Desconto aplicado. Por ser um getter com cálculo, reflete
        // automaticamente qualquer mudança na lista de itens ou no desconto —
        // por isso o PDV mostra o total atualizado sem chamar um método manual.
        public decimal Total => Itens.Sum(i => i.Subtotal) - Desconto;

        // Limpa o cupom da venda corrente: esvazia a lista de itens e zera o desconto.
        // É chamado quando o PDV finaliza/cancela uma venda ou quando o usuário
        // clica em "Nova Venda", garantindo que o próximo cupom comece vazio e
        // sem valores herdados da venda anterior.
        public void Limpar()
        {
            Itens.Clear();
            Desconto = 0;
        }
    }
}
