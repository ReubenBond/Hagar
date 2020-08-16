using Hagar.Activators;
using Hagar.Codecs;
using Hagar.Invocation;
using System;

namespace Hagar.Configuration
{
    internal class DefaultSerializerConfiguration : IConfigurationProvider<SerializerConfiguration>
    {
        public void Configure(SerializerConfiguration configuration)
        {
            var codecs = configuration.FieldCodecs;
            var serializers = configuration.Serializers;
            var activators = configuration.Activators;

            _ = codecs.Add(typeof(BoolCodec));
            _ = codecs.Add(typeof(CharCodec));
            _ = codecs.Add(typeof(ByteCodec));
            _ = codecs.Add(typeof(SByteCodec));
            _ = codecs.Add(typeof(UInt16Codec));
            _ = codecs.Add(typeof(Int16Codec));
            _ = codecs.Add(typeof(UInt32Codec));
            _ = codecs.Add(typeof(Int32Codec));
            _ = codecs.Add(typeof(UInt64Codec));
            _ = codecs.Add(typeof(Int64Codec));
            _ = codecs.Add(typeof(GuidCodec));
            _ = codecs.Add(typeof(StringCodec));

            _ = codecs.Add(typeof(DateTimeCodec));
            _ = codecs.Add(typeof(TimeSpanCodec));
            _ = codecs.Add(typeof(DateTimeOffsetCodec));

            // Add Type and RuntimeType codecs.
            // RuntimeType needs special handling because it is not accessible.
            // ReSharper disable once PossibleMistakenCallToGetType.2
            var runtimeType = typeof(Type).GetType();
            _ = codecs.Add(typeof(AbstractCodecAdapter<,,>).MakeGenericType(runtimeType, typeof(Type),
                typeof(TypeSerializerCodec)));
            _ = codecs.Add(typeof(TypeSerializerCodec));

            _ = codecs.Add(typeof(ArrayCodec<>));
            _ = codecs.Add(typeof(ByteArrayCodec));

            _ = codecs.Add(typeof(ListCodec<>));

            _ = codecs.Add(typeof(DictionaryCodec<,>));
            _ = configuration.Activators.Add(typeof(DictionaryActivator<,>));

            _ = codecs.Add(typeof(KeyValuePairCodec<,>));

            _ = codecs.Add(typeof(TupleCodec<>));
            _ = codecs.Add(typeof(TupleCodec<,>));
            _ = codecs.Add(typeof(TupleCodec<,,>));
            _ = codecs.Add(typeof(TupleCodec<,,,>));
            _ = codecs.Add(typeof(TupleCodec<,,,,>));
            _ = codecs.Add(typeof(TupleCodec<,,,,,>));
            _ = codecs.Add(typeof(TupleCodec<,,,,,,>));
            _ = codecs.Add(typeof(TupleCodec<,,,,,,,>));

            _ = codecs.Add(typeof(ValueTupleCodec));
            _ = codecs.Add(typeof(ValueTupleCodec<>));
            _ = codecs.Add(typeof(ValueTupleCodec<,>));
            _ = codecs.Add(typeof(ValueTupleCodec<,,>));
            _ = codecs.Add(typeof(ValueTupleCodec<,,,>));
            _ = codecs.Add(typeof(ValueTupleCodec<,,,,>));
            _ = codecs.Add(typeof(ValueTupleCodec<,,,,,>));
            _ = codecs.Add(typeof(ValueTupleCodec<,,,,,,>));
            _ = codecs.Add(typeof(ValueTupleCodec<,,,,,,,>));

            // Invocation
            _ = serializers.Add(typeof(PooledResponseCodec<>));
            _ = activators.Add(typeof(PooledResponseActivator<>));
        }
    }
}