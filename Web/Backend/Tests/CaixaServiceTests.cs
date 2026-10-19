using Microsoft.EntityFrameworkCore;
using PDVStore.Domain.Entities;
using PDVStore.Tests.Infrastructure;

namespace PDVStore.Tests;

/// <summary>
/// Testes do caixa: caixa único aberto, fechamento com sangria e validação de sangria.
/// </summary>
public class CaixaServiceTests
{
    [Fact]
    public async Task AbrirCaixa_QuandoJaExisteAberto_DeveSerBloqueado()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        await cenario.AbrirCaixaAsync(usuarioId);

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Caixas.AbrirCaixaAsync(50m, usuarioId));

        Assert.Equal("Já existe um caixa aberto. Feche o caixa atual antes de abrir outro.", ex.Message);
    }

    [Fact]
    public async Task FecharCaixa_DeveCalcularInicialMaisVendasMenosSangria()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var caixa = await cenario.AbrirCaixaAsync(usuarioId, 100m);

        cenario.Context.Vendas.Add(new Venda
        {
            UsuarioCaixaId = usuarioId,
            CaixaId = caixa.Id,
            DataVenda = DateTime.UtcNow,
            ValorTotal = 30m,
            FormaPagamento = FormasPagamento.Dinheiro,
            Status = StatusVenda.Concluida
        });

        // Venda cancelada não entra no fechamento.
        cenario.Context.Vendas.Add(new Venda
        {
            UsuarioCaixaId = usuarioId,
            CaixaId = caixa.Id,
            DataVenda = DateTime.UtcNow,
            ValorTotal = 999m,
            FormaPagamento = FormasPagamento.Pix,
            Status = StatusVenda.Cancelada
        });

        await cenario.UnitOfWork.SalvarAsync();
        await cenario.Caixas.RegistrarSangriaAsync(caixa.Id, 20m);

        var ok = await cenario.Caixas.FecharCaixaAsync(caixa.Id);
        var fechado = await cenario.Context.Caixas.FindAsync(caixa.Id);

        Assert.True(ok);
        Assert.NotNull(fechado);
        Assert.Equal(StatusCaixa.Fechado, fechado!.Status);
        Assert.Equal(110m, fechado.ValorFinal); // 100 + 30 - 20
        Assert.NotNull(fechado.Fechamento);
    }

    [Fact]
    public async Task FecharCaixa_JaFechado_DeveRetornarFalse()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var caixa = await cenario.AbrirCaixaAsync(usuarioId);

        await cenario.Caixas.FecharCaixaAsync(caixa.Id);

        Assert.False(await cenario.Caixas.FecharCaixaAsync(caixa.Id));
    }

    [Fact]
    public async Task Sangria_ComValorZero_DeveSerRejeitada()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var caixa = await cenario.AbrirCaixaAsync(usuarioId);

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.RegraDeNegocioException>(
            () => cenario.Caixas.RegistrarSangriaAsync(caixa.Id, 0m));

        Assert.Equal("Valor da sangria deve ser maior que zero.", ex.Message);
    }

    [Fact]
    public async Task Sangria_AcumulaValores_NoCaixaAberto()
    {
        await using var cenario = CenarioTeste.Criar();
        var usuarioId = await cenario.CriarOperadorAsync();
        var caixa = await cenario.AbrirCaixaAsync(usuarioId);

        await cenario.Caixas.RegistrarSangriaAsync(caixa.Id, 10m);
        await cenario.Caixas.RegistrarSangriaAsync(caixa.Id, 15m);

        var atualizado = await cenario.Context.Caixas.FindAsync(caixa.Id);
        Assert.Equal(25m, atualizado!.Sangria);
    }
}
