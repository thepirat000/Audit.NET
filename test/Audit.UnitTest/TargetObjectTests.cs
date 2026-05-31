using Audit.Core;

using NUnit.Framework;

using System.Linq;
using System.Threading.Tasks;

namespace Audit.UnitTest;

public class TargetObjectTests
{
    [SetUp]
    public void Setup()
    {
        Audit.Core.Configuration.Reset();
    }

    [Test]
    public async Task TargetObject_WhenOldIsNull_ShouldSetTypeAndNew()
    {
        // Arrange
        Audit.Core.Configuration.Setup()
            .UseInMemoryProvider(out var dp)
            .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

        TestObject target = null;

        // Act
        await using (var scope = await AuditScope.CreateAsync("Test", () => target))
        {
            target = new TestObject { Name = "Test" };
        }

        var ev = dp.GetAllEvents().FirstOrDefault();

        // Assert
        Assert.That(ev, Is.Not.Null);
        Assert.That(ev.Target, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ev.Target.Old, Is.Null);
            Assert.That(ev.Target.New, Is.TypeOf<TestObject>());
            Assert.That(ev.Target.Type, Is.EqualTo(nameof(TestObject)));
        });
    }

    private record TestObject
    {
        public string Name { get; set; }
    }
    
}