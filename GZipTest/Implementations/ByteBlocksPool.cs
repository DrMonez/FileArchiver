using GZipTest.Intetfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest.Implementations
{
    internal class ByteBlocksPool : IByteBlocksPool
    {
        private int _capacity = Environment.ProcessorCount * DataConfiguration.DefaultCapacityCoefficient;

        private Dictionary<int, IByteBlock> _byteBlocks;
        private object _byteBlocksLocker = new object();
        private AutoResetEvent _addLocker = new AutoResetEvent(true);
        private int _nextIndex = 0;

        public bool IsEmpty => Count == 0;
        public bool IsFull => !(Count < MaxSize);
        public int MaxSize => _capacity - Environment.ProcessorCount;
        public int Count
        {
            get
            {
                int count;
                lock (_byteBlocksLocker)
                {
                    count = _byteBlocks.Count;
                }
                return count;
            }
        }

        public ByteBlocksPool()
        {
            _byteBlocks = new Dictionary<int, IByteBlock>(_capacity);
        }

        public ByteBlocksPool(int capacity)
        {
            _capacity = capacity;
            _byteBlocks = new Dictionary<int, IByteBlock>(_capacity);
        }

        public IByteBlock GetNext()
        {
            IByteBlock byteBlock;
            lock (_byteBlocksLocker)
            {
                if (_byteBlocks.TryGetValue(_nextIndex, out byteBlock))
                {
                    _byteBlocks.Remove(_nextIndex++);
                    _addLocker.Set();
                }
            }
            return byteBlock;
        }

        public void Add(int index, IByteBlock byteBlock)
        {
            if (IsFull)
            {
                _addLocker.WaitOne();
            }
            lock (_byteBlocksLocker)
            {
                if (_byteBlocks.ContainsKey(index))
                {
                    throw new IndexOutOfRangeException($"There is already a key in the pool: {index.ToString()}");
                }
                _byteBlocks.Add(index, byteBlock);
            }
            if (!IsFull)
            {
                _addLocker.Set();
            }
        }
    }
}
