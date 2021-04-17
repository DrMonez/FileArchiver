using System;

namespace GZipTest.Intetfaces
{
    internal interface IByteBlocksPool
    {
        bool IsEmpty { get; }
        bool IsFull { get; }
        int MaxSize { get; }
        int Count { get; }
        IByteBlock GetNext();
        void Add(int index, IByteBlock byteBlock);
    }
}
