using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.EntityFramework.ConfigurationApi;
using Audit.IntegrationTest;
using Moq;
using NUnit.Framework;

namespace Audit.EntityFramework.Full.UnitTest;

[TestFixture]
[Category(TestCommon.Category.Integration)]
[Category(TestCommon.Category.SqlServer)]
public class AuditIdentityDbContextNonGenericTests
{
    [Test]
    public void Constructor_Default_SetsProperties()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        Assert.IsNotNull(ctx);
        Assert.IsFalse(ctx.AuditDisabled);
        Assert.IsNull(ctx.AuditEventType);
        Assert.IsNotNull(ctx.DbContext);
        Assert.IsNotNull(ctx.ExtraFields);
    }

    [Test]
    public void Constructor_WithNameOrConnectionString_SetsProperties()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric("TestConnection");
        Assert.IsNotNull(ctx);
    }

    [Test]
    public void Constructor_WithModel_SetsProperties()
    {
        var (model, _) = GetDbCompiledModelAndConnection();
        using var ctx = new TestAuditIdentityDbContextNonGeneric(model);
        Assert.IsNotNull(ctx);
    }

    [Test]
    public void Constructor_WithConnectionAndOwns_SetsProperties()
    {
        var (_, conn) = GetDbCompiledModelAndConnection();
        using var ctx = new TestAuditIdentityDbContextNonGeneric(conn, true);
        Assert.IsNotNull(ctx);
    }

    [Test]
    public void Constructor_WithNameAndModel_SetsProperties()
    {
        var (model, _) = GetDbCompiledModelAndConnection();
        using var ctx = new TestAuditIdentityDbContextNonGeneric("TestConnection", model);
        Assert.IsNotNull(ctx);
    }

    [Test]
    public void Constructor_WithConnectionModelOwns_SetsProperties()
    {
        var (model, conn) = GetDbCompiledModelAndConnection();
        using var ctx = new TestAuditIdentityDbContextNonGeneric(conn, model, true);
        Assert.IsNotNull(ctx);
    }

    [Test]
    public void Properties_CanBeSetAndRetrieved()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        ctx.AuditDataProvider = Mock.Of<IAuditDataProvider>();
        ctx.AuditScopeFactory = Mock.Of<IAuditScopeFactory>();
        ctx.AuditDisabled = true;
        ctx.AuditEventType = "TestEvent";
        ctx.IncludeEntityObjects = true;
        ctx.ExcludeValidationResults = true;
        ctx.Mode = AuditOptionMode.OptIn;
        ctx.IncludeIndependantAssociations = true;
        ctx.EntitySettings = new Dictionary<Type, EfEntitySettings>();
        ctx.ExcludeTransactionId = true;
        ctx.ReloadDatabaseValues = true;
        ctx.MapChangesByColumn = true;
        ctx.IncludedPropertyNames = new Dictionary<Type, HashSet<string>>();

        Assert.IsTrue(ctx.AuditDisabled);
        Assert.AreEqual("TestEvent", ctx.AuditEventType);
        Assert.IsTrue(ctx.IncludeEntityObjects);
        Assert.IsTrue(ctx.ExcludeValidationResults);
        Assert.AreEqual(AuditOptionMode.OptIn, ctx.Mode);
        Assert.IsTrue(ctx.IncludeIndependantAssociations);
        Assert.IsNotNull(ctx.EntitySettings);
        Assert.IsTrue(ctx.ExcludeTransactionId);
        Assert.IsTrue(ctx.ReloadDatabaseValues);
        Assert.IsTrue(ctx.MapChangesByColumn);
        Assert.IsNotNull(ctx.IncludedPropertyNames);
    }

    [Test]
    public void AddAuditCustomField_AddsField()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        ctx.AddAuditCustomField("Field1", 123);
        Assert.IsTrue(ctx.ExtraFields.ContainsKey("Field1"));
        Assert.AreEqual(123, ctx.ExtraFields["Field1"]);
    }

    [Test]
    public void OnScopeCreated_CanBeCalled()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        ctx.OnScopeCreated(Mock.Of<IAuditScope>());
    }

    [Test]
    public void OnScopeSaving_CanBeCalled()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        ctx.OnScopeSaving(Mock.Of<IAuditScope>());
    }

    [Test]
    public void OnScopeSaved_CanBeCalled()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        ctx.OnScopeSaved(Mock.Of<IAuditScope>());
    }

    [Test]
    public void SaveChangesBypassAudit_CanBeCalled()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        var bypass = (IAuditBypass)ctx;
        Assert.DoesNotThrow(() => bypass.SaveChangesBypassAudit());
    }

    [Test]
    public async Task SaveChangesBypassAuditAsync_CanBeCalled()
    {
        using var ctx = new TestAuditIdentityDbContextNonGeneric();
        var bypass = (IAuditBypass)ctx;
        Assert.DoesNotThrowAsync(async () => await bypass.SaveChangesBypassAuditAsync(CancellationToken.None));
    }

    private (DbCompiledModel Model, DbConnection DbConnection) GetDbCompiledModelAndConnection()
    {
        using var context = new TestAuditIdentityDbContextNonGeneric(TestHelper.GetConnectionString("testIdentity"));
        context.AuditDisabled = true;
        var conn = context.Database.Connection;
        var builder = new DbModelBuilder();
        var build = builder.Build(conn);

        return (build.Compile(), conn);
    }
}