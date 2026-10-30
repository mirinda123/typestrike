using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using TypeGameMod;

internal static class SessionWorkerTests
{
    static void Check(bool condition, string reason)
    {
        if (!condition)
            throw new Exception(reason);
    }

    internal static void Run()
    {
        using (var entered = new ManualResetEvent(false)) using (
            var mayFinish = new ManualResetEvent(false)) using (var stopReturned =
                                                                    new ManualResetEvent(false))
        {
            bool exited = false;
            var worker = new SessionWorker(delegate {
                entered.Set();
                mayFinish.WaitOne();
                exited = true;
            });
            worker.Start();
            Check(entered.WaitOne(2000), "session worker starts");
            var stopper = new Thread(delegate() {
                worker.Stop();
                stopReturned.Set();
            });
            stopper.Start();
            try
            {
                Check(!stopReturned.WaitOne(650), "cleanup cannot pass a still-running loop after 500ms");
            }
            finally
            {
                mayFinish.Set();
                stopper.Join();
            }
            Check(exited && worker.StopRequested && worker.Failure == null,
                  "Stop waits for actual completion before resource cleanup");
            worker.Stop();
        }

        const BindingFlags privateFields = BindingFlags.Instance | BindingFlags.NonPublic;
        var failure = new InvalidOperationException("test worker failure");
        var failedWorker = new SessionWorker(delegate { throw failure; });
        failedWorker.Start();
        failedWorker.Stop();
        Check(ReferenceEquals(failedWorker.Failure, failure), "worker retains the original exception");
        int reports = 0;
        var session = new ModSession(Process.GetCurrentProcess(), delegate(string text) { reports++; });
        typeof(ModSession).GetField("worker", privateFields).SetValue(session, failedWorker);
        Check(session.IsAlive && ReferenceEquals(session.Failure, failure),
              "session reports worker failure even while the target process is alive");
        // Exercise the real launcher refresh without showing a window or starting a game.
        using (var form = new LauncherForm(new LauncherOptions()))
        {
            typeof(LauncherForm).GetField("mod", privateFields).SetValue(form, session);
            typeof(LauncherForm).GetMethod("RefreshSession", privateFields).Invoke(form, null);
            Check(typeof(LauncherForm).GetField("mod", privateFields).GetValue(form) == null,
                  "launcher detaches a failed session");
            var status = (Label) typeof(LauncherForm).GetField("status", privateFields).GetValue(form);
            var start = (Button) typeof(LauncherForm).GetField("start", privateFields).GetValue(form);
            Check(status.Text.Contains(failure.Message) && start.Enabled && reports == 1,
                  "failure is visible, logged once and permits restarting the MOD");
        }
        Console.WriteLine(
            "PASS session lifecycle: worker faults reach launcher; cleanup waits for completion");
    }
}
