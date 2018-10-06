using System.Buffers;

namespace Hagar.TestKit
{
    public interface IOutputBuffer
    {
        ReadOnlySequence<byte> GetReadOnlySequence(int maxSegmentSize);
    }
}