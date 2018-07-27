using System;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    /// <summary>
    /// Marker object used to denote an unknown field and its position into a stream of data.
    /// </summary>
    public class UnknownFieldMarker
    {
        public UnknownFieldMarker(Field field, SequencePosition position)
        {
            this.Field = field;
            this.Position = position;
        }

        /// <summary>
        /// The position into the stream at which this field occurs.
        /// </summary>
        public SequencePosition Position { get; }

        /// <summary>
        /// The field header.
        /// </summary>
        public Field Field { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{nameof(UnknownFieldMarker)}] {nameof(this.Position)}: {this.Position}, {nameof(this.Field)}: {this.Field}";
        }
    }
}