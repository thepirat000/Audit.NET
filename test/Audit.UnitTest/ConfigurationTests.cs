using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;

using System.Threading;
using System.Threading.Tasks;

namespace Audit.UnitTest
{
    [TestFixture]
    public class ConfigurationTests
    {
        [SetUp]
        public void SetUp()
        {
            Core.Configuration.Reset();
            Core.Configuration.DataProvider = new InMemoryDataProvider();
            Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;
        }

        [Test]
        public void TestConfiguration_DataProviderAs()
        {
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

            // Act
            Core.Configuration.AddOnSavingAction(scope => { test++; });

            var scope = AuditScope.Create("test", null);
            scope.Save();

            // Assert
            Assert.That(test, Is.EqualTo(1));
        }

        [Test]
        public void TestConfiguration_AddOnSavingAction_AsFunc()
        {
            // Arrange
            int test = 0;

            // Act
            Core.Configuration.AddOnSavingAction(scope => 
            { 
                test++;
                return true;
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
        [TestCase(ActionType.OnScopeDisposed)]
        public void TestConfiguration_ResetCustomActionsByType(ActionType type)
        {
            // Arrange
            Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, _ => { });
            Core.Configuration.AddCustomAction(ActionType.OnEventSaving, _ => { });
            Core.Configuration.AddCustomAction(ActionType.OnEventSaved, _ => { });
            Core.Configuration.AddCustomAction(ActionType.OnScopeDisposed, _ => { });

            // Act
            Core.Configuration.ResetCustomActions(type);

            // Assert
            Assert.That(Core.Configuration.AuditScopeActions.Count, Is.EqualTo(4));
            Assert.That(Core.Configuration.AuditScopeActions[type].Count, Is.Zero);
        }

        [Test]
        public void TestConfiguration_AddOnDisposedAction()
        {
            int onDisposed = 0;
            Core.Configuration.AddOnDisposedAction(s => { onDisposed++; });
            Core.Configuration.AddOnDisposedAction(async s =>
            {
                await Task.Yield();
                onDisposed++;
            });
            Core.Configuration.AddOnDisposedAction(async (s, ct) =>
            {
                await Task.Yield();
                onDisposed++;
            });
            Core.Configuration.AddOnDisposedAction(async s =>
            {
                await Task.Yield();
                onDisposed++;
                return true;
            });

            var scope = AuditScope.Create(new AuditScopeOptions() { DataProvider = new NullDataProvider() });
            scope.Dispose();

            Assert.That(onDisposed, Is.EqualTo(4));
        }

        [TestCase(ActionType.OnScopeCreated)]
        [TestCase(ActionType.OnEventSaving)]
        [TestCase(ActionType.OnScopeDisposed)]
        public void TestConfiguration_CustomActionsContinuation(ActionType type)
        {
            // Arrange
            int count = 0;

            // To cover the methods per action type
            switch (type)
            {
                case ActionType.OnScopeCreated:
                    Core.Configuration.AddOnCreatedAction(scope =>
                    {
                        count++;
                        return true;
                    });
                    break;
                case ActionType.OnEventSaving:
                    Core.Configuration.AddOnSavingAction(scope =>
                    {
                        count++;
                        return true;
                    });
                    break;
                case ActionType.OnScopeDisposed:
                    Core.Configuration.AddOnDisposedAction(scope =>
                    {
                        count++;
                        return true;
                    });
                    break;
                default:
                    Core.Configuration.AddCustomAction(type, scope =>
                    {
                        count++;
                        return true;
                    });
                    break;
            }

            Core.Configuration.AddCustomAction(type, async (scope, ct) =>
            {
                count++;
                return await Task.FromResult(false);
            });
            Core.Configuration.AddCustomAction(type, async (scope, ct) =>
            {
                count = -1;
                return await Task.FromResult(true);
            });

            // Act
            var scope = AuditScope.Create("test", null);
            scope.Save();
            scope.Dispose();

            // Assert
            Assert.That(count, Is.EqualTo(2));
        }

        [TestCase(ActionType.OnScopeCreated)]
        [TestCase(ActionType.OnEventSaving)]
        [TestCase(ActionType.OnEventSaved)]
        [TestCase(ActionType.OnScopeDisposed)]
        public void TestConfiguration_CustomActionsContinuation_Async(ActionType type)
        {
            // Arrange
            int count = 0;
            Core.Configuration.AddCustomAction(type, async (scope, ct) =>
            {
                count = 1;
                return await Task.FromResult(true);
            });

            // To cover the methods per action type
            switch (type)
            {
                case ActionType.OnScopeCreated:
                    Core.Configuration.AddOnCreatedAction(async (scope, ct) =>
                    {
                        count++;
                        return await Task.FromResult(false);
                    });
                    break;
                case ActionType.OnEventSaving:
                    Core.Configuration.AddOnSavingAction(async (scope, ct) =>
                    {
                        count++;
                        return await Task.FromResult(false);
                    });
                    break;
                case ActionType.OnScopeDisposed:
                    Core.Configuration.AddOnDisposedAction(async (scope, ct) =>
                    {
                        count++;
                        return await Task.FromResult(false);
                    });
                    break;
                default:
                    Core.Configuration.AddCustomAction(type, async (scope, ct) =>
                    {
                        count++;
                        return await Task.FromResult(false);
                    });
                    break;
            }

            Core.Configuration.AddCustomAction(type, async (scope, ct) =>
            {
                count++;
                return await Task.FromResult(false);
            });
            Core.Configuration.AddCustomAction(type, async (scope, ct) =>
            {
                count = -1;
                return await Task.FromResult(true);
            });

            // Act
            var scope = AuditScope.Create("test", null);
            scope.Save();
            scope.Dispose();

            // Assert
            Assert.That(count, Is.EqualTo(2));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task TestConfiguration_ExcludeEnvironmentInfo(bool exclude)
        {
            // Arrange
            Core.Configuration.ExcludeEnvironmentInfo = exclude;

            // Act
            var scope = await AuditScope.CreateAsync("test", null);
            await scope.SaveAsync();

            // Assert
            Assert.That(scope.Event.Environment, exclude ? Is.Null : Is.Not.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task TestConfiguration_ExcludeEnvironmentInfo_Option(bool exclude)
        {
            // Arrange
            Core.Configuration.ExcludeEnvironmentInfo = !exclude;

            // Act
            var scope = await AuditScope.CreateAsync(c => c.ExcludeEnvironmentInfo(exclude));
            await scope.SaveAsync();

            // Assert
            Assert.That(scope.Event.Environment, exclude ? Is.Null : Is.Not.Null);
        }

        [TestCase(ActionType.OnScopeCreated)]
        [TestCase(ActionType.OnEventSaving)]
        [TestCase(ActionType.OnEventSaved)]
        [TestCase(ActionType.OnScopeDisposed)]
        public void TestConfiguration_CustomAction_Stress(ActionType actionType)
        {
            Configuration.ResetCustomActions();
            const int size = 100;
            var tasks = new Task[size];
            int addActionCounter = 0;

            for (int i = 0; i < size; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    Core.Configuration.AddCustomAction(actionType, async (scope, ct) =>
                    {
                        Interlocked.Increment(ref addActionCounter);
                        await Task.CompletedTask;
                    });
                });
            }

            Task.WaitAll(tasks);

            Assert.That(Core.Configuration.AuditScopeActions[actionType].Count, Is.EqualTo(100));

            int scopeCounter = 0;
            tasks = new Task[size];
            for (int i = 0; i < size; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    await using var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { DataProvider = new NullDataProvider() });
                    {
                        Interlocked.Increment(ref scopeCounter);
                        await scope.SaveAsync();
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.That(addActionCounter, Is.EqualTo(size * size));
            Assert.That(scopeCounter, Is.EqualTo(100));
        }

        [TestCase(ActionType.OnScopeCreated)]
        [TestCase(ActionType.OnEventSaving)]
        [TestCase(ActionType.OnEventSaved)]
        [TestCase(ActionType.OnScopeDisposed)]
        public async Task TestConfiguration_CustomAction_StressAsync(ActionType actionType)
        {
            Configuration.ResetCustomActions();
            const int size = 100;
            var tasks = new Task[size];
            int addActionCounter = 0;

            for (int i = 0; i < size; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    Core.Configuration.AddCustomAction(actionType, async (scope, ct) =>
                    {
                        Interlocked.Increment(ref addActionCounter);
                        await Task.CompletedTask;
                    });
                });
            }
            
            await Task.WhenAll(tasks);
            
            Assert.That(Core.Configuration.AuditScopeActions[actionType].Count, Is.EqualTo(100));

            int scopeCounter = 0;
            tasks = new Task[size];
            for (int i = 0; i < size; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    await using var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { DataProvider = new NullDataProvider() });
                    {
                        Interlocked.Increment(ref scopeCounter);
                        await scope.SaveAsync();
                    }
                });
            }
            
