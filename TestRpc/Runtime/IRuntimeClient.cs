using System.Threading.Tasks;
using Hagar.Invocation;

namespace TestRpc.Runtime
{
    public interface IRuntimeClient
    {
        void SendRequest(GrainId grainId, IResponseCompletionSource completion, IInvokable request);
        void SendResponse(int requestMessageId, GrainId requestMessageSource, Response response);
    }
}