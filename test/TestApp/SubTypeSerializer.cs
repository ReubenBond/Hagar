using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;

namespace TestApp
{
    public class SubTypeSerializer : IPartialSerializer<SubType>
    {
        private readonly IPartialSerializer<BaseType> baseTypeSerializer;
        private readonly IFieldCodec<string> stringCodec;
        private readonly IFieldCodec<int> intCodec;
        private readonly IFieldCodec<object> objectCodec;

        public SubTypeSerializer(IPartialSerializer<BaseType> baseTypeSerializer, IFieldCodec<string> stringCodec, IFieldCodec<int> intCodec, IFieldCodec<object> objectCodec)
        {
            this.baseTypeSerializer = baseTypeSerializer;
            this.stringCodec = stringCodec;
            this.intCodec = intCodec;
            this.objectCodec = objectCodec;
        }

        public void Serialize(Writer writer, SerializerSession session, SubType obj)
        {
            this.baseTypeSerializer.Serialize(writer, session, obj);
            writer.WriteEndBase(); // the base object is complete.
            this.stringCodec.WriteField(writer, session, 0, typeof(string), obj.String);
            this.intCodec.WriteField(writer, session, 1, typeof(int), obj.Int);
            this.objectCodec.WriteField(writer, session, 1, typeof(object), obj.Ref);
            this.intCodec.WriteField(writer, session, 1, typeof(int), obj.Int);
            this.intCodec.WriteField(writer, session, 409, typeof(int), obj.Int);
            /*writer.WriteFieldHeader(session, 1025, typeof(Guid), Guid.Empty.GetType(), WireType.Fixed128);
            writer.WriteFieldHeader(session, 1020, typeof(object), typeof(Program), WireType.Reference);*/
        }

        public void Deserialize(Reader reader, SerializerSession session, SubType obj)
        {
            uint fieldId = 0;
            this.baseTypeSerializer.Deserialize(reader, session, obj);
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        obj.String = this.stringCodec.ReadValue(reader, session, header);
                        break;
                    case 1:
                        obj.Int = this.intCodec.ReadValue(reader, session, header);
                        break;
                    case 2:
                        obj.Ref = this.objectCodec.ReadValue(reader, session, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(session, header);
                        break;
                }
            }
        }
    }
}