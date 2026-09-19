using NUnit.Framework;
using PDVStore.Helpers;

namespace PDVStore.Tests;

[TestFixture]
public class HelpersComunsTests
{
    [Test]
    public void GetConnectionString_ValidaEDefineBanco()
    {
        var cs = ConnectionHelper.GetConnectionString();

        Assert.That(cs, Is.Not.Null.And.Not.Empty);
        Assert.That(cs, Does.Contain("Database="));
        Assert.That(cs, Does.Contain("localdb").IgnoreCase);
    }

    [Test]
    public void GetReadableConnectionStringInfo_DetalhaComponentes()
    {
        var info = ConnectionHelper.GetReadableConnectionStringInfo();

        Assert.That(info, Does.Contain("server:"));
        Assert.That(info, Does.Contain("database:"));
    }
}