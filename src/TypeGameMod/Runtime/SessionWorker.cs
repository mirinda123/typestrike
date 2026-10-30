using System;
using System.Threading;

namespace TypeGameMod
{
    // Owns the session thread. Resources used by the loop may be released only
    // after Stop returns; a failed loop stays observable until the session ends.
    internal sealed class SessionWorker
    {
        readonly Thread thread;
        readonly Action work;
        volatile bool stopRequested;
        volatile Exception failure;
        bool started;

        internal SessionWorker(Action work)
        {
            this.work = work;
            thread = new Thread(Run) { IsBackground = true, Name = "TypeGame session" };
        }

        internal bool StopRequested
        {
            get {
                return stopRequested;
            }
        }
        internal Exception Failure
        {
            get {
                return failure;
            }
        }

        internal void Start()
        {
            thread.Start();
            started = true;
        }

        void Run()
        {
            try
            {
                work();
            }
            catch (Exception error)
            {
                failure = error;
            }
        }

        internal void Stop()
        {
            stopRequested = true;
            if (started && thread != Thread.CurrentThread)
                thread.Join();
        }
    }
}
