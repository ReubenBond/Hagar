using System.Threading.Tasks;
using Hagar.Invocation;

namespace TestRpc.Runtime
{
    public interface IRuntimeClient
    {
        void SendRequest(ActivationId activationId, IResponseCompletionSource completion, IInvokable request);
        void SendResponse(int requestMessageId, ActivationId requestMessageSource, Response response);
    }
}