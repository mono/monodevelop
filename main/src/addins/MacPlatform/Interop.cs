//
// Interop.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Runtime.InteropServices;

namespace MacPlatform
{
	static class Interop
	{
		[DllImport ("libc")]
		extern static int sysctlbyname (string name, IntPtr oldP, ref nint oldLen, IntPtr newP, nint newlen);

		internal static int SysCtl (string name, out string result)
		{
			nint resultLen = 128;
			var resultHandle = Marshal.AllocHGlobal ((int)resultLen);
			var retval = sysctlbyname (name, resultHandle, ref resultLen, IntPtr.Zero, 0);

			// resultLen includes the null terminal, so we want to cut it off
			// but if resultLen < 2 then there's no characters
			if (retval != 0 || resultLen < 2) {
				result = "Unknown";
				return retval;
			}

			result = Marshal.PtrToStringAuto (resultHandle, (int)resultLen - 1);

			Marshal.FreeHGlobal (resultHandle);

			return retval;
		}

		internal static int SysCtl (string name, out int result)
		{
			nint resultLen = 128;
			var resultHandle = Marshal.AllocHGlobal ((int)resultLen);
			var retval = sysctlbyname (name, resultHandle, ref resultLen, IntPtr.Zero, 0);

			if (retval != 0) {
				result = -1;
				return retval;
			}

			result = Marshal.ReadInt32 (resultHandle);

			Marshal.FreeHGlobal (resultHandle);

			return retval;
		}

		internal static int SysCtl (string name, out long result)
		{
			nint resultLen = 128;
			var resultHandle = Marshal.AllocHGlobal ((int)resultLen);
			var retval = sysctlbyname (name, resultHandle, ref resultLen, IntPtr.Zero, 0);

			if (retval != 0) {
				result = -1;
				return retval;
			}

			result = Marshal.ReadInt64 (resultHandle);

			Marshal.FreeHGlobal (resultHandle);

			return retval;
		}
	}
}
