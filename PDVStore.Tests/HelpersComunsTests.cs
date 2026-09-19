using NUnit.Framework;
using PDVStore.Helpers;

namespace PDVStore.Tests;

[TestFixture]
public class HelpersComunsTests
{
    // Cenário: obtenção da connection string padrão do sistema.
    // Valida que ConnectionHelper devolve uma string preenchida, apontando para um
    // banco ("Database=") no SQL Server Express LocalDB. Protege a configuração de
    // infraestrutura necessária para a aplicação iniciar.
    [Test]
    public void GetConnectionString_ValidaEDefineBanco()
    {
        var cs = ConnectionHelper.GetConnectionString();

        Assert.That(cs, Is.Not.Null.And.Not.Empty);
        Assert.That(cs, Does.Contain("Database="));
        Assert.That(cs, Does.Contain("localdb").IgnoreCase);
    }

    // Cenário: resumo legível da connection string.
    // Valida que a versão "didática" da string expõe servidor e banco de dados.
    // Protege a tela de diagnóstico/verificação do sistema, que apresenta essas
    // informações ao usuário de forma compreensível.
    [Test]
    public void GetReadableConnectionStringInfo_DetalhaComponentes()
    {
        var info = ConnectionHelper.GetReadableConnectionStringInfo();

        Assert.That(info, Does.Contain("server:"));
        Assert.That(info, Does.Contain("database:"));
    }
}