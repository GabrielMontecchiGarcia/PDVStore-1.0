using NUnit.Framework;
using PDVStore.Models;

namespace PDVStore.Tests;

[TestFixture]
public class ModelTests
{
    // ===================== ItemVenda =====================

    // Cenário: item de venda com 3 unidades a R$ 4,50.
    // Valida que Subtotal calcula quantidade x preço unitário (13,50). Protege a regra
    // comercial de que cada linha da venda custa a quantidade multiplicada pelo preço.
    [Test]
    public void ItemVenda_Subtotal_MultiplicaQuantidadePeloPreco()
    {
        var item = new ItemVenda { Quantidade = 3, PrecoUnitario = 4.50m };
        Assert.That(item.Subtotal, Is.EqualTo(13.50m));
    }

    // Cenário: item de venda com o produto carregado na propriedade de navegação.
    // Valida que NomeProduto reflete o nome do produto vinculado. Protege a exibição
    // amigável do item nas telas, sem expor a navegação completa.
    [Test]
    public void ItemVenda_NomeProduto_RefleteNavigacao()
    {
        var item = new ItemVenda { Produto = new Produto { Nome = "Arroz" } };
        Assert.That(item.NomeProduto, Is.EqualTo("Arroz"));
    }

    // Cenário: item de venda sem produto navegado.
    // Valida que NomeProduto retorna null em vez de lançar exceção. Protege a UI ao
    // exibir itens cujo produto não foi carregado, sem quebrar a listagem.
    [Test]
    public void ItemVenda_NomeProduto_SemProdutoRetornaNull()
    {
        var item = new ItemVenda();
        Assert.That(item.NomeProduto, Is.Null);
    }

    // ===================== ItemCompra =====================

    // Cenário: item de compra com 10 unidades a R$ 2,00.
    // Valida que Subtotal calcula quantidade x custo (20,00). Protege o cálculo do
    // custo de cada linha da compra, usado no total pago ao fornecedor.
    [Test]
    public void ItemCompra_Subtotal_MultiplicaQuantidadePeloCusto()
    {
        var item = new ItemCompra { Quantidade = 10, PrecoCusto = 2.00m };
        Assert.That(item.Subtotal, Is.EqualTo(20.00m));
    }

    // ===================== Produto =====================

    // Cenário: leitura e escrita da propriedade EstoqueAtual do produto.
    // Valida que EstoqueAtual é um espelho da propriedade Estoque (get e set). Protege
    // a consistência entre o que as telas exibem (EstoqueAtual) e o dado real (Estoque).
    [Test]
    public void Produto_EstoqueAtual_EspelhaEstoque()
    {
        var p = new Produto { Estoque = 7 };
        Assert.That(p.EstoqueAtual, Is.EqualTo(7));
        p.EstoqueAtual = 9;
        Assert.That(p.Estoque, Is.EqualTo(9));
    }

    // Cenário: produto criado sem valores informados.
    // Valida os padrões: preços zero, estoque mínimo zero e produto ativo. Protege a
    // regra de que um produto novo nasce sem valores "mágicos" e pronto para venda.
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

    // Cenário: cliente criado sem valores informados.
    // Valida os padrões: limite de crédito zero, saldo devedor zero e ativo. Protege
    // a regra de que um cliente novo não nasce com dívida nem limite pré-configurado.
    [Test]
    public void Cliente_Padroes_SemLimiteESaldo()
    {
        var c = new Cliente { Nome = "A" };
        Assert.That(c.LimiteCredito, Is.Zero);
        Assert.That(c.SaldoDevedor, Is.Zero);
        Assert.That(c.Ativo, Is.True);
    }

    // ===================== UsuarioCaixa =====================

    // Cenário: usuários com permissão Administrador, Operador e Estoquista.
    // Valida que apenas o Administrador tem EhAdmin() e PodeGerenciarUsuarios().
    // Protege o controle de acesso: somente quem é administrador pode gerenciar
    // usuários do sistema.
    [TestCase(TipoPermissao.Administrador, true, true)]
    [TestCase(TipoPermissao.Operador, false, false)]
    [TestCase(TipoPermissao.Estoquista, false, false)]
    public void UsuarioCaixa_Permissoes(TipoPermissao permissao, bool ehAdmin, bool podeGerenciar)
    {
        var u = new UsuarioCaixa { Nome = "U", Permissao = permissao };
        Assert.That(u.EhAdmin(), Is.EqualTo(ehAdmin));
        Assert.That(u.PodeGerenciarUsuarios(), Is.EqualTo(podeGerenciar));
    }

    // Cenário: definir senha "123456" e autenticar com ela e com uma senha errada.
    // Valida que Autenticar confere o hash armazenado. Protege o login: apenas a senha
    // correta deve passar, e a errada é rejeitada.
    [Test]
    public void UsuarioCaixa_SetSenhaEAutenticar()
    {
        var u = new UsuarioCaixa { Nome = "U" };
        u.SetSenha("123456");

        Assert.That(u.Autenticar("123456"), Is.True);
        Assert.That(u.Autenticar("errada"), Is.False);
    }

    // Cenário: manipular o status ativo e o caminho da foto de um usuário.
    // Valida os métodos encapsulados Get/Set, usados pela camada que não acessa os
    // campos protegidos diretamente. Protege o encapsulamento da entidade.
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

    // Cenário: conversão de cada permissão do usuário para um rótulo amigável.
    // Valida a descrição exibida na interface para cada TipoPermissao. Protege a
    // apresentação correta do papel do usuário na tela de gestão.
    [TestCase(TipoPermissao.Operador, "Operador (Caixa)")]
    [TestCase(TipoPermissao.Administrador, "Administrador")]
    [TestCase(TipoPermissao.Estoquista, "Estoquista")]
    public void UsuarioCaixa_DescreverPermissao(TipoPermissao permissao, string esperado)
    {
        Assert.That(UsuarioCaixa.DescreverPermissao(permissao), Is.EqualTo(esperado));
    }

    // Cenário: usuário criado sem permissão explícita.
    // Valida que a permissão padrão é Operador (caixa). Protege a regra de segurança
    // de que um novo usuário não nasce com privilégios de administrador.
    [Test]
    public void UsuarioCaixa_PermissaoPadrao_EhOperador()
    {
        Assert.That(new UsuarioCaixa().Permissao, Is.EqualTo(TipoPermissao.Operador));
    }

    // ===================== Venda =====================

    // Cenário: venda criada sem valores informados.
    // Valida os padrões: status "Concluida", forma de pagamento "Dinheiro" e caixa 1.
    // Protege o comportamento padrão do modelo de venda no fluxo diário do balcão.
    [Test]
    public void Venda_Padroes()
    {
        var v = new Venda();
        Assert.That(v.Status, Is.EqualTo("Concluida"));
        Assert.That(v.FormaPagamento, Is.EqualTo("Dinheiro"));
        Assert.That(v.CaixaId, Is.EqualTo(1));
    }
}