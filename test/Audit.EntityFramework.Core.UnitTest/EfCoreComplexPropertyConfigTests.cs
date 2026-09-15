using System.Collections.Generic;
using System.Linq;

using Audit.Core.Providers;
using Audit.IntegrationTest;

using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest;

#if EF_CORE_8_OR_GREATER
[TestFixture]
[Category(TestCommon.Category.Integration)]
[Category(TestCommon.Category.SqlServer)]
[NonParallelizable]
public class EfCoreComplexPropertyConfigTests
{
    [SetUp]
    public void Setup()
    {
        Audit.Core.Configuration.Reset();
        Audit.EntityFramework.Configuration.Setup().ForAnyContext().Reset();
#if EF_CORE_10_OR_GREATER
        Audit.EntityFramework.Configuration.Reset<Context_ComplexTypes_Json>();
        Audit.EntityFramework.Configuration.Reset<Context_ComplexNestedCollection>();
#endif
    }

    [TearDown]
    public void TearDown()
    {
#if EF_CORE_10_OR_GREATER
        Audit.EntityFramework.Configuration.Reset<Context_ComplexTypes_Json>();
        Audit.EntityFramework.Configuration.Reset<Context_ComplexNestedCollection>();
#endif
    }

    private static List<EntityFrameworkEvent> GetEntityFrameworkEvents(InMemoryDataProvider dp) =>
        dp.GetAllEventsOfType<AuditEventEntityFramework>().Select(e => e.EntityFrameworkEvent).ToList();

#if EF_CORE_10_OR_GREATER
    [Test]
    public void Test_EF_ComplexProperty_ParentOverride_AppliesToDirectScalars()
    {
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexTypes_Json>(c => c
                .ForEntity<Context_ComplexTypes_Json.Person>(p => p
                    .Override(x => x.Contact, (_, ctx) => $"PARENT:{ctx.ComplexPropertyPath}:{ctx.SourceValue}")));

        using var context = new Context_ComplexTypes_Json();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var person = new Context_ComplexTypes_Json.Person
        {
            Id = 1,
            Name = "John",
            Address = new Context_ComplexTypes_Json.Address
            {
                Street = "Main",
                Country = new Context_ComplexTypes_Json.Country
                {
                    Name = "AT",
                    Code = "at",
                    CountryInfo = new Context_ComplexTypes_Json.CountryInfo { Info = "info" }
                }
            },
            Contact = new Context_ComplexTypes_Json.Contact
            {
                Number = 42,
                ContactType = new Context_ComplexTypes_Json.ContactType { Type = 1, Description = "home" }
            }
        };
        context.People.Add(person);
        context.SaveChanges();

        person.Contact = person.Contact with { Number = 99 };
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        Assert.That(evs, Has.Count.EqualTo(2));
        var update = evs[1].Entries[0];
        var numberChange = update.Changes.FirstOrDefault(c => c.ColumnName == "Contact_Number");
        Assert.That(numberChange, Is.Not.Null);
        Assert.That(numberChange.NewValue, Is.EqualTo("PARENT:Contact.Number:99"));
        Assert.That(numberChange.OriginalValue, Is.EqualTo("PARENT:Contact.Number:42"));

        context.Database.EnsureDeleted();
    }

    [Test]
    public void Test_EF_ComplexProperty_TypeOverride_WinsOverParent()
    {
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexTypes_Json>(c => c
                .ForEntity<Context_ComplexTypes_Json.Person>(p => p
                    .Override(x => x.Contact, (_, _) => "PARENT"))
                .ForEntity<Context_ComplexTypes_Json.Contact>(c => c
                    .Override(x => x.Number, (_, _) => "TYPE")));

        using var context = new Context_ComplexTypes_Json();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var person = new Context_ComplexTypes_Json.Person
        {
            Id = 1,
            Name = "John",
            Address = new Context_ComplexTypes_Json.Address
            {
                Street = "Main",
                Country = new Context_ComplexTypes_Json.Country
                {
                    Name = "AT",
                    Code = "at",
                    CountryInfo = new Context_ComplexTypes_Json.CountryInfo { Info = "info" }
                }
            },
            Contact = new Context_ComplexTypes_Json.Contact { Number = 1, ContactType = new Context_ComplexTypes_Json.ContactType { Type = 1, Description = "x" } }
        };
        context.People.Add(person);
        context.SaveChanges();

        person.Contact = person.Contact with { Number = 2 };
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        var numberChange = evs[1].Entries[0].Changes.First(c => c.ColumnName == "Contact_Number");
        Assert.That(numberChange.NewValue, Is.EqualTo("TYPE"));

        context.Database.EnsureDeleted();
    }

