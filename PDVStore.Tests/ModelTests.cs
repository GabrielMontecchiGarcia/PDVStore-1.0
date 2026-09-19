using NUnit.Framework;
using PDVStore.Models;

namespace PDVStore.Tests;

[TestFixture]
public class ModelTests
{
    // ===================== ItemVenda =====================

    [Test]
    public void ItemVenda_Subtotal_MultiplicaQuantidadePeloPreco()
    {
        var item = new ItemVenda { Quantidade = 3, PrecoUnitario = 4.50m };
        Assert.That(item.Subtotal, Is.EqualTo(13.50m));
    }

    [Test]
    public void ItemVenda_NomeProduto_RefleteNavigacao()
    {
        var item = new ItemVenda { Produto = new Produto { Nome = "Arroz" } };
        Assert.That(item.NomeProduto, Is.EqualTo("Arroz"));
    }

    [Test]
    public void ItemVenda_NomeProduto_SemProdutoRetornaNull()
    {
        var item = new ItemVenda();
        Assert.That(item.NomeProduto, Is.Null);
    }

    // ===================== ItemCompra =====================

    [Test]
    public void ItemCompra_Subtotal_MultiplicaQuantidadePeloCusto()
    {
        var item = new ItemCompra { Quantidade = 10, PrecoCusto = 2.00m };
        Assert.That(item.Subtotal, Is.EqualTo(20.00m));
    }

    // ===================== Produto =====================

    [Test]
    public void Produto_EstoqueAtual_EspelhaEstoque()
    {
        var p = new Produto { Estoque = 7 };
        Assert.That(p.EstoqueAtual, Is.EqualTo(7));
        p.EstoqueAtual = 9;
        Assert.That(p.Estoque, Is.EqualTo(9));
    }

    [Test]
    public void Produto_Padroes_SaoZeroEEstoqueMinimoZero()
    {
        var p = new Produto { Nome = "X" };
        Assert.That(p.Preco, Is.Zero);
        Assert.That(p.PrecoCusto, Is.Zero);
        Assert.That(p.EstoqueMinimo, Is.Zero);
        Assert.That(p.Ativo, Is.True);
    }

    // ===================== Cliente =====================

    [Test]
    public void Cliente_Padroes_SemLimiteESaldo()
    {
        var c = new Cliente { Nome = "A" };
        Assert.That(c.LimiteCredito, Is.Zero);
        Assert.That(c.SaldoDevedor, Is.Zero);
        Assert.That(c.Ativo, Is.True);
    }

    // ===================== UsuarioCaixa =====================

    [TestCase(TipoPermissao.Administrador, true, true)]
    [TestCase(TipoPermissao.Operador, false, false)]
    [TestCase(TipoPermissao.Estoquista, false, false)]
    public void UsuarioCaixa_Permissoes(TipoPermissao permissao, bool ehAdmin, bool podeGerenciar)
    {
        var u = new UsuarioCaixa { Nome = "U", Permissao = permissao };
        Assert.That(u.EhAdmin(), Is.EqualTo(ehAdmin));
        Assert.That(u.PodeGerenciarUsuarios(), Is.EqualTo(podeGerenciar));
    }

    [Test]
    public void UsuarioCaixa_SetSenhaEAutenticar()
    {
        var u = new UsuarioCaixa { Nome = "U" };
        u.SetSenha("123456");

        Assert.That(u.Autenticar("123456"), Is.True);
        Assert.That(u.Autenticar("errada"), Is.False);
    }

    [Test]
    public void UsuarioCaixa_GetSetAtivoEFoto()
    {
        var u = new UsuarioCaixa();
        Assert.That(u.GetAtivo(), Is.True);

        u.SetAtivo(false);
        Assert.That(u.GetAtivo(), Is.False);

        u.SetFotoPath(@"C:\foto.jpg");
        Assert.That(u.GetFotoPath(), Is.EqualTo(@"C:\foto.jpg"));
    }

    [TestCase(TipoPermissao.Operador, "Operador (Caixa)")]
    [TestCase(TipoPermissao.Administrador, "Administrador")]
    [TestCase(TipoPermissao.Estoquista, "Estoquista")]
    public void UsuarioCaixa_DescreverPermissao(TipoPermissao permissao, string esperado)
    {
        Assert.That(UsuarioCaixa.DescreverPermissao(permissao), Is.EqualTo(esperado));
    }

    [Test]
    public void UsuarioCaixa_PermissaoPadrao_EhOperador()
    {
        Assert.That(new UsuarioCaixa().Permissao, Is.EqualTo(TipoPermissao.Operador));
    }

    // ===================== Venda =====================

    [Test]
    public void Venda_Padroes()
    {
        var v = new Venda();
        Assert.That(v.Status, Is.EqualTo("Concluida"));
        Assert.That(v.FormaPagamento, Is.EqualTo("Dinheiro"));
        Assert.That(v.CaixaId, Is.EqualTo(1));
    }
}