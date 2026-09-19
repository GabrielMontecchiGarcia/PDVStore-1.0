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

        public ObservableCollection<ItemRelatorio> ItensMaisVendidos { get; } = new ObservableCollection<ItemRelatorio>();
        public ObservableCollection<Produto> AlertasEstoque { get; } = new ObservableCollection<Produto>();

        public decimal TotalPeriodo { get; private set; }
        public int QuantidadeVendas { get; private set; }
        public Dictionary<string, (int Quantidade, decimal Total)> PagamentosPorForma { get; } = new();

        public DashboardViewModel(RelatorioService relatorioService, VendaService vendaService)
        {
            _relatorioService = relatorioService;
            _vendaService = vendaService;
        }

        public async Task CarregarDadosAsync(DateTime inicio, DateTime fim)
        {
            ItensMaisVendidos.Clear();
            AlertasEstoque.Clear();
            PagamentosPorForma.Clear();

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
        }
    }
}