using Audit.EntityFramework.ConfigurationApi;

using NUnit.Framework;

using System;
using System.Collections.Generic;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class EvalFilterTests
    {
        public class DummyEntity { }
        public class AnotherEntity { }

        [Test]
        public void EvalIncludeFilter_ReturnsTrue_WhenTypeIsExplicitlyIncluded_Local()
        {
            var local = new EfSettings { IncludedTypes = new HashSet<Type> { typeof(DummyEntity) } };
            var global = new EfSettings { IncludedTypes = new HashSet<Type>() };

            var result = DbContextHelper.EvalIncludeFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.True);
        }

        [Test]
        public void EvalIncludeFilter_ReturnsTrue_WhenIncludedTypesFilterMatches_Local()
        {
            var local = new EfSettings { IncludedTypesFilter = t => t == typeof(DummyEntity) };
            var global = new EfSettings();

            var result = DbContextHelper.EvalIncludeFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.True);
        }

        [Test]
        public void EvalIncludeFilter_ReturnsTrue_WhenIncludedTypesFilterMatches_Global()
        {
            var local = new EfSettings();
            var global = new EfSettings { IncludedTypesFilter = t => t == typeof(DummyEntity) };

            var result = DbContextHelper.EvalIncludeFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.True);
        }

        [Test]
        public void EvalIncludeFilter_ReturnsFalse_WhenNoMatch()
        {
            var local = new EfSettings();
            var global = new EfSettings();

            var result = DbContextHelper.EvalIncludeFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.False);
        }

        [Test]
        public void EvalIgnoreFilter_ReturnsTrue_WhenTypeIsExplicitlyIgnored_Local()
        {
            var local = new EfSettings { IgnoredTypes = new HashSet<Type> { typeof(DummyEntity) } };
            var global = new EfSettings { IgnoredTypes = new HashSet<Type>() };

            var result = DbContextHelper.EvalIgnoreFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.True);
        }

        [Test]
        public void EvalIgnoreFilter_ReturnsTrue_WhenIgnoredTypesFilterMatches_Local()
        {
            var local = new EfSettings { IgnoredTypesFilter = t => t == typeof(DummyEntity) };
            var global = new EfSettings();

            var result = DbContextHelper.EvalIgnoreFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.True);
        }

        [Test]
        public void EvalIgnoreFilter_ReturnsTrue_WhenIgnoredTypesFilterMatches_Global()
        {
            var local = new EfSettings();
            var global = new EfSettings { IgnoredTypesFilter = t => t == typeof(DummyEntity) };

            var result = DbContextHelper.EvalIgnoreFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.True);
        }

        [Test]
        public void EvalIgnoreFilter_ReturnsFalse_WhenNoMatch()
        {
            var local = new EfSettings();
            var global = new EfSettings();

            var result = DbContextHelper.EvalIgnoreFilter(typeof(DummyEntity), local, global);

            Assert.That(result, Is.False);
        }
    }
}
