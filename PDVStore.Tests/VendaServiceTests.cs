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

    // Método de setup executado ANTES de cada teste de venda: prepara os serviços de caixa
    // e de venda (com o integrador de pagamento) ligados ao banco em memória. Garante
    // que todos os testes partem da mesma estrutura de serviços pronta para uso.
    [SetUp]
    public async Task VendasSetup()
    {
        _caixaService = new CaixaService(Context);
        _service = new VendaService(Context, new PagamentoIntegrator(), _caixaService);
    }

    // Método auxiliar de montagem: cadastra um produto padrão (arroz) com estoque e preço
    // configuráveis. Usado para montar os cenários de venda, validação e cancelamento.
    private async Task<Produto> CriarProdutoAsync(int estoque = 10, decimal preco = 20m)
    {
        var p = new Produto { Nome = "Arroz", CodigoBarras = "789", Preco = preco, PrecoCusto = 10m, Estoque = estoque, Ativo = true };
        await Context.Produtos.AddAsync(p);
        await Context.SaveChangesAsync();
        return p;
    }

    // Método auxiliar de montagem: cadastra um cliente com limite de crédito, saldo devedor
    // e status ativo/inativo. Usado nos cenários de venda fiado e de devolução de saldo.
    private async Task<Cliente> CriarClienteAsync(decimal limite, decimal saldo = 0, bool ativo = true)
    {
        var c = new Cliente { Nome = "Maria", CpfCnpj = "369.421.892-00", LimiteCredito = limite, SaldoDevedor = saldo, Ativo = ativo };
        await Context.Clientes.AddAsync(c);
        await Context.SaveChangesAsync();
        return c;
    }

    // Método auxiliar de montagem: abre um caixa com valor inicial zero para que as vendas
    // possam ser registradas. Vendas sem caixa aberto são rejeitadas, então quase todos
    // os testes começam por aqui.
    private async Task<Caixa> AbrirCaixaAsync()
    {
        return await _caixaService.AbrirCaixaAsync(0m, 1);
    }

    // Método auxiliar de montagem: constrói (sem gravar) uma venda com um único item,
    // permitindo ajustar quantidade, preço, forma de pagamento, desconto e cliente.
    // Deixa cada teste focar apenas na regra que está avaliando.
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

    // Cenário: registrar venda com lista de itens vazia.
    // Valida a rejeição com InvalidOperationException ("pelo menos um item"). Protege a
    // regra de que uma venda sem itens não tem valor nem movimentação de estoque.
    [Test]
    public async Task Registrar_SemItens_Rejeita()
    {
        await AbrirCaixaAsync();
        var venda = new Venda { FormaPagamento = "Dinheiro", Itens = new List<ItemVenda>() };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(venda));
        Assert.That(ex!.Message, Does.Contain("pelo menos um item"));
    }

    // Cenário: venda com a forma de pagamento em branco.
    // Valida a rejeição ("forma de pagamento"). Protege a regra de que toda venda
    // precisa saber como o pagamento foi feito (e o caixa precisa conferir).
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

    // Cenário: tentar registrar venda sem haver um caixa aberto.
    // Valida a rejeição ("Abra o caixa"). Protege a regra de negócio de que todo
    // recebimento precisa de um caixa aberto para onde o dinheiro vá.
    [Test]
    public async Task Registrar_SemCaixaAberto_Rejeita()
    {
        var p = await CriarProdutoAsync();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id)));
        Assert.That(ex!.Message, Does.Contain("Abra o caixa"));
    }

    // Cenário: venda dua forma "Fiado" sem informar o cliente.
    // Valida a rejeição ("exige um cliente"). Protege a regra de que fiado é um crédito
    // dado a um cliente identificado; sem ele, não há a quem cobrar.
    [Test]
    public async Task Registrar_FiadoSemCliente_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "Fiado")));
        Assert.That(ex!.Message, Does.Contain("exige um cliente"));
    }

    // Cenário: venda fiado para um cliente inativo.
    // Valida a rejeição ("não está ativo"). Protege a regra de que clientes inativados
    // não podem gerar novas dívidas no sistema.
    [Test]
    public async Task Registrar_FiadoClienteInativo_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();
        var c = await CriarClienteAsync(limite: 100, ativo: false);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "Fiado", clienteId: c.Id)));
        Assert.That(ex!.Message, Does.Contain("não está ativo"));
    }

    // Cenário: cliente com limite 50 e saldo 40; nova venda fiado de 60 estoura o total.
    // Valida a rejeição ("Limite de crédito"). Protege a regra comercial de que a soma
    // do saldo devedor com a nova compra jamais pode ultrapassar o limite do cliente.
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

    // Cenário: vender 5 unidades de um produto que tem apenas 1 em estoque.
    // Valida a rejeição ("Estoque insuficiente"). Protege a regra de que nenhuma venda
    // pode levar o estoque a número negativo.
    [Test]
    public async Task Registrar_EstoqueInsuficiente_Rejeita()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync(estoque: 1);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarVendaAsync(CriarVenda(p.Id, qtd: 5)));
        Assert.That(ex!.Message, Does.Contain("Estoque insuficiente"));
    }

    // Cenário: desconto maior que o valor dos produtos (10 de item, 999 de desconto).
    // Valida a rejeição de total negativo ("não pode ser negativo"). Protege a regra de
    // que uma venda jamais pode ser finalizada com valor final menor que zero.
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

    // Cenário: venda feliz em dinheiro — 2 unidades de R$ 20 com desconto de R$ 5.
    // Valida persistência (Id, status "Concluida", caixa vinculado, total 35), a baixa
    // de estoque (10 -> 8) e a movimentação de "Saída" referenciando a venda. Protege o
    // fluxo principal do PDV, que une venda, estoque e histórico do caixa.
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

    // Cenário: venda paga via PIX.
    // Valida que o sistema gera um identificador de transação (TxId) para o pagamento.
    // Protege a integração com o PIX, que exige esse código para conciliar/confirmar o
    // recebimento.
    [Test]
    public async Task Registrar_Pix_GeraTxId()
    {
        await AbrirCaixaAsync();
        var p = await CriarProdutoAsync();

        var venda = await _service.RegistrarVendaAsync(CriarVenda(p.Id, formaPagamento: "PIX"));

        Assert.That(venda.PixTxId, Is.Not.Null.And.Not.Empty);
    }

    // Cenário: venda fiado de R$ 50 para um cliente com limite de R$ 100.
    // Valida que o valor da venda é debitado do saldo devedor do cliente (50). Protege o
    // fluxo do fiado, que transforma a venda em dívida acompanhável do cliente.
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

    // Cenário: venda fiado para um cliente com limite igual a ZERO.
    // Valida que limite zero é tratado como "sem configuração de bloqueio" e a venda é
    // aceita. Protege a regra de que só um limite maior que zero dispara a verificação
    // de crédito.
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

    // Cenário: buscar uma venda registrada e um Id inexistente.
    // Valida que a consulta traz os itens e a navegação de produto completa, e que Id
    // inválido retorna null. Protege a tela de detalhe da venda, que mostra todos os
    // produtos comprados.
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

    // Cenário: listar vendas por período, por caixa e um período sem vendas, incluindo uma
    // venda CANCELADA fora do período atual.
    // Valida os filtros de data/caixa e que vendas canceladas não entram na listagem.
    // Protege relatórios e históricos, que mostram apenas o que de fato foi concluído.
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

    // Cenário: duas vendas (2 x 10 e 1 x 10) no período e um período vazio.
    // Valida o total acumulado (30), a contagem (2) e o zero fora do período. Protege os
    // indicadores usados no caixa e no dashboard gerencial.
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

    // Cenário: cancelar uma venda de 2 unidades de um produto com 5 em estoque.
    // Valida a restauração do estoque (5), o status "Cancelada", a movimentação de
    // estorno e que cancelar de novo (ou Id inexistente) retorna false. Protege a regra
    // de que o estoque é devolvido apenas uma vez por venda.
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

    // Cenário: cancelar uma venda fiado de R$ 50 após o débito no saldo do cliente.
    // Valida que o cancelamento devolve o saldo devedor a zero. Protege a regra de que
    // desfazer um fiado desfaz a dívida gerada no cliente.
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

    // Cenário: relatório de vendas por forma de pagamento com vendas PIX e Dinheiro
    // concluídas, além de uma venda cancelada.
    // Valida que o relatório agrupa somente as vendas concluídas por forma de pagamento.
    // Protege a regra de que formas de pagamento ligadas a vendas canceladas não entram
    // na estatística do período.
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