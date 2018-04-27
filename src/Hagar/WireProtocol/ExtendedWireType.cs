namespace Hagar.WireProtocol
{
    public enum ExtendedWireType : byte
    {
        //Null = 0b00 << 3, // This tag signifies that a value is null.
        //Unused = 0b01 << 3, // This tag is not used in this version of the protocol.
        EndTagDelimited = 0b10 << 3, // This tag marks the end of a tag-delimited object.
        EndBaseFields = 0b11 << 3, // This tag marks the end of a base object in a tag-delimited object.
    }
}