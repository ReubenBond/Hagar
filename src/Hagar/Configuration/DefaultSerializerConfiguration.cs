using System;
using Hagar.Activators;
using Hagar.Codecs;
using Hagar.Invocation;

namespace Hagar.Configuration
{
    internal class DefaultSerializerConfiguration : IConfigurationProvider<SerializerConfiguration>
    {
        public void Configure(SerializerConfiguration configuration)
        {
            var codecs = configuration.FieldCodecs;
            var serializers = configuration.Serializers;
            codecs.Add(typeof(BoolCodec));
            codecs.Add(typeof(CharCodec));
            codecs.Add(typeof(ByteCodec));
            codecs.Add(typeof(SByteCodec));
            codecs.Add(typeof(UInt16Codec));
            codecs.Add(typeof(Int16Codec));
            codecs.Add(typeof(UInt32Codec));
            codecs.Add(typeof(Int32Codec));
            codecs.Add(typeof(UInt64Codec));
            codecs.Add(typeof(Int64Codec));
            codecs.Add(typeof(GuidCodec));
            codecs.Add(typeof(StringCodec));

            codecs.Add(typeof(DateTimeCodec));
            codecs.Add(typeof(TimeSpanCodec));
            codecs.Add(typeof(DateTimeOffsetCodec));

            // Add Type and RuntimeType codecs.
            // RuntimeType needs special handling because it is not accessible.
            // ReSharper disable once PossibleMistakenCallToGetType.2
            var runtimeType = typeof(Type).GetType();
            codecs.Add(typeof(AbstractCodecAdapter<,,>).MakeGenericType(runtimeType, typeof(Type),
                typeof(TypeSerializerCodec)));
            codecs.Add(typeof(TypeSerializerCodec));

            codecs.Add(typeof(ArrayCodec<>));
            codecs.Add(typeof(ByteArrayCodec));

            codecs.Add(typeof(ListCodec<>));

            codecs.Add(typeof(DictionaryCodec<,>));
            configuration.Activators.Add(typeof(DictionaryActivator<,>));

            codecs.Add(typeof(KeyValuePairCodec<,>));

            codecs.Add(typeof(TupleCodec<>));
            codecs.Add(typeof(TupleCodec<,>));
            codecs.Add(typeof(TupleCodec<,,>));
            codecs.Add(typeof(TupleCodec<,,,>));
            codecs.Add(typeof(TupleCodec<,,,,>));
            codecs.Add(typeof(TupleCodec<,,,,,>));
            codecs.Add(typeof(TupleCodec<,,,,,,>));
            codecs.Add(typeof(TupleCodec<,,,,,,,>));

            codecs.Add(typeof(ValueTupleCodec));
            codecs.Add(typeof(ValueTupleCodec<>));
            codecs.Add(typeof(ValueTupleCodec<,>));
            codecs.Add(typeof(ValueTupleCodec<,,>));
            codecs.Add(typeof(ValueTupleCodec<,,,>));
            codecs.Add(typeof(ValueTupleCodec<,,,,>));
            codecs.Add(typeof(ValueTupleCodec<,,,,,>));
            codecs.Add(typeof(ValueTupleCodec<,,,,,,>));
            codecs.Add(typeof(ValueTupleCodec<,,,,,,,>));
            
            // Invocation
            serializers.Add(typeof(PooledResponseCodec<>));
        }
    }
}
