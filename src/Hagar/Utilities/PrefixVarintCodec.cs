using System;
using System.Collections.Generic;
using System.Text;
using Hagar.Buffers;

namespace Hagar.Utilities
{
    public static class PrefixVarintCodec
    {
        public static void WritePrefixVarint(ref Writer writer, ulong value)
        {
            // numBytes = Count leading zeros
            // Ensure numBytes contiguous bytes
            // Write from MSB to LSB
            // S
        }

        public static ulong ReadPrefixVarint(ref Reader reader)
        {
            // numBytes = Count leading ones
            // Ensure numBytes contiguous bytes
            return 0;
        }
    }
}