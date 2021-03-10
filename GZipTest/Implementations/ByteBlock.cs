using GZipTest.Intetfaces;
using System;

namespace GZipTest.Implementations
{
    internal class ByteBlock : IByteBlock
    {
        public ByteBlock(long startPosition)
        {
            StartPosition = startPosition;
            InitialByteBlock = new byte[DefaultByteBlockSize];
        }

        public ByteBlock(long startPosition, long initialByteBlockSize)
        {
            StartPosition = startPosition;
            InitialByteBlock = new byte[initialByteBlockSize];
        }
    }
}
