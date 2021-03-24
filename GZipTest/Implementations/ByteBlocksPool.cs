using GZipTest.Intetfaces;
using System;
using System.Collections.Generic;

namespace GZipTest.Implementations
{
    internal class ByteBlocksPool : IByteBlocksPool
    {
        private static readonly int _capacity = Environment.ProcessorCount * 30;

        private Queue<IByteBlock> _byteBlocks = new Queue<IByteBlock>(_capacity);
        private object _byteBlocksLocker = new object();

        private bool _IsEmpty => _byteBlocks.Count == 0;

        public bool IsEmpty
        {
            get
            {
                return _IsEmpty;
            }
        }

        public int MaxSize => _capacity;

        public int Count
        {
            get
            {
                return _byteBlocks.Count;
            }
        }

        public ByteBlocksPool(IEnumerable<IByteBlock> byteBlocks = null)
        { 
            if(byteBlocks == null)
            {
                return;
            }

            foreach(var byteBlock in byteBlocks)
            {
                _byteBlocks.Enqueue(byteBlock);
            }
        }

        public event Action OnFreedSpace;

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
            OnFreedSpace?.Invoke();

            return byteBlock;
        }

        public void Add(IByteBlock byteBlock)
        {
            if(Count >= MaxSize)
            {
                throw new OverflowException("The pool is overflow");
            }
            lock(_byteBlocksLocker)
            {
                _byteBlocks.Enqueue(byteBlock);
            }
        }
    }
}
