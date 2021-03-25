using System;
using System.Threading.Tasks;

namespace TestRpc.App
{
    public sealed class PingPongGrain : IPingPongGrain
    {
        public ValueTask Ping() => default;
        public ValueTask<string> Echo(string input)
        {
            Console.WriteLine($"Received call to PingPongGrain.Echo(\"{input}\") -> sending response");
            return new ValueTask<string>(input);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TransactionAttribute : Attribute
    {
        public TransactionAttribute(TransactionOption requirement)
        {
            Requirement = requirement;
            ReadOnly = false;
        }

        public TransactionOption Requirement { get; }
        public bool ReadOnly { get; set; }
    }

    public enum TransactionOption
    {
        Suppress,     // Logic is not transactional but can be called from within a transaction.  If called within the context of a transaction, the context will not be passed to the call.
        CreateOrJoin, // Logic is transactional.  If called within the context of a transaction, it will use that context, else it will create a new context.
        Create,       // Logic is transactional and will always create a new transaction context, even if called within an existing transaction context.
        Join,         // Logic is transactional but can only be called within the context of an existing transaction.
        Supported,    // Logic is not transactional but supports transactions.  If called within the context of a transaction, the context will be passed to the call.
        NotAllowed    // Logic is not transactional and cannot be called from within a transaction.  If called within the context of a transaction, it will throw a not supported exception.
    }
}