using Microsoft.EntityFrameworkCore;
using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Tests.Infrastructure;

namespace PDVStore.Tests;

/// <summary>
/// Testes de compras: entrada de estoque, atualização de custo, validações e estorno.
/// </summary>
public class CompraServiceTests
{
    [Fact]
    public async Task RegistrarCompra_DeveEntrarEstoqueAtualizarCustoERegistrarMovimentacao()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var produto = await cenario.CriarProdutoAsync(estoque: 2, custo: 5m);
        var fornecedor = await cenario.CriarFornecedorAsync();

        var compra = await cenario.Compras.RegistrarCompraAsync(new CompraRequest
        {
            FornecedorId = fornecedor.Id,
            NumeroNota = "NF-123",
            Itens = new List<ItemCompraRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 5, PrecoCusto = 7.5m }
            }
        }, usuarioId);

        Assert.Equal(37.5m, compra.ValorTotal);

        var produtoAtualizado = await cenario.Context.Produtos.FindAsync(produto.Id);
        Assert.Equal(7, produtoAtualizado!.Estoque);
        Assert.Equal(7.5m, produtoAtualizado.PrecoCusto);

        var movimentacao = await cenario.Context.MovimentacoesEstoque.SingleAsync();
        Assert.Equal(TiposMovimentacao.Entrada, movimentacao.Tipo);
        Assert.Equal(5, movimentacao.Quantidade);
        Assert.Contains("NF-123", movimentacao.Motivo);
        Assert.Equal(usuarioId, movimentacao.UsuarioId);
    }

    [Fact]
    public async Task RegistrarCompra_SemItens_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var fornecedor = await cenario.CriarFornecedorAsync();

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Compras.RegistrarCompraAsync(new CompraRequest
            {
                FornecedorId = fornecedor.Id,
                Itens = new List<ItemCompraRequest>()
            }, usuarioId));

        Assert.Equal("A compra deve conter pelo menos um item.", ex.Message);
    }

    [Fact]
    public async Task RegistrarCompra_ComQuantidadeZero_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var produto = await cenario.CriarProdutoAsync();
        var fornecedor = await cenario.CriarFornecedorAsync();

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Compras.RegistrarCompraAsync(new CompraRequest
            {
                FornecedorId = fornecedor.Id,
                Itens = new List<ItemCompraRequest>
                {
                    new() { ProdutoId = produto.Id, Quantidade = 0, PrecoCusto = 5m }
                }
            }, usuarioId));

        Assert.Equal("Quantidade dos itens deve ser maior que zero.", ex.Message);
    }

    [Fact]
    public async Task RegistrarCompra_ComProdutoInexistente_DeveSerBloqueada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var fornecedor = await cenario.CriarFornecedorAsync();

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Compras.RegistrarCompraAsync(new CompraRequest
            {
                FornecedorId = fornecedor.Id,
                Itens = new List<ItemCompraRequest>
                {
                    new() { ProdutoId = 12345, Quantidade = 1, PrecoCusto = 5m }
                }
            }, usuarioId));

        Assert.Contains("não encontrado", ex.Message);
    }

    [Fact]
    public async Task CancelarCompra_DeveReduzirEstoqueSemFicarNegativo()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var produto = await cenario.CriarProdutoAsync(estoque: 0);
        var fornecedor = await cenario.CriarFornecedorAsync();

        var compra = await cenario.Compras.RegistrarCompraAsync(new CompraRequest
        {
            FornecedorId = fornecedor.Id,
            Itens = new List<ItemCompraRequest>
            {
                new() { ProdutoId = produto.Id, Quantidade = 5, PrecoCusto = 5m }
            }
        }, usuarioId);

        // Simula saída parcial do estoque antes do cancelamento.
        var produtoRastreado = await cenario.Context.Produtos.FindAsync(produto.Id);
        produtoRastreado!.Estoque = 2;
        await cenario.UnitOfWork.SalvarAsync();

        var ok = await cenario.Compras.CancelarCompraAsync(compra.Id);

        Assert.True(ok);
        Assert.Equal(0, (await cenario.Context.Produtos.FindAsync(produto.Id))!.Estoque);
        Assert.Equal(StatusCompra.Cancelada, (await cenario.Context.Compras.FindAsync(compra.Id))!.Status);
    }
}
