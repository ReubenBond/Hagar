using Hagar.Configuration;
using System;
using System.Collections.Generic;

namespace Hagar.Session
{
    public sealed class WellKnownTypeCollection
    {
        private readonly Dictionary<uint, Type> _wellKnownTypes;
        private readonly Dictionary<Type, uint> _wellKnownTypeToIdMap = new Dictionary<Type, uint>();

        public WellKnownTypeCollection(IConfiguration<TypeConfiguration> typeConfiguration)
        {
            _wellKnownTypes = typeConfiguration?.Value.WellKnownTypes ?? throw new ArgumentNullException(nameof(typeConfiguration));
            foreach (var item in _wellKnownTypes)
            {
                _wellKnownTypeToIdMap[item.Value] = item.Key;
            }
        }

        public Type GetWellKnownType(uint typeId)
        {
            if (typeId == 0)
            {
                return null;
            }

            return _wellKnownTypes[typeId];
        }

        public bool TryGetWellKnownType(uint typeId, out Type type)
        {
            if (typeId == 0)
            {
                type = null;
                return true;
            }

            return _wellKnownTypes.TryGetValue(typeId, out type);
        }

        public bool TryGetWellKnownTypeId(Type type, out uint typeId)
        {
            if (type is null)
            {
                typeId = 0;
                return true;
            }

            return _wellKnownTypeToIdMap.TryGetValue(type, out typeId);
        }
    }
}