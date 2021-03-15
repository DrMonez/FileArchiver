using GZipTest.Intetfaces;
using System.IO;

namespace GZipTest.Helpers
{
    internal static class FileStreamHelper
    {
        public static void WriteToPosition(this FileStream fileStream, long position, byte[] array, int count, int offset = 0)
        {
            fileStream.Position = position;
            fileStream.Write(array, offset, count);
        }

        public static void WriteByteBlock(this FileStream fileStream, IByteBlock byteBlock)
        {
            fileStream.WriteToPosition(byteBlock.StartPosition, byteBlock.FinalByteBlock, byteBlock.FinalByteBlockSize);
        }
    }
}
