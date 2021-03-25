using Hagar;
using Hagar.Invocation;
using System.Threading.Tasks;

namespace CallLog
{
    [GenerateMethodSerializers(typeof(WorkflowProxyBase))]
    public interface IWorkflow
    {
    }

}
