namespace Hagar.Activators
{
    public class DefaultActivator<T> : IActivator<T>
    {
        public T Create()
        {
            // TODO: consider array support using a different abstraction (need parameters)?
            return System.Activator.CreateInstance<T>();
        }
    }
}