    [Test]
    public void Test_EF_ComplexProperty_TypeFormat_WinsOverParentOverride()
    {
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexTypes_Json>(c => c
                .ForEntity<Context_ComplexTypes_Json.Person>(p => p
                    .Override(x => x.Address, (_, _) => "PARENT"))
                .ForEntity<Context_ComplexTypes_Json.Address>(a => a
                    .Format(p => p.Street, street => $"*{street}*")));

        using var context = new Context_ComplexTypes_Json();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var person = new Context_ComplexTypes_Json.Person
        {
            Id = 1,
            Name = "John",
            Address = new Context_ComplexTypes_Json.Address
            {
                Street = "Main St",
                Country = new Context_ComplexTypes_Json.Country
                {
                    Name = "AT",
                    Code = "at",
                    CountryInfo = new Context_ComplexTypes_Json.CountryInfo { Info = "info" }
                }
            },
            Contact = new Context_ComplexTypes_Json.Contact { Number = 1, ContactType = new Context_ComplexTypes_Json.ContactType { Type = 1, Description = "x" } }
        };
        context.People.Add(person);
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        var insert = evs[0].Entries[0];
        Assert.That(insert.ColumnValues["Address.Street"], Is.EqualTo("*Main St*"));
        // Nested complex children do not inherit the parent Address override (one-level cascade).
        Assert.That(insert.ColumnValues["Address.Country.Name"], Is.EqualTo("AT"));

        context.Database.EnsureDeleted();
    }

    [Test]
    public void Test_EF_ComplexProperty_Delete_UsesOriginalValues()
    {
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexTypes_Json>(c => c
                .ForEntity<Context_ComplexTypes_Json.Person>(p => p
                    .Override(x => x.Address, (_, ctx) => ctx.IsOriginal ? $"DEL:{ctx.SourceValue}" : ctx.SourceValue)));

        using var context = new Context_ComplexTypes_Json();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var person = new Context_ComplexTypes_Json.Person
        {
            Id = 1,
            Name = "John",
            Address = new Context_ComplexTypes_Json.Address
            {
                Street = "Main",
                Country = new Context_ComplexTypes_Json.Country
                {
                    Name = "AT",
                    Code = "at",
                    CountryInfo = new Context_ComplexTypes_Json.CountryInfo { Info = "info" }
                }
            },
            Contact = new Context_ComplexTypes_Json.Contact { Number = 1, ContactType = new Context_ComplexTypes_Json.ContactType { Type = 1, Description = "x" } }
        };
        context.People.Add(person);
        context.SaveChanges();

        context.People.Remove(person);
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        var delete = evs[1].Entries[0];
        Assert.That(delete.Action, Is.EqualTo("Delete"));
        Assert.That(delete.ColumnValues["Address.Street"], Is.EqualTo("DEL:Main"));

        context.Database.EnsureDeleted();
    }

    [Test]
    public void Test_EF_ComplexProperty_ReloadAfterSave_IgnoredParent_NotReloaded()
    {
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexTypes_Json>(c => c
                .ReloadDatabaseValuesAfterSave()
                .ForEntity<Context_ComplexTypes_Json.Person>(p => p
                    .Ignore(x => x.Address)));

        using var context = new Context_ComplexTypes_Json();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var person = new Context_ComplexTypes_Json.Person
        {
            Id = 1,
            Name = "John",
            Address = new Context_ComplexTypes_Json.Address
            {
                Street = "Main",
                Country = new Context_ComplexTypes_Json.Country
                {
                    Name = "AT",
                    Code = "at",
                    CountryInfo = new Context_ComplexTypes_Json.CountryInfo { Info = "info" }
                }
            },
            Contact = new Context_ComplexTypes_Json.Contact { Number = 1, ContactType = new Context_ComplexTypes_Json.ContactType { Type = 1, Description = "x" } }
        };
        context.People.Add(person);
        context.SaveChanges();

        person.Name = "Jane";
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        var update = evs[1].Entries[0];
        Assert.That(update.ColumnValues.ContainsKey("Address.Street"), Is.False);
        Assert.That(update.ColumnValues.ContainsKey("Address.Country.Name"), Is.False);
        Assert.That(update.ColumnValues["Name"], Is.EqualTo("Jane"));

        context.Database.EnsureDeleted();
    }

