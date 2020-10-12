using Hagar.Invocation;
using System.Threading.Tasks;

namespace CallLog
{
    public interface IWorkflowContext
    {
        IdSpan Id { get; }

        ValueTask ActivateAsync();

        void OnMessage(object message);

        bool OnCreateRequest(IResponseCompletionSource completion, out long sequenceNumber);

        ValueTask DeactivateAsync();
    }

}
