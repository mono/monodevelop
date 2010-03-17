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
using System.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Profiling
{
	public partial class SelectProcessDialog : Gtk.Dialog
	{
		private ListStore store;
		
		private const int colName = 0;
		private const int colProfiler = 1;
		private const int colProfilerIdentifier = 2;
		private const int colPid = 3;
		private const int colProcess = 4;
		
		private Process process;
		private string profiler;
		
		public SelectProcessDialog()
		{
			this.Build();
			
			store = new ListStore (typeof (string), typeof (string), typeof (string), typeof (string), typeof (object));
			list.Model = store;
			list.RulesHint = true;
			
			CellRendererText nameRenderer = new CellRendererText ();
			CellRendererText profilerRenderer = new CellRendererText ();
			CellRendererText pidRenderer = new CellRendererText ();
			
			TreeViewColumn columnName = new TreeViewColumn ();
			TreeViewColumn columnProfiler = new TreeViewColumn ();
			TreeViewColumn columnPid = new TreeViewColumn ();
			
			columnName.Title = GettextCatalog.GetString ("Name");
			columnProfiler.Title = GettextCatalog.GetString ("Profiler");
			columnPid.Title = GettextCatalog.GetString ("Pid");
			
			columnName.PackStart (nameRenderer, true);
			columnProfiler.PackStart (profilerRenderer, true);
			columnPid.PackStart (pidRenderer, true);
			
			columnName.AddAttribute (nameRenderer, "text", colName);
			columnProfiler.AddAttribute (profilerRenderer, "text", colProfiler);
			columnPid.AddAttribute (pidRenderer, "text", colPid);
			
			list.AppendColumn (columnName);
			list.AppendColumn (columnProfiler);
			list.AppendColumn (columnPid);
			
			list.Selection.Changed += new EventHandler (OnSelectionChanged);
			list.ShowAll ();
			
			ListProcesses ();
		}
		
		public Process Process {
			get { return process; }
		}
		
		public string ProfilerIdentifier {
			get { return profiler; }
		}
		
		public IProfiler Profiler {
			get { return ProfilingService.GetProfiler (profiler); }
		}
		
		private void ListProcesses ()
		{
			System.Threading.ThreadPool.QueueUserWorkItem (new System.Threading.WaitCallback (ListProcessesAsync));
		}
		
		private void ListProcessesAsync (object state)
		{
			foreach (Process proc in Process.GetProcesses ()) {
				string profiler;
				string filename;
				if (ProfilingService.GetProfilerInformation (proc.Id, out profiler, out filename)) {
					IProfiler prof = ProfilingService.GetProfiler (profiler);					
					if (prof != null && prof.IsSupported) {
						DispatchService.GuiDispatch (delegate () {
							store.AppendValues (proc.ProcessName, prof.Name, profiler, proc.Id.ToString (), proc);
						});
					}
				}
			}
		}
		
		private void OnSelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (list.Selection.GetSelected (out iter)) {
				process = (Process)store.GetValue (iter, colProcess);
				profiler = (string)store.GetValue (iter, colProfilerIdentifier);
				buttonOk.Sensitive = true;
			} else {
				buttonOk.Sensitive = false;
			}
		}

		protected virtual void OkClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Ok);
			this.Hide ();
		}

		protected virtual void CancelClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Cancel);
			this.Hide ();
		}
	}
}
