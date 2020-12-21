using System;

namespace Hagar.Serializers
{
    public interface ICodecProvider : ITypedCodecProvider, IUntypedCodecProvider, IPartialSerializerProvider, IValueSerializerProvider, IActivatorProvider
    {
        IServiceProvider Services { get; }
    }
}