            await Task.WhenAll(tasks);

            Assert.That(addActionCounter, Is.EqualTo(size * size));
            Assert.That(scopeCounter, Is.EqualTo(100));
        }

        [TestCase(ActionType.OnScopeCreated)]
        [TestCase(ActionType.OnEventSaving)]
        [TestCase(ActionType.OnEventSaved)]
        [TestCase(ActionType.OnScopeDisposed)]
        public async Task TestConfiguration_CustomAction_InOrderAsync(ActionType actionType)
        {
            Configuration.ResetCustomActions();

            int actionCounter = 0;
            Core.Configuration.AddCustomAction(actionType, async (scope, ct) =>
            {
                Assert.That(actionCounter, Is.EqualTo(0));
                actionCounter++;
                await Task.CompletedTask;
            });
            Core.Configuration.AddCustomAction(actionType, async (scope, ct) =>
            {
                Assert.That(actionCounter, Is.EqualTo(1));
                actionCounter++;
                await Task.CompletedTask;
            });
            Core.Configuration.AddCustomAction(actionType, async (scope, ct) =>
            {
                Assert.That(actionCounter, Is.EqualTo(2));
                actionCounter++;
                await Task.CompletedTask;
            });

            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { DataProvider = new NullDataProvider() }))
            {
                await scope.SaveAsync();
            }

            Assert.That(actionCounter, Is.EqualTo(3));
        }

        [Test]
        public void TestConfiguration_ResetCustomActions_ResetAllActions()
        {
            Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, async (scope, ct) => await Task.CompletedTask);
            Core.Configuration.AddCustomAction(ActionType.OnEventSaving, async (scope, ct) => await Task.CompletedTask);
            Core.Configuration.AddCustomAction(ActionType.OnEventSaved, async (scope, ct) => await Task.CompletedTask);
            Core.Configuration.AddCustomAction(ActionType.OnScopeDisposed, async (scope, ct) => await Task.CompletedTask);

            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnScopeCreated].Count, Is.EqualTo(1));
            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnEventSaving].Count, Is.EqualTo(1));
            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnEventSaved].Count, Is.EqualTo(1));
            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnScopeDisposed].Count, Is.EqualTo(1));

            Core.Configuration.ResetCustomActions();

            Assert.That(Core.Configuration.AuditScopeActions.Count, Is.EqualTo(4));
            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnScopeCreated].Count, Is.Zero);
            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnEventSaving].Count, Is.Zero);
            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnEventSaved].Count, Is.Zero);
            Assert.That(Core.Configuration.AuditScopeActions[ActionType.OnScopeDisposed].Count, Is.Zero);
        }
    }
}
