namespace Hagar.ObjectModel
{
    /*
    public static class TokenStreamParser
    {
        public static IEnumerable<string> Parse(Reader reader, SerializerSession session)
        {
            var objectDepth = 0;
            var fieldIdStack = new Stack<uint>();
            var fieldId = 0U;
            while (true)
            {
                var field = reader.ReadFieldHeader(session);
                yield return field.ToString();

                if (field.IsEndObject)
                {
                    --objectDepth;
                    fieldId = fieldIdStack.Pop();
                }
                else if (field.IsEndBaseFields)
                {
                    fieldId = 0;
                }
                else
                {
                    fieldId += field.FieldIdDelta;

                    switch (field.WireType)
                    {
                        case WireType.VarInt:
                            yield return $"[VarInt: {reader.ReadVarUInt64():X}]";
                            break;
                        case WireType.TagDelimited:
                            ++objectDepth;
                            fieldIdStack.Push(fieldId);
                            break;
                        case WireType.LengthPrefixed:
                            var length = reader.ReadVarUInt32();
                            var bytes = reader.ReadBytes(length);
                            yield return $"[Length: {length}, Bytes: {bytes.Take(80).Select(b => b.ToString("X2"))}{(length > 80 ? "..." : string.Empty)}]";
                            break;
                        case WireType.Fixed32:
                            yield return $"[Fixed32: {reader.ReadUInt32():X8}]";
                            break;
                        case WireType.Fixed64:
                            yield return $"[Fixed64: {reader.ReadUInt64():X16}]";
                            break;
                        case WireType.Fixed128:
                            yield return $"[Fixed128: {reader.ReadUInt64():X16}{reader.ReadUInt64():X16}]";
                            break;
                        case WireType.Reference:
                            yield return $"[Reference: {reader.ReadVarUInt32()}]";
                            break;
                        case WireType.Extended:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (objectDepth == 0) yield break;
            }
        }
    }*/
}
