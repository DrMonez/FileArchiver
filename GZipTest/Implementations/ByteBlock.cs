using GZipTest.Intetfaces;

namespace GZipTest.Implementations
{
    internal class ByteBlock : IByteBlock
    {
        public int Index { get; set; }
        public int BufferSize => Buffer.Length;
        public byte[] Buffer { get; set; }

        public ByteBlock(int number, long initialByteBlockSize)
        {
            Index = number;
            Buffer = new byte[initialByteBlockSize];
        }
    }
}
