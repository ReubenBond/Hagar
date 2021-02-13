using Hagar.Invocation;

namespace CallLog
{
    internal struct RequestState
    {
        public Response Response { get; set; }
        public IResponseCompletionSource Completion { get; set; }
        public long Dependent { get; set; }
        public long DependsOn { get; set; }
    }
}
