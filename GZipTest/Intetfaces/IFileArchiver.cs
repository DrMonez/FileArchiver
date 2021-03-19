using System.IO;

namespace GZipTest.Intetfaces
{
    public interface IFileArchiver
    {
        public abstract string DestinationFileExtension { get; }
        public abstract void Compress(FileInfo fileToCompress, FileInfo compressedFile);
        public abstract void Decompress(FileInfo fileToDecompress, FileInfo decompressedFile);
    }
}
