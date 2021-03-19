namespace GZipTest.Intetfaces
{
    internal interface IByteBlock
    {
        int InitialByteBlockSize { get; }
        int FinalByteBlockSize { get; }
        long StartPosition { get; set; }
        byte[] InitialByteBlock { get; set; }
        byte[] FinalByteBlock { get; set; }
    }
}
