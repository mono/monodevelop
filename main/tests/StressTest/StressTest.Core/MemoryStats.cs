using System;
using System.Diagnostics;

namespace LeakTest
{
	[Serializable]
	public struct MemoryStats
	{
		public long PrivateMemory;
		public long VirtualMemory;
		public long WorkingSet;
		public long PeakVirtualMemory;
		public long PagedSystemMemory;
		public long PagedMemory;
		public long NonPagedSystemMemory;

		public static MemoryStats GetMemoryStats (int pid)
		{
			using (var proc = Process.GetProcessById (pid)) {
				return new MemoryStats {
					PrivateMemory = proc.PrivateMemorySize64,
					VirtualMemory = proc.VirtualMemorySize64,
					WorkingSet = proc.WorkingSet64,
					PeakVirtualMemory = proc.PeakVirtualMemorySize64,
					PagedSystemMemory = proc.PagedSystemMemorySize64,
					PagedMemory = proc.PagedMemorySize64,
					NonPagedSystemMemory = proc.NonpagedSystemMemorySize64
				};
			}
		}
	};
}
