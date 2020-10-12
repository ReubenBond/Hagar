using Hagar;
using Hagar.Invocation;
using System;
using System.Threading.Tasks;

namespace CallLog
{
    [GenerateSerializer]
    internal class ActivationMarker : IInvokable
    {
        [NonSerialized]
        private WorkflowContext _context;

        /// <summary>
        /// The time of this activation.
        /// </summary>
        [Id(1)]
        public DateTime Time { get; set; }

        /// <summary>
        /// The unique identifier for this activation.
        /// </summary>
        [Id(2)]
        public Guid InvocationId { get; set; }

        /// <summary>
        /// The version of the grain at the time of this activation.
        /// </summary>
        [Id(3)]
        public int Version { get; set; }

        public int ArgumentCount => 0;

        public void Dispose()
        {
        }

        public TArgument GetArgument<TArgument>(int index) => default;

        public TTarget GetTarget<TTarget>() => (TTarget)(object)_context;

        public async ValueTask<Response> Invoke()
        {
            try
            {
                await _context.OnActivationMarker(this);
                return Response.Completed;
            }
            catch (Exception exception)
            {
                return Response.FromException<object>(exception);
            }
        }

        public void SetArgument<TArgument>(int index, in TArgument value) { }

        public void SetTarget<TTargetHolder>(TTargetHolder holder) where TTargetHolder : ITargetHolder
        {
            _context = holder.GetComponent<WorkflowContext>();
        }
    }
}
