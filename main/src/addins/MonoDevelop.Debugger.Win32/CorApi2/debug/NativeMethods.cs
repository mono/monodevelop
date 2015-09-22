using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Samples.Debugging.CorPublish.Metahost;

namespace Microsoft.Samples.Debugging.CorDebug
{
    internal static class NativeMethods
    {
        private const string Kernel32LibraryName = "kernel32.dll";
        private const string Ole32LibraryName = "ole32.dll";
        private const string ShimLibraryName = "mscoree.dll";

        [
            System.Runtime.ConstrainedExecution.ReliabilityContract (System.Runtime.ConstrainedExecution.Consistency.WillNotCorruptState, System.Runtime.ConstrainedExecution.Cer.Success),
            DllImport (Kernel32LibraryName)
        ]
        public static extern bool CloseHandle (IntPtr handle);


        [
            DllImport (ShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)
        ]
        public static extern ICorDebug CreateDebuggingInterfaceFromVersion (int iDebuggerVersion
            , string szDebuggeeVersion);

        [
            DllImport (ShimLibraryName, CharSet = CharSet.Unicode)
        ]
        public static extern int GetCORVersion ([Out, MarshalAs (UnmanagedType.LPWStr)] StringBuilder szName
            , Int32 cchBuffer
            , out Int32 dwLength);

        [
            DllImport (ShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)
        ]
        public static extern void GetVersionFromProcess (ProcessSafeHandle hProcess, StringBuilder versionString,
            Int32 bufferSize, out Int32 dwLength);

        [DllImport ("mscoree.dll", CharSet = CharSet.Auto, SetLastError = true, PreserveSig = false)]
        public static extern void CLRCreateInstance (
            ref Guid clsid,
            ref Guid riid,
            [MarshalAs (UnmanagedType.Interface)] out IClrMetaHost metahostInterface);

        public static Guid CLSID_CLRMetaHost =
            new Guid ("9280188D-0E8E-4867-B30C-7FA83884E8DE");

        public static Guid IID_ICLRMetaHost =
            new Guid ("D332DB9E-B9B3-4125-8207-A14884F53216");

        [
            DllImport (ShimLibraryName, CharSet = CharSet.Unicode, PreserveSig = false)
        ]
        public static extern void GetRequestedRuntimeVersion (string pExe, StringBuilder pVersion,
            Int32 cchBuffer, out Int32 dwLength);

        public enum ProcessAccessOptions : int
        {
            PROCESS_TERMINATE = 0x0001,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_SET_SESSIONID = 0x0004,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_SUSPEND_RESUME = 0x0800,
            SYNCHRONIZE = 0x100000,
        }

        [
            DllImport (Kernel32LibraryName, PreserveSig = true)
        ]
        public static extern ProcessSafeHandle OpenProcess (Int32 dwDesiredAccess, bool bInheritHandle, Int32 dwProcessId);

        public static Guid IIDICorDebug = new Guid ("3d6f5f61-7538-11d3-8d5b-00104b35e7ef");

        [
            DllImport (Ole32LibraryName, PreserveSig = false)
        ]
        public static extern void CoCreateInstance (ref Guid rclsid, IntPtr pUnkOuter,
            Int32 dwClsContext,
            ref Guid riid, // must be "ref NativeMethods.IIDICorDebug"
            [MarshalAs (UnmanagedType.Interface)] out ICorDebug debuggingInterface
        );
    }
}