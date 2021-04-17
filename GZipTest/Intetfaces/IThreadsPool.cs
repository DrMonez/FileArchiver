namespace GZipTest.Intetfaces
{
    internal interface IThreadsPool
    {
        bool IsWorking { get; }
        void Start();
    }
}
