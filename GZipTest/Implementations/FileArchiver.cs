using GZipTest.Helpers;
using GZipTest.Implementations;
using GZipTest.Intetfaces;
using System.IO;

namespace GZipTest
{
    public class FileArchiver : IFileArchiver
    {
        private IByteBlocksPool _byteBlocksPool;
        private IThreadsPool _threadsPool;
        private IFileManager _outputManager;

        public override string DestinationFileExtension => ".gz";

        public override void Compress(FileInfo fileToCompress, FileInfo compressedFile)
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
                    block.Compress();
                    if (_outputManager.CanWrite)
                    {
                        _outputManager.Write(block);
                    }
                });
                _threadsPool.Start();
            }
            ConsoleHelper.WriteInfoMessage($"Compressed {fileToCompress.Name} from {fileToCompress.Length} to {compressedFile.Length} bytes.");
        }

        public override void Decompress(FileInfo fileToDecompress, FileInfo decompressedFile)
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
                    block.Decompress();
                    if (_outputManager.CanWrite)
                    {
                        _outputManager.Write(block);
                    }
                });
                _threadsPool.Start();
            }
            ConsoleHelper.WriteInfoMessage($"Decompressed {fileToDecompress.Name} from {fileToDecompress.Length} to {decompressedFile.Length} bytes.");
        }
    }
}
