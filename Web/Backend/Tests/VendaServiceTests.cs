using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Tests.Infrastructure;

namespace PDVStore.Tests;

/// <summary>
/// Testes das regras de bloqueio da venda — as mesmas do VendaService original.
/// </summary>
public partial class VendaServiceTests
{
    [Fact]
    public async Task Venda_SemCaixaAberto_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var produto = await cenario.CriarProdutoAsync();

        var request = new VendaRequest
        {
            FormaPagamento = FormasPagamento.Pix,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = produto.Preco }
            }
        };

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Vendas.RegistrarVendaAsync(request, usuarioId));

        Assert.Equal("Nenhum caixa aberto. Abra o caixa antes de registrar vendas.", ex.Message);
    }

    [Fact]
    public async Task Venda_FiadoSemCliente_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync();

        var request = new VendaRequest
        {
            FormaPagamento = FormasPagamento.Fiado,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = produto.Preco }
            }
        };

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Vendas.RegistrarVendaAsync(request, usuarioId));

        Assert.Equal("Venda fiada exige um cliente selecionado.", ex.Message);
    }

    [Fact]
    public async Task Venda_FiadoAcimaDoLimite_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 80m);
        var cliente = await cenario.CriarClienteAsync(limite: 100m, saldo: 30m);

        var request = new VendaRequest
        {
            FormaPagamento = FormasPagamento.Fiado,
            ClienteId = cliente.Id,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = 80m }
            }
        };

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Vendas.RegistrarVendaAsync(request, usuarioId));

        Assert.Contains("Limite de crédito do cliente excedido", ex.Message);

        // Nenhum efeito deve ter sido persistido (rollback).
        Assert.Equal(30m, (await cenario.Context.Clientes.FindAsync(cliente.Id))!.SaldoDevedor);
        Assert.Equal(10, (await cenario.Context.Produtos.FindAsync(produto.Id))!.Estoque);
    }

    [Fact]
    public async Task Venda_FiadoParaClienteInativo_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync();
        var cliente = await cenario.CriarClienteAsync(ativo: false);

        var request = new VendaRequest
        {
            FormaPagamento = FormasPagamento.Fiado,
            ClienteId = cliente.Id,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = produto.Preco }
            }
        };

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Vendas.RegistrarVendaAsync(request, usuarioId));

        Assert.Equal("Cliente informado não está ativo.", ex.Message);
    }

    [Fact]
    public async Task Venda_EstoqueInsuficiente_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(estoque: 1);

        var request = new VendaRequest
        {
            FormaPagamento = FormasPagamento.Pix,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 2, PrecoUnitario = produto.Preco }
            }
        };

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Vendas.RegistrarVendaAsync(request, usuarioId));

        Assert.Contains("Estoque insuficiente", ex.Message);
        Assert.Equal(1, (await cenario.Context.Produtos.FindAsync(produto.Id))!.Estoque);
    }

    [Fact]
    public async Task Venda_DinheiroComValorRecebidoInsuficiente_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);
        var produto = await cenario.CriarProdutoAsync(preco: 50m);

        var request = new VendaRequest
        {
            FormaPagamento = FormasPagamento.Dinheiro,
            ValorRecebido = 20m,
            Itens = new List<ItemVendaRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 1, PrecoUnitario = 50m }
            }
        };

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Vendas.RegistrarVendaAsync(request, usuarioId));

        Assert.Equal("Valor recebido é insuficiente para completar a venda.", ex.Message);
    }
}
