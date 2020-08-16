using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Marker object used to denote an unknown field and its position into a stream of data.
    /// </summary>
    public class UnknownFieldMarker
    {
        public UnknownFieldMarker(Field field, long position)
        {
            Field = field;
            Position = position;
        }

        /// <summary>
        /// The position into the stream at which this field occurs.
        /// </summary>
        public long Position { get; }

        /// <summary>
        /// The field header.
        /// </summary>
        public Field Field { get; }

        /// <inheritdoc />
        public override string ToString() => $"[{nameof(UnknownFieldMarker)}] {nameof(Position)}: {Position}, {nameof(Field)}: {Field}";
    }
}