using System.Runtime.InteropServices.ComTypes;

namespace Hagar.Invocation
{
    public interface IResponseCompletionSource
    {
        /// <summary>
        /// Sets the result.
        /// </summary>
        /// <param name="value">The result value.</param>
        void Complete(Response value);

        void Complete(); 
    }
}