using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hagar.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TestRpc.Runtime
{
    public sealed class ProxyFactory
    {
        private readonly IServiceProvider services;
        private readonly HashSet<Type> knownProxies;
        private readonly ConcurrentDictionary<Type, Type> proxyMap = new ConcurrentDictionary<Type, Type>();

        public ProxyFactory(IConfiguration<SerializerConfiguration> configuration, IServiceProvider services)
        {
            this.services = services;
            this.knownProxies = new HashSet<Type>(configuration.Value.InterfaceProxies);
        }

        private Type GetProxyType(Type interfaceType)
        {
            if (interfaceType.IsGenericType)
            {
                var unbound = interfaceType.GetGenericTypeDefinition();
                var parameters = interfaceType.GetGenericArguments();
                foreach (var proxyType in this.knownProxies)
                {
                    if (!proxyType.IsGenericType) continue;
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

            return this.knownProxies.First(interfaceType.IsAssignableFrom);
        }

        public TInterface GetProxy<TInterface>(GrainId id)
        {
            if (!this.proxyMap.TryGetValue(typeof(TInterface), out var proxyType))
            {
                proxyType = this.proxyMap[typeof(TInterface)] = this.GetProxyType(typeof(TInterface));
            }

            return (TInterface)ActivatorUtilities.CreateInstance(this.services, proxyType, id);
        }
    }
}