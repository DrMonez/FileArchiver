namespace GZipTest.Intetfaces
{
    internal abstract class IByteBlock
    {
        public static int DefaultByteBlockSize => 1000000;
        public int InitialByteBlockSize => InitialByteBlock.Length;
        public int FinalByteBlockSize => FinalByteBlock.Length;
        public long StartPosition { get; set; }
        public byte[] InitialByteBlock { get; set; }
        public byte[] FinalByteBlock { get; protected set; }

        public abstract void Compress();
        public abstract void Decompress();
    }
}
