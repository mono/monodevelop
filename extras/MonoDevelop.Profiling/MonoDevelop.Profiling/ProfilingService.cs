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
using System.Diagnostics;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Profiling
{
	public static class ProfilingService
	{
		public static event ProfilerEventHandler ActiveProfilerChanged;
		public static event ProfilingSnapshotEventHandler SnapshotTaken;
		public static event EventHandler SnapshotFailed;
		
		private static IProfiler activeProfiler;
		private static Dictionary<string, IProfiler> profilers;
		private static ProfilingSnapshotCollection profilingSnapshots;
		
		private static ProfilingSnapshotEventHandler snapshotHandler;
		private static EventHandler snapshotFailedHandler;
		private static ProfilerStateEventHandler stateHandler;
		
		static ProfilingService ()
		{
			profilers = new Dictionary<string, IProfiler> ();

			foreach (ProfilerCodon codon in AddinManager.GetExtensionNodes ("/MonoDevelop/Profiling/Profilers")) {
				IProfiler prof = codon.Profiler;
				profilers.Add (prof.Identifier, prof);
			}
			
			snapshotHandler = new ProfilingSnapshotEventHandler (HandleSnapshotTaken);
			stateHandler = new ProfilerStateEventHandler (HandleStateChanged);
			snapshotFailedHandler = new EventHandler (HandleSnapshotFailed);
			
			string configFile = Path.Combine (PropertyService.ConfigPath, "MonoDevelop.Profiling.xml");
			profilingSnapshots = new ProfilingSnapshotCollection (configFile);
			profilingSnapshots.Load ();
		}
		
		public static bool IsProfilerActive {
			get { return activeProfiler != null; }
		}
		
		public static IProfiler ActiveProfiler {
			get { return activeProfiler; }
			set {
				if (activeProfiler != value) {
					if (activeProfiler != null) {
						if (activeProfiler.State != ProfilerState.Inactive)
						          activeProfiler.Stop ();
						activeProfiler.SnapshotTaken -= snapshotHandler;
						activeProfiler.SnapshotFailed -= snapshotFailedHandler;
						activeProfiler.StateChanged -= stateHandler;						
					}
					
					activeProfiler = value;
					if (activeProfiler != null) {
						activeProfiler.SnapshotTaken += snapshotHandler;
						activeProfiler.SnapshotFailed += snapshotFailedHandler;
						activeProfiler.StateChanged += stateHandler;
					} else {
						ProfilingOperations.RestoreWorkbenchContext ();
					}
					
					ProfilerEventArgs args = new ProfilerEventArgs (value);
					if (ActiveProfilerChanged != null)
						ActiveProfilerChanged (null, args);
				}
			}
		}
		
		public static IEnumerable<IProfiler> Profilers {
			get { return profilers.Values; }
		}
		
		public static ProfilingSnapshotCollection ProfilingSnapshots {
			get { return profilingSnapshots; }
		}
		
		public static int ProfilerCount {
			get { return profilers.Count; }
		}
		
		public static int SupportedProfilerCount {
			get {
				int count = 0;
				foreach (IProfiler prof in profilers.Values)
					if (prof.IsSupported)
						count++;
				return count;
			}
		}
		
		public static IProfiler GetProfiler (string identifier)
		{
			if (identifier == null)
				throw new ArgumentNullException ("identifier");
			
			IProfiler prof = null;
			if (profilers.TryGetValue (identifier, out prof))
				return prof;
			return null;
		}
		
		public static void LoadSnapshot (string profilerIdentifier, string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");
			if (profilerIdentifier == null)
				throw new ArgumentNullException ("profilerIdentifier");
			
			IProfiler prof = GetProfiler (profilerIdentifier);
			if (prof != null) {
				IProfilingSnapshot snapshot = prof.Load (filename);
				if (snapshot != null) {
					profilingSnapshots.Add (snapshot);
					return;
				}
			}
			MessageService.ShowError (GettextCatalog.GetString ("Unable to load profiling snapshot '{0}'."), filename);
		}
		
		public static IProfilingSnapshot LoadSnapshot (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");
			
			foreach (IProfiler prof in profilers.Values) {
				if (prof.CanLoad (filename)) {
					IProfilingSnapshot snapshot = prof.Load (filename);
					if (snapshot != null) {
						profilingSnapshots.Add (snapshot);
						return snapshot;
					}
				}
			}
			
			MessageService.ShowError (GettextCatalog.GetString ("Unable to load profiling snapshot '{0}'."), filename);
			return null;
		}
		
		public static void RemoveSnapshot (IProfilingSnapshot snapshot)
		{
			AlertButton removeFromProject = new AlertButton (GettextCatalog.GetString ("_Remove from Project"), Gtk.Stock.Remove);
			AlertButton result = MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to remove snapshot '{0}'?", snapshot.Name),
			                                                 GettextCatalog.GetString ("Delete physically removes the file from disc."), 
			                                                 AlertButton.Delete, AlertButton.Cancel, removeFromProject);
			
			if (result != AlertButton.Cancel) {
				ProfilingService.ProfilingSnapshots.Remove (snapshot);
				if (result == AlertButton.Delete && File.Exists (snapshot.FileName))
					FileService.DeleteFile (snapshot.FileName);
			}		
		}
		
		private static void HandleSnapshotTaken (object sender, ProfilingSnapshotEventArgs args)
		{
			profilingSnapshots.Add (args.Snapshot);
			
			if (SnapshotTaken != null)
				SnapshotTaken (sender, args);
		}
		
		private static void HandleSnapshotFailed (object sender, EventArgs args)
		{
			MessageService.ShowError (GettextCatalog.GetString ("Unable to take a profiling snapshot."));
			
			if (SnapshotFailed != null)
				SnapshotFailed (sender, args);
		}
		
		private static void HandleStateChanged (object sender, ProfilerStateEventArgs args)
		{
			if (args.State == ProfilerState.Inactive)
				ActiveProfiler = null;
		}
		
		internal static bool GetProfilerInformation (int pid, out string profiler, out string filename)
		{
			//TODO: make sure this works on mac+windows
			string fn = "/proc/" + pid.ToString () + "/cmdline";
			using (FileStream stream = new FileStream (fn, FileMode.Open, FileAccess.Read)) {
				using (StreamReader reader = new StreamReader(stream)) {
					string[] args = reader.ReadToEnd ().Split (new char[]{'\0'}, StringSplitOptions.RemoveEmptyEntries);

					if (args[0].EndsWith ("mono")) {
						//a process launched with mono, either "mono" or something like "/usr/bin/mono

						for (int i=1; i<args.Length; i++) {
							if (args[i].StartsWith ("--profile=")) {
								int index = args[i].IndexOf (':');

								if (index >= 0) {
									profiler = args[i].Substring (10, index);
									filename = args[i].Substring (index + 1);
								} else {
									profiler = args[i].Substring (10);
									filename = null;
								}
								return true;
							}
						}
					}
				}
			}
			
			profiler = null;
			filename = null;
			return false;
		}
		
		internal static string GetProcessDirectory (int pid)
		{
			string fn = "/proc/" + pid.ToString () + "/environ";
			using (FileStream stream = new FileStream (fn, FileMode.Open, FileAccess.Read)) {
				using (StreamReader reader = new StreamReader(stream)) {
					string[] args = reader.ReadToEnd ().Split (new char[]{'\0'}, StringSplitOptions.RemoveEmptyEntries);

					for (int i=0; i<args.Length; i++) {
						if (args[i].StartsWith ("PWD=")) {
							return args[i].Substring (4);
						}
					}
				}
			}
			return null;
		}
		
		public static string PrettySize (uint num_bytes)
		{
			if (num_bytes < 1024)
				return String.Format ("{0}b", num_bytes);

			if (num_bytes < 1024*10)
				return String.Format ("{0:0.0}k", num_bytes / 1024.0);

			if (num_bytes < 1024*1024)
				return String.Format ("{0}k", num_bytes / 1024);

			return String.Format ("{0:0.0}M", num_bytes / (1024 * 1024.0));
		}
		
		public static string PrettySize (long num_bytes)
		{
			return PrettySize ((uint)num_bytes);
		}
	}
}