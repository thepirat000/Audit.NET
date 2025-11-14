#if NET9_0_OR_GREATER
using Audit.Core;
using Audit.EntityFramework.ConfigurationApi;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

using Moq;

using NUnit.Framework;

using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable EF1001

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class AuditEntityMappingTests
    {
        private AuditEvent _auditEvent;
        private EventEntry _eventEntry;

        [SetUp]
        public void Setup()
        {
            _auditEvent = new AuditEvent { EventType = "TestEvent" };
            _eventEntry = new EventEntry
            {
                Table = "TestTable",
                EntityType = typeof(SourceEntity),
                Entity = new SourceEntity()
            };
        }

        [Test]
        public void Map_Generic_ShouldAddMapping()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>();
            Assert.IsTrue(mapping._mapping.ContainsKey(typeof(SourceEntity)));
            Assert.That(mapping._mapping[typeof(SourceEntity)].TargetTypeMapper(null), Is.EqualTo(typeof(AuditEntity)));
        }

        [Test]
        public async Task Map_Action_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity, AuditEntity>((ev, ent, audit) =>
            {
                called = true;
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Map_ActionOverload_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity, AuditEntity>((ev, ent, audit) =>
            {
                called = true;
                return true;
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Map_AsyncAction_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity, AuditEntity>(async (ev, ent, audit) =>
            {
                called = true;
                await Task.CompletedTask;
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Map_FuncBool_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>((ev, ent, audit) => false);

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task Map_FuncTaskBool_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>(async (ev, ent, audit) => await Task.FromResult(false));

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task Map_ActionTask_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>((source, audit) => {});

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            _eventEntry.Entry = GetEntityEntry();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(result);
        }

        [Test]
        public async Task Map_AsyncActionWithSource_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity, AuditEntity>(async (src, audit) =>
            {
                called = true;
                await Task.CompletedTask;
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            _eventEntry.Entity = new SourceEntity();
            _eventEntry.Entry = GetEntityEntry();
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }
        
        [Test]
        public async Task Map_FuncBoolWithSource_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>((src, audit) => false);

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            _eventEntry.Entity = new SourceEntity();
            _eventEntry.Entry = GetEntityEntry();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task Map_FuncTaskBoolWithSource_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>(async (src, audit) => await Task.FromResult(false));

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            _eventEntry.Entity = new SourceEntity();
            _eventEntry.Entry = GetEntityEntry();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public void Map_MapperFunc_ShouldSetTargetTypeMapper()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity>(e => typeof(AuditEntity));
            Assert.IsNotNull(mapping._mapping[typeof(SourceEntity)].TargetTypeMapper);
        }

        [Test]
        public async Task Map_MapperFuncWithAction_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity>(e => typeof(AuditEntity), (ev, ent, obj) =>
            {
                called = true;
                return true;
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Map_MapperFuncWithAsyncAction_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity>(e => typeof(AuditEntity), async (ev, ent, obj) =>
            {
                called = true;
                return await Task.FromResult(true);
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Map_MapperFuncWithActionObject_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity>(e => typeof(AuditEntity), (ev, ent, obj) => called = true);

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Map_MapperFuncWithAsyncActionObject_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity>(e => typeof(AuditEntity), async (ev, ent, obj) =>
            {
                called = true;
                await Task.CompletedTask;
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Map_MapperFuncWithSyncActionObject_ShouldInvokeAction()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.Map<SourceEntity>(e => typeof(AuditEntity), (ev, ent, obj) =>
            {
                called = true;
            });

            var action = mapping._mapping[typeof(SourceEntity)].Action;
            await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public void MapExplicit_Action_ShouldAddExplicitMapping()
        {
            var mapping = new AuditEntityMapping();
            mapping.MapExplicit<AuditEntity>(e => true, (entry, audit) => { });
            Assert.AreEqual(1, mapping._explicitMapping.Count);
        }

        [Test]
        public async Task MapExplicit_AsyncAction_ShouldAddExplicitMappingAndInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.MapExplicit<AuditEntity>(e => true, async (entry, audit) =>
            {
                called = true;
                await Task.CompletedTask;
            });

            var kvp = mapping._explicitMapping[0];
            await kvp.Value.Action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public void MapTable_Action_ShouldAddExplicitMapping()
        {
            var mapping = new AuditEntityMapping();
            mapping.MapTable<AuditEntity>("TestTable", (entry, audit) => { });
            Assert.AreEqual(1, mapping._explicitMapping.Count);
            Assert.IsTrue(mapping._explicitMapping[0].Key(_eventEntry));
        }

        [Test]
        public async Task MapTable_AsyncAction_ShouldAddExplicitMappingAndInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.MapTable<AuditEntity>("TestTable", async (entry, audit) =>
            {
                called = true;
                await Task.CompletedTask;
            });

            var kvp = mapping._explicitMapping[0];
            await kvp.Value.Action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task AuditEntityAction_Func_ShouldInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.AuditEntityAction((ev, ent, obj) => called = true);

            Assert.IsNotNull(mapping._commonAction);
            await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task AuditEntityAction_Action_ShouldInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.AuditEntityAction((ev, ent, obj) =>
            {
                called = true;
            });

            Assert.IsNotNull(mapping._commonAction);
            await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task AuditEntityAction_FuncGeneric_ShouldInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.AuditEntityAction<AuditEntity>((ev, ent, obj) =>
            {
                called = true;
            });

            Assert.IsNotNull(mapping._commonAction);
            await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task AuditEntityAction_FuncBool_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.AuditEntityAction((ev, ent, obj) => false);

            var result = await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AuditEntityAction_GenericAction_ShouldInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.AuditEntityAction<AuditEntity>((ev, ent, obj) => called = true);

            await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task AuditEntityAction_GenericFuncBool_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.AuditEntityAction<AuditEntity>((ev, ent, obj) => false);

            var result = await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AuditEntityAction_AsyncFuncBool_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.AuditEntityAction(async (ev, ent, obj) => await Task.FromResult(false));

            var result = await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AuditEntityAction_GenericAsyncFuncBool_ShouldReturnBool()
        {
            var mapping = new AuditEntityMapping();
            mapping.AuditEntityAction<AuditEntity>(async (ev, ent, obj) => await Task.FromResult(false));

            var result = await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AuditEntityAction_AsyncAction_ShouldInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.AuditEntityAction(async (ev, ent, obj) =>
            {
                called = true;
                await Task.CompletedTask;
            });

            await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public async Task AuditEntityAction_GenericAsyncAction_ShouldInvoke()
        {
            var mapping = new AuditEntityMapping();
            bool called = false;
            mapping.AuditEntityAction<AuditEntity>(async (ev, ent, obj) =>
            {
                called = true;
                await Task.CompletedTask;
            });

            await mapping._commonAction(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(called);
        }

        [Test]
        public void GetMapper_ShouldReturnTargetTypeMapper()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity>(e => typeof(AuditEntity));
            var mapper = mapping.GetMapper();
            var type = mapper(typeof(SourceEntity), _eventEntry);
            Assert.AreEqual(typeof(AuditEntity), type);
        }

        [Test]
        public void GetExplicitMapper_ShouldReturnTargetTypeMapper()
        {
            var mapping = new AuditEntityMapping();
            mapping.MapExplicit<AuditEntity>(e => true, null);
            mapping._explicitMapping[0].Value.TargetTypeMapper = e => typeof(AuditEntity);
            var explicitMapper = mapping.GetExplicitMapper();
            var type = explicitMapper(_eventEntry);
            Assert.AreEqual(typeof(AuditEntity), type);
        }

        [Test]
        public async Task GetAction_ShouldInvokeExplicitAndCommonActions()
        {
            var mapping = new AuditEntityMapping();
            bool explicitCalled = false, commonCalled = false;
            mapping.MapExplicit<AuditEntity>(e => true, (entry, audit) => explicitCalled = true);
            mapping.AuditEntityAction((ev, ent, obj) =>
            {
                commonCalled = true;
                return true;
            });

            var action = mapping.GetAction();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(explicitCalled);
            Assert.IsTrue(commonCalled);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetAction_ShouldInvokeMappingAndCommonActions()
        {
            var mapping = new AuditEntityMapping();
            bool mappingCalled = false, commonCalled = false;
            mapping.Map<SourceEntity, AuditEntity>((ev, ent, audit) =>
            {
                mappingCalled = true;
                return true;
            });
            mapping.AuditEntityAction((ev, ent, obj) =>
            {
                commonCalled = true;
                return true;
            });

            var action = mapping.GetAction();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(mappingCalled);
            Assert.IsTrue(commonCalled);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetAction_ShouldReturnTrueIfNoCommonAction()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>((ev, ent, audit) => true);

            var action = mapping.GetAction();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetAction_ShouldReturnFalseIfMappingReturnsFalse()
        {
            var mapping = new AuditEntityMapping();
            mapping.Map<SourceEntity, AuditEntity>((ev, ent, audit) => false);

            var action = mapping.GetAction();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetAction_ShouldReturnFalseIfExplicitReturnsFalse()
        {
            var mapping = new AuditEntityMapping();
            mapping.MapExplicit<AuditEntity>(e => true, (entry, audit) => { return; });
            mapping._explicitMapping[0].Value.Action = (ev, ent, obj) => Task.FromResult(false);

            var action = mapping.GetAction();
            var result = await action(_auditEvent, _eventEntry, new AuditEntity());
            Assert.IsFalse(result);
        }
        
        private static EntityEntry GetEntityEntry()
        {
            var entity = new SourceEntity();
            var model = new RuntimeModel();  // EF Core metadata model

#if EF_CORE_10_OR_GREATER
            var entityType = new RuntimeEntityType("test",
                typeof(SourceEntity), false, model, null, ChangeTrackingStrategy.ChangedNotifications, null, true, null, 1, 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0);
#else
            var entityType = new RuntimeEntityType("test",
                typeof(SourceEntity), false, model, null, null, ChangeTrackingStrategy.ChangedNotifications, null, true, null, 1, 1, 0, 1, 1, 0, 1, 0, 0, 1, 0);
#endif

            var stateManagerMock = new Mock<IStateManager>();

            return new EntityEntry(new InternalEntityEntry(
                stateManagerMock.Object,
                entityType,
                entity
            ));
        }

        private class SourceEntity
        {
        }

        private class AuditEntity
        {
        }

        private class AnotherAuditEntity
        {
        }
    }
}
#endif
