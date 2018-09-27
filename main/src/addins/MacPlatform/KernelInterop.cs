//
// KernelInterop.cs
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
	static class KernelInterop
	{
		const int TASK_VM_INFO = 22;
		const int KERN_SUCCESS = 0;

		struct task_vm_info
		{
			public ulong virtual_size; /* virtual memory size (bytes) */
			public int region_count; /* number of memory regions */
			public int page_size;
			public ulong resident_size; /* resident memory size (bytes) */
			public ulong resident_size_peak; /* peak resident size (bytes) */

			public ulong	device;
			public ulong	device_peak;
			public ulong	@internal;
			public ulong	internal_peak;
			public ulong	external;
			public ulong	external_peak;
			public ulong	reusable;
			public ulong	reusable_peak;
			public ulong	purgeable_volatile_pmap;
			public ulong	purgeable_volatile_resident;
			public ulong	purgeable_volatile_virtual;
			public ulong	compressed;
			public ulong	compressed_peak;
			public ulong	compressed_lifetime;

			/* added for rev1 */
			public ulong	phys_footprint;

			/* added for rev2 */
			public ulong	min_address;
			public ulong	max_address;
		}

		[DllImport ("/usr/lib/system/libsystem_kernel.dylib")]
		static extern IntPtr mach_task_self ();

		[DllImport ("/usr/lib/system/libsystem_kernel.dylib")]
		static extern int task_info (IntPtr target_task, uint flavor, ref task_vm_info task_info_out, ref int size);

		static bool TryGetTaskVMInfo (out task_vm_info vm_info)
		{
			vm_info = new task_vm_info ();
			int size;
			unsafe {
				size = sizeof (task_vm_info);
			}

			int ret = task_info (mach_task_self (), TASK_VM_INFO, ref vm_info, ref size);
			return ret == KERN_SUCCESS;
		}

		public static void GetCompressedMemoryInfo (out ulong compressedBytes, out ulong virtualBytes)
		{
			compressedBytes = virtualBytes = 0;

			if (TryGetTaskVMInfo (out var info)) {
				compressedBytes = info.compressed;
				virtualBytes = info.virtual_size;
			}
		}
	}
}
