using Hagar.Configuration;
using Hagar.Session;
using Hagar.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hagar.UnitTests
{
    public class PolymorphismTests
    {
        private readonly ServiceProvider _serviceProvider;

        public PolymorphismTests()
        {
            _serviceProvider = new ServiceCollection().AddHagar(hagar =>
                {
                    hagar.AddAssembly(typeof(PolymorphismTests).Assembly);
                })
                .AddSingleton<IConfigurationProvider<TypeConfiguration>, TypeConfigurationProvider>()
                .BuildServiceProvider();
        }

        private class TypeConfigurationProvider : IConfigurationProvider<TypeConfiguration>
        {
            public void Configure(TypeConfiguration configuration)
            {
                configuration.WellKnownTypes[1000] = typeof(SomeBaseClass);
                configuration.WellKnownTypes[1001] = typeof(SomeSubClass);
                configuration.WellKnownTypes[1002] = typeof(OtherSubClass);
                configuration.WellKnownTypes[1003] = typeof(SomeSubClassChild);
            }
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer_Polymorphic()
        {
            var original = new SomeSubClass
            { SbcString = "Shaggy", SbcInteger = 13, SscString = "Zoinks!", SscInteger = -1 };

            var getSubClassSerializerResult = RoundTripToExpectedType<SomeSubClass, SomeSubClass>(original);
            Assert.Equal(original.SscString, getSubClassSerializerResult.SscString);
            Assert.Equal(original.SscInteger, getSubClassSerializerResult.SscInteger);

            var getBaseClassSerializerResult = RoundTripToExpectedType<SomeBaseClass, SomeSubClass>(original);
            Assert.Equal(original.SscString, getBaseClassSerializerResult.SscString);
            Assert.Equal(original.SscInteger, getBaseClassSerializerResult.SscInteger);
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer_PolymorphicMultiHierarchy()
        {
            var someSubClass = new SomeSubClass
            { SbcString = "Shaggy", SbcInteger = 13, SscString = "Zoinks!", SscInteger = -1 };

            var otherSubClass = new OtherSubClass
            { SbcString = "sbcs", SbcInteger = 2000, OtherSubClassString = "oscs", OtherSubClassInt = 1000 };

            var someSubClassChild = new SomeSubClassChild
            { SbcString = "a", SbcInteger = 0, SscString = "Zoinks!", SscInteger = -1, SomeSubClassChildString = "string!", SomeSubClassChildInt = 5858 };

            var someSubClassResult = RoundTripToExpectedType<SomeBaseClass, SomeSubClass>(someSubClass);
            Assert.Equal(someSubClass.SscString, someSubClassResult.SscString);
            Assert.Equal(someSubClass.SscInteger, someSubClassResult.SscInteger);
            Assert.Equal(someSubClass.SbcString, someSubClassResult.SbcString);
            Assert.Equal(someSubClass.SbcInteger, someSubClassResult.SbcInteger);

            var otherSubClassResult = RoundTripToExpectedType<SomeBaseClass, OtherSubClass>(otherSubClass);
            Assert.Equal(otherSubClass.OtherSubClassString, otherSubClassResult.OtherSubClassString);
            Assert.Equal(otherSubClass.OtherSubClassInt, otherSubClassResult.OtherSubClassInt);
            Assert.Equal(otherSubClass.SbcString, otherSubClassResult.SbcString);
            Assert.Equal(otherSubClass.SbcInteger, otherSubClassResult.SbcInteger);

            var someSubClassChildResult = RoundTripToExpectedType<SomeBaseClass, SomeSubClassChild>(someSubClassChild);
            Assert.Equal(someSubClassChild.SomeSubClassChildString, someSubClassChildResult.SomeSubClassChildString);
            Assert.Equal(someSubClassChild.SomeSubClassChildInt, someSubClassChildResult.SomeSubClassChildInt);
            Assert.Equal(someSubClassChild.SscString, someSubClassChildResult.SscString);
            Assert.Equal(someSubClassChild.SscInteger, someSubClassChildResult.SscInteger);
            Assert.Equal(someSubClassChild.SbcString, someSubClassChildResult.SbcString);
            Assert.Equal(someSubClassChild.SbcInteger, someSubClassChildResult.SbcInteger);
        }

        private TActual RoundTripToExpectedType<TBase, TActual>(TActual original)
            where TActual : TBase
        {
            var serializer = _serviceProvider.GetService<Serializer<TBase>>();
            var array = serializer.SerializeToArray(original);

            string formatted;
            {
                using var session = _serviceProvider.GetRequiredService<SerializerSessionPool>().GetSession();
                formatted = BitStreamFormatter.Format(array, session);
            }

            return (TActual)serializer.Deserialize(array);
        }

        [Id(1000)]
        [GenerateSerializer]
        public class SomeBaseClass
        {
            [Id(0)]
            public string SbcString { get; set; }

            [Id(1)]
            public int SbcInteger { get; set; }
        }

        [Id(1001)]
        [GenerateSerializer]
        public class SomeSubClass : SomeBaseClass
        {
            [Id(0)]
            public int SscInteger { get; set; }

            [Id(1)]
            public string SscString { get; set; }
        }

        [Id(1002)]
        [GenerateSerializer]
        public class OtherSubClass : SomeBaseClass
        {
            [Id(0)]
            public int OtherSubClassInt { get; set; }

            [Id(1)]
            public string OtherSubClassString { get; set; }
        }

        [Id(1003)]
        [GenerateSerializer]
        public class SomeSubClassChild : SomeSubClass
        {
            [Id(0)]
            public int SomeSubClassChildInt { get; set; }

            [Id(1)]
            public string SomeSubClassChildString { get; set; }
        }
    }
}