using GZipTest.Intetfaces;
using System;
using System.Threading;

namespace GZipTest.Implementations
{
    internal class ThreadsPool : IThreadsPool
    {
        private static readonly int _maxThreadsCount = Environment.ProcessorCount;
        private static Semaphore _activeThreadsSemaphore = new Semaphore(_maxThreadsCount, _maxThreadsCount);
        private static readonly int _maxHandlersCount = 64;
        private AutoResetEvent[] _autoHandlers;

        private FuncToParallel _FuncToParallel;
        private FunkToCheckCycleEnd _FunkToCheckCycleEnd;

        public delegate void FuncToParallel();
        public delegate bool FunkToCheckCycleEnd();


        public ThreadsPool(FunkToCheckCycleEnd funkToCheckCycleEnd, FuncToParallel funcToParallel)
        {
            _FuncToParallel = funcToParallel;
            InitAutoHandlers(_maxHandlersCount);
            _FunkToCheckCycleEnd = funkToCheckCycleEnd;
        }

        public void Start()
        {
            for (int i = 0, index = 0; _FunkToCheckCycleEnd(); i++, index = i % _autoHandlers.Length)
            {
                _autoHandlers[index].WaitOne();
                _activeThreadsSemaphore.WaitOne();
                var thread = new Thread((i) =>
                {
                    var index = (int)i;
                    _FuncToParallel();
                    _autoHandlers[index].Set();
                    _activeThreadsSemaphore.Release();
                });
                thread.Name = $"Thread {i}";
                thread.Start(index);
            }
            WaitHandle.WaitAll(_autoHandlers);
        }

        private void InitAutoHandlers(int size)
        {
            _autoHandlers = new AutoResetEvent[size];
            for (var i = 0; i< _autoHandlers.Length; i++)
            {
                _autoHandlers[i] = new AutoResetEvent(true);
            }
        }
    }
}
