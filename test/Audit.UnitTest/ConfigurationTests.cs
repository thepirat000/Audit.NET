using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class ConfigurationTests
    {
        [SetUp]
        public void SetUp()
        {
            Core.Configuration.Reset();
        }

        [Test]
        public void TestConfiguration_DataProviderAs()
        {
            // Arrange
            Core.Configuration.DataProvider = new InMemoryDataProvider();

            // Act
            var dataProvider = Core.Configuration.DataProviderAs<InMemoryDataProvider>();

            // Assert
            Assert.That(Core.Configuration.DataProvider, Is.TypeOf<InMemoryDataProvider>());
            Assert.That(dataProvider, Is.TypeOf<InMemoryDataProvider>());
        }

        [Test]
        public async Task TestConfiguration_AddCustomActionAsync()
        {
            // Arrange
            int test = 0;
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            // Act
            Core.Configuration.AddCustomAction(ActionType.OnEventSaved, async (scope, ct) =>
            {
                test++;
                await Task.Delay(1, ct);
            });
            var scope = await AuditScope.CreateAsync("test", null);
            await scope.SaveAsync();

            // Assert
            Assert.That(test, Is.EqualTo(1));
        }

        [Test]
        public void TestConfiguration_AddOnSavingAction()
        {
            // Arrange
            int test = 0;
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;
            
            // Act
            Core.Configuration.AddOnSavingAction(scope =>
            {
                test++;
            });

            var scope = AuditScope.Create("test", null);
            scope.Save();

            // Assert
            Assert.That(test, Is.EqualTo(1));
        }

        [Test]
        public async Task TestConfiguration_AddOnSavingActionAsync()
        {
            // Arrange
            int test = 0;
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            // Act
            Core.Configuration.AddOnSavingAction(async (scope, ct) =>
            {
                test++;
                await Task.Delay(1, ct);
            });

            var scope = await AuditScope.CreateAsync("test", null);
            await scope.SaveAsync();

            // Assert
            Assert.That(test, Is.EqualTo(1));
        }

        [Test]
        public async Task TestConfiguration_AddOnSavingActionAsyncNoCancellationToken()
        {
            // Arrange
            int test = 0;
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            // Act
            Core.Configuration.AddOnSavingAction(async scope =>
            {
                test++;
                await Task.Delay(1);
            });

            var scope = await AuditScope.CreateAsync("test", null);
            await scope.SaveAsync();

            // Assert
            Assert.That(test, Is.EqualTo(1));
        }

        [Test]
        public async Task TestConfiguration_AddOnCreatedActionAsync()
        {
            // Arrange
            int test = 0;
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            // Act
            Core.Configuration.AddOnCreatedAction(async (scope, ct) =>
            {
                test++;
                await Task.Delay(1, ct);
            });

            var scope = await AuditScope.CreateAsync("test", null);
            await scope.SaveAsync();

            // Assert
            Assert.That(test, Is.EqualTo(1));
        }

        [Test]
        public async Task TestConfiguration_AddOnCreatedActionAsyncNoCancellationToken()
        {
            // Arrange
            int test = 0;
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;

            // Act
            Core.Configuration.AddOnCreatedAction(async scope =>
            {
                test++;
                await Task.Delay(1);
            });

            var scope = await AuditScope.CreateAsync("test", null);
            await scope.SaveAsync();

            // Assert
            Assert.That(test, Is.EqualTo(1));
        }

        [TestCase(ActionType.OnScopeCreated)]
        [TestCase(ActionType.OnEventSaving)]
        [TestCase(ActionType.OnEventSaved)]
        public void TestConfiguration_ResetCustomActionsByType(ActionType type)
        {
            // Arrange
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;
            Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, _ => { });
            Core.Configuration.AddCustomAction(ActionType.OnEventSaving, _ => { });
            Core.Configuration.AddCustomAction(ActionType.OnEventSaved, _ => { });

            // Act
            Core.Configuration.ResetCustomActions(type);

            // Assert
            Assert.That(Core.Configuration.AuditScopeActions.Count, Is.EqualTo(3));
            Assert.That(Core.Configuration.AuditScopeActions[type].Count, Is.Zero);
        }


    }
}
