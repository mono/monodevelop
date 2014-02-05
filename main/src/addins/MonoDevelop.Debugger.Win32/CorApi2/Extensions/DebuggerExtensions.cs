//
// DebuggerExtensions.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Samples.Debugging.CorDebug;
using Microsoft.Samples.Debugging.CorDebug.NativeApi;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Samples.Debugging.Extensions
{
	[CLSCompliant (false)]
	public static class DebuggerExtensions
	{
		// [Xamarin] Output redirection.
		public const int CREATE_REDIRECT_STD = 0x40000000;
		const string Kernel32LibraryName = "kernel32.dll";

		[
			DllImport (Kernel32LibraryName, CharSet = CharSet.Auto, SetLastError = true)
		]
		public static extern bool CreatePipe (out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

		[
			DllImport (Kernel32LibraryName)
		]
		public static extern bool DuplicateHandle (
			IntPtr hSourceProcessHandle,
			SafeFileHandle hSourceHandle,
			IntPtr hTargetProcessHandle,
			out SafeFileHandle lpTargetHandle,
			uint dwDesiredAccess,
			bool bInheritHandle,
			uint dwOptions
		);

		const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
		const uint DUPLICATE_SAME_ACCESS = 0x00000002;

		[
			DllImport (Kernel32LibraryName)
		]
		public static extern SafeFileHandle GetStdHandle (uint nStdHandle);

		const uint STD_INPUT_HANDLE = unchecked ((uint)-10);
		const uint STD_OUTPUT_HANDLE = unchecked ((uint)-11);
		const uint STD_ERROR_HANDLE = unchecked ((uint)-12);

		[
			DllImport (Kernel32LibraryName)
		]
		public static extern bool ReadFile (
			SafeFileHandle hFile,
			byte[] lpBuffer,
			int nNumberOfBytesToRead,
			out int lpNumberOfBytesRead,
			IntPtr lpOverlapped
		);

		[
			DllImport (Kernel32LibraryName, CharSet = CharSet.Auto, SetLastError = true)
		]
		public static extern IntPtr GetCurrentProcess ();

		static void CreateHandles (STARTUPINFO si, out SafeFileHandle outReadPipe, out SafeFileHandle errorReadPipe)
		{
			si.dwFlags |= 0x00000100; /*			STARTF_USESTDHANDLES*/
			var sa = new SECURITY_ATTRIBUTES ();
			sa.bInheritHandle = true;
			IntPtr curProc = GetCurrentProcess ();

			SafeFileHandle outWritePipe, outReadPipeTmp;
			if (!CreatePipe (out outReadPipeTmp, out outWritePipe, sa, 0))
				throw new Exception ("Pipe creation failed");

			// Create the child error pipe.
			SafeFileHandle errorWritePipe, errorReadPipeTmp;
			if (!CreatePipe (out errorReadPipeTmp, out errorWritePipe, sa, 0))
				throw new Exception ("Pipe creation failed");

			// Create new output read and error read handles. Set
			// the Properties to FALSE. Otherwise, the child inherits the
			// properties and, as a result, non-closeable handles to the pipes
			// are created.
			if (!DuplicateHandle (curProc, outReadPipeTmp, curProc, out outReadPipe, 0, false, DUPLICATE_SAME_ACCESS))
				throw new Exception ("Pipe creation failed");
			if (!DuplicateHandle (curProc, errorReadPipeTmp, curProc, out errorReadPipe, 0, false, DUPLICATE_SAME_ACCESS))
				throw new Exception ("Pipe creation failed");

			NativeMethods.CloseHandle (curProc);

			// Close inheritable copies of the handles you do not want to be
			// inherited.
			outReadPipeTmp.Close ();
			errorReadPipeTmp.Close ();

			si.hStdInput = GetStdHandle (STD_INPUT_HANDLE);
			si.hStdOutput = outWritePipe;
			si.hStdError = errorWritePipe;
		}

		internal static void SetupOutputRedirection (STARTUPINFO si, ref int flags, SafeFileHandle outReadPipe, SafeFileHandle errorReadPipe)
		{
			if ((flags & CREATE_REDIRECT_STD) != 0) {
				CreateHandles (si, out outReadPipe, out errorReadPipe);
				flags &= ~CREATE_REDIRECT_STD;
			}
			else {
				si.hStdInput = new SafeFileHandle (IntPtr.Zero, false);
				si.hStdOutput = new SafeFileHandle (IntPtr.Zero, false);
				si.hStdError = new SafeFileHandle (IntPtr.Zero, false);
			}
		}

		internal static void TearDownOutputRedirection (SafeFileHandle outReadPipe, SafeFileHandle errorReadPipe, STARTUPINFO si, CorProcess ret)
		{
			if (outReadPipe != null) {
				// Close pipe handles (do not continue to modify the parent).
				// You need to make sure that no handles to the write end of the
				// output pipe are maintained in this process or else the pipe will
				// not close when the child process exits and the ReadFile will hang.

				si.hStdInput.Close ();
				si.hStdOutput.Close ();
				si.hStdError.Close ();

				ret.TrackStdOutput (outReadPipe, errorReadPipe);
			}
		}

		internal static IntPtr SetupEnvironment (IDictionary<string, string> environment)
		{
			IntPtr env = IntPtr.Zero;
			if (environment != null) {
				string senv = null;
				foreach (KeyValuePair<string, string> var in environment) {
					senv += var.Key + "=" + var.Value + "\0";
				}
				senv += "\0";
				env = Marshal.StringToHGlobalAnsi (senv);
			}
			return env;
		}

		internal static void TearDownEnvironment (IntPtr env)
		{
			if (env != IntPtr.Zero)
				Marshal.FreeHGlobal (env);
		}
	}
}

