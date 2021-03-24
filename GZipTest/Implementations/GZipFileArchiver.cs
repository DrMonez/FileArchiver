using GZipTest.Helpers;
using GZipTest.Implementations;
using GZipTest.Intetfaces;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    public class GZipFileArchiver : IFileArchiver
    {
        private IByteBlocksPool _byteBlocksPool = new ByteBlocksPool();
        private IThreadsPool _threadsPool;
        private IFileManager _inputManager;
        private IFileManager _outputManager;

        private Thread _readingThread;
        private AutoResetEvent _readingThreadLocker = new AutoResetEvent(true);

        public string DestinationFileExtension => ".gz";

        public void Compress(FileInfo fileToCompress, FileInfo compressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Compressing {fileToCompress.Name}...");
            using (_inputManager = new FileManager(fileToCompress, FileManagerMode.Read))
            {
                _readingThread = new Thread(Read);
                _readingThread.Name = "Read Thread";
                _readingThread.Start();

                using (_outputManager = new FileManager(compressedFile, FileManagerMode.WriteArchive))
                {
                    _byteBlocksPool.OnFreedSpace += RunReading;
                    _threadsPool = new ThreadsPool(
                        () => !_byteBlocksPool.IsEmpty || _readingThread.IsAlive,
                        () =>
                        {
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
                        }
                    );
                    _threadsPool.Start();
                }
            }
            ConsoleHelper.WriteInfoMessage($"Compressed {fileToCompress.Name} from {fileToCompress.Length} to {compressedFile.Length} bytes.");
        }

        public void Decompress(FileInfo fileToDecompress, FileInfo decompressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Decompressing {fileToDecompress.Name}...");
            using (_inputManager = new FileManager(fileToDecompress, FileManagerMode.ReadArchive))
            {
                _readingThread = new Thread(Read);
                _readingThread.Name = "Read Thread";
                _readingThread.Start();

                using (_outputManager = new FileManager(decompressedFile, FileManagerMode.Write))
                {
                    _byteBlocksPool.OnFreedSpace += RunReading;
                    _threadsPool = new ThreadsPool(
                        () => !_byteBlocksPool.IsEmpty || _readingThread.IsAlive,
                        () =>
                        {
                            var block = _byteBlocksPool.GetNext();
                            if (block == null)
                            {
                                return;
                            }
                            DecompressByteBlock(block);
                            if (_outputManager.CanWrite)
                            {
                                _outputManager.Write(block);
                            }
                        }
                    );
                    _threadsPool.Start();
                }
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

        private void Read()
        {
            while(_inputManager.CanRead)
            {
                _readingThreadLocker.WaitOne();
                var byteBlock = _inputManager.Read();
                _byteBlocksPool.Add(byteBlock);
                if (_byteBlocksPool.Count < _byteBlocksPool.MaxSize)
                {
                    _readingThreadLocker.Set();
                }
            }
        }

        private void RunReading()
        {
            _readingThreadLocker.Set();
        }
    }
}
