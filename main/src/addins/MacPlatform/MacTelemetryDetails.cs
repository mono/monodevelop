//
// MacTelemetryDetails.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2018 
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
using MonoDevelop.Ide.Desktop;

using Foundation;
using System.Runtime.InteropServices;
using CoreFoundation;
using System.Linq;

namespace MacPlatform
{
	internal class MacTelemetryDetails : IPlatformTelemetryDetails
	{
		int family;
		long freq;
		string arch;
		ulong size;
		ulong freeSize;

		PlatformHardDriveMediaType osType;

		internal MacTelemetryDetails ()
		{
			SysCtl ("hw.machine", out arch);
			SysCtl ("hw.cpufamily", out family);
			SysCtl ("hw.cpufrequency", out freq);

			var attrs = NSFileManager.DefaultManager.GetFileSystemAttributes ("/");
			size = attrs.Size;
			freeSize = attrs.FreeSize;

			osType = GetMediaType ("/");
		}

		public TimeSpan TimeSinceMachineStart => TimeSpan.FromMilliseconds (NSProcessInfo.ProcessInfo.SystemUptime);

		public TimeSpan TimeSinceLogin => TimeSpan.Zero;

		public TimeSpan KernelAndUserTime => TimeSpan.Zero;

		public TimeSpan KernelTime => TimeSpan.Zero;

		public TimeSpan UserTime => TimeSpan.Zero;

		public string CpuArchitecture => arch;

		public int CpuCount => (int)NSProcessInfo.ProcessInfo.ActiveProcessorCount;

		public int CpuFamily => family;

		public long CpuFrequency => freq;

		public ulong HardDriveTotalVolumeSize => size;

		public ulong HardDriveFreeVolumeSize => freeSize;

		public ulong RamTotal => NSProcessInfo.ProcessInfo.PhysicalMemory;

		public PlatformHardDriveMediaType HardDriveOsMediaType => osType;

		static int SysCtl (string name, out string result)
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

		static int SysCtl (string name, out int result)
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

		static int SysCtl (string name, out long result)
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

		static PlatformHardDriveMediaType GetMediaType (string path)
		{
			IntPtr diskHandle = IntPtr.Zero;
			IntPtr sessionHandle = IntPtr.Zero;
			IntPtr charDictRef = IntPtr.Zero;
			uint service = 0;

			try {
				sessionHandle = DASessionCreate (IntPtr.Zero);

				// This seems to only work for '/'
				var url = CFUrl.FromFile (path);
				diskHandle = DADiskCreateFromVolumePath (IntPtr.Zero, sessionHandle, url.Handle);
				if (diskHandle == IntPtr.Zero) {
					return PlatformHardDriveMediaType.Unknown;
				}

				service = DADiskCopyIOMedia (diskHandle);

				var cfStr = new CFString ("Device Characteristics");
				charDictRef = IORegistryEntrySearchCFProperty (service, "IOService", cfStr.Handle, IntPtr.Zero, 3);

				// CFDictionary owns the object pointed to by resultHandle, so no need to release it
				var resultHandle = CFDictionaryGetValue (charDictRef, new CFString ("Medium Type").Handle);
				if (resultHandle == IntPtr.Zero) {
					return PlatformHardDriveMediaType.Unknown;
				}
				var resultString = (string)NSString.FromHandle (resultHandle);

				if (resultString == "Solid State") {
					return PlatformHardDriveMediaType.SolidState;
				} else if (resultString == "Rotational") {
					return PlatformHardDriveMediaType.Rotational;
				} else {
					return PlatformHardDriveMediaType.Unknown;
				}
			} finally {
				if (service != 0) {
					IOObjectRelease (service);
				}
				CFRelease (sessionHandle);
				CFRelease (charDictRef);
			}
		}

		[DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", EntryPoint="CFRelease")]
		static extern void CFReleaseInternal (IntPtr cfRef);

		static void CFRelease (IntPtr cfRef)
		{
			if (cfRef != IntPtr.Zero)
				CFReleaseInternal (cfRef);
		}

		/*
		 * It appears that getlastlogxbyname only works if you have elevated permissions
		 * but it also doesn't distinguish between user login into the system and user opening a new login terminal
		static TimeSpan GetLoginTime ()
		{
			unsafe {
				byte* llP = stackalloc byte[304];
				var llHandle = new IntPtr (llP);
				getlastlogxbyname (Environment.UserName, llHandle);

				var ll = Marshal.PtrToStructure<LastLogX> (llHandle);

				Console.WriteLine ($"{ll.ll_tv_tv_sec} - {ll.ll_tv_tv_usec}");
				var result = new TimeSpan (((ll.ll_tv_tv_sec * 100000) + ll.ll_tv_tv_usec) * 10);
			}

			return TimeSpan.Zero;
		}
		*/

		[DllImport ("/System/Library/Frameworks/IOKit.framework/IOKit")]
		extern static IntPtr IORegistryEntrySearchCFProperty(uint service, string plane, IntPtr key, IntPtr allocator, int options);

		[DllImport ("/System/Library/Frameworks/IOKit.framework/IOKit")]
		extern static int IOObjectRelease (uint handle);

		[DllImport ("/System/Library/Frameworks/DiskArbitration.framework/DiskArbitration")]
		extern static IntPtr DADiskCreateFromVolumePath (IntPtr allocator, IntPtr sessionRef, IntPtr pathRef);

		[DllImport ("/System/Library/Frameworks/DiskArbitration.framework/DiskArbitration")]
		extern static IntPtr DASessionCreate (IntPtr allocator);

		[DllImport ("/System/Library/Frameworks/DiskArbitration.framework/DiskArbitration")]
		extern static uint DADiskCopyIOMedia (IntPtr diskRef);

		[DllImport ("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
		extern static IntPtr CFDictionaryGetValue (IntPtr handle, IntPtr keyHandle);

		[DllImport ("libc")]
		extern static int sysctlbyname (string name, IntPtr oldP, ref nint oldLen, IntPtr newP, nint newlen);

		/*
		[StructLayout(LayoutKind.Explicit, Size = 304)]
		struct LastLogX {

			// In C this is a struct timeval but just expand it here
			[FieldOffset (0)]
			public long ll_tv_tv_sec;
			[FieldOffset (8)]
			public int ll_tv_tv_usec;

			#pragma warning disable 0169
			[FieldOffset(16)]
			public byte ll_line;
			[FieldOffset (48)]
			public byte ll_host;
			#pragma warning restore 0169
		}

		[DllImport ("libc")]
		extern static IntPtr getlastlogxbyname (string name, IntPtr ll);
		*/
	}
}
