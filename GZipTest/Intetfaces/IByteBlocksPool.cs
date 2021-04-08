using System;

namespace GZipTest.Intetfaces
{
    internal interface IByteBlocksPool
    {
        event Action OnFreedSpace;
        event Action OnNoLongerEmpty;
        bool IsEmpty { get; }
        bool IsFull { get; }
        int MaxSize { get; }
        int Count { get; }
        IByteBlock GetNext();
        void Add(IByteBlock byteBlock);
    }
}
