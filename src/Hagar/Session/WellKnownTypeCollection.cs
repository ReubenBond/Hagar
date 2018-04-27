using System;
using System.Collections.Generic;
using Hagar.Configuration;

namespace Hagar.Session
{
    public sealed class WellKnownTypeCollection
    {
        private readonly Dictionary<uint, Type> wellKnownTypes;
        private readonly Dictionary<Type, uint> wellKnownTypeToIdMap = new Dictionary<Type, uint>();

        public WellKnownTypeCollection(IConfiguration<TypeConfiguration> typeConfiguration)
        {
            this.wellKnownTypes = typeConfiguration?.Value.WellKnownTypes ?? throw new ArgumentNullException(nameof(typeConfiguration));
            foreach (var item in this.wellKnownTypes)
            {
                this.wellKnownTypeToIdMap[item.Value] = item.Key;
            }
        }

        public Type GetWellKnownType(uint typeId)
        {
            if (typeId == 0) return null;
            return this.wellKnownTypes[typeId];
        }

        public bool TryGetWellKnownType(uint typeId, out Type type)
        {
            if (typeId == 0)
            {
                type = null;
                return true;
            }

            return this.wellKnownTypes.TryGetValue(typeId, out type);
        }

        public bool TryGetWellKnownTypeId(Type type, out uint typeId)
        {
            if (type == null)
            {
                typeId = 0;
                return true;
            }

            return this.wellKnownTypeToIdMap.TryGetValue(type, out typeId);
        }
    }
}