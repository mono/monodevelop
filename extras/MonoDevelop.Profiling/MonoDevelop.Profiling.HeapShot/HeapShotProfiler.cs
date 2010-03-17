//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Profiling;

namespace MonoDevelop.Profiling.HeapShot
{
	public sealed class HeapShotProfiler : AbstractProfiler
	{
		private int dumpCount;

		public HeapShotProfiler ()
		{
			CheckSupported ("libmono-profiler-heap-shot");
		}
		
		public override string Identifier {
			get { return "heap-shot"; }
		}

		public override string Name {
			get { return GettextCatalog.GetString ("Heap Shot (Explore memory allocation patterns)"); }
		}

		public override string IconString {
			get { return "md-prof-snapshot"; }
		}
		
		public override string GetSnapshotFileName (string workingDirectory, string filename)
		{
			if (filename == null && workingDirectory == null)
				return "outfile_" + dumpCount.ToString () + ".omap";
			else if (filename == null)
				return Path.Combine (workingDirectory, "outfile_" + dumpCount.ToString () + ".omap");
			else
				return filename + "_" + dumpCount + ".omap";
		}
		
		public override void TakeSnapshot ()
		{
			lock (sync) {
				State = ProfilerState.TakingSnapshot;

				System.Diagnostics.Process.Start ("kill", "-PROF " + Context.AsyncOperation.ProcessId);
				ThreadPool.QueueUserWorkItem (new WaitCallback (AsyncTakeSnapshot));
			}
		}
		
		private void AsyncTakeSnapshot (object state)
		{
			string dumpFile = null;
			lock (sync)
				dumpFile = Context.FileName;
			
			int attempts = 40;
			bool success = false;
			
			while (!success) {
				if (--attempts == 0) {
					OnSnapshotFailed (EventArgs.Empty);
					return;
				}
				
				Thread.Sleep (500);
				if (!File.Exists (dumpFile))
					continue;

				try {
					string destFile = GetSaveLocation ();
					if (destFile != null) { //ignore if Cancel is clicked in the save dialog
						File.Copy (dumpFile, destFile);
						File.Delete (dumpFile);

						IProfilingSnapshot snapshot = new HeapShotProfilingSnapshot (this, destFile);
						OnSnapshotTaken (new ProfilingSnapshotEventArgs (snapshot));
					}
					success = true;
				} catch (Exception ex) {
					LoggingService.LogError ("HeapShotProfiler", "AsyncTakeSnapshot", ex);
				}
			}
			
			lock (sync)
				State = ProfilerState.Profiling;
		}
		
		public override void Start (ProfilingContext context)
		{
			base.Start (context);
			dumpCount = 0;
		}

		public override void Stop ()
		{
			lock (sync) {
				if (State != ProfilerState.Inactive) {
					Context.AsyncOperation.Cancel ();
					State = ProfilerState.Inactive;
				}
			}
		}
		
		public override bool CanLoad (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");
			
			using (Stream stream = new FileStream (filename, FileMode.Open, FileAccess.Read)) {
				using (BinaryReader reader = new BinaryReader (stream)) {
					uint magic_number = reader.ReadUInt32 ();
					if (magic_number != 0x4eabbdd1)
						return false;
					
					reader.ReadInt32 (); //skip the version
					string label = reader.ReadString ();
					
					return label == "heap-shot logfile";
				}
			}
		}
		
		public override IProfilingSnapshot Load (string filename)
		{
			return new HeapShotProfilingSnapshot (this, filename);
		}
			
		public override string GetSaveLocation ()
		{
			string location = null;
			DispatchService.GuiSyncDispatch (new MessageHandler (delegate () {
				location = GetSaveLocation ("HeapShot Snapshots", "omap");
			}));
			return location;
		}
	}
}