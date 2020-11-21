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
            _serviceProvider = new ServiceCollection().AddHagar(hagar => hagar.AddAssembly(typeof(PolymorphismTests).Assembly)).BuildServiceProvider();
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer_Polymorphic()
        {
            var original = new SomeSubClass
            { SbcString = "Shaggy", SbcInteger = 13, SscString = "Zoinks!", SscInteger = -1 };

            var resultAsBase = RoundTripToExpectedType<SomeBaseClass, SomeSubClass>(original);

            var castAsSubclass = resultAsBase;
            Assert.NotNull(resultAsBase);
            Assert.NotNull(castAsSubclass);
            Assert.Equal(original.SscString, castAsSubclass.SscString);
            Assert.Equal(original.SscInteger, castAsSubclass.SscInteger);

            var resultAsSub = RoundTripToExpectedType<SomeBaseClass, SomeSubClass>(original);
            Assert.Equal(original.SscString, resultAsSub.SscString);
            Assert.Equal(original.SscInteger, resultAsSub.SscInteger);
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

        [GenerateSerializer]
        public class SomeBaseClass
        {
            [Id(0)]
            public string SbcString { get; set; }

            [Id(1)]
            public int SbcInteger { get; set; }
        }

        [GenerateSerializer]
        public class SomeSubClass : SomeBaseClass
        {
            [Id(0)]
            public int SscInteger { get; set; }

            [Id(1)]
            public string SscString { get; set; }
        }
    }
}