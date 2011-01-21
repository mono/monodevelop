// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.WindowsAPICodePack.ExtendedLinguisticServices
{

    internal static class InteropTools
    {
        internal static readonly IntPtr SizeOfGuid = (IntPtr)Marshal.SizeOf(typeof(Guid));
        internal static readonly IntPtr SizeOfWin32EnumOptions = (IntPtr)Marshal.SizeOf(typeof(Win32EnumOptions));
        internal static readonly IntPtr SizeOfWin32Options = (IntPtr)Marshal.SizeOf(typeof(Win32Options));
        internal static readonly UInt64 SizeOfService = (UInt64)Marshal.SizeOf(typeof(Win32Service));
        internal static readonly IntPtr SizeOfWin32PropertyBag = (IntPtr)Marshal.SizeOf(typeof(Win32PropertyBag));
        internal static readonly UInt64 SizeOfWin32DataRange = (UInt64)Marshal.SizeOf(typeof(Win32DataRange));
        internal static readonly UInt64 OffsetOfGuidInService = (UInt64)Marshal.OffsetOf(typeof(Win32Service), "_guid");
        internal static readonly Type TypeOfGuid = typeof(Guid);

        internal static T Unpack<T>(IntPtr value) where T : struct
        {
            if (value == IntPtr.Zero)
            {
                return default(T);
            }

            return (T)Marshal.PtrToStructure(value, typeof(T));
        }

        internal static IntPtr Pack<T>(ref T data) where T : struct
        {
            IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
            Marshal.StructureToPtr(data, pointer, false);
            return pointer;
        }

        internal static void Free<T>(ref IntPtr pointer) where T : struct
        {
            if (pointer != IntPtr.Zero)
            {
                // Thus we clear the strings previously allocated to the struct:
                Marshal.StructureToPtr(default(T), pointer, true);
                // Here we clean up the memory for the struct itself:
                Marshal.FreeHGlobal(pointer);
                // This is to avoid calling freeing this pointer multiple times:
                pointer = IntPtr.Zero;
            }
        }

        internal static string[] UnpackStringArray(IntPtr strPtr, uint count)
        {
            if (strPtr == IntPtr.Zero && count != 0)
            {
                throw new LinguisticException(LinguisticException.InvalidArgs);
            }

            string[] retVal = new string[count];

            int offset = 0;
            for (int i = 0; i < count; i++)
            {
                retVal[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(strPtr, offset));
                offset += IntPtr.Size;
            }

            return retVal;
        }

    }

}
