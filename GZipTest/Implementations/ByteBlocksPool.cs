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
        private object _poolThreadLocker = new object();

        private delegate void _OnFreePlaceInPoolHandler();
        private event _OnFreePlaceInPoolHandler _OnFreePlaceInPool;

        private bool _IsEmpty => _byteBlocks.Count == 0 && (_poolThread == null || !_poolThread.IsAlive) && !_readerManager.CanRead;

        public bool IsEmpty
        {
            get
            {
                lock(_poolThreadLocker)
                lock(_byteBlocksLocker)
                {
                    return _IsEmpty;
                }
            }
        }

        public ByteBlocksPool(FileInfo fileToRead, FileManagerMode mode)
        {
            _readerManager = new FileManager(fileToRead, mode); 
            FillIn();

            _OnFreePlaceInPool += ByteBlocksPool__OnPlaceInPool;
        }

        public IByteBlock GetNext()
        {
            IByteBlock byteBlock;
            double queueFullness;
            bool isThreadWorking = false;
            
            lock(_poolThreadLocker)
            {
                isThreadWorking = _poolThread != null && _poolThread.IsAlive;
            }
            lock (_byteBlocksLocker)
            {
                if (_IsEmpty)
                {
                    return null;
                }
                try
                {
                    byteBlock = _byteBlocks.Dequeue();
                }
                catch(InvalidOperationException ex)
                {
                    return null;
                }
                queueFullness = (double)_byteBlocks.Count / _capacity;
            }
            if (!isThreadWorking)
            {
                _OnFreePlaceInPool?.Invoke();
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
            if (!_readerManager.CanRead)
            {
                _OnFreePlaceInPool -= ByteBlocksPool__OnPlaceInPool;
                return;
            }

            int byteBlocksCount;
            lock (_byteBlocksLocker)
            {
                byteBlocksCount = _byteBlocks.Count;
            }
            while (byteBlocksCount < _capacity && _readerManager.CanRead)
            {
                var byteBlock = _readerManager.Read();
                lock (_byteBlocksLocker)
                {
                    _byteBlocks.Enqueue(byteBlock);
                    byteBlocksCount = _byteBlocks.Count;
                }
            }
        }

        private void ByteBlocksPool__OnPlaceInPool()
        {
            lock (_poolThreadLocker)
            {
                if (_poolThread == null || !_poolThread.IsAlive)
                {
                    _poolThread = new Thread(FillIn);
                    _poolThread.Name = "Pool thread";
                    _poolThread.Start();
                }
            }
        }
    }
}
