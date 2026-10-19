using Microsoft.EntityFrameworkCore;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Tests.Infrastructure;

namespace PDVStore.Tests;

/// <summary>
/// Testes dos efeitos da venda (estoque, movimentações, fiado, PIX) e do estorno.
/// </summary>
public partial class VendaServiceTests
{
    [Fact]
    public async Task Venda_ComSucesso_BaixaEstoqueGeraMovimentacaoESaldoFiado()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 25m, estoque: 10);
        var cliente = await cenario.CriarClienteAsync(limite: 200m);

        var request = new VendaRequest
        {
            FormaPagamento = FormasPagamento.Fiado,
            ClienteId = cliente.Id,
            Desconto = 5m,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 2, PrecoUnitario = 25m }
            }
        };

        var venda = await cenario.Vendas.RegistrarVendaAsync(request, usuarioId);

        Assert.Equal(45m, venda.ValorTotal); // 2 x 25 - 5
        Assert.Equal(StatusVenda.Concluida, venda.Status);
        Assert.Equal(8, (await cenario.Context.Produtos.FindAsync(produto.Id))!.Estoque);
        Assert.Equal(45m, (await cenario.Context.Clientes.FindAsync(cliente.Id))!.SaldoDevedor);

        var movimentacao = await cenario.Context.MovimentacoesEstoque.SingleAsync();
        Assert.Equal(TiposMovimentacao.Saida, movimentacao.Tipo);
        Assert.Equal(2, movimentacao.Quantidade);
        Assert.Equal(venda.Id, movimentacao.ReferenciaVendaId);
    }

    [Fact]
    public async Task Venda_Pix_DeveGerarTxId()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 10m);

        var venda = await cenario.Vendas.RegistrarVendaAsync(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Pix,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = 10m }
            }
        }, usuarioId);

        Assert.False(string.IsNullOrWhiteSpace(venda.PixTxId));
    }

    [Fact]
    public async Task CancelarVenda_DeveDevolverEstoqueEReduzirSaldoFiado()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 30m, estoque: 5);
        var cliente = await cenario.CriarClienteAsync(limite: 100m);

        var venda = await cenario.Vendas.RegistrarVendaAsync(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Fiado,
            ClienteId = cliente.Id,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 2, PrecoUnitario = 30m }
            }
        }, usuarioId);

        var ok = await cenario.Vendas.CancelarVendaAsync(venda.Id, "Teste", usuarioId);

        Assert.True(ok);
        Assert.Equal(5, (await cenario.Context.Produtos.FindAsync(produto.Id))!.Estoque);
        Assert.Equal(0m, (await cenario.Context.Clientes.FindAsync(cliente.Id))!.SaldoDevedor);
        Assert.Equal(StatusVenda.Cancelada, (await cenario.Context.Vendas.FindAsync(venda.Id))!.Status);

        var estorno = await cenario.Context.MovimentacoesEstoque
            .SingleAsync(m => m.Tipo == TiposMovimentacao.Entrada);

        Assert.Equal($"Estorno - Venda #{venda.Id}", estorno.Motivo);
    }

    [Fact]
    public async Task Venda_PixMinusculo_DeveSerNormalizadaEGerarTxId()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 10m);

        var venda = await cenario.Vendas.RegistrarVendaAsync(new VendaRequest
        {
            FormaPagamento = "pix",
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = 10m }
            }
        }, usuarioId);

        Assert.Equal(FormasPagamento.Pix, venda.FormaPagamento);
        Assert.False(string.IsNullOrWhiteSpace(venda.PixTxId));
    }

    [Fact]
    public async Task Venda_DinheiroComTotalZero_PodeSerFinalizadaSemValorRecebido()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 10m);

        var venda = await cenario.Vendas.RegistrarVendaAsync(new VendaRequest
        {
            FormaPagamento = FormasPagamento.Dinheiro,
            Desconto = 10m,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = 10m }
            }
        }, usuarioId);

        Assert.Equal(0m, venda.ValorTotal);
    }

    [Fact]
    public async Task CancelarVenda_Inexistente_DeveRetornarFalse()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();

        Assert.False(await cenario.Vendas.CancelarVendaAsync(999, null, usuarioId));
    }
}
