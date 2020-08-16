using Hagar.Activators;

namespace Hagar.Serializers
{
    public interface IActivatorProvider
    {
        IActivator<T> GetActivator<T>();
    }
}