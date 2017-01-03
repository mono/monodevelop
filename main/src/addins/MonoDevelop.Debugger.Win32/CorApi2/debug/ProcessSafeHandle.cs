using System;

namespace Microsoft.Samples.Debugging.CorDebug
{
    public class ProcessSafeHandle : Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private ProcessSafeHandle ()
            : base (true)
        {
        }

        private ProcessSafeHandle (IntPtr handle, bool ownsHandle) : base (ownsHandle)
        {
            SetHandle (handle);
        }

        override protected bool ReleaseHandle ()
        {
            return NativeMethods.CloseHandle (handle);
        }
    }
}