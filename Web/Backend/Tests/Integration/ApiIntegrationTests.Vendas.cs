using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PDVStore.Tests.Integration;

namespace PDVStore.Tests;

/// <summary>
/// Cenários de integração do fluxo de venda, validações, exportação e upload.
/// </summary>
public partial class ApiIntegrationTests
{
    [Fact]
    public async Task Operador_DeveRegistrarVenda_EPdv_DeveBloquearVendaSemCaixa()
    {
        using var factory = new ApiFactory();
        using var admin = await ComoAdministradorAsync(factory);

        // Produto de demonstração (seed do modo InMemory).
        var produtos = await admin.GetFromJsonAsync<JsonElement>("/api/produtos");
        var produto = produtos.EnumerateArray().First();
        var produtoId = produto.GetProperty("id").GetInt32();
        var preco = produto.GetProperty("preco").GetDecimal();
        var estoqueInicial = produto.GetProperty("estoque").GetInt32();

        using var operador = factory.CreateClient();
        operador.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await TokenAsync(operador, "Admin", "admin123"));

        // Sem caixa aberto a venda é bloqueada (regra do PDV).
        var semCaixa = await operador.PostAsJsonAsync("/api/vendas", new
        {
            itens = new[] { new { produtoId, quantidade = 1, precoUnitario = preco } },
            desconto = 0,
            formaPagamento = "PIX"
        });
        var corpoSemCaixa = await semCaixa.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(HttpStatusCode.BadRequest, semCaixa.StatusCode);
        Assert.Equal("Nenhum caixa aberto. Abra o caixa antes de registrar vendas.",
            corpoSemCaixa.GetProperty("mensagem").GetString());

        // Administrador abre o caixa e a venda é concluída.
        await admin.PostAsJsonAsync("/api/caixas/abrir", new { valorInicial = 100m });

        var venda = await operador.PostAsJsonAsync("/api/vendas", new
        {
            itens = new[] { new { produtoId, quantidade = 1, precoUnitario = preco } },
            desconto = 0,
            formaPagamento = "PIX"
        });
        var corpoVenda = await venda.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(HttpStatusCode.Created, venda.StatusCode);
        Assert.Equal("PIX", corpoVenda.GetProperty("formaPagamento").GetString());
        Assert.False(string.IsNullOrWhiteSpace(corpoVenda.GetProperty("pixTxId").GetString()));

        var produtosDepois = await admin.GetFromJsonAsync<JsonElement>("/api/produtos");
        var produtoDepois = produtosDepois.EnumerateArray().First(p => p.GetProperty("id").GetInt32() == produtoId);

        Assert.Equal(estoqueInicial - 1, produtoDepois.GetProperty("estoque").GetInt32());
    }

    [Fact]
    public async Task ClienteInvalido_DeveRetornar400ComMensagensEmPortugues()
    {
        using var factory = new ApiFactory();
        using var admin = await ComoAdministradorAsync(factory);

        var resposta = await admin.PostAsJsonAsync("/api/clientes", new
        {
            nome = "Cliente Inválido",
            cpfCnpj = "111.111.111-11",
            telefone = "123",
            email = "invalido",
            endereco = "",
            limiteCredito = 5m
        });

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        var detalhes = corpo.GetProperty("detalhes").EnumerateArray().Select(d => d.GetString()).ToList();

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("Informe um CPF ou CNPJ válido.", detalhes);
        Assert.Contains("Informe um e-mail válido.", detalhes);
        Assert.Contains("Informe um limite de crédito válido (número positivo e maior ou igual a 10).", detalhes);
    }

    [Fact]
    public async Task Exportacao_DeveRetornarArquivoExcel()
    {
        using var factory = new ApiFactory();
        using var admin = await ComoAdministradorAsync(factory);

        var resposta = await admin.GetAsync("/api/relatorios/exportar/produtos/excel");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            resposta.Content.Headers.ContentType?.MediaType);
        Assert.True((await resposta.Content.ReadAsByteArrayAsync()).Length > 0);
    }

    [Fact]
    public async Task UploadDeFoto_DeveValidarFormatoEArmazenarArquivo()
    {
        using var factory = new ApiFactory();
        using var admin = await ComoAdministradorAsync(factory);

        // Extensão inválida deve ser rejeitada.
        using (var conteudo = new ByteArrayContent(Encoding.UTF8.GetBytes("nao sou imagem")))
        {
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            var multipart = new MultipartFormDataContent();
            multipart.Add(conteudo, "arquivo", "documento.txt");

            var invalido = await admin.PostAsync("/api/usuarios/foto", multipart);
            Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        }

        // PNG válido (1x1) deve ser aceito e devolvendo o caminho público.
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        using (var conteudo = new ByteArrayContent(png))
        {
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            var multipart = new MultipartFormDataContent();
            multipart.Add(conteudo, "arquivo", "foto.png");

            var valido = await admin.PostAsync("/api/usuarios/foto", multipart);
            var corpo = await valido.Content.ReadFromJsonAsync<JsonElement>(Json);

            Assert.Equal(HttpStatusCode.OK, valido.StatusCode);
            Assert.StartsWith("/uploads/usuarios/user_", corpo.GetProperty("caminho").GetString());
        }
    }
}
