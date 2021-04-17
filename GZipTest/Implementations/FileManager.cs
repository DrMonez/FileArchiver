using GZipTest.Intetfaces;
using System;
using System.IO;

namespace GZipTest.Implementations
{
    internal class FileManager : IFileManager
    {
        private FileStream _fileStream;
        private object _fileStreamLocker = new object();
        private FileInfo _fileInfo;
        private bool _disposed = false;
        private int _blockIndex = 0;

        public bool CanRead => _fileStream.CanRead && _fileStream.Position < _fileStream.Length;

        public bool CanWrite => _fileStream.CanWrite;

        private delegate IByteBlock ReadFunction();
        private delegate void WriteFunction(IByteBlock byteBlock);

        private ReadFunction _Read;
        private WriteFunction _Write;


        public FileManager(FileInfo fileInfo, FileManagerMode mode)
        {
            _fileInfo = fileInfo;
            switch(mode)
            {
                case FileManagerMode.Read:
                    _Read = ReadByteBlock;
                    _fileStream = _fileInfo.OpenRead();
                    break;
                case FileManagerMode.ReadArchive:
                    _Read = ReadByteBlockFromArchive;
                    _fileStream = _fileInfo.OpenRead();
                    break;
                case FileManagerMode.Write:
                    _Write = WriteByteBlock;
                    _fileStream = _fileInfo.Create();
                    break;
                case FileManagerMode.WriteArchive:
                    _Write = WriteByteBlockToArchive;
                    _fileStream = _fileInfo.Create();
                    break;
                default:
                    throw new NotSupportedException("The file manager mode is not supported.");
            }
        }

        public IByteBlock Read()
        {
            lock (_fileStreamLocker)
            {
                return _Read();
            }
        }

        public void Write(IByteBlock byteBlock)
        {
            lock (_fileStreamLocker)
            {
                _Write(byteBlock);
            }
        }

        private IByteBlock ReadByteBlock()
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("The file couldn't be read.");
            }
            var nextPosition = _fileStream.Position + DataConfiguration.DefaultByteBlockSize;
            var blockSize = nextPosition > _fileInfo.Length ? _fileInfo.Length - _fileStream.Position : nextPosition - _fileStream.Position;

            var byteBlock = new ByteBlock(_blockIndex++, blockSize);
            _fileStream.Read(byteBlock.Buffer, 0, byteBlock.BufferSize);
            return byteBlock;
        }

        private IByteBlock ReadByteBlockFromArchive()
        {
            if (!CanRead)
            {
                throw new InvalidOperationException("The file couldn't be read.");
            }
            byte[] binaryFinalByteBlockSize = new byte[sizeof(int)];
            byte[] binaryIndexByteBlock = new byte[sizeof(int)];
            _fileStream.Read(binaryFinalByteBlockSize, 0, binaryFinalByteBlockSize.Length);
            _fileStream.Read(binaryIndexByteBlock, 0, binaryIndexByteBlock.Length);

            var byteBlock = new ByteBlock(BitConverter.ToInt32(binaryIndexByteBlock), BitConverter.ToInt32(binaryFinalByteBlockSize));
            _fileStream.Read(byteBlock.Buffer, 0, byteBlock.BufferSize);

            return byteBlock;
        }

        private void WriteByteBlockToArchive(IByteBlock byteBlock)
        {
            if(!CanWrite)
            {
                throw new InvalidOperationException("The file couldn't be written.");
            }
            _fileStream.Write(BitConverter.GetBytes(byteBlock.BufferSize), 0, sizeof(int));
            _fileStream.Write(BitConverter.GetBytes(byteBlock.Index), 0, sizeof(int));
            _fileStream.Write(byteBlock.Buffer, 0, byteBlock.BufferSize);
        }

        private void WriteByteBlock(IByteBlock byteBlock)
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException("The file couldn't be written.");
            }
            _fileStream.Write(byteBlock.Buffer, 0, byteBlock.BufferSize);
        }

        ~FileManager() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _fileStream.Dispose();
            _disposed = true;
        }
    }
}
