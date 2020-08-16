using Hagar.ISerializable;
using Hagar.Serializers;
using Microsoft.Extensions.DependencyInjection;
using System.Security;

namespace Hagar
{
    public static class ServiceCollectionExtensions
    {
        [SecurityCritical]
        public static IHagarBuilder AddISerializableSupport(this IHagarBuilder builder) => ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.AddSingleton<IGeneralizedCodec, DotNetSerializableCodec>());
    }
}