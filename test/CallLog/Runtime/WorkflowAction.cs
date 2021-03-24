using Hagar.Invocation;
using System;
using System.Threading.Tasks;

namespace CallLog
{
    public class WorkflowAction
    {
        public static ValueTask<DateTime> GetUtcNow() => WorkflowAction.Record(() => DateTime.UtcNow);

        public static async ValueTask Delay(TimeSpan duration)
        {
            var until = await Record(() => DateTime.UtcNow + duration);
            await DelayUntil(until);
        }
        public static ValueTask DelayUntil(DateTime until)
        {
            return RecordAsync(async () =>
            {
                var now = DateTime.UtcNow;
                if (now > until)
                {
                    return;
                }
                else
                {
                    // TODO: durably schedule execution at some point in the future.
                    await Task.Delay(until - now);
                }
            });
        }

        public static ValueTask<T> Record<T>(Func<T> func)
        {
            var completion = ResponseCompletionSourcePool.Get<T>();
            var current = RuntimeContext.Current;
            if (current.OnCreateRequest(completion, out var sequenceNumber))
            {
                try
                {
                    current.OnMessage(new Message { SenderId = current.Id, SequenceNumber = sequenceNumber, Body = Response.FromResult<T>(func()), });
                }
                catch (Exception exception)
                {
                    current.OnMessage(new Message { SenderId = current.Id, SequenceNumber = sequenceNumber, Body = Response.FromException(exception) });
                }
            }    

            return completion.AsValueTask();
        }

        public static async ValueTask<T> RecordAsync<T>(Func<Task<T>> func)
        {
            var completion = ResponseCompletionSourcePool.Get<T>();
            var current = RuntimeContext.Current;
            if (current.OnCreateRequest(completion, out var sequenceNumber))
            {
                try
                {
                    var result = await func();
                    current.OnMessage(new Message { SenderId = current.Id, SequenceNumber = sequenceNumber, Body = Response.FromResult<T>(result) });
                }
                catch (Exception exception)
                {
                    current.OnMessage(new Message { SenderId = current.Id, SequenceNumber = sequenceNumber, Body = Response.FromException(exception) });
                }
            }    

            return await completion.AsValueTask();
        }

        public static async ValueTask RecordAsync(Func<Task> func)
        {
            var completion = ResponseCompletionSourcePool.Get<int>();
            var current = RuntimeContext.Current;
            if (current.OnCreateRequest(completion, out var sequenceNumber))
            {
                try
                {
                    await func();
                    current.OnMessage(new Message { SenderId = current.Id, SequenceNumber = sequenceNumber, Body = Response.FromResult<int>(0) });
                }
                catch (Exception exception)
                {
                    current.OnMessage(new Message { SenderId = current.Id, SequenceNumber = sequenceNumber, Body = Response.FromException(exception) });
                }
            }    

            await completion.AsVoidValueTask();
        }
    }

}
