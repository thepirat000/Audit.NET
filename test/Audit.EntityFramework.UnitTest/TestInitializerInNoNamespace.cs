using NUnit.Framework;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

[SetUpFixture]
public class TestInitializerInNoNamespace
{
    [OneTimeSetUp]
    public void Setup()
    {
        DbConfiguration.Loaded += (_, a) =>
        {
            //a.ReplaceService<DbProviderServices>((s, k) => SqlProviderServices.Instance);
            a.ReplaceService<IDbConnectionFactory>((s, k) => new LocalDbConnectionFactory("mssqllocaldb"));
        };
    }

    [OneTimeTearDown]
    public void Teardown()
    {
    }
}
