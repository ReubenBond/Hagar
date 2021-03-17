using Hagar.Cloning;
using Hagar.Serializers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Hagar.TestKit
{
    [Trait("Category", "BVT")]
    [ExcludeFromCodeCoverage]
    public abstract class CopierTester<TValue, TCopier> where TCopier : class, IDeepCopier<TValue>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CodecProvider _codecProvider;

        protected CopierTester()
        {
            var services = new ServiceCollection();
            _ = services.AddHagar(hagar => hagar.Configure(config => config.Copiers.Add(typeof(TCopier))));

            if (!typeof(TCopier).IsAbstract && !typeof(TCopier).IsInterface)
            {
                _ = services.AddSingleton<TCopier>();
            }

            _ = services.AddHagar(Configure);

            _serviceProvider = services.BuildServiceProvider();
            _codecProvider = _serviceProvider.GetRequiredService<CodecProvider>();
        }

        protected IServiceProvider ServiceProvider => _serviceProvider;

        protected virtual bool IsImmutable => false;

        protected virtual void Configure(IHagarBuilder builder)
        {
        }

        protected virtual TCopier CreateCopier() => _serviceProvider.GetRequiredService<TCopier>();
        protected abstract TValue CreateValue();
        protected abstract TValue[] TestValues { get; }
        protected virtual bool Equals(TValue left, TValue right) => EqualityComparer<TValue>.Default.Equals(left, right);

        protected virtual Action<Action<TValue>> ValueProvider { get; }
        
        [Fact]
        public void CopiedValuesAreEqual()
        {
            var copier = CreateCopier();
            foreach (var original in TestValues)
            {
                Test(original);
            }

            if (ValueProvider is { } valueProvider)
            {
                valueProvider(Test);
            }

            void Test(TValue original)
            {
                var output = copier.DeepCopy(original, new CopyContext(_codecProvider, _ => { }));
                Assert.True(Equals(original, output), $"Copy value \"{output}\" must equal original value \"{original}\"");
            }
        }

        [Fact]
        public void ReferencesAreAddedToCopyContext()
        {
            if (typeof(TValue).IsValueType)
            {
                return;
            }

            var value = CreateValue();
            var array = new TValue[] { value, value };
            var arrayCopier = _serviceProvider.GetRequiredService<DeepCopier<TValue[]>>();
            var arrayCopy = arrayCopier.Copy(array);
            Assert.Same(arrayCopy[0], arrayCopy[1]);

            if (!IsImmutable)
            {
                Assert.NotSame(value, arrayCopy[0]);
            }
        }
    }
}