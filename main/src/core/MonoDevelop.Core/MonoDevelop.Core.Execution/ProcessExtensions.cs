// 
// ProcessExtensions.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;

namespace MonoDevelop.Core.Execution
{
	public static class ProcessExtensions
	{
		public static void KillProcessTree (this Process p)
		{
			if (Platform.IsWindows) {
				var procRelations = GetProcRelations ();
				
				foreach (int pid in GetAllChildren (procRelations, p.Id)) {
					Process cp = Process.GetProcessById (pid);
					try {
						cp.Kill ();
					} catch {
						// Ignore
					}
				}
			}
			p.Kill ();
		}
		
		static IEnumerable<int> GetAllChildren (Dictionary<int,List<int>> procRelations, int pid)
		{
			List<int> children;
			if (!procRelations.TryGetValue (pid, out children))
				yield break;
			foreach (int cpid in children) {
				foreach (int c in GetAllChildren (procRelations, cpid))
					yield return c;
				yield return cpid;
			}
		}

		static Dictionary<int,List<int>> GetProcRelations ()
		{
			Dictionary<int,List<int>> procRelations = new Dictionary<int, List<int>> ();
			
			IntPtr oHnd = CreateToolhelp32Snapshot (TH32CS_SNAPPROCESS, 0);
		
			if (oHnd == IntPtr.Zero)
				return procRelations;
		
			PROCESSENTRY32 oProcInfo = new PROCESSENTRY32 ();
		
			oProcInfo.dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf (typeof(PROCESSENTRY32));
		
			if (Process32First (oHnd, ref oProcInfo) == false)
				return procRelations;
		
			do {
				List<int> children;
				if (!procRelations.TryGetValue ((int)oProcInfo.th32ParentProcessID, out children)) {
					children = new List<int> ();
					procRelations [(int)oProcInfo.th32ParentProcessID] = children;
				}
				children.Add ((int)oProcInfo.th32ProcessID);
			} while (Process32Next (oHnd, ref oProcInfo));
			return procRelations;
		}

		const uint TH32CS_SNAPPROCESS = 2;

		[StructLayout(LayoutKind.Sequential)]
		public struct PROCESSENTRY32
		{
			public uint dwSize;
			public uint cntUsage;
			public uint th32ProcessID;
			public IntPtr th32DefaultHeapID;
			public uint th32ModuleID;
			public uint cntThreads;
			public uint th32ParentProcessID;
			public int pcPriClassBase;
			public uint dwFlags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szExeFile;
		};
	
		const string kernel = "kernel32.dll";
		[DllImport(kernel, SetLastError = true)]
		static extern IntPtr CreateToolhelp32Snapshot (uint dwFlags, uint th32ProcessID);

		[DllImport(kernel)]
		static extern bool Process32First (IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		[DllImport(kernel)]
		static extern bool Process32Next (IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
	}
}

