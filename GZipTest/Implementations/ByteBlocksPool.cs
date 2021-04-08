using GZipTest.Intetfaces;
using System;
using System.Collections.Generic;

namespace GZipTest.Implementations
{
    internal class ByteBlocksPool : IByteBlocksPool
    {
        private static readonly int _capacity = Environment.ProcessorCount * 30;

        private Dictionary<long, IByteBlock> _byteBlocks = new Dictionary<long, IByteBlock>(_capacity);
        private object _byteBlocksLocker = new object();
        private int _nextIndex = 0;

        public bool IsEmpty => _byteBlocks.Count == 0;
        public bool IsFull => !(Count < MaxSize);
        public int MaxSize => _capacity - Environment.ProcessorCount;
        public int Count
        {
            get
            {
                int count;
                lock (_byteBlocksLocker)
                {
                    count =_byteBlocks.Count;
                }
                return count;
            }
        }

        public event Action OnFreedSpace;
        public event Action OnNoLongerEmpty;

        public IByteBlock GetNext()
        {
            IByteBlock byteBlock;

            lock (_byteBlocksLocker)
            {
                if (_byteBlocks.Count == 0 || !_byteBlocks.TryGetValue(_nextIndex, out byteBlock))
                {
                    return null;
                }
                _byteBlocks.Remove(_nextIndex++);
            }
            OnFreedSpace?.Invoke();

            return byteBlock;
        }

        public void Add(IByteBlock byteBlock)
        {
            if(Count >= _capacity)
            {
                throw new OverflowException("The pool is overflow.");
            }
            lock(_byteBlocksLocker)
            {
                _byteBlocks.Add(byteBlock.Index, byteBlock);
            }
            OnNoLongerEmpty?.Invoke();
        }
    }
}
