using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Hagar.CodeGenerator
{
    internal class LibraryTypes
    {
        private LibraryTypes() { }

        public static LibraryTypes FromCompilation(Compilation compilation, CodeGeneratorOptions options)
        {
            return new LibraryTypes
            {
                Compilation = compilation,
                ApplicationPartAttribute = Type("Hagar.ApplicationPartAttribute"),
                Action_2 = Type("System.Action`2"),
                Byte = compilation.GetSpecialType(SpecialType.System_Byte),
                ConfigurationProvider = Type("Hagar.Configuration.IConfigurationProvider`1"),
                Field = Type("Hagar.WireProtocol.Field"),
                WireType = Type("Hagar.WireProtocol.WireType"),
                FieldCodec = Type("Hagar.Codecs.IFieldCodec"),
                FieldCodec_1 = Type("Hagar.Codecs.IFieldCodec`1"),
                DeepCopier_1 = Type("Hagar.Cloning.IDeepCopier`1"),
                CopyContext = Type("Hagar.Cloning.CopyContext"),
                Func_2 = Type("System.Func`2"),
                GenerateMethodSerializersAttribute = Type("Hagar.GenerateMethodSerializersAttribute"),
                GenerateSerializerAttribute = Type("Hagar.GenerateSerializerAttribute"),
                IActivator_1 = Type("Hagar.Activators.IActivator`1"),
                IBufferWriter = Type("System.Buffers.IBufferWriter`1"),
                IdAttributeTypes = options.IdAttributes.Select(Type).ToList(),
                WellKnownAliasAttribute = Type("Hagar.WellKnownAliasAttribute"), 
                WellKnownIdAttribute = Type("Hagar.WellKnownIdAttribute"), 
                IInvokable = Type("Hagar.Invocation.IInvokable"),
                SubmitInvokableMethodNameAttribute = Type("Hagar.SubmitInvokableMethodNameAttribute"),
                InvokablePropertyValueAttribute = Type("Hagar.InvokablePropertyValueAttribute"),
                DefaultInvokableBaseTypeAttribute = Type("Hagar.DefaultInvokableBaseTypeAttribute"),
                InvokableBaseTypeAttribute = Type("Hagar.InvokableBaseTypeAttribute"),
                RegisterSerializerAttribute = Type("Hagar.RegisterSerializerAttribute"),
                RegisterActivatorAttribute = Type("Hagar.RegisterActivatorAttribute"),
                RegisterCopierAttribute = Type("Hagar.RegisterCopierAttribute"),
                UseActivatorAttribute = Type("Hagar.UseActivatorAttribute"),
                SuppressReferenceTrackingAttribute = Type("Hagar.SuppressReferenceTrackingAttribute"),
                OmitDefaultMemberValuesAttribute = Type("Hagar.OmitDefaultMemberValuesAttribute"),
                Int32 = compilation.GetSpecialType(SpecialType.System_Int32),
                UInt32 = compilation.GetSpecialType(SpecialType.System_UInt32),
                InvalidOperationException = Type("System.InvalidOperationException"),
                InvokablePool = Type("Hagar.Invocation.InvokablePool"),
                IResponseCompletionSource = Type("Hagar.Invocation.IResponseCompletionSource"),
                ITargetHolder = Type("Hagar.Invocation.ITargetHolder"),
                MetadataProviderAttribute = Type("Hagar.Configuration.MetadataProviderAttribute"),
                NonSerializedAttribute = Type("System.NonSerializedAttribute"),
                Object = compilation.GetSpecialType(SpecialType.System_Object),
                ObsoleteAttribute = Type("System.ObsoleteAttribute"),
                BaseCodec_1 = Type("Hagar.Serializers.IBaseCodec`1"),
                BaseCopier_1 = Type("Hagar.Cloning.IBaseCopier`1"),
                Reader = Type("Hagar.Buffers.Reader`1"),
                Request = Type("Hagar.Invocation.Request"),
                Request_1 = Type("Hagar.Invocation.Request`1"),
                ResponseCompletionSourcePool = Type("Hagar.Invocation.ResponseCompletionSourcePool"),
                SerializerConfiguration = Type("Hagar.Configuration.SerializerConfiguration"),
                SerializerSession = Type("Hagar.Session.SerializerSession"),
                Task = Type("System.Threading.Tasks.Task"),
                Task_1 = Type("System.Threading.Tasks.Task`1"),
                TaskRequest = Type("Hagar.Invocation.TaskRequest"),
                TaskRequest_1 = Type("Hagar.Invocation.TaskRequest`1"),
                Type = Type("System.Type"),
                ICodecProvider = Type("Hagar.Serializers.ICodecProvider"),
                ValueSerializer = Type("Hagar.Serializers.IValueSerializer`1"),
                ValueTask = Type("System.Threading.Tasks.ValueTask"),
                ValueTask_1 = Type("System.Threading.Tasks.ValueTask`1"),
                ValueTypeSetter_2 = Type("Hagar.Utilities.ValueTypeSetter`2"),
                Void = compilation.GetSpecialType(SpecialType.System_Void),
                VoidRequest = Type("Hagar.Invocation.VoidRequest"),
                Writer = Type("Hagar.Buffers.Writer`1"),
                StaticCodecs = new List<WellKnownCodecDescription>
                {
                    new(compilation.GetSpecialType(SpecialType.System_Object), Type("Hagar.Codecs.ObjectCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Boolean), Type("Hagar.Codecs.BoolCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Char), Type("Hagar.Codecs.CharCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Byte), Type("Hagar.Codecs.ByteCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_SByte), Type("Hagar.Codecs.SByteCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Int16), Type("Hagar.Codecs.Int16Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_Int32), Type("Hagar.Codecs.Int32Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_Int64), Type("Hagar.Codecs.Int64Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_UInt16), Type("Hagar.Codecs.UInt16Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_UInt32), Type("Hagar.Codecs.UInt32Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_UInt64), Type("Hagar.Codecs.UInt64Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_String), Type("Hagar.Codecs.StringCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Object), Type("Hagar.Codecs.ObjectCodec")),
                    new(compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Byte), 1), Type("Hagar.Codecs.ByteArrayCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Single), Type("Hagar.Codecs.FloatCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Double), Type("Hagar.Codecs.DoubleCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Decimal), Type("Hagar.Codecs.DecimalCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_DateTime), Type("Hagar.Codecs.DateTimeCodec")),
                    new(Type("System.TimeSpan"), Type("Hagar.Codecs.TimeSpanCodec")),
                    new(Type("System.DateTimeOffset"), Type("Hagar.Codecs.DateTimeOffsetCodec")),
                    new(Type("System.Guid"), Type("Hagar.Codecs.GuidCodec")),
                    new(Type("System.Type"), Type("Hagar.Codecs.TypeSerializerCodec")),
                    new(Type("System.ReadOnlyMemory`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)), Type("Hagar.Codecs.ReadOnlyMemoryOfByteCodec")),
                    new(Type("System.Memory`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)), Type("Hagar.Codecs.MemoryOfByteCodec")),
                    new(Type("System.Net.IPAddress"), Type("Hagar.Codecs.IPAddressCodec")),
                    new(Type("System.Net.IPEndPoint"), Type("Hagar.Codecs.IPEndPointCodec")),
                },
                WellKnownCodecs = new List<WellKnownCodecDescription>
                {
                    new(Type("System.Collections.Generic.Dictionary`2"), Type("Hagar.Codecs.DictionaryCodec`2")),
                    new(Type("System.Collections.Generic.List`1"), Type("Hagar.Codecs.ListCodec`1")),
                },
                StaticCopiers = new List<WellKnownCopierDescription>
                {
                    //new WellKnownCopierDescription(compilation.GetSpecialType(SpecialType.System_Object), Type("Hagar.Codecs.ObjectCopier")),
                    new(compilation.GetSpecialType(SpecialType.System_Boolean), Type("Hagar.Codecs.BoolCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Char), Type("Hagar.Codecs.CharCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Byte), Type("Hagar.Codecs.ByteCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_SByte), Type("Hagar.Codecs.SByteCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Int16), Type("Hagar.Codecs.Int16Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_Int32), Type("Hagar.Codecs.Int32Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_Int64), Type("Hagar.Codecs.Int64Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_UInt16), Type("Hagar.Codecs.UInt16Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_UInt32), Type("Hagar.Codecs.UInt32Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_UInt64), Type("Hagar.Codecs.UInt64Codec")),
                    new(compilation.GetSpecialType(SpecialType.System_String), Type("Hagar.Codecs.StringCopier")),
                    new(compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Byte), 1), Type("Hagar.Codecs.ByteArrayCopier")),
                    new(compilation.GetSpecialType(SpecialType.System_Single), Type("Hagar.Codecs.FloatCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Double), Type("Hagar.Codecs.DoubleCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_Decimal), Type("Hagar.Codecs.DecimalCodec")),
                    new(compilation.GetSpecialType(SpecialType.System_DateTime), Type("Hagar.Codecs.DateTimeCodec")),
                    new(Type("System.TimeSpan"), Type("Hagar.Codecs.TimeSpanCopier")),
                    new(Type("System.DateTimeOffset"), Type("Hagar.Codecs.DateTimeOffsetCopier")),
                    new(Type("System.Guid"), Type("Hagar.Codecs.GuidCopier")),
                    new(Type("System.Type"), Type("Hagar.Codecs.TypeCopier")),
                    new(Type("System.ReadOnlyMemory`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)), Type("Hagar.Codecs.ReadOnlyMemoryOfByteCopier")),
                    new(Type("System.Memory`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)), Type("Hagar.Codecs.MemoryOfByteCopier")),
                    new(Type("System.Net.IPAddress"), Type("Hagar.Codecs.IPAddressCopier")),
                    new(Type("System.Net.IPEndPoint"), Type("Hagar.Codecs.IPEndPointCopier")),
                },
                WellKnownCopiers = new List<WellKnownCopierDescription>
                {
                    new(Type("System.Collections.Generic.Dictionary`2"), Type("Hagar.Codecs.DictionaryCopier`2")),
                    new(Type("System.Collections.Generic.List`1"), Type("Hagar.Codecs.ListCopier`1")),
                },
                ImmutableTypes = new List<ITypeSymbol>
                {
                    compilation.GetSpecialType(SpecialType.System_Boolean),
                    compilation.GetSpecialType(SpecialType.System_Char),
                    compilation.GetSpecialType(SpecialType.System_Byte),
                    compilation.GetSpecialType(SpecialType.System_SByte),
                    compilation.GetSpecialType(SpecialType.System_Int16),
                    compilation.GetSpecialType(SpecialType.System_Int32),
                    compilation.GetSpecialType(SpecialType.System_Int64),
                    compilation.GetSpecialType(SpecialType.System_UInt16),
                    compilation.GetSpecialType(SpecialType.System_UInt32),
                    compilation.GetSpecialType(SpecialType.System_UInt64),
                    compilation.GetSpecialType(SpecialType.System_String),
                    compilation.GetSpecialType(SpecialType.System_Single),
                    compilation.GetSpecialType(SpecialType.System_Double),
                    compilation.GetSpecialType(SpecialType.System_Decimal),
                    compilation.GetSpecialType(SpecialType.System_DateTime),
                },
                    Exception = Type("System.Exception"),
                    ImmutableAttributes = options.ImmutableAttributes.Select(Type).ToList(),
                    Immutable_1 = Type("Hagar.Immutable`1"),
                    ValueTuple = Type("System.ValueTuple"),
                    TimeSpan = Type("System.TimeSpan"),
                    DateTimeOffset = Type("System.DateTimeOffset"),
                    Guid = Type("System.Guid"),
                    IPAddress = Type("System.Net.IPAddress"),
                    IPEndPoint = Type("System.Net.IPEndPoint"),
                    CancellationToken = Type("System.Threading.CancellationToken"),
            TupleTypes = new[]
            {
                Type("System.Tuple`1"),
                Type("System.Tuple`2"),
                Type("System.Tuple`3"),
                Type("System.Tuple`4"),
                Type("System.Tuple`5"),
                Type("System.Tuple`6"),
                Type("System.Tuple`7"),
                Type("System.Tuple`8"),
                Type("System.ValueTuple`1"),
                Type("System.ValueTuple`2"),
                Type("System.ValueTuple`3"),
                Type("System.ValueTuple`4"),
                Type("System.ValueTuple`5"),
                Type("System.ValueTuple`6"),
                Type("System.ValueTuple`7"),
                Type("System.ValueTuple`8"),
            },
            };

            INamedTypeSymbol Type(string metadataName)
            {
                var result = compilation.GetTypeByMetadataName(metadataName);
                if (result is null)
                {
                    throw new InvalidOperationException("Cannot find type with metadata name " + metadataName);
                }

                return result;
            }
        }

        public INamedTypeSymbol Action_2 { get; private set; }
        public INamedTypeSymbol Byte { get; private set; }
        public INamedTypeSymbol ConfigurationProvider { get; private set; }
        public INamedTypeSymbol Field { get; private set; }
        public INamedTypeSymbol WireType { get; private set; }
        public INamedTypeSymbol DeepCopier_1 { get; private set; }
        public INamedTypeSymbol FieldCodec_1 { get; private set; }
        public INamedTypeSymbol FieldCodec { get; private set; }
        public INamedTypeSymbol Func_2 { get; private set; }
        public INamedTypeSymbol GenerateMethodSerializersAttribute { get; private set; }
        public INamedTypeSymbol GenerateSerializerAttribute { get; private set; }
        public INamedTypeSymbol IActivator_1 { get; private set; }
        public INamedTypeSymbol IBufferWriter { get; private set; }
        public INamedTypeSymbol IInvokable { get; private set; }
        public INamedTypeSymbol Int32 { get; private set; }
        public INamedTypeSymbol UInt32 { get; private set; }
        public INamedTypeSymbol InvalidOperationException { get; private set; }
        public INamedTypeSymbol InvokablePool { get; private set; }
        public INamedTypeSymbol IResponseCompletionSource { get; private set; }
        public INamedTypeSymbol ITargetHolder { get; private set; }
        public INamedTypeSymbol MetadataProviderAttribute { get; private set; }
        public INamedTypeSymbol NonSerializedAttribute { get; private set; }
        public INamedTypeSymbol Object { get; private set; }
        public INamedTypeSymbol ObsoleteAttribute { get; private set; }
        public INamedTypeSymbol BaseCodec_1 { get; private set; }
        public INamedTypeSymbol BaseCopier_1 { get; private set; }
        public INamedTypeSymbol Reader { get; private set; }
        public INamedTypeSymbol Request { get; private set; }
        public INamedTypeSymbol Request_1 { get; private set; }
        public INamedTypeSymbol ResponseCompletionSourcePool { get; private set; }
        public INamedTypeSymbol SerializerConfiguration { get; private set; }
        public INamedTypeSymbol SerializerSession { get; private set; }
        public INamedTypeSymbol Task { get; private set; }
        public INamedTypeSymbol Task_1 { get; private set; }
        public INamedTypeSymbol TaskRequest { get; private set; }
        public INamedTypeSymbol TaskRequest_1 { get; private set; }
        public INamedTypeSymbol Type { get; private set; }
        public INamedTypeSymbol ICodecProvider { get; private set; }
        public INamedTypeSymbol ValueSerializer { get; private set; }
        public INamedTypeSymbol ValueTask { get; private set; }
        public INamedTypeSymbol ValueTask_1 { get; private set; }
        public INamedTypeSymbol ValueTypeSetter_2 { get; private set; }
        public INamedTypeSymbol Void { get; private set; }
        public INamedTypeSymbol Writer { get; private set; }
        public List<INamedTypeSymbol> IdAttributeTypes { get; private set; }
        public INamedTypeSymbol WellKnownAliasAttribute { get; private set; }
        public INamedTypeSymbol WellKnownIdAttribute { get; private set; }
        public List<WellKnownCodecDescription> StaticCodecs { get; private set; }
        public List<WellKnownCodecDescription> WellKnownCodecs { get; private set; }
        public List<WellKnownCopierDescription> StaticCopiers { get; private set; }
        public List<WellKnownCopierDescription> WellKnownCopiers { get; private set; }
        public INamedTypeSymbol RegisterCopierAttribute { get; private set; }
        public INamedTypeSymbol RegisterSerializerAttribute { get; private set; }
        public INamedTypeSymbol RegisterActivatorAttribute { get; private set; }
        public INamedTypeSymbol UseActivatorAttribute { get; private set; }
        public INamedTypeSymbol SuppressReferenceTrackingAttribute { get; private set; }
        public INamedTypeSymbol OmitDefaultMemberValuesAttribute { get; private set; }
        public INamedTypeSymbol CopyContext { get; private set; }
        public Compilation Compilation { get; private set; }
        public List<ITypeSymbol> ImmutableTypes { get; private set; }
        public INamedTypeSymbol TimeSpan { get; private set; }
        public INamedTypeSymbol DateTimeOffset { get; private set; }
        public INamedTypeSymbol Guid { get; private set; }
        public INamedTypeSymbol IPAddress { get; private set; }
        public INamedTypeSymbol IPEndPoint { get; private set; }
        public INamedTypeSymbol CancellationToken { get; private set; }
        public INamedTypeSymbol[] TupleTypes { get; private set; }
        public INamedTypeSymbol ValueTuple { get; private set; }
        public INamedTypeSymbol Immutable_1 { get; private set; }
        public List<INamedTypeSymbol> ImmutableAttributes { get; private set; }
        public INamedTypeSymbol Exception { get; private set; }
        public INamedTypeSymbol VoidRequest { get; private set; }
        public INamedTypeSymbol ApplicationPartAttribute { get; private set; }
        public INamedTypeSymbol SubmitInvokableMethodNameAttribute { get; private set; }
        public INamedTypeSymbol InvokablePropertyValueAttribute { get; private set; }
        public INamedTypeSymbol InvokableBaseTypeAttribute { get; private set; }
        public INamedTypeSymbol DefaultInvokableBaseTypeAttribute { get; private set; }

#pragma warning disable RS1024 // Compare symbols correctly
        private readonly ConcurrentDictionary<ITypeSymbol, bool> _shallowCopyableTypes = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly

        public bool IsShallowCopyable(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                case SpecialType.System_DateTime:
                    return true;
            }

            if (SymbolEqualityComparer.Default.Equals(TimeSpan, type)
                || SymbolEqualityComparer.Default.Equals(IPAddress, type)
                || SymbolEqualityComparer.Default.Equals(IPEndPoint, type)
                || SymbolEqualityComparer.Default.Equals(CancellationToken, type)
                || SymbolEqualityComparer.Default.Equals(Type, type))
            {
                return true;
            }

            if (_shallowCopyableTypes.TryGetValue(type, out var result))
            {
                return result;
            }

            foreach (var attr in ImmutableAttributes)
            {
                if (type.HasAttribute(attr))
                {
                    return _shallowCopyableTypes[type] = true;
                }
            }

            if (type.HasBaseType(Exception))
            {
                return _shallowCopyableTypes[type] = true;
            }

            if (!(type is INamedTypeSymbol namedType))
            {
                return _shallowCopyableTypes[type] = false;
            }

            if (namedType.IsTupleType)
            {
                return _shallowCopyableTypes[type] = namedType.TupleElements.All(f => IsShallowCopyable(f.Type));
            }
            else if (namedType.IsGenericType)
            {
                var def = namedType.ConstructedFrom;
                if (def.SpecialType == SpecialType.System_Nullable_T)
                {
                    return _shallowCopyableTypes[type] = IsShallowCopyable(namedType.TypeArguments.Single());
                }

                if (SymbolEqualityComparer.Default.Equals(Immutable_1, def))
                {
                    return _shallowCopyableTypes[type] = true;
                }

                if (TupleTypes.Any(t => SymbolEqualityComparer.Default.Equals(t, def)))
                {
                    return _shallowCopyableTypes[type] = namedType.TypeArguments.All(IsShallowCopyable);
                }
            }
            else
            {
                if (type.TypeKind == TypeKind.Enum)
                {
                    return _shallowCopyableTypes[type] = true;
                }

                if (type.TypeKind == TypeKind.Struct && !namedType.IsUnboundGenericType)
                {
                    return _shallowCopyableTypes[type] = IsValueTypeFieldsShallowCopyable(type);
                }
            }

            return _shallowCopyableTypes[type] = false;
        }

        private bool IsValueTypeFieldsShallowCopyable(ITypeSymbol type)
        {
            foreach (var field in type.GetDeclaredInstanceMembers<IFieldSymbol>())
            {
                if (field.Type is not INamedTypeSymbol fieldType)
                {
                    return false;
                }

                if (SymbolEqualityComparer.Default.Equals(type, fieldType))
                {
                    return false;
                }

                if (!IsShallowCopyable(fieldType))
                {
                    return false;
                }
            }

            return true;
        }
    }
}