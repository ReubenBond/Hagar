using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Hagar
{
    public interface IHagarBuilderImplementation : IHagarBuilder
    {
        IHagarBuilderImplementation ConfigureServices(Action<IServiceCollection> configureDelegate);
        Dictionary<object, object>  Properties { get; }
    }
}