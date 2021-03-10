using GZipTest.Helpers;
using GZipTest.Implementations;
using GZipTest.Intetfaces;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace GZipTest
{
    public class FileArchiver : IFileArchiver
    {
        private int _maxThreadsCount = Environment.ProcessorCount;
        private int _activeThreadsCount;
        private object _activeThreadsCountLocker = new object();
        private FileStream _inputFileStream;
        private FileStream _outputFileStream;
        private object _inputFileStreamLocker = new object();
        private object _outputFileStreamLocker = new object();
        private ConcurrentDictionary<int, AutoResetEvent> _autoHandlers = new ConcurrentDictionary<int, AutoResetEvent>();
        private int _maxHandlersCount = 64;

        public override string DestinationFileExtension => ".gz";

        public override void Compress(FileInfo fileToCompress)
        {
            using (_inputFileStream = fileToCompress.OpenRead())
            using (_outputFileStream = File.Create(fileToCompress.FullName + ".gz"))
            {
                var fileSize = fileToCompress.Length;
                var blocksCount = (int)Math.Ceiling((float)fileSize / IByteBlock.DefaultByteBlockSize);
                var inputFileStreamPosition = _inputFileStream.Position;
                var inputFileStreamPositionLock = new object();
                for (var i = 0; i < blocksCount; i++)
                {
                    while(_activeThreadsCount >= _maxThreadsCount)
                    {
                        Thread.Sleep(100);
                    }
                    var thread = new Thread((i) =>
                    {
                        var index = (int)i;
                        var handler = _autoHandlers[index];
                        
                        handler.WaitOne();
                        CompressByteBlock(ref inputFileStreamPosition, inputFileStreamPositionLock, fileSize);
                        handler.Set();
                        lock(_activeThreadsCountLocker)
                        {
                            _activeThreadsCount--;
                        }
                    });
                    var index = i % _maxHandlersCount;
                    thread.Name = $"Thread {index}";
                    var handler = new AutoResetEvent(true);
                    _autoHandlers.AddOrUpdate(index, handler, (i, firstHandler) => { return handler; });
                    thread.Start(index);
                    lock (_activeThreadsCountLocker)
                    {
                        _activeThreadsCount++;
                    }
                }
                AutoResetEvent.WaitAll(_autoHandlers.Select(x => x.Value).ToArray());
                FileInfo info = new FileInfo(fileToCompress.FullName + ".gz");
                ConsoleHelper.WriteInfoMessage($"Compressed {fileToCompress.Name} from {fileToCompress.Length} to {info.Length} bytes.");
            }
        }

        public override void Decompress(FileInfo fileToDecompress)
        {
            string currentFileName = fileToDecompress.FullName
                                                            .Replace(fileToDecompress.Name,
                                                                    "decompressed_" + fileToDecompress.Name);
            string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

            using (_inputFileStream = fileToDecompress.OpenRead())
            using (_outputFileStream = File.Create(newFileName))
            {
                var readPosition = _inputFileStream.Position;
                while (readPosition != _inputFileStream.Length)
                {
                    readPosition = ReadByteBlockFromArchive(out var byteBlock, readPosition);
                    byteBlock.Decompress();
                    _outputFileStream.WriteByteBlock(byteBlock);
                }
                ConsoleHelper.WriteInfoMessage($"Decompressed {fileToDecompress.Name}");
            }
        }

        private void CompressByteBlock(ref long inputFileStreamPosition, object inputFileStreamPositionLock, long fileSize)
        {
            IByteBlock byteBlock;
            lock (inputFileStreamPositionLock)
            {
                var nextPosition = inputFileStreamPosition + IByteBlock.DefaultByteBlockSize;
                var blockSize = nextPosition > fileSize ? fileSize - inputFileStreamPosition : nextPosition - inputFileStreamPosition;
                byteBlock = new ByteBlock(inputFileStreamPosition, blockSize);
                inputFileStreamPosition = byteBlock.StartPosition + byteBlock.InitialByteBlockSize;
            }
            lock (_inputFileStreamLocker)
            {
                _inputFileStream.ReadFromPosition(byteBlock.StartPosition, byteBlock.InitialByteBlock, 0, byteBlock.InitialByteBlockSize);
            }
            byteBlock.Compress();
            WriteByteBlockInArchive(byteBlock);
        }

        private void WriteByteBlockInArchive(IByteBlock byteBlock)
        {
            lock (_outputFileStreamLocker)
            {
                _outputFileStream.Write(BitConverter.GetBytes(byteBlock.FinalByteBlockSize), 0, sizeof(int));
                _outputFileStream.Write(BitConverter.GetBytes(byteBlock.StartPosition), 0, sizeof(long));
                _outputFileStream.Write(byteBlock.FinalByteBlock, 0, byteBlock.FinalByteBlockSize);
            }
        }

        private long ReadByteBlockFromArchive(out IByteBlock byteBlock, long readPosition)
        {
            byte[] binaryFinalByteBlockSize = new byte[sizeof(int)];
            byte[] binaryStartPositionByteBlock = new byte[sizeof(long)];
            _inputFileStream.ReadFromPosition(readPosition, binaryFinalByteBlockSize, 0, binaryFinalByteBlockSize.Length);
            readPosition = _inputFileStream.Position;
            _inputFileStream.ReadFromPosition(readPosition, binaryStartPositionByteBlock, 0, binaryStartPositionByteBlock.Length);
            readPosition = _inputFileStream.Position;

            var finalByteBlockSize = BitConverter.ToInt32(binaryFinalByteBlockSize);
            byteBlock = new ByteBlock(BitConverter.ToInt64(binaryStartPositionByteBlock), finalByteBlockSize);

            _inputFileStream.ReadFromPosition(readPosition, byteBlock.InitialByteBlock, 0, finalByteBlockSize);
            readPosition = _inputFileStream.Position;

            return readPosition;
        }
    }
}
