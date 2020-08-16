using Hagar.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TestRpc.Runtime
{
    public sealed class ProxyFactory
    {
        private readonly IServiceProvider _services;
        private readonly HashSet<Type> _knownProxies;
        private readonly ConcurrentDictionary<Type, Type> _proxyMap = new ConcurrentDictionary<Type, Type>();

        public ProxyFactory(IConfiguration<SerializerConfiguration> configuration, IServiceProvider services)
        {
            _services = services;
            _knownProxies = new HashSet<Type>(configuration.Value.InterfaceProxies);
        }

        private Type GetProxyType(Type interfaceType)
        {
            if (interfaceType.IsGenericType)
            {
                var unbound = interfaceType.GetGenericTypeDefinition();
                var parameters = interfaceType.GetGenericArguments();
                foreach (var proxyType in _knownProxies)
                {
                    if (!proxyType.IsGenericType)
                    {
                        continue;
                    }

                    var matching = proxyType.FindInterfaces(
                            (type, criteria) =>
                                type.IsGenericType && type.GetGenericTypeDefinition() == (Type)criteria,
                            unbound)
                        .FirstOrDefault();
                    if (matching != null)
                    {
                        return proxyType.GetGenericTypeDefinition().MakeGenericType(parameters);
                    }
                }
            }

            return _knownProxies.First(interfaceType.IsAssignableFrom);
        }

        public TInterface GetProxy<TInterface>(GrainId id)
        {
            if (!_proxyMap.TryGetValue(typeof(TInterface), out var proxyType))
            {
                proxyType = _proxyMap[typeof(TInterface)] = GetProxyType(typeof(TInterface));
            }

            return (TInterface)ActivatorUtilities.CreateInstance(_services, proxyType, id);
        }
    }
}