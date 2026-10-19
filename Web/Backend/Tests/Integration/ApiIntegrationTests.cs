using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PDVStore.Tests.Integration;

namespace PDVStore.Tests;

/// <summary>
/// Testes de integração da Web API (HTTP de ponta a ponta): autenticação,
/// autorização por perfil, regras de venda, validações e upload de foto.
/// Classe parcial: cenários splitados entre os arquivos do mesmo diretório.
/// </summary>
public partial class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Autentica como Admin e devolve o cliente com o token aplicado.</summary>
    private static async Task<HttpClient> ComoAdministradorAsync(ApiFactory factory)
    {
        var cliente = factory.CreateClient();
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { nome = "Admin", senha = "admin123" });
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);
        var token = corpo.GetProperty("token").GetString()!;

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    /// <summary>Autentica com as credenciais informadas e devolve o token.</summary>
    private static async Task<string> TokenAsync(HttpClient cliente, string nome, string senha)
    {
        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { nome, senha });
        var corpoLogin = await login.Content.ReadFromJsonAsync<JsonElement>(Json);
        return corpoLogin.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarTokenEUsuario()
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { nome = "Admin", senha = "admin123" });
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("token").GetString()));
        Assert.Equal("Admin", corpo.GetProperty("usuario").GetProperty("nome").GetString());
        Assert.Equal("Administrador", corpo.GetProperty("usuario").GetProperty("permissao").GetString());
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_DeveRetornar401ComMensagemPadrao()
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { nome = "Admin", senha = "errada" });
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>(Json);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("Credenciais inválidas!", corpo.GetProperty("mensagem").GetString());
    }

    [Fact]
    public async Task RotaProtegida_SemToken_DeveRetornar401()
    {
        using var factory = new ApiFactory();
        using var cliente = factory.CreateClient();

        var resposta = await cliente.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Operador_NaoDeveAcessarDashboard_NemListarVendas()
    {
        using var factory = new ApiFactory();
        using var admin = await ComoAdministradorAsync(factory);

        var criacao = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "caixa-integracao",
            senha = "123456",
            confirmacaoSenha = "123456",
            permissao = 1
        });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        using var operador = factory.CreateClient();
        operador.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await TokenAsync(operador, "caixa-integracao", "123456"));

        var dashboard = await operador.GetAsync("/api/dashboard");
        var vendas = await operador.GetAsync("/api/vendas");

        Assert.Equal(HttpStatusCode.Forbidden, dashboard.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, vendas.StatusCode);
    }
}
