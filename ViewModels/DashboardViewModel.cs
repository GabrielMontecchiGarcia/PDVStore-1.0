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

        // Indicador do dashboard: total (faturamento) em R$ do período. Recebe o valor
        // de VendaService.CalcularTotalVendasAsync durante CarregarDadosAsync e é
        // exibido nos cartões de resumo da tela inicial.
        public decimal TotalPeriodo { get; private set; }
        // Indicador do dashboard: quantidade de vendas concluídas no período.
        // Preenchido por CarregarDadosAsync com base em
        // VendaService.ContarVendasAsync; alimenta o card de resumo do dashboard.
        public int QuantidadeVendas { get; private set; }
        public Dictionary<string, (int Quantidade, decimal Total)> PagamentosPorForma { get; } = new();

        public List<(DateTime Dia, int Entradas, int Saidas)> MovimentacoesPorDia { get; } = new();
        public List<(DateTime Dia, decimal TotalVendas)> VendasPorDia { get; } = new();

        // CONSTRUTOR: recebe via injeção de dependência os três serviços que o
        // dashboard precisa — RelatorioService (ranking/estoque mínimo),
        // VendaService (faturamento/vendas por forma) e EstoqueService
        // (movimentações). O ViewModel apenas ORQUESTRA as consultas; a lógica de
        // negócio fica nos serviços. Não lança exceções; guarda as dependências
        // para CarregarDadosAsync.
        public DashboardViewModel(RelatorioService relatorioService, VendaService vendaService, EstoqueService estoqueService)
        {
            _relatorioService = relatorioService;
            _vendaService = vendaService;
            _estoqueService = estoqueService;
        }

        // Carrega TODOS os dados do dashboard para um período. Primeiro limpa todas as
        // coleções para não misturar período anterior com o novo; depois chama, na
        // ordem: RelatorioService (itens mais vendidos + estoque mínimo),
        // VendaService (TotalPeriodo, QuantidadeVendas e vendas por forma de
        // pagamento — agrupadas localmente com LINQ) e, por fim,
        // EstoqueService.ObterHistoricoMovimentacoesAsync para montar o gráfico de
        // movimentações por dia (Entradas x Saídas). As datas são convertidas
        // com ToLocalTime().Date pois as consultas vêm em UTC. Quem chama: a tela
        // do Dashboard ao abrir ou ao alterar o período selecionado.
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