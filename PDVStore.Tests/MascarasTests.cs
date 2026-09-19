using NUnit.Framework;
using PDVStore.Helpers;

namespace PDVStore.Tests;

[TestFixture]
public class MascarasTests
{
    // ===================== SomenteDigitos =====================

    // Cenário: entradas com letras, pontuação, vazias e nulas.
    // Valida que SomenteDigitos mantém apenas os números (e retorna vazio para
    // entradas sem dígitos). Protege a normalização de CPF, CNPJ e telefone antes de
    // comparar, buscar ou gravar.
    [TestCase("abc.123-4", "1234")]
    [TestCase("(69) 9999-0000", "6999990000")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void SomenteDigitos_MantemApenasNumeros(string? entrada, string esperado)
    {
        Assert.That(Mascaras.SomenteDigitos(entrada), Is.EqualTo(esperado));
    }

    // ===================== ParseDecimal =====================

    // Cenário: valores decimais com vírgula ou ponto, inteiros, vazios, nulos e negativos.
    // Valida a conversão de texto para decimal e que entradas inválidas viram 0 (nunca
    // um valor negativo). Protege os campos de valor do PDV, que não podem receber
    // números negativos vindos de máscara e precisam tolerar formatos variados.
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

    // Cenário: ParseDecimal chamado com texto vazio e um valor padrão informado.
    // Valida que o valor padrão (42) é usado quando a entrada está vazia. Protege
    // formulários que desejam um valor sugerido quando o campo não é preenchido.
    [Test]
    public void ParseDecimal_UsoValorPadraoQuandoVazio()
    {
        Assert.That(Mascaras.ParseDecimal("", 42), Is.EqualTo(42));
    }

    // ===================== FormatarCpfCnpj =====================

    // Cenário: dígitos parciais de CPF (11) e CNPJ (14), com e sem máscara aplicada.
    // Valida a formatação progressiva enquanto o usuário digita o documento. Protege
    // a usabilidade do campo, que deve ganhar pontos e barras conforme cresce.
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

    // Cenário: entrada com mais de 14 dígitos.
    // Valida que o excedente é truncado (CPF/CNPJ não aceitam mais que 14 números).
    // Protege a regra de tamanho máximo do documento cadastral.
    [Test]
    public void FormatarCpfCnpj_TruncaMaisDe14Digitos()
    {
        Assert.That(Mascaras.FormatarCpfCnpj("1234567890012345"), Is.EqualTo("12.345.678/9001-23"));
    }

    // ===================== FormatarTelefone =====================

    // Cenário: dígitos parciais de telefone, de 1 até 11 números.
    // Valida a máscara progressiva "(XX) XXXXX-XXXX". Protege a usabilidade do campo
    // telefone no cadastro de clientes e fornecedores.
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

    // Cenário: CPFs com dígitos verificadores válidos e inválidos, digitos repetidos,
    // tamanho errado, vazios e nulos.
    // Valida o algoritmo de validação do CPF. Protege o cadastro de clientes contra
    // documentos forjados, repetidos ou incorretamente digitados.
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

    // Cenário: CNPJs com dígitos verificadores válidos e inválidos, repetidos, curtos,
    // vazios e nulos.
    // Valida o algoritmo de validação do CNPJ. Protege o cadastro de fornecedores
    // contra documentos inválidos ou forjados.
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

    // Cenário: e-mails válidos (inclusive .com.br), sem domínio, sem arroba, vazios e nulos.
    // Valida a validação simples de e-mail. Protege o cadastro de clientes e
    // fornecedores contra contatos malformados que impediriam comunicação futura.
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