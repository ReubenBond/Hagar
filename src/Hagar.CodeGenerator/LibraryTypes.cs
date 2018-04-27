using Microsoft.CodeAnalysis;

namespace Hagar.CodeGenerator
{
    internal class LibraryTypes
    {
        public static LibraryTypes FromCompilation(Compilation compilation)
        {
            return new LibraryTypes
            {
                PartialSerializer = compilation.GetTypeByMetadataName("Hagar.Serializers.IPartialSerializer`1"),
                FieldCodec = compilation.GetTypeByMetadataName("Hagar.Codecs.IFieldCodec`1"),
                TypedCodecProvider = compilation.GetTypeByMetadataName("Hagar.Serializers.ITypedCodecProvider"),
                Writer = compilation.GetTypeByMetadataName("Hagar.Buffers.Writer"),
                Reader = compilation.GetTypeByMetadataName("Hagar.Buffers.Reader"),
                SerializerSession = compilation.GetTypeByMetadataName("Hagar.Session.SerializerSession"),
                Object = compilation.GetSpecialType(SpecialType.System_Object),
                Type = compilation.GetTypeByMetadataName("System.Type"),
                SerializerConfiguration = compilation.GetTypeByMetadataName("Hagar.Configuration.SerializerConfiguration"),
                ConfigurationProvider = compilation.GetTypeByMetadataName("Hagar.Configuration.IConfigurationProvider`1")
            };
        }

        public INamedTypeSymbol TypedCodecProvider { get; private set; }

        public INamedTypeSymbol ConfigurationProvider { get; private set; }

        public INamedTypeSymbol SerializerConfiguration { get; private set; }

        public INamedTypeSymbol Type { get; private set; }

        public INamedTypeSymbol Object { get; private set; }

        public INamedTypeSymbol SerializerSession { get; private set; }

        public INamedTypeSymbol Reader { get; private set; }

        public INamedTypeSymbol Writer { get; private set; }

        public INamedTypeSymbol FieldCodec { get; private set; }

        public INamedTypeSymbol PartialSerializer { get; private set; }
    }
}