using System.IO;
using System.IO.Compression;

namespace GZipTest.Intetfaces
{
    internal abstract class IByteBlock
    {
        public static int DefaultByteBlockSize => 1000000;
        public int InitialByteBlockSize => InitialByteBlock.Length;
        public int FinalByteBlockSize => FinalByteBlock.Length;
        public long StartPosition { get; set; }
        public byte[] InitialByteBlock { get; set; }
        public byte[] FinalByteBlock { get; private set; }

        public void Compress()
        {
            using var memoryStream = new MemoryStream();
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Compress);
            compressStream.Write(InitialByteBlock, 0, InitialByteBlockSize);
            FinalByteBlock = memoryStream.ToArray();
        }

        public void Decompress()
        {
            using var memoryStream = new MemoryStream(InitialByteBlock);
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            FinalByteBlock = new byte[DefaultByteBlockSize];
            compressStream.Read(FinalByteBlock, 0, InitialByteBlockSize);
        }
    }
}
