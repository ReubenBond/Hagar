using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Marker object used to denote an unknown field and its offset into a stream of data.
    /// </summary>
    public class UnknownFieldMarker
    {
        public UnknownFieldMarker(Field field, int offset)
        {
            this.Field = field;
            this.Offset = offset;
        }

        /// <summary>
        /// The offset into the stream at which this field occurs.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The field header.
        /// </summary>
        public Field Field { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{nameof(UnknownFieldMarker)}] {nameof(this.Offset)}: {this.Offset}, {nameof(this.Field)}: {this.Field}";
        }
    }
}