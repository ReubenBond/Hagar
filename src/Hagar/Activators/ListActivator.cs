using System.Collections.Generic;

namespace Hagar.Activators
{
    public class ListActivator<T>
    {
        public List<T> Create(int arg) => new List<T>(arg);
    }
}