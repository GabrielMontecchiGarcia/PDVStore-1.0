using PDVStore.Domain.Entities;
using PDVStore.DTOs;
using PDVStore.Tests.Infrastructure;

namespace PDVStore.Tests;

/// <summary>Testes de produtos/estoque e clientes/fiado.</summary>
public class ProdutoClienteTests
{
    [Fact]
    public async Task BaixarEstoque_ComSaldoInsuficiente_DeveRetornarFalseSemAlterarSaldo()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var produto = await cenario.CriarProdutoAsync(estoque: 2);

        var ok = await cenario.Produtos.BaixarEstoqueAsync(produto.Id, 3, "Ajuste", usuarioId);

        Assert.False(ok);
        Assert.Equal(2, (await cenario.Context.Produtos.FindAsync(produto.Id))!.Estoque);
        Assert.Empty(cenario.Context.MovimentacoesEstoque);
    }

    [Fact]
    public async Task AdicionarEstoque_DeveRegistrarMovimentacaoDeEntrada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var produto = await cenario.CriarProdutoAsync(estoque: 2);

        var ok = await cenario.Produtos.AdicionarEstoqueAsync(produto.Id, 4, "Devolução", usuarioId);

        Assert.True(ok);
        Assert.Equal(6, (await cenario.Context.Produtos.FindAsync(produto.Id))!.Estoque);

        var mov = Assert.Single(cenario.Context.MovimentacoesEstoque);
        Assert.Equal(TiposMovimentacao.Entrada, mov.Tipo);
        Assert.Equal("Devolução", mov.Motivo);
        Assert.Equal(usuarioId, mov.UsuarioId);
    }

    [Fact]
    public async Task AjusteEstoque_ComQuantidadeZero_DeveSerRejeitado()
    {
        await using var cenario = CenarioTeste.Criar();
        var produto = await cenario.CriarProdutoAsync();

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Produtos.AdicionarEstoqueAsync(produto.Id, 0, null, null));

        Assert.Equal("Quantidade deve ser maior que zero.", ex.Message);
    }

    [Fact]
    public async Task DesativarProduto_DeveSerSoftDelete()
    {
        await using var cenario = CenarioTeste.Criar();
        var produto = await cenario.CriarProdutoAsync();

        await cenario.Produtos.DesativarAsync(produto.Id);

        var atualizado = await cenario.Context.Produtos.FindAsync(produto.Id);
        Assert.NotNull(atualizado);
        Assert.False(atualizado!.Ativo);

        // Continua no banco (histórico preservado).
        Assert.Single(await cenario.Produtos.ListarAsync(false, null));
        Assert.Empty(await cenario.Produtos.ListarAsync(true, null));
    }

    [Fact]
    public async Task ReceberFiado_DeveReduzirSaldoENuncaFicarNegativo()
    {
        await using var cenario = CenarioTeste.Criar();
        var cliente = await cenario.CriarClienteAsync(limite: 100m, saldo: 50m);

        var ok = await cenario.Clientes.ReceberFiadoAsync(cliente.Id, 80m);

        Assert.True(ok);
        Assert.Equal(0m, (await cenario.Context.Clientes.FindAsync(cliente.Id))!.SaldoDevedor);
    }

    [Fact]
    public async Task ReceberFiado_ComValorInvalido_DeveSerRejeitado()
    {
        await using var cenario = CenarioTeste.Criar();
        var cliente = await cenario.CriarClienteAsync();

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Clientes.ReceberFiadoAsync(cliente.Id, 0m));

        Assert.Equal("Valor deve ser maior que zero.", ex.Message);
    }

    [Fact]
    public async Task CriarCliente_DeveNormalizarDocumentoETelefoneComMascara()
    {
        await using var cenario = CenarioTeste.Criar();

        var cliente = await cenario.Clientes.CriarAsync(new ClienteRequest
        {
            Nome = "Maria",
            CpfCnpj = "52998224725",
            Telefone = "69999999999",
            Email = "maria@teste.com",
            Endereco = "Rua A, 1",
            LimiteCredito = 150m
        });

        Assert.Equal("529.982.247-25", cliente.CpfCnpj);
        Assert.Equal("(69) 99999-9999", cliente.Telefone);
        Assert.True(cliente.Ativo);
        Assert.NotEqual(default, cliente.CadastradoEm);
    }

    [Fact]
    public async Task Fornecedor_AtualizarStatus_DeveDesativar()
    {
        await using var cenario = CenarioTeste.Criar();
        var fornecedor = await cenario.CriarFornecedorAsync();

        var ok = await cenario.Fornecedores.AtualizarStatusAsync(fornecedor.Id, false);

        Assert.True(ok);
        Assert.False((await cenario.Context.Fornecedores.FindAsync(fornecedor.Id))!.Ativo);
    }
}
