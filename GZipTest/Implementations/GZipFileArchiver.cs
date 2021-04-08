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
        private IThreadsPool _threadsPool;
        private IFileManager _inputManager;
        private IFileManager _outputManager;
        private IByteBlocksPool _initialByteBlocksPool = new ByteBlocksPool();
        private IByteBlocksPool _finalByteBlocksPool = new ByteBlocksPool();
        private AutoResetEvent _finalByteBlocksPoolAddLocker = new AutoResetEvent(true);

        private Thread _readingThread;
        private AutoResetEvent _readingThreadLocker = new AutoResetEvent(true);
        private Thread _writingThread;
        private AutoResetEvent _writingThreadLocker = new AutoResetEvent(false);
        private AutoResetEvent _writingThreadEndingWaiter = new AutoResetEvent(true);


        public string DestinationFileExtension => ".gz";

        public void Compress(FileInfo fileToCompress, FileInfo compressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Compressing {fileToCompress.Name}...");
            using (_inputManager = new FileManager(fileToCompress, FileManagerMode.Read))
            {
                InitThreads();
                using (_outputManager = new FileManager(compressedFile, FileManagerMode.WriteArchive))
                {
                    InitHandlers();
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
                            _finalByteBlocksPoolAddLocker.WaitOne();
                            _finalByteBlocksPool.Add(compressedBlock);
                            if (!_finalByteBlocksPool.IsFull)
                            {
                                _finalByteBlocksPoolAddLocker.Set();
                            }
                        }
                    );
                    _threadsPool.Start();
                    _writingThreadEndingWaiter.WaitOne();
                }
            }
            ConsoleHelper.WriteInfoMessage($"Compressed {fileToCompress.Name} from {fileToCompress.Length} to {compressedFile.Length} bytes.");
        }

        public void Decompress(FileInfo fileToDecompress, FileInfo decompressedFile)
        {
            ConsoleHelper.WriteProcessMessage($"Decompressing {fileToDecompress.Name}...");
            using (_inputManager = new FileManager(fileToDecompress, FileManagerMode.ReadArchive))
            {
                InitThreads();
                using (_outputManager = new FileManager(decompressedFile, FileManagerMode.Write))
                {
                    InitHandlers();
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
                            _finalByteBlocksPoolAddLocker.WaitOne();
                            _finalByteBlocksPool.Add(decompressedBlock);
                            if(!_finalByteBlocksPool.IsFull)
                            {
                                _finalByteBlocksPoolAddLocker.Set();
                            }
                        }
                    );
                    _threadsPool.Start();
                    _writingThreadEndingWaiter.WaitOne();
                }
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

        private void InitHandlers()
        {
            _initialByteBlocksPool.OnFreedSpace += RunReading;
            _finalByteBlocksPool.OnNoLongerEmpty += RunWriting;
            _finalByteBlocksPool.OnFreedSpace += AllowAddByteBlockInFinalPool;
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
                _readingThreadLocker.WaitOne();
                var byteBlock = _inputManager.Read();
                _initialByteBlocksPool.Add(byteBlock);
                if (!_initialByteBlocksPool.IsFull)
                {
                    _readingThreadLocker.Set();
                }
            }
        }

        private void Write()
        {
            _writingThreadEndingWaiter.WaitOne();
            while (!_finalByteBlocksPool.IsEmpty || !_initialByteBlocksPool.IsEmpty || _readingThread.IsAlive)
            {
                _writingThreadLocker.WaitOne();
                var block = _finalByteBlocksPool.GetNext();
                if(!_finalByteBlocksPool.IsEmpty)
                {
                    _writingThreadLocker.Set();
                }
                if (block == null)
                {
                    continue;
                }
                _outputManager.Write(block);
            }
            _writingThreadEndingWaiter.Set();
        }

        private void RunReading()
        {
            _readingThreadLocker.Set();
        }

        private void RunWriting()
        {
            _writingThreadLocker.Set();
        }

        private void AllowAddByteBlockInFinalPool()
        {
            _finalByteBlocksPoolAddLocker.Set();
        }
    }
}
