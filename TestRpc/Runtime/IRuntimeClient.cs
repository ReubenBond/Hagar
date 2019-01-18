using System.Threading.Tasks;
using Hagar.Invocation;

namespace TestRpc.Runtime
{
    public interface IRuntimeClient
    {
        ValueTask SendRequest<TInvokable>(ActivationId activationId, TInvokable request) where TInvokable : IInvokable;
        ValueTask SendResponse(ActivationId activationId, object response);
    }
}