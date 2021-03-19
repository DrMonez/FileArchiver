using GZipTest.Helpers;
using GZipTest.Intetfaces;
using System;
using System.Threading;

namespace GZipTest.Implementations
{
    internal class ThreadsPool : IThreadsPool
    {
        private static readonly int _maxThreadsCount = Environment.ProcessorCount;
        private Thread[] _threads = new Thread[_maxThreadsCount];
        private AutoResetEvent[] _autoHandlers;

        private FuncToParallel _FuncToParallel;
        private FunkToCheckCycleEnd _FunkToCheckCycleEnd;

        public delegate void FuncToParallel();
        public delegate bool FunkToCheckCycleEnd();


        public ThreadsPool(FunkToCheckCycleEnd funkToCheckCycleEnd, FuncToParallel funcToParallel)
        {
            _FuncToParallel = funcToParallel;
            InitAutoHandlers(_maxThreadsCount);
            _FunkToCheckCycleEnd = funkToCheckCycleEnd;
        }

        public void Start()
        {
            for(var i = 0; i < _threads.Length; i++)
            {
                _threads[i] = new Thread(ThreadFunction);
                _threads[i].Name = $"Thread {i}";
                _threads[i].Start(i);
            }
            WaitHandle.WaitAll(_autoHandlers);
        }

        private void ThreadFunction(object i)
        {
            try
            {
                var index = (int)i;
                _autoHandlers[index].WaitOne();
                // Бутылочное горлышко
                while (_FunkToCheckCycleEnd())
                {
                    _FuncToParallel();
                }
                _autoHandlers[index].Set();
            }
            catch(Exception ex)
            {
                ConsoleHelper.WriteErrorMessage(ex.Message);
            }
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
