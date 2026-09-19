using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDVStore.Models;
using PDVStore.Services;

namespace PDVStore.Tests;

[TestFixture]
public class CompraServiceTests : TesteBanco
{
    // Método auxiliar de montagem: cadastra um fornecedor e um produto com o estoque e
    // o custo desejados, devolvendo o serviço pronto e as entidades criadas.
    // Centraliza a preparação de dados comum a todos os testes de compra.
    private async Task<(CompraService Service, Produto Produto, Fornecedor Fornecedor)> SetupAsync(
        int estoque = 10, int qtd = 5, decimal custo = 2.50m)
    {
        var fornecedor = new Fornecedor { Nome = "Distribuidora" };
        await Context.Fornecedores.AddAsync(fornecedor);

        var produto = new Produto { Nome = "Arroz", CodigoBarras = "789", Preco = 5m, PrecoCusto = 2m, Estoque = estoque, Ativo = true };
        await Context.Produtos.AddAsync(produto);
        await Context.SaveChangesAsync();

        return (new CompraService(Context), produto, fornecedor);
    }

    // Método auxiliar de montagem: constrói (sem gravar) uma compra válida com um único
    // item. Recebe produto, quantidade e custo, deixando o teste livre para adaptar o
    // cenário (itens vazios, quantidades inválidas, custo negativo etc.).
    private Compra CriarCompra(int produtoId, int qtd = 5, decimal custo = 2.50m)
    {
        return new Compra
        {
            FornecedorId = 1,
            UsuarioCaixaId = 1,
            NumeroNota = "N-001",
            Itens = new List<ItemCompra>
            {
                new() { ProdutoId = produtoId, Quantidade = qtd, PrecoCusto = custo }
            }
        };
    }

