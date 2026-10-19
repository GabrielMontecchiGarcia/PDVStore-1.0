using PDVStore.Domain.Common;

namespace PDVStore.Tests;

/// <summary>
/// Testes das máscaras/validações portadas do projeto Windows (Helpers/Mascaras.cs).
/// </summary>
public class MascarasTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224725", true)]
    [InlineData("111.111.111-11", false)]
    [InlineData("123.456.789-00", false)]
    [InlineData("", false)]
    public void ValidarCpf_DeveSeguirAlgoritmoOficial(string cpf, bool esperado) =>
        Assert.Equal(esperado, Mascaras.ValidarCpf(cpf));

    [Theory]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("11222333000181", true)]
    [InlineData("11.111.111/1111-11", false)]
    [InlineData("11.222.333/0001-80", false)]
    public void ValidarCnpj_DeveSeguirAlgoritmoOficial(string cnpj, bool esperado) =>
        Assert.Equal(esperado, Mascaras.ValidarCnpj(cnpj));

    [Theory]
    [InlineData("11004249011401", true)]
    [InlineData("11004249011400", false)]
    [InlineData("123", false)]
    public void ValidarInscEstadual_DeveSeguirRegraDoProjeto(string ie, bool esperado) =>
        Assert.Equal(esperado, Mascaras.ValidarInscEstadual(ie));

    [Theory]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1234.56", 1234.56)]
    [InlineData("10,5", 10.5)]
    [InlineData("-5", 0)]
    [InlineData("", 0)]
    public void ParseDecimal_DeveAceitarVirgulaEPonto(string texto, decimal esperado) =>
        Assert.Equal(esperado, Mascaras.ParseDecimal(texto));

    [Fact]
    public void FormatarCpfCnpj_DeveAplicarMascaraProgressiva()
    {
        Assert.Equal("529.982.247-25", Mascaras.FormatarCpfCnpj("52998224725"));
        Assert.Equal("11.222.333/0001-81", Mascaras.FormatarCpfCnpj("11222333000181"));
        Assert.Equal(string.Empty, Mascaras.FormatarCpfCnpj(null));
    }

    [Fact]
    public void FormatarTelefone_DeveAplicarMascaraProgressiva()
    {
        Assert.Equal("(69) 99999-9999", Mascaras.FormatarTelefone("69999999999"));
        Assert.Equal("(69) 3333-3333", Mascaras.FormatarTelefone("6933333333"));
    }

    [Theory]
    [InlineData("cliente@teste.com", true)]
    [InlineData("cliente.teste@dominio.com.br", true)]
    [InlineData("cliente@", false)]
    [InlineData("", false)]
    public void ValidarEmail_DeveUsarMesmoPadrao(string email, bool esperado) =>
        Assert.Equal(esperado, Mascaras.ValidarEmail(email));

    [Fact]
    public void SomenteDigitos_DeveRemoverFormatacao() =>
        Assert.Equal("52998224725", Mascaras.SomenteDigitos("529.982.247-25"));
}
