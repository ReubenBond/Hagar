using Hagar.Activators;
using Hagar.Cloning;
using Hagar.Codecs;
using Hagar.Configuration;
using Hagar.Serializers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hagar.TypeSystem
{
    /// <summary>
    /// Formats and parses <see cref="Type"/> instances using configured rules.
    /// </summary>
    public class TypeConverter
    {
        private readonly ITypeConverter[] _converters;
        private readonly ITypeFilter[] _filters;
        private readonly TypeResolver _resolver;
        private readonly RuntimeTypeNameRewriter.Rewriter _convertToDisplayName;
        private readonly RuntimeTypeNameRewriter.Rewriter _convertFromDisplayName;
        private readonly Dictionary<QualifiedType, QualifiedType> _wellKnownAliasToType;
        private readonly Dictionary<QualifiedType, QualifiedType> _wellKnownTypeToAlias;
        private readonly HashSet<string> _allowedTypes;

        public TypeConverter(IEnumerable<ITypeConverter> formatters, IEnumerable<ITypeFilter> filters, IConfiguration<SerializerConfiguration> configuration, TypeResolver typeResolver)
        {
            _resolver = typeResolver;
            _converters = formatters.ToArray();
            _filters = filters.ToArray();
            _convertToDisplayName = ConvertToDisplayName;
            _convertFromDisplayName = ConvertFromDisplayName;

            _wellKnownAliasToType = new Dictionary<QualifiedType, QualifiedType>();
            _wellKnownTypeToAlias = new Dictionary<QualifiedType, QualifiedType>();

            _allowedTypes = new HashSet<string>(configuration.Value.AllowedTypes, StringComparer.Ordinal);
            ConsumeMetadata(configuration.Value);

            var aliases = configuration.Value.WellKnownTypeAliases;
            foreach (var item in aliases)
            {
                var alias = new QualifiedType(null, item.Key);
                var spec = RuntimeTypeNameParser.Parse(RuntimeTypeNameFormatter.Format(item.Value));
                string asmName = null;
                if (spec is AssemblyQualifiedTypeSpec asm)
                {
                    asmName = asm.Assembly;
                    spec = asm.Type;
                }

                var originalQualifiedType = new QualifiedType(asmName, spec.Format());
                _wellKnownTypeToAlias[originalQualifiedType] = alias;
                if (asmName is { Length: > 0 })
                {
                    _wellKnownTypeToAlias[new QualifiedType(null, spec.Format())] = alias;
                }

                _wellKnownAliasToType[alias] = originalQualifiedType;
            }
        }

        private void ConsumeMetadata(SerializerConfiguration metadata)
        {
            AddFromMetadata(_allowedTypes, metadata.Serializers, typeof(IPartialSerializer<>));
            AddFromMetadata(_allowedTypes, metadata.Serializers, typeof(IValueSerializer<>));
            AddFromMetadata(_allowedTypes, metadata.Serializers, typeof(IFieldCodec<>));
            AddFromMetadata(_allowedTypes, metadata.FieldCodecs, typeof(IFieldCodec<>));
            AddFromMetadata(_allowedTypes, metadata.Activators, typeof(IActivator<>));
            AddFromMetadata(_allowedTypes, metadata.Copiers, typeof(IDeepCopier<>));
            AddFromMetadata(_allowedTypes, metadata.Copiers, typeof(IPartialCopier<>));

            void AddFromMetadata(HashSet<string> allowedTypes, IEnumerable<Type> metadataCollection, Type genericType)
            {
                Debug.Assert(genericType.GetGenericArguments().Length == 1);

                foreach (var type in metadataCollection)
                {
                    var interfaces = type.GetInterfaces();
                    foreach (var @interface in interfaces)
                    {
                        if (!@interface.IsGenericType)
                        {
                            continue;
                        }

                        if (genericType != @interface.GetGenericTypeDefinition())
                        {
                            continue;
                        }

                        var genericArgument = @interface.GetGenericArguments()[0];
                        if (typeof(object) == genericArgument)
                        {
                            continue;
                        }

                        if (genericArgument.IsConstructedGenericType && genericArgument.GenericTypeArguments.Any(arg => arg.IsGenericParameter))
                        {
                            genericArgument = genericArgument.GetGenericTypeDefinition();
                        }

                        if (genericArgument.IsGenericParameter || genericArgument.IsArray)
                        {
                            continue;
                        }

                        AddAllowedType(allowedTypes, genericArgument);
                    }
                }
            }

            static void AddAllowedType(HashSet<string> allowedTypes, Type type)
            {
                var formatted = RuntimeTypeNameFormatter.Format(type);
                var parsed = RuntimeTypeNameParser.Parse(formatted);
                if (parsed is AssemblyQualifiedTypeSpec qualified)
                {
                    allowedTypes.Add(qualified.Type.Format());
                }
                else
                {
                    allowedTypes.Add(parsed.Format());
                }

                if (type.DeclaringType is { } declaring)
                {
                    AddAllowedType(allowedTypes, declaring);
                }
            }
        }

        /// <summary>
        /// Formats the provided type.
        /// </summary>
        public string Format(Type type) => FormatInternal(type);

        /// <summary>
        /// Formats the provided type, rewriting elements using the provided delegate.
        /// </summary>
        internal string Format(Type type, Func<TypeSpec, TypeSpec> rewriter) => FormatInternal(type, rewriter);

        /// <summary>
        /// Parses the provided type string.
        /// </summary>
        public Type Parse(string formatted)
        {
            if (ParseInternal(formatted, out var type))
            {
                return type;
            }

            throw new TypeLoadException($"Unable to parse or load type \"{formatted}\"");
        }

        /// <summary>
        /// Parses the provided type string.
        /// </summary>
        public bool TryParse(string formatted, out Type result)
        {
            if (ParseInternal(formatted, out result))
            {
                return true;
            }

            return false;
        }

        private string FormatInternal(Type type, Func<TypeSpec, TypeSpec> rewriter = null)
        {
            string runtimeType = null;
            foreach (var converter in _converters)
            {
                if (converter.TryFormat(type, out var value))
                {
                    runtimeType = value;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(runtimeType))
            {
                runtimeType = RuntimeTypeNameFormatter.Format(type);
            }

            var runtimeTypeSpec = RuntimeTypeNameParser.Parse(runtimeType);
            var displayTypeSpec = RuntimeTypeNameRewriter.Rewrite(runtimeTypeSpec, _convertToDisplayName);
            if (rewriter is object)
            {
                displayTypeSpec = rewriter(displayTypeSpec);
            }

            var formatted = displayTypeSpec.Format();

            return formatted;
        }

        private bool ParseInternal(string formatted, out Type type)
        {
            var parsed = RuntimeTypeNameParser.Parse(formatted);
            var runtimeTypeSpec = RuntimeTypeNameRewriter.Rewrite(parsed, _convertFromDisplayName);
            var runtimeType = runtimeTypeSpec.Format();

            foreach (var converter in _converters)
            {
                if (converter.TryParse(runtimeType, out type))
                {
                    return true;
                }
            }

            return _resolver.TryResolveType(runtimeType, out type);
        }

        private bool IsTypeAllowed(in QualifiedType type)
        {
            foreach (var filter in _filters)
            {
                var isAllowed = filter.IsTypeNameAllowed(type.Type, type.Assembly);
                if (isAllowed.HasValue)
                {
                    return isAllowed.Value;
                }
            }

            return _allowedTypes.Contains(type.Type);
        }

        private QualifiedType ConvertToDisplayName(in QualifiedType input) => input switch
        {
            (_, "System.Object") => new QualifiedType(null, "object"),
            (_, "System.String") => new QualifiedType(null, "string"),
            (_, "System.Char") => new QualifiedType(null, "char"),
            (_, "System.SByte") => new QualifiedType(null, "sbyte"),
            (_, "System.Byte") => new QualifiedType(null, "byte"),
            (_, "System.Boolean") => new QualifiedType(null, "bool"),
            (_, "System.Int16") => new QualifiedType(null, "short"),
            (_, "System.UInt16") => new QualifiedType(null, "ushort"),
            (_, "System.Int32") => new QualifiedType(null, "int"),
            (_, "System.UInt32") => new QualifiedType(null, "uint"),
            (_, "System.Int64") => new QualifiedType(null, "long"),
            (_, "System.UInt64") => new QualifiedType(null, "ulong"),
            (_, "System.Single") => new QualifiedType(null, "float"),
            (_, "System.Double") => new QualifiedType(null, "double"),
            (_, "System.Decimal") => new QualifiedType(null, "decimal"),
            (_, "System.Guid") => new QualifiedType(null, "Guid"),
            (_, "System.TimeSpan") => new QualifiedType(null, "TimeSpan"),
            (_, "System.DateTime") => new QualifiedType(null, "DateTime"),
            (_, "System.DateTimeOffset") => new QualifiedType(null, "DateTimeOffset"),
            (_, "System.Type") => new QualifiedType(null, "Type"),
            (_, "System.RuntimeType") => new QualifiedType(null, "Type"),
            _ when _wellKnownTypeToAlias.TryGetValue(input, out var alias) => alias,
            var value when IsTypeAllowed(in value) => input,
            var value => ThrowTypeNotAllowed(in value)
        };

        private QualifiedType ConvertFromDisplayName(in QualifiedType input) => input switch
        {
            (_, "object") => new QualifiedType(null, "System.Object"),
            (_, "string") => new QualifiedType(null, "System.String"),
            (_, "char") => new QualifiedType(null, "System.Char"),
            (_, "sbyte") => new QualifiedType(null, "System.SByte"),
            (_, "byte") => new QualifiedType(null, "System.Byte"),
            (_, "bool") => new QualifiedType(null, "System.Boolean"),
            (_, "short") => new QualifiedType(null, "System.Int16"),
            (_, "ushort") => new QualifiedType(null, "System.UInt16"),
            (_, "int") => new QualifiedType(null, "System.Int32"),
            (_, "uint") => new QualifiedType(null, "System.UInt32"),
            (_, "long") => new QualifiedType(null, "System.Int64"),
            (_, "ulong") => new QualifiedType(null, "System.UInt64"),
            (_, "float") => new QualifiedType(null, "System.Single"),
            (_, "double") => new QualifiedType(null, "System.Double"),
            (_, "decimal") => new QualifiedType(null, "System.Decimal"),
            (_, "Guid") => new QualifiedType(null, "System.Guid"),
            (_, "TimeSpan") => new QualifiedType(null, "System.TimeSpan"),
            (_, "DateTime") => new QualifiedType(null, "System.DateTime"),
            (_, "DateTimeOffset") => new QualifiedType(null, "System.DateTimeOffset"),
            (_, "Type") => new QualifiedType(null, "System.Type"),
            _ when _wellKnownAliasToType.TryGetValue(input, out var type) => type,
            var value when IsTypeAllowed(in value) => input,
            var value => ThrowTypeNotAllowed(in value)
        };

        private static QualifiedType ThrowTypeNotAllowed(in QualifiedType value)
        {
            string message;
            if (!string.IsNullOrWhiteSpace(value.Assembly))
            {
                message = $"Type \"{value.Type}\" from assembly \"{value.Assembly}\" is not allowed. Add type to {nameof(SerializerConfiguration)}.{nameof(SerializerConfiguration.AllowedTypes)} to allow it.";
            }
            else
            {
                message = $"Type \"{value.Type}\" is not allowed. Add type to {nameof(SerializerConfiguration)}.{nameof(SerializerConfiguration.AllowedTypes)} to allow it.";
            }

            throw new InvalidOperationException(message);
        }
    }
}