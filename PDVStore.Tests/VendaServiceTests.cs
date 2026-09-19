using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVLoja.Services;
using PDVStore.Integrations;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class VendaServiceTests : TesteBanco
{
    private CaixaService _caixaService = null!;
    private VendaService _service = null!;

    [SetUp]
    public async Task VendasSetup()
    {
        _caixaService = new CaixaService(Context);
        _service = new VendaService(Context, new PagamentoIntegrator(), _caixaService);
    }

    private async Task<Produto> CriarProdutoAsync(int estoque = 10, decimal preco = 20m)
    {
        var p = new Produto { Nome = "Arroz", CodigoBarras = "789", Preco = preco, PrecoCusto = 10m, Estoque = estoque, Ativo = true };
        await Context.Produtos.AddAsync(p);
        await Context.SaveChangesAsync();
        return p;
    }

    private async Task<Cliente> CriarClienteAsync(decimal limite, decimal saldo = 0, bool ativo = true)
    {
        var c = new Cliente { Nome = "Maria", CpfCnpj = "369.421.892-00", LimiteCredito = limite, SaldoDevedor = saldo, Ativo = ativo };
        await Context.Clientes.AddAsync(c);
        await Context.SaveChangesAsync();
        return c;
    }

    private async Task<Caixa> AbrirCaixaAsync()
    {
        return await _caixaService.AbrirCaixaAsync(0m, 1);
    }

    private static Venda CriarVenda(int produtoId, int qtd = 2, decimal preco = 20m,
        string formaPagamento = "Dinheiro", decimal desconto = 0, int? clienteId = null)
    {
        return new Venda
        {
            UsuarioCaixaId = 1,
            FormaPagamento = formaPagamento,
            Desconto = desconto,
            ClienteId = clienteId,
            Itens = new List<ItemVenda>
            {
                new() { ProdutoId = produtoId, Quantidade = qtd, PrecoUnitario = preco }
            }
        };
    }

    // ============================= Validações =============================

    [Test]
    public async Task Registrar_SemItens_Rejeita()
    {
        await AbrirCaixaAsync();
        var venda = new Venda { FormaPagamento = "Dinheiro", Itens = new List<ItemVenda>() };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(venda));
        Assert.That(ex!.Message, Does.Contain("pelo menos um item"));
    }

    [Test]
    public async Task Registrar_SemFormaPagamento_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();
        var venda = CriarVenda(p.Id);
        venda.FormaPagamento = "  ";

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(venda));
        Assert.That(ex!.Message, Does.Contain("forma de pagamento"));
    }

    [Test]
    public async Task Registrar_SemCaixaAberto_Rejeita()
    {
        var p = await CriarProdutoAsync();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id)));
        Assert.That(ex!.Message, Does.Contain("Abra o caixa"));
    }

    [Test]
    public async Task Registrar_FiadoSemCliente_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "Fiado")));
        Assert.That(ex!.Message, Does.Contain("exige um cliente"));
    }

    [Test]
    public async Task Registrar_FiadoClienteInativo_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();
        var c = await CriarClienteAsync(limite: 100, ativo: false);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "Fiado", clienteId: c.Id)));
        Assert.That(ex!.Message, Does.Contain("não está ativo"));
    }

    [Test]
    public async Task Registrar_FiadoAcimaDoLimite_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(preco: 30m);
        var c = await CriarClienteAsync(limite: 50m, saldo: 40m); // 40 + 60 > 50

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 2, preco: 30m, formaPagamento: "Fiado", clienteId: c.Id)));
        Assert.That(ex!.Message, Does.Contain("Limite de crédito"));
    }

    [Test]
    public async Task Registrar_EstoqueInsuficiente_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(estoque: 1);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 5)));
        Assert.That(ex!.Message, Does.Contain("Estoque insuficiente"));
    }

    [Test]
    public async Task Registrar_ValorTotalNegativo_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(preco: 10m);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 1, preco: 10m, desconto: 999m)));
        Assert.That(ex!.Message, Does.Contain("não pode ser negativo"));
    }

    // ============================= Sucessos =============================

    [Test]
    public async Task Registrar_Dinheiro_SucessoCompleto()
    {
        var caixa = await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(estoque: 10, preco: 20m);

        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 2, preco: 20m, desconto: 5m));

        Assert.That(venda.Id, Is.GreaterThan(0));
        Assert.That(venda.Status, Is.EqualTo("Concluida"));
        Assert.That(venda.CaixaId, Is.EqualTo(caixa.Id));
        Assert.That(venda.ValorTotal, Is.EqualTo(35m)); // 40 - 5

        var produto = await Context.Produtos.FindAsync(p.Id);
        Assert.That(produto!.Estoque, Is.EqualTo(8));

        var mov = await Context.MovimentacoesEstoque.ToListAsync();
        Assert.That(mov.Count, Is.EqualTo(1));
        Assert.That(mov[0].Tipo, Is.EqualTo("Saída"));
        Assert.That(mov[0].ReferenciaVendaId, Is.EqualTo(venda.Id));
    }

    [Test]
    public async Task Registrar_Pix_GeraTxId()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();

        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "PIX"));

        Assert.That(venda.PixTxId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Registrar_Fiado_DebitaSaldoDevedorDoCliente()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(preco: 25m);
        var c = await CriarClienteAsync(limite: 100m);

        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 2, preco: 25m, formaPagamento: "Fiado", clienteId: c.Id));

        Assert.That(venda.ValorTotal, Is.EqualTo(50m));
        var cliente = await Context.Clientes.FindAsync(c.Id);
        Assert.That(cliente!.SaldoDevedor, Is.EqualTo(50m));
    }

    [Test]
    public async Task Registrar_FiadoDentroDoLimite_ComLimiteZeroPermite()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(preco: 25m);
        var c = await CriarClienteAsync(limite: 0); // limite 0 = sem configuração de bloqueio

        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "Fiado", clienteId: c.Id));
        Assert.That(venda.Id, Is.GreaterThan(0));
    }

    // ============================= Consultas =============================

    [Test]
    public async Task ObterPorIdAsync_ReturnsCompleto()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();
        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id));

        var obtida = await _service.ObterPorIdAsync(venda.Id);
        Assert.That(obtida, Is.Not.Null);
        Assert.That(obtida!.Itens.Count, Is.EqualTo(1));
        Assert.That(obtida.Itens.Single().Produto?.Nome, Is.EqualTo("Arroz"));
        Assert.That(await _service.ObterPorIdAsync(999), Is.Null);
    }

    [Test]
    public async Task ListarPorPeriodo_GerenciaFiltros()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();
        var caixa = await _caixaService.ObterCaixaAbertoAsync();
        await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 1, formaPagamento: "PIX"));

        Context.Vendas.Add(new Venda
        {
            UsuarioCaixaId = 1,
            DataVenda = DateTime.UtcNow.AddMonths(-1),
            FormaPagamento = "Dinheiro",
            Status = "Cancelada",
            Itens = new List<ItemVenda> { new() { ProdutoId = p.Id, Quantidade = 1, PrecoUnitario = 20m } }
        });
        await Context.SaveChangesAsync();

        var periodo = await _service.ListarPorPeriodoAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.That(periodo.Count(), Is.EqualTo(1)); // a cancelada não entra

        var porCaixa = await _service.ListarPorPeriodoAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), caixa!.Id);
        Assert.That(porCaixa.Count(), Is.EqualTo(1));

        var vazio = await _service.ListarPorPeriodoAsync(DateTime.UtcNow.AddMonths(-3).AddDays(5), DateTime.UtcNow.AddMonths(-3).AddDays(6));
        Assert.That(vazio, Is.Empty);
    }

    [Test]
    public async Task CalcularTotalEContar_GerenciaPeriodo()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(preco: 10m);
        await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 2, preco: 10m));
        await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 1, preco: 10m));

        var inicio = DateTime.UtcNow.AddDays(-1);
        var fim = DateTime.UtcNow.AddDays(1);

        Assert.That(await _service.CalcularTotalVendasAsync(inicio, fim), Is.EqualTo(30m));
        Assert.That(await _service.ContarVendasAsync(inicio, fim), Is.EqualTo(2));
        Assert.That(await _service.CalcularTotalVendasAsync(DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow.AddMonths(-3).AddDays(1)), Is.EqualTo(0m));
    }

    // ============================= Cancelamento =============================

    [Test]
    public async Task CancelarVenda_RestauraEstoqueEMarcaCancelada()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(estoque: 5);
        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 2));

        Assert.That((await Context.Produtos.FindAsync(p.Id))!.Estoque, Is.EqualTo(3));

        Assert.That(await _service.CancelarVendaAsync(venda.Id, "Erro de digitação"), Is.True);

        Assert.That((await Context.Produtos.FindAsync(p.Id))!.Estoque, Is.EqualTo(5));
        Assert.That((await Context.Vendas.FindAsync(venda.Id))!.Status, Is.EqualTo("Cancelada"));

        var mov = await Context.MovimentacoesEstoque.ToListAsync();
        Assert.That(mov.Count, Is.EqualTo(2)); // saída da venda + entrada do estorno
        Assert.That(mov[1].Tipo, Is.EqualTo("Entrada"));
        Assert.That(mov[1].Motivo, Does.Contain("Estorno"));

        Assert.That(await _service.CancelarVendaAsync(venda.Id, "outra vez"), Is.False);
        Assert.That(await _service.CancelarVendaAsync(999, "x"), Is.False);
    }

    [Test]
    public async Task CancelarVenda_Fiado_DevolveSaldoDevedor()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(preco: 50m);
        var c = await CriarClienteAsync(limite: 200m);
        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 1, preco: 50m, formaPagamento: "Fiado", clienteId: c.Id));

        Assert.That((await Context.Clientes.FindAsync(c.Id))!.SaldoDevedor, Is.EqualTo(50m));

        await _service.CancelarVendaAsync(venda.Id, "teste");

        Assert.That((await Context.Clientes.FindAsync(c.Id))!.SaldoDevedor, Is.EqualTo(0m));
    }

    // ============================= Relatório rápido =============================

    [Test]
    public async Task RelatorioVendasPorFormaPagamentoAsync_SomenteConcluidas()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();
        await _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "PIX"));
        await _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 1, formaPagamento: "Dinheiro"));

        Context.Vendas.Add(new Venda { UsuarioCaixaId = 1, FormaPagamento = "Fiado", Status = "Cancelada" });
        await Context.SaveChangesAsync();

        var vendas = await _service.RelatorioVendasPorFormaPagamentoAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        var formas = vendas.Select(v => v.FormaPagamento).OrderBy(x => x).ToList();

        Assert.That(formas, Is.EqualTo(new List<string> { "Dinheiro", "PIX" }));
    }
}