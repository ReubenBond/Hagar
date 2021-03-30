using Hagar.Cloning;
using System;

namespace Hagar.Serializers
{
    public interface ICodecProvider :
        IFieldCodecProvider,
        IBaseCodecProvider,
        IValueSerializerProvider,
        IActivatorProvider,
        IDeepCopierProvider
    {
        IServiceProvider Services { get; }
    }
}