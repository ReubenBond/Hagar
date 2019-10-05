using System;
using Hagar.Activators;
using Hagar.Codecs;
using Hagar.GeneratedCodeHelpers;
using Hagar.Serializers;

namespace Hagar.Invocation
{
    internal sealed class PooledResponseActivator<TResult> : IActivator<PooledResponse<TResult>>
    {
        public PooledResponse<TResult> Create() => ResponsePool.Get<TResult>();
    }

    internal sealed class PooledResponseCodec<TResult> : IPartialSerializer<PooledResponse<TResult>>
    {
        private static readonly Type ExceptionType = typeof(Exception);
        private static readonly Type ResultType = typeof(TResult);
        private readonly IFieldCodec<Exception> exceptionCodec;
        private readonly IFieldCodec<TResult> resultCodec;

        public PooledResponseCodec(IFieldCodec<Exception> exceptionCodec, IFieldCodec<TResult> resultCodec)
        {
            this.exceptionCodec = HagarGeneratedCodeHelper.UnwrapService(this, exceptionCodec);
            this.resultCodec = HagarGeneratedCodeHelper.UnwrapService(this, resultCodec);
        }

        public void Serialize<TBufferWriter>(ref Buffers.Writer<TBufferWriter> writer, PooledResponse<TResult> instance)
            where TBufferWriter : System.Buffers.IBufferWriter<byte>
        {
            if (instance.Exception is null)
            {
                this.resultCodec.WriteField(ref writer, 0U, ResultType, instance.TypedResult);
            }
            else
            {
                this.exceptionCodec.WriteField(ref writer, 1U, ExceptionType, instance.Exception);
            }
        }

        public void Deserialize(ref Buffers.Reader reader, PooledResponse<TResult> instance)
        {
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader();
                if (header.IsEndBaseOrEndObject)
                    break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 1U:
                        instance.Exception = this.exceptionCodec.ReadValue(ref reader, header);
                        break;
                    case 0U:
                        instance.TypedResult = this.resultCodec.ReadValue(ref reader, header);
                        break;
                    default:
                        reader.ConsumeUnknownField(header);
                        break;
                }
            }
        }
    }
}
