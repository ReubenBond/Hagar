using System;
using System.Runtime.CompilerServices;
using System.Text;
using Hagar.Buffers;
using Hagar.Serializers;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class StringCodec : TypedCodecBase<string, StringCodec>, IFieldCodec<string>
    {
        string IFieldCodec<string>.ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            return ReadValue(ref reader, session, field);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<string>(ref reader, session, field);
            if (field.WireType != WireType.LengthPrefixed) ThrowUnsupportedWireTypeException(field);
            var length = reader.ReadVarUInt32();

            string result;
#if NETCOREAPP2_1
            if (reader.TryReadBytes((int) length, out var span))
            {
                result = Encoding.UTF8.GetString(span);
            }
            else      
#endif
            {
                var bytes = reader.ReadBytes(length);
                result = Encoding.UTF8.GetString(bytes);
            }

            ReferenceCodec.RecordObject(session, result);
            return result;
        }

        void IFieldCodec<string>.WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, string value)
        {
            WriteField(ref writer, session, fieldIdDelta, expectedType, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, string value)
        {
            if (ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value)) return;

            writer.WriteFieldHeader(session, fieldIdDelta, expectedType, typeof(string), WireType.LengthPrefixed);
            // TODO: use Span<byte>
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.WriteVarInt((uint) bytes.Length);
            writer.Write(bytes);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for string fields. {field}");
    }
}