using GZipTest.Helpers;
using GZipTest.Implementations;
using GZipTest.Intetfaces;
using System.IO;
using System.IO.Compression;

namespace GZipTest
{
    public class GZipFileArchiver : IFileArchiver
    {
        private IByteBlocksPool _byteBlocksPool;
        private IThreadsPool _threadsPool;
        private IFileManager _outputManager;

        public string DestinationFileExtension => ".gz";

        public void Compress(FileInfo fileToCompress, FileInfo compressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Compressing {fileToCompress.Name}...");
            using (_byteBlocksPool = new ByteBlocksPool(fileToCompress, FileManagerMode.Read))
            using (_outputManager = new FileManager(compressedFile, FileManagerMode.WriteArchive))
            {
                _threadsPool = new ThreadsPool(() => !_byteBlocksPool.IsEmpty, () => {
                    var block = _byteBlocksPool.GetNext();
                    if (block == null)
                    {
                        return;
                    }
                    CompressByteBlock(block);
                    if (_outputManager.CanWrite)
                    {
                        _outputManager.Write(block);
                    }
                });
                _threadsPool.Start();
            }
            ConsoleHelper.WriteInfoMessage($"Compressed {fileToCompress.Name} from {fileToCompress.Length} to {compressedFile.Length} bytes.");
        }

        public void Decompress(FileInfo fileToDecompress, FileInfo decompressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Decompressing {fileToDecompress.Name}...");
            using (_byteBlocksPool = new ByteBlocksPool(fileToDecompress, FileManagerMode.ReadArchive))
            using (_outputManager = new FileManager(decompressedFile, FileManagerMode.Write))
            {
                _threadsPool = new ThreadsPool(() => !_byteBlocksPool.IsEmpty, () =>
                {
                    var block = _byteBlocksPool.GetNext();
                    if(block == null)
                    {
                        return;
                    }
                    DecompressByteBlock(block);
                    if (_outputManager.CanWrite)
                    {
                        _outputManager.Write(block);
                    }
                });
                _threadsPool.Start();
            }
            ConsoleHelper.WriteInfoMessage($"Decompressed {fileToDecompress.Name} from {fileToDecompress.Length} to {decompressedFile.Length} bytes.");
        }

        private IByteBlock CompressByteBlock(IByteBlock byteBlock)
        {
            using var memoryStream = new MemoryStream();
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Compress);
            compressStream.Write(byteBlock.InitialByteBlock, 0, byteBlock.InitialByteBlockSize);
            byteBlock.FinalByteBlock = memoryStream.ToArray();
            return byteBlock;
        }

        private IByteBlock DecompressByteBlock(IByteBlock byteBlock)
        {
            using var memoryStream = new MemoryStream(byteBlock.InitialByteBlock);
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            byteBlock.FinalByteBlock = new byte[DataConfiguration.DefaultByteBlockSize];
            compressStream.Read(byteBlock.FinalByteBlock, 0, byteBlock.InitialByteBlockSize);
            return byteBlock;
        }
    }
}
