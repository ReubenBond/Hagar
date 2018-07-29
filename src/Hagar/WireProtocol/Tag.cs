namespace Hagar.WireProtocol
{
    public struct Tag
    {
        // [W W W] [S S] [F F F]
        public const byte WireTypeMask = 0b1110_0000; // The first 3 bits are dedicated to the wire type.
        public const byte SchemaTypeMask = 0b0001_1000; // The next 2 bits are dedicated to the schema type specifier, if the schema type is expected.
        public const byte FieldIdMask = 0b000_0111; // The final 3 bits are used for the field id delta, if the delta is expected.
        public const byte FieldIdCompleteMask = 0b0000_0111;
        public const byte ExtendedWireTypeMask = 0b0001_1000;
        
        private byte tag;

        public Tag(byte tag)
        {
            this.tag = tag;
        }

        public static implicit operator Tag(byte tag) => new Tag(tag);
        public static implicit operator byte(Tag tag) => tag.tag;

        /// <summary>
        /// Returns the wire type of the data following this tag.
        /// </summary>
        public WireType WireType
        {
            get => (WireType)(this.tag & WireTypeMask);
            set => this.tag = (byte)((this.tag & ~WireTypeMask) | ((byte)value & WireTypeMask));
        }

        public bool HasExtendedWireType => this.WireType == WireType.Extended;

        /// <summary>
        /// Returns the wire type of the data following this tag.
        /// </summary>
        public ExtendedWireType ExtendedWireType
        {
            get => (ExtendedWireType)(this.tag & ExtendedWireTypeMask);
            set => this.tag = (byte)((this.tag & ~ExtendedWireTypeMask) | ((byte)value & ExtendedWireTypeMask));
        }

        /// <summary>
        /// Returns <see langword="true"/> if this field represents a value of the expected type, <see langword="false"/> otherwise.
        /// </summary>
        /// <remarks>
        /// If this value is <see langword="false"/>, this tag and field id must be followed by a type specification.
        /// </remarks>
        public SchemaType SchemaType
        {
            get => (SchemaType)(this.tag & SchemaTypeMask);
            set => this.tag = (byte)((this.tag & ~SchemaTypeMask) | ((byte)value & SchemaTypeMask));
        }

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="SchemaType"/> is valid, <see langword="false"/> otherwise.
        /// </summary>
        public bool IsSchemaTypeValid => this.WireType != WireType.Extended;

        /// <summary>
        /// Returns the <see cref="FieldIdDelta"/> of the field represented by this tag.
        /// </summary>
        /// <remarks>
        /// If <see cref="IsFieldIdValid"/> is <see langword="false"/>, this value is not a complete field id delta.
        /// </remarks>
        public uint FieldIdDelta
        {
            get => (uint)(this.tag & FieldIdMask);
            set => this.tag = (byte)((this.tag & ~FieldIdMask) | ((byte)value & FieldIdMask));
        }

        /// <summary>
        /// Clears the <see cref="FieldIdDelta"/>.
        /// </summary>
        public void ClearFieldId() => this.tag = (byte)(this.tag & ~FieldIdMask);

        /// <summary>
        /// Invalidates <see cref="FieldIdDelta"/>.
        /// </summary>
        public void SetFieldIdInvalid() => this.tag |= FieldIdCompleteMask;

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="FieldIdDelta"/> represents a complete id, <see langword="false"/> if more data is required.
        /// </summary>
        /// <remarks>
        /// If all bits are set in the field id portion of the tag, this field id is not valid and this tag must be followed by a field id.
        /// Therefore, field ids 0-7 can be represented without additional bytes.
        /// </remarks>
        public bool IsFieldIdValid => (this.tag & FieldIdCompleteMask) != FieldIdCompleteMask && this.WireType != WireType.Extended;

        /// <summary>
        /// Returns <see langword="true"/> if this tag must be followed by a field id.
        /// </summary>
        public bool HasExtendedFieldId => (this.tag & FieldIdCompleteMask) == FieldIdCompleteMask && this.WireType != WireType.Extended;
    }
}