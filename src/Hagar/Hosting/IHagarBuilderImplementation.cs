using System;
using Microsoft.Extensions.DependencyInjection;

namespace Hagar
{
    public interface IHagarBuilderImplementation : IHagarBuilder
    {
        IHagarBuilderImplementation ConfigureServices(Action<IServiceCollection> configureDelegate);
    }
}
