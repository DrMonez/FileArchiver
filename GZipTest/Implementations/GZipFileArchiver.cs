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
        private static readonly int _compressionRatio = 1000;

        private IThreadsPool _threadsPool;
        private IFileManager _inputManager;
        private IFileManager _outputManager;
        private IByteBlocksPool _initialByteBlocksPool;
        private IByteBlocksPool _finalByteBlocksPool;

        private Thread _readingThread;
        private Thread _writingThread;
        private AutoResetEvent _writingThreadEndingWaiter = new AutoResetEvent(true);


        public string DestinationFileExtension => ".gz";

        public void Compress(FileInfo fileToCompress, FileInfo compressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Compressing {fileToCompress.Name}...");
            using (_inputManager = new FileManager(fileToCompress, FileManagerMode.Read))
            using (_outputManager = new FileManager(compressedFile, FileManagerMode.WriteArchive))
            {
                _initialByteBlocksPool = new ByteBlocksPool();
                _finalByteBlocksPool = new ByteBlocksPool(_initialByteBlocksPool.MaxSize * _compressionRatio);
                _writingThreadEndingWaiter.WaitOne();
                InitThreads();
                
                _threadsPool = new ThreadsPool(
                    () => _readingThread.IsAlive || !_initialByteBlocksPool.IsEmpty,
                    () =>
                    {
                        var block = _initialByteBlocksPool.GetNext();
                        if (block == null)
                        {
                            return;
                        }
                        var compressedBlock = CompressByteBlock(block);
                        _finalByteBlocksPool.Add(compressedBlock.Index, compressedBlock);
                    }
                );
                _writingThreadEndingWaiter.Set();
                _threadsPool.Start();
                _writingThreadEndingWaiter.WaitOne();
            }
            ConsoleHelper.WriteInfoMessage($"Compressed {fileToCompress.Name} from {fileToCompress.Length} to {compressedFile.Length} bytes.");
        }

        public void Decompress(FileInfo fileToDecompress, FileInfo decompressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Decompressing {fileToDecompress.Name}...");
            using (_inputManager = new FileManager(fileToDecompress, FileManagerMode.ReadArchive))
            using (_outputManager = new FileManager(decompressedFile, FileManagerMode.Write))
            {
                _finalByteBlocksPool = new ByteBlocksPool();
                _initialByteBlocksPool = new ByteBlocksPool(_finalByteBlocksPool.MaxSize * _compressionRatio);
                _writingThreadEndingWaiter.WaitOne();
                InitThreads();
                
                _threadsPool = new ThreadsPool(
                    () => _readingThread.IsAlive || !_initialByteBlocksPool.IsEmpty,
                    () =>
                    {
                        var block = _initialByteBlocksPool.GetNext();
                        if (block == null)
                        {
                            return;
                        }
                        var decompressedBlock = DecompressByteBlock(block);
                        _finalByteBlocksPool.Add(decompressedBlock.Index, decompressedBlock);
                    }
                );
                _writingThreadEndingWaiter.Set();
                _threadsPool.Start();
                _writingThreadEndingWaiter.WaitOne();
            }
            ConsoleHelper.WriteInfoMessage($"Decompressed {fileToDecompress.Name} from {fileToDecompress.Length} to {decompressedFile.Length} bytes.");
        }

        private IByteBlock CompressByteBlock(IByteBlock byteBlock)
        {
            using var memoryStream = new MemoryStream();
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Compress);
            compressStream.Write(byteBlock.Buffer, 0, byteBlock.BufferSize);
            return new ByteBlock(byteBlock.Index, memoryStream.Length) { Buffer = memoryStream.ToArray() };
        }

        private IByteBlock DecompressByteBlock(IByteBlock byteBlock)
        {
            using var memoryStream = new MemoryStream(byteBlock.Buffer);
            using var compressStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            var resultByteBlock = new ByteBlock(byteBlock.Index, DataConfiguration.DefaultByteBlockSize);
            compressStream.Read(resultByteBlock.Buffer, 0, resultByteBlock.BufferSize);
            return resultByteBlock;
        }

        private void InitThreads()
        {
            _readingThread = new Thread(Read);
            _readingThread.Name = "Read Thread";
            _readingThread.Start();

            _writingThread = new Thread(Write);
            _writingThread.Name = "Write Thread";
            _writingThread.Start();
        }

        private void Read()
        {
            while(_inputManager.CanRead)
            {
                var byteBlock = _inputManager.Read();
                _initialByteBlocksPool.Add(byteBlock.Index, byteBlock);
            }
        }

        private void Write()
        {
            _writingThreadEndingWaiter.WaitOne();
            while (_threadsPool.IsWorking || !_finalByteBlocksPool.IsEmpty || !_initialByteBlocksPool.IsEmpty || _readingThread.IsAlive)
            {
                var block = _finalByteBlocksPool.GetNext();
                if (block == null)
                {
                    continue;
                }
                _outputManager.Write(block);
            }
            _writingThreadEndingWaiter.Set();
        }
    }
}
