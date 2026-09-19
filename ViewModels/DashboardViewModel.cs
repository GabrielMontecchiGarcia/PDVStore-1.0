using PDVStore.Services;
using PDVStore.Models;
using PDVLoja.Services;
using System.Collections.ObjectModel;

namespace PDVStore.ViewModels
{
    public class DashboardViewModel
    {
        private readonly RelatorioService _relatorioService;
        private readonly VendaService _vendaService;
        private readonly EstoqueService _estoqueService;

        public ObservableCollection<ItemRelatorio> ItensMaisVendidos { get; } = new ObservableCollection<ItemRelatorio>();
        public ObservableCollection<Produto> AlertasEstoque { get; } = new ObservableCollection<Produto>();

        public decimal TotalPeriodo { get; private set; }
        public int QuantidadeVendas { get; private set; }
        public Dictionary<string, (int Quantidade, decimal Total)> PagamentosPorForma { get; } = new();

        public List<(DateTime Dia, int Entradas, int Saidas)> MovimentacoesPorDia { get; } = new();
        public List<(DateTime Dia, decimal TotalVendas)> VendasPorDia { get; } = new();

        public DashboardViewModel(RelatorioService relatorioService, VendaService vendaService, EstoqueService estoqueService)
        {
            _relatorioService = relatorioService;
            _vendaService = vendaService;
            _estoqueService = estoqueService;
        }

        public async Task CarregarDadosAsync(DateTime inicio, DateTime fim)
        {
            ItensMaisVendidos.Clear();
            AlertasEstoque.Clear();
            PagamentosPorForma.Clear();
            MovimentacoesPorDia.Clear();
            VendasPorDia.Clear();

            foreach (var item in _relatorioService.GerarRelatorioItensMaisVendidos(inicio, fim))
                ItensMaisVendidos.Add(item);

            foreach (var p in _relatorioService.GerarRelatorioEstoqueMinimo())
                AlertasEstoque.Add(p);

            TotalPeriodo = await _vendaService.CalcularTotalVendasAsync(inicio, fim);
            QuantidadeVendas = await _vendaService.ContarVendasAsync(inicio, fim);

            var vendas = await _vendaService.RelatorioVendasPorFormaPagamentoAsync(inicio, fim);
            foreach (var grupo in vendas.GroupBy(v => v.FormaPagamento))
            {
                PagamentosPorForma[grupo.Key] = (grupo.Count(), grupo.Sum(v => v.ValorTotal));
            }

            foreach (var g in vendas.GroupBy(v => v.DataVenda.ToLocalTime().Date).OrderBy(g => g.Key))
            {
                VendasPorDia.Add((g.Key, g.Sum(v => v.ValorTotal)));
            }

            var movimentacoes = await _estoqueService.ObterHistoricoMovimentacoesAsync(inicio, fim);
            foreach (var g in movimentacoes.GroupBy(m => m.DataMovimentacao.ToLocalTime().Date).OrderBy(g => g.Key))
            {
                MovimentacoesPorDia.Add((
                    g.Key,
                    g.Where(m => m.Tipo == "Entrada").Sum(m => m.Quantidade),
                    g.Where(m => m.Tipo == "Saída").Sum(m => m.Quantidade)));
            }
        }
    }
}