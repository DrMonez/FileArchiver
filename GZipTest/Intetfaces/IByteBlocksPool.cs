using System;

namespace GZipTest.Intetfaces
{
    internal interface IByteBlocksPool : IDisposable
    {
        bool IsEmpty { get; }
        IByteBlock GetNext();
    }
}
