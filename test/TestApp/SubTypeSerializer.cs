using System.Buffers;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;

namespace TestApp
{
    public class SubTypeSerializer : IPartialSerializer<SubType>
    {
        private readonly IPartialSerializer<SubType> subTypeSerializer;
        private readonly IPartialSerializer<BaseType> baseTypeSerializer;
        private readonly IFieldCodec<string> stringCodec;
        private readonly IFieldCodec<int> intCodec;
        private readonly IFieldCodec<object> objectCodec;

        public SubTypeSerializer(IPartialSerializer<BaseType> baseTypeSerializer, IPartialSerializer<SubType> subTypeSerializer, IFieldCodec<string> stringCodec, IFieldCodec<int> intCodec, IFieldCodec<object> objectCodec)
        {
            this.subTypeSerializer = HagarGeneratedCodeHelper.UnwrapService(this, subTypeSerializer);
            this.baseTypeSerializer = HagarGeneratedCodeHelper.UnwrapService(this, baseTypeSerializer);
            this.stringCodec = HagarGeneratedCodeHelper.UnwrapService(this, stringCodec);
            this.intCodec = HagarGeneratedCodeHelper.UnwrapService(this, intCodec);
            this.objectCodec = HagarGeneratedCodeHelper.UnwrapService(this, objectCodec);
        }

        public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, SubType obj) where TBufferWriter : IBufferWriter<byte>
        {
            this.baseTypeSerializer.Serialize(ref writer, obj);
            writer.WriteEndBase(); // the base object is complete.
            this.stringCodec.WriteField(ref writer, 0, typeof(string), obj.String);
            this.intCodec.WriteField(ref writer, 1, typeof(int), obj.Int);
            this.objectCodec.WriteField(ref writer, 1, typeof(object), obj.Ref);
            this.intCodec.WriteField(ref writer, 1, typeof(int), obj.Int);
            this.intCodec.WriteField(ref writer, 409, typeof(int), obj.Int);
            /*writer.WriteFieldHeader(session, 1025, typeof(Guid), Guid.Empty.GetType(), WireType.Fixed128);
            writer.WriteFieldHeader(session, 1020, typeof(object), typeof(Program), WireType.Reference);*/
        }

        public void Deserialize(ref Reader reader, SubType obj)
        {
            uint fieldId = 0;
            this.baseTypeSerializer.Deserialize(ref reader, obj);
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        obj.String = this.stringCodec.ReadValue(ref reader, header);
                        break;
                    case 1:
                        obj.Int = this.intCodec.ReadValue(ref reader, header);
                        break;
                    case 2:
                        obj.Ref = this.objectCodec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }
        }
    }
}