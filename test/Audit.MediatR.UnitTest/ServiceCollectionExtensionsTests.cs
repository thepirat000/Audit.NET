using Audit.Core;
using Audit.Core.Providers;
using Audit.MediatR.ConfigurationApi;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using System.Linq;

namespace Audit.MediatR.UnitTest
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        [Test]
        public void AddMediatRAudit_WithConfigurator_Registers_Behaviors_And_Options()
        {
            var services = new ServiceCollection();

            var inMemoryProvider = new InMemoryDataProvider();

            services.AddMediatRAudit(config =>
            {
                config.IncludeRequest();
                config.IncludeResponse();
                config.EventCreationPolicy(EventCreationPolicy.InsertOnEnd);
                config.DataProvider(inMemoryProvider);
            }, ServiceLifetime.Scoped);

            Assert.Multiple(() =>
            {
                // Behaviors
                var reqBehaviorDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IPipelineBehavior<,>) &&
                    d.ImplementationType == typeof(AuditMediatRBehavior<,>));

                var streamBehaviorDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IStreamPipelineBehavior<,>) &&
                    d.ImplementationType == typeof(AuditMediatRStreamBehavior<,>));

                Assert.That(reqBehaviorDescriptor, Is.Not.Null);
                Assert.That(streamBehaviorDescriptor, Is.Not.Null);
                Assert.That(reqBehaviorDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(streamBehaviorDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));

                // Options registered as singleton
                var optionsDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(AuditMediatROptions) && d.Lifetime == ServiceLifetime.Singleton);
                Assert.That(optionsDescriptor, Is.Not.Null);
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<AuditMediatROptions>();

            Assert.Multiple(() =>
            {
                Assert.That(options, Is.Not.Null);
                Assert.That(options.IncludeRequest, Is.Not.Null);
                Assert.That(options.IncludeResponse, Is.Not.Null);
                Assert.That(options.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.InsertOnEnd));
                Assert.That(options.DataProvider, Is.Not.Null);
                Assert.That(options.DataProvider(new MediatRCallContext()), Is.SameAs(inMemoryProvider));
            });
        }

        [Test]
        public void AddMediatRAudit_WithOptions_Registers_Behaviors_And_Options()
        {
            var services = new ServiceCollection();

            var inMemoryProvider = new InMemoryDataProvider();
            var options = new AuditMediatROptions
            {
                IncludeRequest = _ => true,
                IncludeResponse = _ => true,
                EventCreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                DataProvider = _ => inMemoryProvider
            };

            services.AddMediatRAudit(options);

            Assert.Multiple(() =>
            {
                // Behaviors
                var reqBehavior = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IPipelineBehavior<,>) &&
                    d.ImplementationType == typeof(AuditMediatRBehavior<,>));
                var streamBehavior = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IStreamPipelineBehavior<,>) &&
                    d.ImplementationType == typeof(AuditMediatRStreamBehavior<,>));

                Assert.That(reqBehavior, Is.Not.Null);
                Assert.That(streamBehavior, Is.Not.Null);
                Assert.That(reqBehavior.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
                Assert.That(streamBehavior.Lifetime, Is.EqualTo(ServiceLifetime.Transient));

                // Options registered as singleton
                var optionsDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(AuditMediatROptions) && d.Lifetime == ServiceLifetime.Singleton);
                Assert.That(optionsDescriptor, Is.Not.Null);
                Assert.That(optionsDescriptor.ImplementationInstance, Is.SameAs(options));
            });

            var provider = services.BuildServiceProvider();
            var resolvedOptions = provider.GetRequiredService<AuditMediatROptions>();
            Assert.Multiple(() =>
            {
                Assert.That(resolvedOptions, Is.SameAs(options));
                Assert.That(resolvedOptions.EventCreationPolicy,
                    Is.EqualTo(EventCreationPolicy.InsertOnStartReplaceOnEnd));
                Assert.That(resolvedOptions.DataProvider(new MediatRCallContext()), Is.SameAs(inMemoryProvider));
            });
        }

        [Test]
        public void ConfigureMediatRAudit_WithConfigurator_Registers_Singleton_Options()
        {
            var services = new ServiceCollection();

            var inMemoryProvider = new InMemoryDataProvider();

            services.ConfigureMediatRAudit(config =>
            {
                config.IncludeRequest();
                config.IncludeResponse(false);
                config.EventCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);
                config.DataProvider(inMemoryProvider);
            });

            var descriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(AuditMediatROptions) && d.Lifetime == ServiceLifetime.Singleton);
            Assert.That(descriptor, Is.Not.Null);

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<AuditMediatROptions>();

            Assert.Multiple(() =>
            {
                Assert.That(options, Is.Not.Null);
                Assert.That(options.IncludeRequest, Is.Not.Null);
                Assert.That(options.IncludeResponse, Is.Not.Null);
                Assert.That(options.IncludeResponse(new MediatRCallContext()), Is.False);
                Assert.That(options.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.InsertOnStartInsertOnEnd));
                Assert.That(options.DataProvider(new MediatRCallContext()), Is.SameAs(inMemoryProvider));
            });
        }

        [Test]
        public void ConfigureMediatRAudit_WithOptions_Registers_Singleton_Options_Instance()
        {
            var services = new ServiceCollection();

            var inMemoryProvider = new InMemoryDataProvider();
            var options = new AuditMediatROptions
            {
                IncludeRequest = _ => true,
                IncludeResponse = _ => false,
                EventCreationPolicy = EventCreationPolicy.Manual,
                DataProvider = _ => inMemoryProvider
            };

            services.ConfigureMediatRAudit(options);

            var descriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(AuditMediatROptions) &&
                d.Lifetime == ServiceLifetime.Singleton);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor, Is.Not.Null);
                Assert.That(descriptor.ImplementationInstance, Is.SameAs(options));
            });

            var provider = services.BuildServiceProvider();
            var resolvedOptions = provider.GetRequiredService<AuditMediatROptions>();

            Assert.Multiple(() =>
            {
                Assert.That(resolvedOptions, Is.SameAs(options));
                Assert.That(resolvedOptions.IncludeRequest(new MediatRCallContext()), Is.True);
                Assert.That(resolvedOptions.IncludeResponse(new MediatRCallContext()), Is.False);
                Assert.That(resolvedOptions.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.Manual));
                Assert.That(resolvedOptions.DataProvider(new MediatRCallContext()), Is.SameAs(inMemoryProvider));
            });
        }
    }
}