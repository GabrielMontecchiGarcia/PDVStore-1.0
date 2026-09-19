using NUnit.Framework;
using PDVStore.Models;
using PDVStore.ViewModels;

namespace PDVStore.Tests;

[TestFixture]
public class PDVViewModelTests
{
    [Test]
    public void Total_InicialmenteZero()
    {
        var vm = new PDVViewModel();
        Assert.That(vm.Total, Is.Zero);
        Assert.That(vm.Itens, Is.Empty);
    }

    [Test]
    public void Total_SomaOsSubtotaisDosItens()
    {
        var vm = new PDVViewModel();
        vm.Itens.Add(new ItemVenda { ProdutoId = 1, Quantidade = 2, PrecoUnitario = 10.00m });
        vm.Itens.Add(new ItemVenda { ProdutoId = 2, Quantidade = 1, PrecoUnitario = 5.50m });

        Assert.That(vm.Total, Is.EqualTo(25.50m));
    }

    [Test]
    public void Total_SubtraiDesconto()
    {
        var vm = new PDVViewModel();
        vm.Itens.Add(new ItemVenda { ProdutoId = 1, Quantidade = 1, PrecoUnitario = 100m });
        vm.Desconto = 20m;

        Assert.That(vm.Total, Is.EqualTo(80m));
    }

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