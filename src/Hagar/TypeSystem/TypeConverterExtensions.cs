using System;

namespace Hagar.TypeSystem
{
    /// <summary>
    /// Extensions for working with <see cref="TypeConverter"/>.
    /// </summary>
    public static class TypeConverterExtensions
    {
        private const char GenericTypeIndicator = '`';
        private const char StartArgument = '[';

        /// <summary>
        /// Returns true if the provided type string is a generic type.
        /// </summary>
        public static bool IsGenericType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            return type.IndexOf(GenericTypeIndicator) >= 0;
        }

        /// <summary>
        /// Returns true if the provided type string is a constructed generic type.
        /// </summary>
        public static bool IsConstructed(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            var index = type.IndexOf(StartArgument);
            return index > 0;
        }

        /// <summary>
        /// Returns the deconstructed form of the provided generic type.
        /// </summary>
        public static string GetDeconstructed(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return null;
            }

            var index = type.IndexOf(StartArgument);

            if (index <= 0)
            {
                return type;
            }

            return type.Substring(0, index);
        }

        /// <summary>
        /// Returns the constructed form of the provided generic type.
        /// </summary>
        public static string GetConstructed(this TypeConverter formatter, string unconstructed, params Type[] typeArguments)
        {
            var typeString = unconstructed;
            var indicatorIndex = typeString.IndexOf(GenericTypeIndicator);
            var argumentsIndex = typeString.IndexOf(StartArgument, indicatorIndex);
            if (argumentsIndex >= 0)
            {
                throw new InvalidOperationException("Cannot construct an already-constructed type");
            }

            var arityString = typeString.Substring(indicatorIndex + 1);
            var arity = int.Parse(arityString);
            if (typeArguments.Length != arity)
            {
                throw new InvalidOperationException($"Insufficient number of type arguments, {typeArguments.Length}, provided while constructing type \"{unconstructed}\" of arity {arity}");
            }

            var typeSpecs = new TypeSpec[typeArguments.Length];
            for (var i = 0; i < typeArguments.Length; i++)
            {
                typeSpecs[i] = RuntimeTypeNameParser.Parse(formatter.Format(typeArguments[i]));
            }

            var constructed = new ConstructedGenericTypeSpec(new NamedTypeSpec(null, typeString, typeArguments.Length), typeSpecs).Format();
            return constructed;
        }

        /// <summary>
        /// Returns the type arguments for the provided constructed generic type string.
        /// </summary>
        public static string GetArgumentsString(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return null;
            }

            var index = type.IndexOf(StartArgument);

            if (index <= 0)
            {
                return null;
            }

            return type.Substring(index);
        }

        /// <summary>
        /// Returns the type arguments for the provided constructed generic type string.
        /// </summary>
        public static Type[] GetArguments(this TypeConverter formatter, string constructed)
        {
            var str = constructed;
            var index = str.IndexOf(StartArgument);
            if (index <= 0)
            {
                return Array.Empty<Type>();
            }

            var safeString = "safer" + str.Substring(str.IndexOf(GenericTypeIndicator));
            var parsed = RuntimeTypeNameParser.Parse(safeString);
            if (!(parsed is ConstructedGenericTypeSpec spec))
            {
                throw new InvalidOperationException($"Unable to correctly parse grain type {str}");
            }

            var result = new Type[spec.Arguments.Length];
            for (var i = 0; i < result.Length; i++)
            {
                var arg = spec.Arguments[i];
                var formattedArg = arg.Format();
                result[i] = formatter.Parse(formattedArg);
                if (result[i] is null)
                {
                    throw new InvalidOperationException($"Unable to parse argument \"{formattedArg}\" as a type for grain type \"{str}\"");
                }
            }

            return result;
        }
    }
}