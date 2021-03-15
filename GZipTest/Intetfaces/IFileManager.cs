using System;

namespace GZipTest.Intetfaces
{
    internal interface IFileManager : IDisposable
    {
        bool CanRead { get; }
        bool CanWrite { get; }
        IByteBlock Read();
        void Write(IByteBlock byteBlock);
    }

    enum FileManagerMode
    {
        Read,
        Write,
        ReadArchive,
        WriteArchive
    }
}
