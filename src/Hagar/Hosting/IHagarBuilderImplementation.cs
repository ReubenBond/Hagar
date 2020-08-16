using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hagar
{
    public interface IHagarBuilderImplementation : IHagarBuilder
    {
        IHagarBuilderImplementation ConfigureServices(Action<IServiceCollection> configureDelegate);
    }
}