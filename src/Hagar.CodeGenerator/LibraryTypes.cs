using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable InconsistentNaming

namespace Hagar.CodeGenerator
{
    internal class LibraryTypes
    {
        private LibraryTypes() { }

        public static LibraryTypes FromCompilation(Compilation compilation, CodeGeneratorOptions options)
        {
            return new LibraryTypes
            {
                Action_2 = Type("System.Action`2"),
                Byte = compilation.GetSpecialType(SpecialType.System_Byte),
                ConfigurationProvider = Type("Hagar.Configuration.IConfigurationProvider`1"),
                Field = Type("Hagar.WireProtocol.Field"),
                FieldCodec = Type("Hagar.Codecs.IFieldCodec"),
                FieldCodec_1 = Type("Hagar.Codecs.IFieldCodec`1"),
                Func_2 = Type("System.Func`2"),
                GenerateMethodSerializersAttribute = Type("Hagar.GenerateMethodSerializersAttribute"),
                GenerateSerializerAttribute = Type("Hagar.GenerateSerializerAttribute"),
                IActivator_1 = Type("Hagar.Activators.IActivator`1"),
                IBufferWriter = Type("System.Buffers.IBufferWriter`1"),
                IdAttributeTypes = options.IdAttributeTypes.Select(Type).ToList(),
                IInvokable = Type("Hagar.Invocation.IInvokable"),
                RegisterSerializerAttribute = Type("Hagar.RegisterSerializerAttribute"),
                RegisterActivatorAttribute = Type("Hagar.RegisterActivatorAttribute"),
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
                PartialSerializer = Type("Hagar.Serializers.IPartialSerializer`1"),
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
                TypedCodecProvider = Type("Hagar.Serializers.ITypedCodecProvider"),
                ValueSerializer = Type("Hagar.Serializers.IValueSerializer`1"),
                ValueTask = Type("System.Threading.Tasks.ValueTask"),
                ValueTask_1 = Type("System.Threading.Tasks.ValueTask`1"),
                ValueTypeSetter_2 = Type("Hagar.Utilities.ValueTypeSetter`2"),
                Void = compilation.GetSpecialType(SpecialType.System_Void),
                Writer = Type("Hagar.Buffers.Writer`1"),
                StaticCodecs = new List<StaticCodecDescription>
                {
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Boolean), Type("Hagar.Codecs.BoolCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Char), Type("Hagar.Codecs.CharCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Byte), Type("Hagar.Codecs.ByteCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_SByte), Type("Hagar.Codecs.SByteCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Int16), Type("Hagar.Codecs.Int16Codec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Int32), Type("Hagar.Codecs.Int32Codec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Int64), Type("Hagar.Codecs.Int64Codec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_UInt16), Type("Hagar.Codecs.UInt16Codec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_UInt32), Type("Hagar.Codecs.UInt32Codec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_UInt64), Type("Hagar.Codecs.UInt64Codec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_String), Type("Hagar.Codecs.StringCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Object), Type("Hagar.Codecs.ObjectCodec")),
                    new StaticCodecDescription(compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Byte), 1), Type("Hagar.Codecs.ByteArrayCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Single), Type("Hagar.Codecs.FloatCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Double), Type("Hagar.Codecs.DoubleCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_Decimal), Type("Hagar.Codecs.DecimalCodec")),
                    new StaticCodecDescription(compilation.GetSpecialType(SpecialType.System_DateTime), Type("Hagar.Codecs.DateTimeCodec")),
                    new StaticCodecDescription(Type("System.TimeSpan"), Type("Hagar.Codecs.TimeSpanCodec")),
                    new StaticCodecDescription(Type("System.DateTimeOffset"), Type("Hagar.Codecs.DateTimeOffsetCodec")),
                    new StaticCodecDescription(Type("System.Guid"), Type("Hagar.Codecs.GuidCodec")),
                    new StaticCodecDescription(Type("System.Type"), Type("Hagar.Codecs.TypeSerializerCodec")),
                    new StaticCodecDescription(Type("System.ReadOnlyMemory`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)), Type("Hagar.Codecs.ReadOnlyMemoryOfByteCodec")),
                    new StaticCodecDescription(Type("System.Memory`1").Construct(compilation.GetSpecialType(SpecialType.System_Byte)), Type("Hagar.Codecs.MemoryOfByteCodec")),
                }
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
        public INamedTypeSymbol PartialSerializer { get; private set; }
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
        public INamedTypeSymbol TypedCodecProvider { get; private set; }
        public INamedTypeSymbol ValueSerializer { get; private set; }
        public INamedTypeSymbol ValueTask { get; private set; }
        public INamedTypeSymbol ValueTask_1 { get; private set; }
        public INamedTypeSymbol ValueTypeSetter_2 { get; private set; }
        public INamedTypeSymbol Void { get; private set; }
        public INamedTypeSymbol Writer { get; private set; }
        public List<INamedTypeSymbol> IdAttributeTypes { get; private set; }
        public List<StaticCodecDescription> StaticCodecs { get; private set; }
        public INamedTypeSymbol RegisterSerializerAttribute { get; private set; }
        public INamedTypeSymbol RegisterActivatorAttribute { get; private set; }
    }
}