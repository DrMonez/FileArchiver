using System;

namespace GZipTest.Intetfaces
{
    internal interface IByteBlocksPool
    {
        event Action OnFreedSpace;
        bool IsEmpty { get; }
        int MaxSize { get; }
        int Count { get; }
        IByteBlock GetNext();
        void Add(IByteBlock byteBlock);
    }
}
