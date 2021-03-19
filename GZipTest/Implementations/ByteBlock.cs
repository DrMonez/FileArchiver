using GZipTest.Intetfaces;

namespace GZipTest.Implementations
{
    internal class ByteBlock : IByteBlock
    {
        public int InitialByteBlockSize => InitialByteBlock.Length;

        public int FinalByteBlockSize => FinalByteBlock.Length;

        public long StartPosition { get; set; }
        public byte[] InitialByteBlock { get; set; }
        public byte[] FinalByteBlock { get; set; }


        public ByteBlock(long startPosition)
        {
            StartPosition = startPosition;
            InitialByteBlock = new byte[DataConfiguration.DefaultByteBlockSize];
        }

        public ByteBlock(long startPosition, long initialByteBlockSize)
        {
            StartPosition = startPosition;
            InitialByteBlock = new byte[initialByteBlockSize];
        }
    }
}
