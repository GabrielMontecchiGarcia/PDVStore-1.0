using NUnit.Framework;
using PDVStore.Models;
using PDVStore.ViewModels;

namespace PDVStore.Tests;

[TestFixture]
public class PDVViewModelTests
{
    // Cenário: ViewModel recém-criado, sem nenhum item adicionado.
    // Valida que Total começa em zero e a lista de itens está vazia. Protege o estado
    // inicial do PDV, que deve abrir "zerado" antes do primeiro produto.
    [Test]
    public void Total_InicialmenteZero()
    {
        var vm = new PDVViewModel();
        Assert.That(vm.Total, Is.Zero);
        Assert.That(vm.Itens, Is.Empty);
    }

    // Cenário: dois itens (2 x R$ 10,00 e 1 x R$ 5,50) na lista.
    // Valida que Total soma os subtotais de todos os itens (25,50). Protege o cálculo
    // final da venda exibido ao operador no balcão.
    [Test]
    public void Total_SomaOsSubtotaisDosItens()
    {
        var vm = new PDVViewModel();
        vm.Itens.Add(new ItemVenda { ProdutoId = 1, Quantidade = 2, PrecoUnitario = 10.00m });
        vm.Itens.Add(new ItemVenda { ProdutoId = 2, Quantidade = 1, PrecoUnitario = 5.50m });

        Assert.That(vm.Total, Is.EqualTo(25.50m));
    }

    // Cenário: item de R$ 100,00 com desconto de R$ 20,00.
    // Valida que Total subtrai o desconto informado (chega a 80). Protege a regra
    // comercial de que o desconto é abatido automaticamente do total da venda.
    [Test]
    public void Total_SubtraiDesconto()
    {
        var vm = new PDVViewModel();
        vm.Itens.Add(new ItemVenda { ProdutoId = 1, Quantidade = 1, PrecoUnitario = 100m });
        vm.Desconto = 20m;

        Assert.That(vm.Total, Is.EqualTo(80m));
    }

    // Cenário: PDV com itens e desconto aplicado, prontos para uma venda.
    // Valida que Limpar esvazia os itens e zera desconto e total. Protege a operação
    // de "iniciar nova venda", impedindo que valores da compra anterior vazem para a
    // próxima cobrança.
    [Test]
    public void Limpar_RemoveItensEZeraDesconto()
    {
        var vm = new PDVViewModel();
        vm.Itens.Add(new ItemVenda { ProdutoId = 1, Quantidade = 1, PrecoUnitario = 10m });
        vm.Desconto = 5m;

        vm.Limpar();

        Assert.That(vm.Itens, Is.Empty);
        Assert.That(vm.Desconto, Is.Zero);
        Assert.That(vm.Total, Is.Zero);
    }
}