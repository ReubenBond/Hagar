using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;
using System;

namespace Hagar.Invocation
{
    internal sealed class PooledResponseCodec<TResult> : IPartialSerializer<PooledResponse<TResult>>
    {
        private static readonly Type ExceptionType = typeof(Exception);
        private static readonly Type ResultType = typeof(TResult);
        private readonly IFieldCodec<Exception> _exceptionCodec;
        private readonly IFieldCodec<TResult> _resultCodec;

        public PooledResponseCodec(IFieldCodec<Exception> exceptionCodec, IFieldCodec<TResult> resultCodec)
        {
            _exceptionCodec = HagarGeneratedCodeHelper.UnwrapService(this, exceptionCodec);
            _resultCodec = HagarGeneratedCodeHelper.UnwrapService(this, resultCodec);
        }

        public void Serialize<TBufferWriter>(ref Buffers.Writer<TBufferWriter> writer, PooledResponse<TResult> instance)
            where TBufferWriter : System.Buffers.IBufferWriter<byte>
        {
            if (instance.Exception is null)
            {
                _resultCodec.WriteField(ref writer, 0U, ResultType, instance.TypedResult);
            }
            else
            {
                _exceptionCodec.WriteField(ref writer, 1U, ExceptionType, instance.Exception);
            }
        }

        public void Deserialize<TInput>(ref Buffers.Reader<TInput> reader, PooledResponse<TResult> instance)
        {
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                {
                    break;
                }

                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1U:
                        instance.Exception = _exceptionCodec.ReadValue(ref reader, header);
                        break;
                    case 0U:
                        instance.TypedResult = _resultCodec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }
        }
    }
}