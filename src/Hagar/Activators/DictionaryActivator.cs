using System.Collections.Generic;

namespace Hagar.Activators
{
    public class DictionaryActivator<TKey, TValue>
    {
        public Dictionary<TKey, TValue> Create(IEqualityComparer<TKey> arg)
        {
            return new Dictionary<TKey, TValue>(arg);
        }
    }
}