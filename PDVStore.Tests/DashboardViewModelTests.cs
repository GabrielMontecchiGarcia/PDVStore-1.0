using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVLoja.Services;
using PDVStore.Integrations;
using PDVStore.Models;
using PDVStore.Services;
using PDVStore.ViewModels;

namespace PDVStore.Tests;

[TestFixture]
public class DashboardViewModelTests : TesteBanco
{
    // Método auxiliar de montagem: cria o DashboardViewModel com os serviços de
    // relatório, venda e estoque ligados ao contexto de banco dos testes.
    private async Task<DashboardViewModel> CriarViewModelAsync()
    {
        var relatorio = new RelatorioService(Context);
        var venda = new VendaService(Context, new PagamentoIntegrator(), new CaixaService(Context));
        var estoque = new EstoqueService(Context);
        return new DashboardViewModel(relatorio, venda, estoque);
    }

    // Método auxiliar de montagem: cadastra um produto com nome, estoque e estoque
    // mínimo informados. Usado para montar os cenários de "itens mais vendidos" e de
    // alertas de reposição.
    private async Task<Produto> CriarProdutoAsync(string nome, int estoque, int minimo)
    {
        var p = new Produto { Nome = nome, Preco = 10m, Estoque = estoque, EstoqueMinimo = minimo, Ativo = true };
        await Context.Produtos.AddAsync(p);
        await Context.SaveChangesAsync();
        return p;
    }

    // Cenário: período com uma venda concluída (R$ 40), movimentações de estoque e
    // produtos em situações de estoque mínimo diferentes.
    // Valida que o dashboard preenche todos os indicadores: mais vendidos, alertas de
    // estoque, total/quantidade, formas de pagamento, vendas e movimentações por dia.
    // Protege a visão gerencial do PDV, que não pode vir com dados faltando.
    [Test]
    public async Task CarregarDadosAsync_PreencherTodosIndicadores()
    {
        var vm = await CriarViewModelAsync();
        var cafe = await CriarProdutoAsync("Café", estoque: 5, minimo: 2);
        var acucar = await CriarProdutoAsync("Açúcar", estoque: 1, minimo: 3);

        // vendas e itens
        var vPi = new Venda
        {
            UsuarioCaixaId = 1,
            FormaPagamento = "Dinheiro",
            Status = "Concluida",
            DataVenda = DateTime.UtcNow,
            ValorTotal = 40m,
            Itens = new List<ItemVenda> { new() { ProdutoId = cafe.Id, Quantidade = 4, PrecoUnitario = 10m } }
        };
        Context.Vendas.Add(vPi);

        await Context.SaveChangesAsync();

        // movimentações de estoque
        Context.MovimentacoesEstoque.Add(new MovimentacaoEstoque { ProdutoId = cafe.Id, Tipo = "Saída", Quantidade = 2, DataMovimentacao = DateTime.UtcNow });
        Context.MovimentacoesEstoque.Add(new MovimentacaoEstoque { ProdutoId = acucar.Id, Tipo = "Entrada", Quantidade = 3, DataMovimentacao = DateTime.UtcNow });
        await Context.SaveChangesAsync();

        await vm.CarregarDadosAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        // Itens mais vendidos (joins só contam vendas com itens "Concluida")
        var maisVendido = vm.ItensMaisVendidos.SingleOrDefault();
        Assert.That(maisVendido, Is.Not.Null);
        Assert.That(maisVendido!.NomeProduto, Is.EqualTo("Café"));
        Assert.That(maisVendido.TotalVendido, Is.EqualTo(4));
        Assert.That(maisVendido.StatusMinimo, Is.EqualTo("OK")); // estoque 5 >= min 2

        // Alertas de estoque
        Assert.That(vm.AlertasEstoque.Select(p => p.Nome), Is.EqualTo(new[] { "Açúcar" }));

        // Total e quantidade
        Assert.That(vm.TotalPeriodo, Is.EqualTo(40m));
        Assert.That(vm.QuantidadeVendas, Is.EqualTo(1));

        // Formas de pagamento
        Assert.That(vm.PagamentosPorForma.Keys, Is.EqualTo(new[] { "Dinheiro" }));
        Assert.That(vm.PagamentosPorForma["Dinheiro"].Total, Is.EqualTo(40m));
        Assert.That(vm.PagamentosPorForma["Dinheiro"].Quantidade, Is.EqualTo(1));

        // Vendas por dia
        Assert.That(vm.VendasPorDia.Count, Is.EqualTo(1));
        Assert.That(vm.VendasPorDia[0].TotalVendas, Is.EqualTo(40m));

        // Movimentações por dia (entradas e saídas)
        Assert.That(vm.MovimentacoesPorDia.Count, Is.EqualTo(1));
        Assert.That(vm.MovimentacoesPorDia[0].Entradas, Is.EqualTo(3));
        Assert.That(vm.MovimentacoesPorDia[0].Saidas, Is.EqualTo(2));
    }

    // Cenário: período sem vendas e sem movimentações de estoque.
    // Valida que o dashboard não quebra quando não há dados, zerando/limpando todos os
    // indicadores. Protege a regra de que telas gerenciais precisam lidar com períodos
    // vazios de forma previsível.
    [Test]
    public async Task CarregarDadosAsync_SemDados_ZeraTudo()
    {
        var vm = await CriarViewModelAsync();

        await vm.CarregarDadosAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        Assert.That(vm.TotalPeriodo, Is.Zero);
        Assert.That(vm.QuantidadeVendas, Is.Zero);
        Assert.That(vm.ItensMaisVendidos, Is.Empty);
        Assert.That(vm.AlertasEstoque, Is.Empty);
        Assert.That(vm.PagamentosPorForma, Is.Empty);
        Assert.That(vm.MovimentacoesPorDia, Is.Empty);
    }
}