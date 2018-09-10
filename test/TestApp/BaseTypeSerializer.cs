using System.Buffers;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;

namespace TestApp
{
    public class BaseTypeSerializer : IPartialSerializer<BaseType>
    {
        public void Serialize<TBufferWriter>(ref Writer<TBufferWriter> writer, BaseType obj) where TBufferWriter : IBufferWriter<byte>
        {
            StringCodec.WriteField(ref writer, 0, typeof(string), obj.BaseTypeString);
            StringCodec.WriteField(ref writer, 234, typeof(string), obj.AddedLaterString);
        }

        public void Deserialize(ref Reader reader, BaseType obj)
        {
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                    {
                        obj.BaseTypeString = StringCodec.ReadValue(ref reader, header);
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
                        reader.ConsumeUnknownField(header);
                        break;
                    }
                }
            }
        }
    }
}