Add-Type -TypeDefinition @"
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;

public class NativeClipboard {
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    private const uint CF_HDROP = 15;
    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint GMEM_ZEROINIT = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    public struct DROPFILES {
        public int pFiles;
        public int x;
        public int y;
        public int fNC;
        public int fWide;
    }

    public static void SetFileDrop(string filePath) {
        if (!File.Exists(filePath)) return;
        string fullPath = Path.GetFullPath(filePath);

        int dropFilesSize = Marshal.SizeOf(typeof(DROPFILES));
        byte[] pathBytes = Encoding.Unicode.GetBytes(fullPath + "\0\0");
        int totalAllocSize = dropFilesSize + pathBytes.Length;

        IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE | GMEM_ZEROINIT, (UIntPtr)totalAllocSize);
        if (hMem == IntPtr.Zero) return;

        IntPtr pMem = GlobalLock(hMem);
        try {
            DROPFILES dropFiles = new DROPFILES {
                pFiles = dropFilesSize,
                fWide = 1
            };
            Marshal.StructureToPtr(dropFiles, pMem, false);
            Marshal.Copy(pathBytes, 0, (IntPtr)(pMem.ToInt64() + dropFilesSize), pathBytes.Length);
        } finally {
            GlobalUnlock(hMem);
        }

        if (OpenClipboard(IntPtr.Zero)) {
            try {
                EmptyClipboard();
                SetClipboardData(CF_HDROP, hMem);
            } finally {
                CloseClipboard();
            }
        }
    }
}
"@
