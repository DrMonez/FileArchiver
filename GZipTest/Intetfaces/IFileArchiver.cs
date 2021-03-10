using System;
using System.IO;

namespace GZipTest.Intetfaces
{
    public abstract class IFileArchiver
    {
        public abstract string DestinationFileExtension { get; }
        public abstract void Compress(FileInfo fileToCompress);
        public abstract void Decompress(FileInfo fileToDecompress);
    }
}
