using Hagar.Configuration;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Hagar.TypeSystem
{
    /// <summary>
    /// Formats and parses <see cref="Type"/> instances using configured rules.
    /// </summary>
    public class TypeConverter
    {
        private readonly ITypeConverter[] _converters;
        private readonly CachedTypeResolver _resolver = new CachedTypeResolver();
        private readonly Func<QualifiedType, QualifiedType> _convertToDisplayName;
        private readonly Func<QualifiedType, QualifiedType> _convertFromDisplayName;
        private readonly Dictionary<QualifiedType, QualifiedType> _wellKnownAliasToType;
        private readonly Dictionary<QualifiedType, QualifiedType> _wellKnownTypeToAlias;

        public TypeConverter(IEnumerable<ITypeConverter> formatters, IConfiguration<SerializerConfiguration> configuration)
        {
            _converters = formatters.ToArray();
            _convertToDisplayName = ConvertToDisplayName;
            _convertFromDisplayName = ConvertFromDisplayName;

            _wellKnownAliasToType = new Dictionary<QualifiedType, QualifiedType>();
            _wellKnownTypeToAlias = new Dictionary<QualifiedType, QualifiedType>();

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

        private QualifiedType ConvertToDisplayName(QualifiedType input) => input switch
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
            _ when _wellKnownTypeToAlias.TryGetValue(input, out var alias) => alias,
            _ => input,
        };

        private QualifiedType ConvertFromDisplayName(QualifiedType input) => input switch
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
            _ when _wellKnownAliasToType.TryGetValue(input, out var type) => type,
            _ => input
        };
    }
}