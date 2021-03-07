using Hagar.Cloning;
using System;

namespace Hagar.Serializers
{
    public interface ICodecProvider :
        IFieldCodecProvider,
        IPartialSerializerProvider,
        IValueSerializerProvider,
        IActivatorProvider,
        IDeepCopierProvider
    {
        IServiceProvider Services { get; }
    }
}