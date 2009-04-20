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

using Gtk;
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Profiling
{
	public abstract class AbstractProfiler : IProfiler
	{
		protected ProfilerState state;
		protected ProfilingContext context;
		protected bool isSupported;
		
		protected object sync = new object ();

		public event ProfilingSnapshotEventHandler SnapshotTaken;
		public event EventHandler SnapshotFailed;
		public event ProfilerStateEventHandler StateChanged;
		
		public event EventHandler Started;
		public event EventHandler Stopped;
		
		public abstract string Identifier { get; }
		public abstract string Name { get; }
		public abstract string IconString { get; }

		public ProfilerState State {
			get { return state; }
			protected internal set {
				if (state != value) {
					state = value;
					OnStateChanged (new ProfilerStateEventArgs (value));
				}
			}
		}
		
		public ProfilingContext Context {
			get { return context; }
		}
		
		public virtual bool IsSupported {
			get { return isSupported; }
		}
		
		public virtual IExecutionHandler GetDefaultExecutionHandlerFactory ()
		{
			return new ApplicationExecutionHandlerFactory (this);
		}

		public virtual IExecutionHandler GetProcessExecutionHandlerFactory (Process process)
		{
			return new ProcessExecutionHandlerFactory (this, process);
		}
		
		public abstract string GetSnapshotFileName (string workingDirectory, string filename);
		
		public virtual void Start (ProfilingContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");

			lock (sync) {
				if (State != ProfilerState.Inactive)
					throw new InvalidOperationException ("The profiler is already running.");
				State = ProfilerState.Profiling;
			}

			this.context = context;				
		}

		public abstract void Stop ();

		public abstract void TakeSnapshot ();

		protected virtual void OnSnapshotTaken (ProfilingSnapshotEventArgs args)
		{
			if (SnapshotTaken != null)
				SnapshotTaken (this, args);
		}
		
		protected virtual void OnSnapshotFailed (EventArgs args)
		{
			if (SnapshotFailed != null)
				SnapshotFailed (this, args);
		}
		
		protected virtual void OnStateChanged (ProfilerStateEventArgs args)
		{
			if (StateChanged != null)
				StateChanged (this, args);
		}
		
		protected virtual void OnStarted (EventArgs args)
		{
			if (Started != null)
				Started (this, args);
		}

		protected virtual void OnStopped (EventArgs args)
		{
			if (Stopped != null)
				Stopped (this, args);
		}
		
		public virtual bool CanLoad (string fileName)
		{
			return false;
		}

		public virtual IProfilingSnapshot Load (string filename)
		{
			return null;
		}
		
		public abstract string GetSaveLocation ();
		
		protected virtual string GetSaveLocation (string name, string extension)
		{
			FileChooserDialog dlg = new FileChooserDialog (
				GettextCatalog.GetString ("Save Snapshot"), null, FileChooserAction.Save,
				"gtk-cancel", ResponseType.Cancel,
				"gtk-save", ResponseType.Accept
			);
			dlg.SelectMultiple = false;
			dlg.LocalOnly = true;
			dlg.Modal = true;

			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
				dlg.SetCurrentFolder (IdeApp.ProjectOperations.CurrentSelectedSolution.BaseDirectory);
			else
				dlg.SetCurrentFolder (Environment.GetFolderPath (Environment.SpecialFolder.Personal));
			
			if (extension != null) {
				FileFilter filterExt = new FileFilter ();
				filterExt.AddPattern ("*." + extension);
				filterExt.Name = GettextCatalog.GetString (name);
				dlg.AddFilter (filterExt);
			}
			FileFilter filterAll = new FileFilter ();
			filterAll.AddPattern ("*");
			filterAll.Name = GettextCatalog.GetString ("All files");
			dlg.AddFilter (filterAll);

			string filename = null;
			if (dlg.Run () == (int)ResponseType.Accept)
				filename = dlg.Filename;
			dlg.Destroy ();
			return filename;
		}
		
		protected virtual void CheckSupported (string profilerName)
		{
			string prefix = GetMonoPrefix ();
			if (prefix == null) {
				isSupported = false;
				return;
			}
			string dir = Path.Combine (prefix, "lib");
			
			string[] exts = new string[] {".so", ".dylib", ".dll"};
			foreach (string ext in exts) {
				string file = Path.Combine (dir, profilerName + ext);
				if (File.Exists (file)) {
					isSupported = true;
					return;
				}
			}
			
			isSupported = false;
		}
		
		//code taken from mono->Managed.Windows.Forms/System.Windows.Forms/Application.cs
		private static string GetMonoPrefix ()
		{
			PropertyInfo gac = typeof (Environment).GetProperty ("GacPath", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo get_gac = null;
			if (gac != null)
				get_gac = gac.GetGetMethod (true);

			if (get_gac != null) {
				string gac_path = Path.GetDirectoryName ((string)get_gac.Invoke (null, null));
				return Path.GetDirectoryName (Path.GetDirectoryName (gac_path));
			}

			return null;
		}
		
		protected internal class ApplicationExecutionHandlerFactory : IExecutionHandler
		{
			IProfiler profiler;
			
			public ApplicationExecutionHandlerFactory (IProfiler profiler)
			{
				this.profiler = profiler;
			}
			
			public bool CanExecute (ExecutionCommand command)
			{
				return command is DotNetExecutionCommand;
			}
			
			public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
			{
				MonoProfilerExecutionHandler h = new MonoProfilerExecutionHandler (profiler);
				return h.Execute (command, console);
			}
		}
		
		protected internal class ProcessExecutionHandlerFactory : IExecutionHandler
		{
			IProfiler profiler;
			Process process;
			
			public ProcessExecutionHandlerFactory (IProfiler profiler, Process process)
			{
				this.profiler = profiler;
				this.process = process;
			}

			public bool CanExecute (ExecutionCommand command)
			{
				return true;
			}
			
			public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
			{
				ProcessProfilerExecutionHandler h = new ProcessProfilerExecutionHandler (profiler, process);
				return h.Execute (command, console);
			}
		}
	}
}