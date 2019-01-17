//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Security.Permissions;
namespace MS.WindowsAPICodePack.Internal
{
    /// <summary>
    /// Safe Region Handle
    /// </summary>
    public class SafeRegionHandle : ZeroInvalidHandle
    {
        /// <summary>
        /// Release the handle
        /// </summary>
        /// <returns>true if handled is release successfully, false otherwise</returns>
        protected override bool ReleaseHandle()
        {
            if (CoreNativeMethods.DeleteObject(handle))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