    // Cenário: registrar compra sem nenhum item.
    // Valida a rejeição com InvalidOperationException ("pelo menos um item").
    // Protege a regra de que toda compra precisa trazer ao menos um produto.
    [Test]
    public async Task RegistrarCompraAsync_SemItens_Rejeita()
    {
        var (service, _, _) = await SetupAsync();
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(new Compra { Itens = new List<ItemCompra>() }));
        Assert.That(ex!.Message, Does.Contain("pelo menos um item"));
    }

    // Cenário: item de compra com quantidade zero.
    // Valida a rejeição ("maior que zero"). Protege a regra de que quantidades de
    // compra devem ser positivas para não corromper o estoque.
    [Test]
    public async Task RegistrarCompraAsync_QuantidadeInvalida_Rejeita()
    {
        var (service, p, _) = await SetupAsync();
        var compra = CriarCompra(p.Id, qtd: 0);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(compra));
        Assert.That(ex!.Message, Does.Contain("maior que zero"));
    }

    // Cenário: item de compra com custo negativo.
    // Valida a rejeição ("não pode ser negativo"). Protege a regra de que o custo
    // de mercadoria nunca é menor que zero.
    [Test]
    public async Task RegistrarCompraAsync_CustoNegativo_Rejeita()
    {
        var (service, p, _) = await SetupAsync();
        var compra = CriarCompra(p.Id, custo: -1m);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(compra));
        Assert.That(ex!.Message, Does.Contain("não pode ser negativo"));
    }

    // Cenário: compra apontando para um produto que não existe (999).
    // Valida a rejeição ("não encontrado"). Protege a integridade referencial — não
    // se pode dar entrada em estoque de um produto desconhecido.
    [Test]
    public async Task RegistrarCompraAsync_ProdutoInexistente_Rejeita()
    {
        var (service, _, _) = await SetupAsync();
        var compra = CriarCompra(produtoId: 999);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => service.RegistrarCompraAsync(compra));
        Assert.That(ex!.Message, Does.Contain("não encontrado"));
    }

    // Cenário: compra feliz de 4 unidades a R$ 2,50 num produto com 3 em estoque.
    // Valida persistência da compra (status "Concluida", total 10 e data), entrada de
    // 4 unidades no estoque, atualização do PrecoCusto e movimentação "Entrada" com a
    // referência à compra. Protege o fluxo completo de recebimento de mercadoria.
    [Test]
    public async Task RegistrarCompraAsync_Sucesso_EntraEstoqueEGravaMovimentacao()
    {
        var (service, p, _) = await SetupAsync(estoque: 3);

        var compra = CriarCompra(p.Id, qtd: 4, custo: 2.50m);
        var registrada = await service.RegistrarCompraAsync(compra);

        Assert.That(registrada.Id, Is.GreaterThan(0));
        Assert.That(registrada.Status, Is.EqualTo("Concluida"));
        Assert.That(registrada.ValorTotal, Is.EqualTo(10m)); // 4 * 2,50
        Assert.That(registrada.DataCompra, Is.Not.EqualTo(default));

        var produto = await Context.Produtos.FindAsync(p.Id);
        Assert.That(produto!.Estoque, Is.EqualTo(7)); // 3 + 4
        Assert.That(produto.PrecoCusto, Is.EqualTo(2.50m));

        var mov = await Context.MovimentacoesEstoque.ToListAsync();
        Assert.That(mov.Count, Is.EqualTo(1));
        Assert.That(mov[0].Tipo, Is.EqualTo("Entrada"));
        Assert.That(mov[0].Quantidade, Is.EqualTo(4));
        Assert.That(mov[0].Motivo, Does.Contain("Compra #"));
    }

    // Cenário: compra com custo zero para um produto cujo PrecoCusto já é 2.
    // Valida que o custo zero entra no estoque mas NÃO sobrescreve o último custo
    // conhecido. Protege a regra de que só um custo informado recalcula o PrecoCusto.
    [Test]
    public async Task RegistrarCompraAsync_CustoZero_NaocalculaPrecoCusto()
    {
        var (service, p, _) = await SetupAsync(estoque: 0, custo: 0m);
        var compra = CriarCompra(p.Id, qtd: 2, custo: 0m);

        await service.RegistrarCompraAsync(compra);

        var produto = await Context.Produtos.FindAsync(p.Id);
        Assert.That(produto!.Estoque, Is.EqualTo(2));
        Assert.That(produto.PrecoCusto, Is.EqualTo(2m)); // inalterado
    }

    // Cenário: leitura de uma compra já registrada e de um Id inexistente.
    // Valida que ObterPorIdAsync carrega os relacionamentos (fornecedor, itens e
    // produto) via Include e retorna null para Id inválido. Protege a tela de detalhe
    // da compra, que precisa desses dados sem consultas extras.
    [Test]
    public async Task ObterPorIdAsync_ComInclude_RetornaDadosRelacionados()
    {
        var (service, p, f) = await SetupAsync();
        var registrada = await service.RegistrarCompraAsync(CriarCompra(p.Id));

        var obtida = await service.ObterPorIdAsync(registrada.Id);

        Assert.That(obtida, Is.Not.Null);
        Assert.That(obtida!.Fornecedor?.Nome, Is.EqualTo(f.Nome));
        Assert.That(obtida.Itens.Count, Is.EqualTo(1));
        Assert.That(obtida.Itens.Single().Produto?.Id, Is.EqualTo(p.Id));

        Assert.That(await service.ObterPorIdAsync(999), Is.Null);
    }

    // Cenário: duas compras em períodos diferentes (uma recente, outra de 2 meses atrás).
    // Valida a ordenação pela data (mais recente primeiro) e o filtro por período.
    // Protege as telas de histórico e relatório de compras.
    [Test]
    public async Task ListarAsync_FiltraPorPeriodoEOrdenaDesc()
    {
        var (service, p, _) = await SetupAsync();
        await service.RegistrarCompraAsync(CriarCompra(p.Id));

        Context.Compras.Add(new Compra
        {
            FornecedorId = 1,
            UsuarioCaixaId = 1,
            DataCompra = DateTime.UtcNow.AddMonths(-2),
            Itens = new List<ItemCompra> { new() { ProdutoId = p.Id, Quantidade = 1, PrecoCusto = 1m } }
        });
        await Context.SaveChangesAsync();

        var todas = await service.ListarAsync();
        Assert.That(todas.Count, Is.EqualTo(2));
        Assert.That(todas[0].DataCompra >= todas[1].DataCompra, Is.True);

        var recentes = await service.ListarAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        Assert.That(recentes.Count, Is.EqualTo(1));
    }

    // Cenário: cancelar uma compra que deu entrada em 5 unidades de um produto com 2.
    // Valida o estorno do estoque (7 -> 2), o status "Cancelada" e que um segundo
    // cancelamento (ou Id inexistente) retorna false. Protege a regra de que o
    // estoque é devolvido apenas uma vez.
    [Test]
    public async Task CancelarCompraAsync_EstornaEstoqueSoUmaVez()
    {
        var (service, p, _) = await SetupAsync(estoque: 2);
        var compra = await service.RegistrarCompraAsync(CriarCompra(p.Id, qtd: 5));

        Assert.That((await Context.Produtos.FindAsync(p.Id))!.Estoque, Is.EqualTo(7));

        Assert.That(await service.CancelarCompraAsync(compra.Id), Is.True);
        Assert.That((await Context.Produtos.FindAsync(p.Id))!.Estoque, Is.EqualTo(2));
        Assert.That((await service.ObterPorIdAsync(compra.Id))!.Status, Is.EqualTo("Cancelada"));

        Assert.That(await service.CancelarCompraAsync(compra.Id), Is.False); // já cancelada
        Assert.That(await service.CancelarCompraAsync(999), Is.False);
    }
}