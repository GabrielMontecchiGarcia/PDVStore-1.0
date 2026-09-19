using NUnit.Framework;
using PDVStore.Helpers;

namespace PDVStore.Tests;

[TestFixture]
public class MascarasTests
{
    // ===================== SomenteDigitos =====================

    [TestCase("abc.123-4", "1234")]
    [TestCase("(69) 9999-0000", "6999990000")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void SomenteDigitos_MantemApenasNumeros(string? entrada, string esperado)
    {
        Assert.That(Mascaras.SomenteDigitos(entrada), Is.EqualTo(esperado));
    }

    // ===================== ParseDecimal =====================

    [TestCase("12,50", 12.50)]
    [TestCase("12.50", 12.50)]
    [TestCase("987,65", 987.65)]
    [TestCase("7", 7)]
    [TestCase("", 0)]
    [TestCase(null, 0)]
    [TestCase("-99", 0)]
    [TestCase("-1", 0)]
    public void ParseDecimal_ConverteE_NuncaRetornaNegativo(string? texto, decimal esperado)
    {
        var resultado = Mascaras.ParseDecimal(texto);
        Assert.That(Math.Abs(resultado - esperado), Is.LessThan(0.001m));
    }

    [Test]
    public void ParseDecimal_UsoValorPadraoQuandoVazio()
    {
        Assert.That(Mascaras.ParseDecimal("", 42), Is.EqualTo(42));
    }

    // ===================== FormatarCpfCnpj =====================

    [TestCase("12345678900", "123.456.789-00")]
    [TestCase("123.456.789-00", "123.456.789-00")]
    [TestCase("11222333000181", "11.222.333/0001-81")]
    [TestCase("1122233300018", "11.222.333/0001-8")]
    [TestCase("112223330001", "11.222.333/0001")]
    [TestCase("369421892", "369.421.892")]
    [TestCase("369421", "369.421")]
    [TestCase("123", "123")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void FormatarCpfCnpj_FormataProgressivamente(string? entrada, string esperado)
    {
        Assert.That(Mascaras.FormatarCpfCnpj(entrada), Is.EqualTo(esperado));
    }

    [Test]
    public void FormatarCpfCnpj_TruncaMaisDe14Digitos()
    {
        Assert.That(Mascaras.FormatarCpfCnpj("1234567890012345"), Is.EqualTo("12.345.678/9001-23"));
    }

    // ===================== FormatarTelefone =====================

    [TestCase("11987654321", "(11) 98765-4321")]
    [TestCase("1198765432", "(11) 9876-5432")]
    [TestCase("1234567", "(12) 3456-7")]
    [TestCase("123456", "(12) 3456")]
    [TestCase("12", "(12) ")]
    [TestCase("1", "1")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void FormatarTelefone_FormataProgressivamente(string? entrada, string esperado)
    {
        Assert.That(Mascaras.FormatarTelefone(entrada), Is.EqualTo(esperado));
    }

    // ===================== ValidarCpf =====================

    [TestCase("123.456.789-09", true)]
    [TestCase("12345678909", true)]
    [TestCase("52998224725", true)]
    [TestCase("123.456.789-00", false)]
    [TestCase("111.111.111-11", false)]
    [TestCase("1234567", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void ValidarCpf_CasosRepresentativos(string? cpf, bool esperado)
    {
        Assert.That(Mascaras.ValidarCpf(cpf), Is.EqualTo(esperado));
    }

    // ===================== ValidarCnpj =====================

    [TestCase("11.222.333/0001-81", true)]
    [TestCase("11222333000181", true)]
    [TestCase("11.111.111/1111-11", false)]
    [TestCase("11222333000182", false)]
    [TestCase("1234", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void ValidarCnpj_CasosRepresentativos(string? cnpj, bool esperado)
    {
        Assert.That(Mascaras.ValidarCnpj(cnpj), Is.EqualTo(esperado));
    }

    // ===================== ValidarEmail =====================

    [TestCase("teste@email.com", true)]
    [TestCase("nome.sobrenome@dominio.com.br", true)]
    [TestCase("teste@email", false)]
    [TestCase("teste", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void ValidarEmail_CasosRepresentativos(string? email, bool esperado)
    {
        Assert.That(Mascaras.ValidarEmail(email), Is.EqualTo(esperado));
    }
}