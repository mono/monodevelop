﻿//
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CoreFoundation;
using Foundation;

using MonoDevelop.Core;
using MonoDevelop.Ide.Desktop;

namespace MacPlatform
{
	internal class MacTelemetryDetails : IPlatformTelemetryDetails
	{
		int family;
		int coreCount;
		long freq;
		string arch;
		ulong size;
		ulong freeSize;

		PlatformHardDriveMediaType osType;
		TimeSpan sinceLogin;

		internal MacTelemetryDetails ()
		{
		}

		internal static MacTelemetryDetails CreateTelemetryDetails ()
		{
			var result = new MacTelemetryDetails ();

			Interop.SysCtl ("hw.machine", out result.arch);
			Interop.SysCtl ("hw.cpufamily", out result.family);
			Interop.SysCtl ("hw.cpufrequency", out result.freq);
			Interop.SysCtl ("hw.physicalcpu", out result.coreCount);

			var attrs = NSFileManager.DefaultManager.GetFileSystemAttributes ("/");
			result.size = attrs.Size;
			result.freeSize = attrs.FreeSize;

			result.osType = GetMediaType ("/");

			try {
				var login = GetLoginTime ();
				if (login != DateTimeOffset.MinValue) {
					var epoch = new DateTimeOffset (1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);

					var timeSinceEpoch = DateTimeOffset.UtcNow - epoch;
					var loginSinceEpoch = login - epoch;

					result.sinceLogin = timeSinceEpoch - loginSinceEpoch;
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error getting logintime", e);
				result.sinceLogin = TimeSpan.Zero;
			}
			return result;
		}

		public TimeSpan TimeSinceMachineStart => FromNSTimeInterval (NSProcessInfo.ProcessInfo.SystemUptime);

		public TimeSpan TimeSinceLogin => sinceLogin;

		public TimeSpan KernelAndUserTime => TimeSpan.Zero;

		public TimeSpan KernelTime => TimeSpan.Zero;

		public TimeSpan UserTime => TimeSpan.Zero;

		public string CpuArchitecture => arch;

		public int CpuCount => (int)NSProcessInfo.ProcessInfo.ActiveProcessorCount;

		public int PhysicalCpuCount => coreCount;

		public int CpuFamily => family;

		public long CpuFrequency => freq;

		public ulong HardDriveTotalVolumeSize => size;

		public ulong HardDriveFreeVolumeSize => freeSize;

		public ulong RamTotal => NSProcessInfo.ProcessInfo.PhysicalMemory;

		public PlatformHardDriveMediaType HardDriveOsMediaType => osType;

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
		 */
		static DateTimeOffset GetLoginTime ()
		{
			LastLogX ll = new LastLogX ();
			if (IntPtr.Zero == getlastlogxbyname (Environment.UserName, ref ll)) {
				// getlastlogxbyname doesn't work if SIP is disabled
				return DateTimeOffset.MinValue;
			}

			var dt = DateTimeOffset.FromUnixTimeSeconds (ll.ll_tv_tv_sec);

			return dt;
		}

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
		extern static IntPtr getlastlogxbyname (string name, ref LastLogX ll);

		public bool TrySampleHostCpuLoad (out double value)
		{
			return KernelInterop.TrySampleHostCpu (out value);
		}

		public TimeSpan GetEventTime (Gdk.EventKey eventKey)
		{
			// If this GDK event is the current Gtk.Application event, assume that NSApplication's
			// current event is the event from which the GDK event was created and use its timestamp
			// instead, which has a much higher precision than the GDK time.
			if (Gtk.Application.CurrentEvent == eventKey)
				return FromNSTimeInterval (AppKit.NSApplication.SharedApplication.CurrentEvent.Timestamp);

			return TimeSpan.FromMilliseconds (eventKey.Time);
		}

		/// <summary>
		/// <para>
		/// Converts a high precision <c>NSTimeInterval</c> to a <see cref="TimeSpan"/>,
		/// preserving as much precision as <see cref="TimeSpan"/> allows.
		/// </para>
		/// <para>
		/// n.b. An <c>NSTimeInterval</c> value is always specified in seconds; it yields
		/// sub-millisecond precision over a range of 10,000 years.
		/// </para>
		/// </summary>
		/// <remarks>
		/// Uses <see cref="TimeSpan.FromTicks"/> and not <see cref="TimeSpan.FromTicks"/>
		/// (which truncates the time value).
		/// </remarks>
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		static TimeSpan FromNSTimeInterval (double nsTimeInterval)
		{
			const double ticksPerSecond = 1e7;
			return TimeSpan.FromTicks ((long)(nsTimeInterval * ticksPerSecond));
		}
	}
}
