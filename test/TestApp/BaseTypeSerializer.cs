using Hagar;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;

namespace TestApp
{
    public class BaseTypeSerializer : IPartialSerializer<BaseType>
    {
        private readonly IFieldCodec<string> stringCodec;

        public BaseTypeSerializer(IFieldCodec<string> stringCodec)
        {
            this.stringCodec = stringCodec;
        }

        public void Serialize(Writer writer, SerializerSession session, BaseType obj)
        {
            this.stringCodec.WriteField(writer, session, 0, typeof(string), obj.BaseTypeString);
            this.stringCodec.WriteField(writer, session, 234, typeof(string), obj.AddedLaterString);
        }

        public void Deserialize(Reader reader, SerializerSession session, BaseType obj)
        {
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(session);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                    {
                        obj.BaseTypeString = this.stringCodec.ReadValue(reader, session, header);
                        /*var type = header.FieldType ?? typeof(string);
                            Console.WriteLine(
                            $"\tReading field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");*/
                        break;
                    }
                    default:
                    {
                        /*var type = header.FieldType;
                        Console.WriteLine(
                            $"\tReading UNKNOWN field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");*/
                        reader.ConsumeUnknownField(session, header);
                        break;
                    }
                }
            }
        }
    }
}