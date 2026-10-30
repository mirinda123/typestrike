using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TypeGameMod
{
    // LOAD_LIBRARY_AS_DATAFILE reads RT_BITMAP without running GameRes.dll code.
    internal sealed class GameBitmapResources : IDisposable
    {
        IntPtr module;
        internal GameBitmapResources(string path)
        {
            module = LoadLibraryEx(path, IntPtr.Zero, 2);
            if (module == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        internal byte[] Read(string name)
        {
            IntPtr resource = FindResource(module, name, new IntPtr(2));
            if (resource == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            uint size = SizeofResource(module, resource);
            IntPtr bytes = LockResource(LoadResource(module, resource));
            if (bytes == IntPtr.Zero || size == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            var data = new byte[size];
            Marshal.Copy(bytes, data, 0, data.Length);
            return data;
        }

        public void Dispose()
        {
            if (module != IntPtr.Zero)
            {
                FreeLibrary(module);
                module = IntPtr.Zero;
            }
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string file, IntPtr reserved, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr FindResource(IntPtr module, string name, IntPtr type);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint SizeofResource(IntPtr module, IntPtr resource);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
        [DllImport("kernel32.dll")]
        static extern IntPtr LockResource(IntPtr resource);
        [DllImport("kernel32.dll")]
        static extern bool FreeLibrary(IntPtr module);
    }
}
