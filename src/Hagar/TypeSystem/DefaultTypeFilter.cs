using System;

namespace Hagar.TypeSystem
{
    /// <summary>
    /// Type which allows any exception type to be resolved.
    /// </summary>
    public sealed class DefaultTypeFilter : ITypeFilter
    {
        public bool? IsTypeNameAllowed(string typeName, string assemblyName)
        {
            if (typeName.EndsWith(nameof(Exception)))
            {
                return true;
            }

            if (typeName.StartsWith("System.Collections."))
            {
                return true;
            }

            if (typeName.StartsWith("System.") && typeName.EndsWith("Comparer"))
            {
                return true;
            }

            return null;
        }
    }
}