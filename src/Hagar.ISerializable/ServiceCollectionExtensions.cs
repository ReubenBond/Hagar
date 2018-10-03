using System.Security;
using Hagar.ISerializable;
using Hagar.Serializers;
using Microsoft.Extensions.DependencyInjection;

namespace Hagar
{
    public static class ServiceCollectionExtensions
    {
        [SecurityCritical]
        public static IServiceCollection AddISerializableSupport(this IServiceCollection services)
        {
            return services.AddSingleton<IGeneralizedCodec, DotNetSerializableCodec>();
        }
    }
}
