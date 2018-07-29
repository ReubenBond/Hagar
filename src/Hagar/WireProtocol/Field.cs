using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hagar.WireProtocol
{
    public struct Field
    {
        private uint fieldIdDelta;
        private Type fieldType;
        public Tag Tag;

        public Field(Tag tag)
        {
            this.Tag = tag;
            this.fieldIdDelta = 0;
            this.fieldType = null;
#if DEBUG
            if (!this.HasFieldId) ThrowFieldIdInvalid();
#endif
        }

        public uint FieldIdDelta
        {
            // If the embedded field id delta is valid, return it, otherwise return the extended field id delta.
            // The extended field id might not be valid if this field has the Extended wire type.
            get
            {
                if (this.Tag.IsFieldIdValid) return this.Tag.FieldIdDelta;
#if DEBUG
                if (!this.HasFieldId) ThrowFieldIdInvalid();
#endif
                return this.fieldIdDelta;
            }
            set
            {
                // If the field id delta can fit into the tag, embed it there, otherwise invalidate the embedded field id delta and set the full field id delta.
                if (value < 7)
                {
                    this.Tag.FieldIdDelta = value;
                    this.fieldIdDelta = 0;
                }
                else
                {
                    this.Tag.SetFieldIdInvalid();
                    this.fieldIdDelta = value;
                }
            }
        }

        public Type FieldType
        {
            get
            {
#if DEBUG
                if (this.IsSchemaTypeValid) ThrowFieldTypeInvalid();
#endif
                return this.fieldType;
            }

            set
            {
#if DEBUG
                if (this.IsSchemaTypeValid) ThrowFieldTypeInvalid();
#endif
                this.fieldType = value;
            }
        }

        public bool HasFieldId => this.Tag.WireType != WireType.Extended;
        public bool HasExtendedFieldId => this.Tag.HasExtendedFieldId;

        public WireType WireType
        {
            get => this.Tag.WireType;
            set => this.Tag.WireType = value;
        }

        public SchemaType SchemaType
        {
            get
            {
#if DEBUG
                if (!this.IsSchemaTypeValid) ThrowSchemaTypeInvalid();
#endif

                return this.Tag.SchemaType;
            }

            set => this.Tag.SchemaType = value;
        }

        public ExtendedWireType ExtendedWireType
        {
            get
            {
#if DEBUG
                if (this.WireType != WireType.Extended) ThrowExtendedWireTypeInvalid();
#endif
                return this.Tag.ExtendedWireType;
            }
            set => this.Tag.ExtendedWireType = value;
        }

        public bool IsSchemaTypeValid => this.Tag.IsSchemaTypeValid;
        public bool HasExtendedSchemaType => this.IsSchemaTypeValid && this.SchemaType != SchemaType.Expected;
        public bool IsEndBaseFields => this.Tag.HasExtendedWireType && this.Tag.ExtendedWireType == ExtendedWireType.EndBaseFields;
        public bool IsEndObject => this.Tag.HasExtendedWireType && this.Tag.ExtendedWireType == ExtendedWireType.EndTagDelimited;

        public bool IsEndBaseOrEndObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Tag.HasExtendedWireType &&
                   (this.Tag.ExtendedWireType == ExtendedWireType.EndTagDelimited ||
                    this.Tag.ExtendedWireType == ExtendedWireType.EndBaseFields);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append('[').Append((string) this.WireType.ToString());
            if (this.HasFieldId) builder.Append($", IdDelta:{this.FieldIdDelta}");
            if (this.IsSchemaTypeValid) builder.Append($", SchemaType:{this.SchemaType}");
            if (this.HasExtendedSchemaType) builder.Append($", RuntimeType:{this.FieldType}");
            if (this.WireType == WireType.Extended) builder.Append($": {this.ExtendedWireType}");
            builder.Append(']');
            return builder.ToString();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFieldIdInvalid() => throw new FieldIdNotPresentException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSchemaTypeInvalid() => throw new SchemaTypeInvalidException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFieldTypeInvalid() => throw new FieldTypeInvalidException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowExtendedWireTypeInvalid() => throw new ExtendedWireTypeInvalidException();
    }
}