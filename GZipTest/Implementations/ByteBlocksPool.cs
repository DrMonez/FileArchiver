using GZipTest.Intetfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GZipTest.Implementations
{
    internal class ByteBlocksPool : IByteBlocksPool
    {
        private static readonly int _capacity = Environment.ProcessorCount * 30;

        private IFileManager _readerManager;
        private Queue<IByteBlock> _byteBlocks = new Queue<IByteBlock>(_capacity);
        private object _byteBlocksLocker = new object();
        private bool _disposed = false;
        private Thread _poolThread;
        private AutoResetEvent _poolThreadLocker = new AutoResetEvent(true);

        private bool _IsEmpty => _byteBlocks.Count == 0 && (_poolThread == null || !_poolThread.IsAlive) && !_readerManager.CanRead;

        public bool IsEmpty
        {
            get
            {
                bool isEmty;
                lock (_byteBlocksLocker)
                {
                    isEmty = _IsEmpty;
                }
                return isEmty;
            }
        }

        public ByteBlocksPool(FileInfo fileToRead, FileManagerMode mode)
        {
            _readerManager = new FileManager(fileToRead, mode);
            
            // КОСТЫЛЬ
            while(_byteBlocks.Count < _capacity && _readerManager.CanRead)
            {
                _byteBlocks.Enqueue(_readerManager.Read());
            }    

            _poolThread = new Thread(FillIn);
            _poolThread.Name = "Read Thread";
            _poolThread.Start();
        }

        public IByteBlock GetNext()
        {
            IByteBlock byteBlock;
            double queueFullness;
            lock (_byteBlocksLocker)
            {
                if (_byteBlocks.Count == 0)
                {
                    return null;
                }

                byteBlock = _byteBlocks.Dequeue();
                queueFullness = (double)_byteBlocks.Count / _capacity;
            }
            if (_poolThread.IsAlive)
            {
                _poolThreadLocker.Set();
            }

            return byteBlock;
        }

        ~ByteBlocksPool() => CommonDispose();

        public void Dispose()
        {
            CommonDispose();
            GC.SuppressFinalize(this);
        }

        protected virtual void CommonDispose()
        {
            if (_disposed)
            {
                return;
            }
            _readerManager.Dispose();
            _disposed = true;
        }

        private void FillIn()
        {
            int byteBlocksCount;
            lock (_byteBlocksLocker)
            {
                byteBlocksCount = _byteBlocks.Count;
            }
            while (_readerManager.CanRead)
            {
                _poolThreadLocker.WaitOne();
                var byteBlock = _readerManager.Read();
                lock (_byteBlocksLocker)
                {
                    _byteBlocks.Enqueue(byteBlock);
                    byteBlocksCount = _byteBlocks.Count;
                }
                if(byteBlocksCount < _capacity)
                {
                    _poolThreadLocker.Set();
                }
            }
        }
    }
}
