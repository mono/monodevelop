using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using MS.WindowsAPICodePack.Internal;

namespace Microsoft.WindowsAPICodePack.Shell
{
    internal class ChangeNotifyLock
    {
        private uint _event = 0;

        internal ChangeNotifyLock(Message message)
        {
            IntPtr pidl;
            IntPtr lockId = ShellNativeMethods.SHChangeNotification_Lock(
                    message.WParam, (int)message.LParam, out pidl, out _event);
            try
            {
                Trace.TraceInformation("Message: {0}", (ShellObjectChangeTypes)_event);

                var notifyStruct = pidl.MarshalAs<ShellNativeMethods.ShellNotifyStruct>();

                Guid guid = new Guid(ShellIIDGuid.IShellItem2);
                if (notifyStruct.item1 != IntPtr.Zero &&
                    (((ShellObjectChangeTypes)_event) & ShellObjectChangeTypes.SystemImageUpdate) == ShellObjectChangeTypes.None)
                {
                    IShellItem2 nativeShellItem;
                    if (CoreErrorHelper.Succeeded(ShellNativeMethods.SHCreateItemFromIDList(
                        notifyStruct.item1, ref guid, out nativeShellItem)))
                    {
                        string name;
                        nativeShellItem.GetDisplayName(ShellNativeMethods.ShellItemDesignNameOptions.FileSystemPath,
                            out name);                        
                        ItemName = name;

                        Trace.TraceInformation("Item1: {0}", ItemName);
                    }
                }
                else
                {
                    ImageIndex = notifyStruct.item1.ToInt32();
                }

                if (notifyStruct.item2 != IntPtr.Zero)
                {
                    IShellItem2 nativeShellItem;
                    if (CoreErrorHelper.Succeeded(ShellNativeMethods.SHCreateItemFromIDList(
                        notifyStruct.item2, ref guid, out nativeShellItem)))
                    {                        
                        string name;
                        nativeShellItem.GetDisplayName(ShellNativeMethods.ShellItemDesignNameOptions.FileSystemPath,
                            out name);
                        ItemName2 = name;

                        Trace.TraceInformation("Item2: {0}", ItemName2);
                    }
                }
            }
            finally
            {
                if (lockId != IntPtr.Zero)
                {
                    ShellNativeMethods.SHChangeNotification_Unlock(lockId);
                }
            }

        }

        public bool FromSystemInterrupt
        {
            get
            {
                return ((ShellObjectChangeTypes)_event & ShellObjectChangeTypes.FromInterrupt)
                    != ShellObjectChangeTypes.None;
            }
        }

        public int ImageIndex { get; private set; }
        public string ItemName { get; private set; }
        public string ItemName2 { get; private set; }

        public ShellObjectChangeTypes ChangeType { get { return (ShellObjectChangeTypes)_event; } }


    }
}
