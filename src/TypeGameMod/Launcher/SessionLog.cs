using System;
using System.IO;

namespace TypeGameMod
{
    internal sealed class SessionLog
    {
        readonly object sync = new object();
        readonly string path;
        internal SessionLog(string file)
        {
            path = file;
        }

        internal void Write(string message)
        {
            // UI, music and PCM workers share this file. Serialize whole lines so
            // simultaneous messages cannot fail with a file-sharing violation.
            lock (sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.AppendAllText(path, DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
            }
        }
    }
}