    [Test]
    public void Test_EF_ComplexNestedCollection_ParentIgnore_SkipsCollection()
    {
        var db = nameof(Test_EF_ComplexNestedCollection_ParentIgnore_SkipsCollection);
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexNestedCollection>(c => c
                .ForEntity<Context_ComplexNestedCollection.Person>(p => p
                    .Ignore(x => x.Address)));

        using var context = new Context_ComplexNestedCollection(db);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.People.Add(new Context_ComplexNestedCollection.Person
        {
            Id = 1,
            Name = "John",
            Address = new Context_ComplexNestedCollection.Address
            {
                Street = "Main",
                Tags = new List<Context_ComplexNestedCollection.Tag>
                {
                    new() { Label = "a" }
                }
            }
        });
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        var insert = evs[0].Entries[0];
        Assert.That(insert.ColumnValues.ContainsKey("address.Street"), Is.False);
        Assert.That(insert.ColumnValues.ContainsKey("address.Tags"), Is.False);

        context.Database.EnsureDeleted();
    }

    [Test]
    public void Test_EF_ComplexNestedCollection_ParentOverride_AppliesToCollection()
    {
        var db = nameof(Test_EF_ComplexNestedCollection_ParentOverride_AppliesToCollection);
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexNestedCollection>(c => c
                .ForEntity<Context_ComplexNestedCollection.Person>(p => p
                    .Override(x => x.Address, (_, ctx) => "OVERRIDE")));

        using var context = new Context_ComplexNestedCollection(db);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.People.Add(new Context_ComplexNestedCollection.Person
        {
            Id = 1,
            Name = "John",
            Address = new Context_ComplexNestedCollection.Address
            {
                Street = "Main",
                Tags = new List<Context_ComplexNestedCollection.Tag> { new() { Label = "a" } }
            }
        });
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        var insert = evs[0].Entries[0];
        Assert.That(insert.ColumnValues["address.Street"], Is.EqualTo("OVERRIDE"));
        Assert.That(insert.ColumnValues.ContainsKey("address.Tags"), Is.True);
        Assert.That(insert.ColumnValues["address.Tags"], Is.EqualTo("OVERRIDE"));

        context.Database.EnsureDeleted();
    }

    [Test]
    public void Test_EF_ComplexCollection_RootCollection_ComplexPropertyPath_IsNotDuplicated()
    {
        var db = nameof(Test_EF_ComplexCollection_RootCollection_ComplexPropertyPath_IsNotDuplicated);
        Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<Context_ComplexCollections>(c => c
                .ForEntity<Context_ComplexCollections.Person>(p => p
                    .Override(x => x.Complexes, (_, ctx) => ctx.ComplexPropertyPath)));

        using var context = new Context_ComplexCollections(db);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.People.Add(new Context_ComplexCollections.Person
        {
            Id = 1,
            Name = "John",
            Complexes = new List<Context_ComplexCollections.ComplexItem> { new() { Title = "First" } }
        });
        context.SaveChanges();

        var person = context.People.Single();
        person.Complexes.Add(new Context_ComplexCollections.ComplexItem { Title = "Second" });
        context.SaveChanges();

        var evs = GetEntityFrameworkEvents(dp);
        var complexesChange = evs[1].Entries[0].Changes.First(c => c.ColumnName == "Complexes");
        Assert.That(complexesChange.NewValue, Is.EqualTo("Complexes"));
        Assert.That(complexesChange.OriginalValue, Is.EqualTo("Complexes"));

        context.Database.EnsureDeleted();
    }
#endif
}
#endif
