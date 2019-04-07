namespace Hagar.Activators
{
    public class DefaultActivator<T> : IActivator<T>
    {
        private static readonly bool HasDefaultConstructor;

        static DefaultActivator()
        {
            foreach (var ctor in typeof(T).GetConstructors())
            {
                if (ctor.GetParameters().Length == 0) HasDefaultConstructor = true;
            }
        }

        public T Create()
        {
            if (HasDefaultConstructor) return System.Activator.CreateInstance<T>();
            return CreateUnformatted();
        }

        private static T CreateUnformatted() => (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
    }
}