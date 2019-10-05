using System.Security;
using Hagar.ISerializable;
using Hagar.Serializers;
using Microsoft.Extensions.DependencyInjection;

namespace Hagar
{
    public static class ServiceCollectionExtensions
    {
        [SecurityCritical]
        public static IHagarBuilder AddISerializableSupport(this IHagarBuilder builder)
        {
            return ((IHagarBuilderImplementation)builder).ConfigureServices(services => services.AddSingleton<IGeneralizedCodec, DotNetSerializableCodec>());
        }
    }
}
