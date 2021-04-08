namespace GZipTest.Intetfaces
{
    internal interface IByteBlock
    {
        int Index { get; set; }
        int BufferSize { get; }
        byte[] Buffer { get; set; }
    }
}
