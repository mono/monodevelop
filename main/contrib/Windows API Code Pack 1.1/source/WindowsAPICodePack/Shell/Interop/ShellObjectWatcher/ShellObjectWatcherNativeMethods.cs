using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MS.WindowsAPICodePack.Internal;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.WindowsAPICodePack.Shell.Interop
{

    internal static class ShellObjectWatcherNativeMethods
    {
        [DllImport("Ole32.dll")]
        public static extern HResult CreateBindCtx(
            int reserved, // must be 0
            [Out] out IBindCtx bindCtx);

        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RegisterClassEx(
            ref WindowClassEx windowClass
            );

        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            int extendedStyle,
            [MarshalAs(UnmanagedType.LPWStr)]
            string className, //string className, //optional
            [MarshalAs(UnmanagedType.LPWStr)]
            string windowName, //window name
            int style,
            int x,
            int y,
            int width,
            int height,
            IntPtr parentHandle,
            IntPtr menuHandle,
            IntPtr instanceHandle,
            IntPtr additionalData);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMessage(
            [Out] out Message message,
            IntPtr windowHandle,
            uint filterMinMessage,
            uint filterMaxMessage);

        [DllImport("User32.dll")]
        public static extern int DefWindowProc(
            IntPtr hwnd,
            uint msg,
            IntPtr wparam,
            IntPtr lparam);

        [DllImport("User32.dll")]
        public static extern void DispatchMessage([In] ref Message message);

        public delegate int WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WindowClassEx
    {
        internal uint Size;
        internal uint Style;
        
        internal ShellObjectWatcherNativeMethods.WndProcDelegate WndProc;
        
        internal int ExtraClassBytes;
        internal int ExtraWindowBytes;
        internal IntPtr InstanceHandle;
        internal IntPtr IconHandle;
        internal IntPtr CursorHandle;
        internal IntPtr BackgroundBrushHandle;
        
        internal string MenuName;
        internal string ClassName;
        
        internal IntPtr SmallIconHandle;
    }

    /// <summary>
    /// Wraps the native Windows MSG structure.
    /// </summary>
    public struct Message
    {
        private IntPtr windowHandle;
        private uint msg;
        private IntPtr wparam;
        private IntPtr lparam;
        private int time;
        private NativePoint point;

        /// <summary>
        /// Gets the window handle
        /// </summary>
        public IntPtr WindowHandle { get { return windowHandle; } }

        /// <summary>
        /// Gets the window message
        /// </summary>
        public uint Msg { get { return msg; } }

        /// <summary>
        /// Gets the WParam
        /// </summary>
        public IntPtr WParam { get { return wparam; } }

        /// <summary>
        /// Gets the LParam
        /// </summary>
        public IntPtr LParam { get { return lparam; } }

        /// <summary>
        /// Gets the time
        /// </summary>
        public int Time { get { return time; } }

        /// <summary>
        /// Gets the point
        /// </summary>
        public NativePoint Point { get { return point; } }

        /// <summary>
        /// Creates a new instance of the Message struct
        /// </summary>
        /// <param name="windowHandle">Window handle</param>
        /// <param name="msg">Message</param>
        /// <param name="wparam">WParam</param>
        /// <param name="lparam">LParam</param>
        /// <param name="time">Time</param>
        /// <param name="point">Point</param>
        internal Message(IntPtr windowHandle, uint msg, IntPtr wparam, IntPtr lparam, int time, NativePoint point)
            : this()
        {
            this.windowHandle = windowHandle;
            this.msg = msg;
            this.wparam = wparam;
            this.lparam = lparam;
            this.time = time;
            this.point = point;
        }

        /// <summary>
        /// Determines if two messages are equal.
        /// </summary>
        /// <param name="first">First message</param>
        /// <param name="second">Second message</param>
        /// <returns>True if first and second message are equal; false otherwise.</returns>
        public static bool operator ==(Message first, Message second)
        {
            return first.WindowHandle == second.WindowHandle
                && first.Msg == second.Msg
                && first.WParam == second.WParam
                && first.LParam == second.LParam
                && first.Time == second.Time
                && first.Point == second.Point;
        }

        /// <summary>
        /// Determines if two messages are not equal.
        /// </summary>
        /// <param name="first">First message</param>
        /// <param name="second">Second message</param>
        /// <returns>True if first and second message are not equal; false otherwise.</returns>
        public static bool operator !=(Message first, Message second)
        {
            return !(first == second);
        }

        /// <summary>
        /// Determines if this message is equal to another.
        /// </summary>
        /// <param name="obj">Another message</param>
        /// <returns>True if this message is equal argument; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return (obj != null && obj is Message) ? this == (Message)obj : false;
        }

        /// <summary>
        /// Gets a hash code for the message.
        /// </summary>
        /// <returns>Hash code for this message.</returns>
        public override int GetHashCode()
        {
            int hash = WindowHandle.GetHashCode();
            hash = hash * 31 + Msg.GetHashCode();
            hash = hash * 31 + WParam.GetHashCode();
            hash = hash * 31 + LParam.GetHashCode();
            hash = hash * 31 + Time.GetHashCode();
            hash = hash * 31 + Point.GetHashCode();
            return hash;
        }
    }

}
