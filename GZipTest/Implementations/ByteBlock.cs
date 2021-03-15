using GZipTest.Intetfaces;
using System.IO;
using System.IO.Compression;

namespace GZipTest.Implementations
{
    internal class ByteBlock : IByteBlock
    {
        public ByteBlock(long startPosition)
        {
            StartPosition = startPosition;
            InitialByteBlock = new byte[DefaultByteBlockSize];
        }

        public ByteBlock(long startPosition, long initialByteBlockSize)
        {
            StartPosition = startPosition;
            InitialByteBlock = new byte[initialByteBlockSize];
        }

        public override void Compress()
        {
            using var memoryStream = new MemoryStream();
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Compress);
            compressStream.Write(InitialByteBlock, 0, InitialByteBlockSize);
            FinalByteBlock = memoryStream.ToArray();
        }

        public override void Decompress()
        {
            using var memoryStream = new MemoryStream(InitialByteBlock);
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            FinalByteBlock = new byte[DefaultByteBlockSize];
            compressStream.Read(FinalByteBlock, 0, InitialByteBlockSize);
        }
    }
}
