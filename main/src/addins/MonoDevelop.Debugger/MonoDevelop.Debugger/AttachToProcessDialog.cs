// AttachToProcessDialog.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Components;
using Mono.Debugging.Client;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Debugger
{
	public partial class AttachToProcessDialog : Gtk.Dialog
	{
		List<IDebuggerEngine> currentDebEngines;
		Dictionary<int, List<IDebuggerEngine>> procEngines = new Dictionary<int,List<IDebuggerEngine>> ();
		List<ProcessInfo> procs = new List<ProcessInfo> ();
		Gtk.ListStore store;
		TreeViewState state;
		
		public AttachToProcessDialog()
		{
			this.Build();
			
			store = new Gtk.ListStore (typeof(ProcessInfo), typeof(string), typeof(string));
			tree.Model = store;
			tree.AppendColumn ("PID", new Gtk.CellRendererText (), "text", 1);
			tree.AppendColumn ("Process Name", new Gtk.CellRendererText (), "text", 2);
			
			IDebuggerEngine[] debEngines = DebuggingService.GetDebuggerEngines ();
			foreach (IDebuggerEngine de in debEngines) {
				if ((de.SupportedFeatures & DebuggerFeatures.Attaching) == 0)
					continue;
				try {
					foreach (ProcessInfo pi in de.GetAttachablePocesses ()) {
						List<IDebuggerEngine> engs;
						if (!procEngines.TryGetValue (pi.Id, out engs)) {
							engs = new List<IDebuggerEngine> ();
							procEngines [pi.Id] = engs;
							procs.Add (pi);
						}
						engs.Add (de);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Could not get attachablbe processes.", ex);
				}
				comboDebs.AppendText (de.Name);
			}
			
			state = new TreeViewState (tree, 1);
			
			FillList ();
			
			comboDebs.Sensitive = false;
			buttonOk.Sensitive = false;
			tree.Selection.Changed += OnSelectionChanged;
			
			Gtk.TreeIter it;
			if (store.GetIterFirst (out it))
				tree.Selection.SelectIter (it);
		}
		
		void FillList ()
		{
			state.Save ();
			
			store.Clear ();
			string filter = entryFilter.Text;
			foreach (ProcessInfo pi in procs) {
				if (filter.Length == 0 || pi.Id.ToString().Contains (filter) || pi.Name.Contains (filter))
					store.AppendValues (pi, pi.Id.ToString (), pi.Name);
			}
			
			state.Load ();
			
			if (tree.Selection.CountSelectedRows () == 0) {
				Gtk.TreeIter it;
				if (store.GetIterFirst (out it))
					tree.Selection.SelectIter (it);
			}
		}
		
		void OnSelectionChanged (object s, EventArgs args)
		{
			((Gtk.ListStore)comboDebs.Model).Clear ();
			
			Gtk.TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				ProcessInfo pi = (ProcessInfo) store.GetValue (iter, 0);
				currentDebEngines = procEngines [pi.Id];
				foreach (IDebuggerEngine de in currentDebEngines) {
					comboDebs.AppendText (de.Name);
				}
				comboDebs.Sensitive = true;
				buttonOk.Sensitive = currentDebEngines.Count > 0;
				comboDebs.Active = 0;
			}
			else {
				comboDebs.Sensitive = false;
				buttonOk.Sensitive = false;
			}
		}

		protected virtual void OnEntryFilterChanged (object sender, System.EventArgs e)
		{
			FillList ();
		}
		
		public ProcessInfo SelectedProcess {
			get {
				Gtk.TreeIter iter;
				tree.Selection.GetSelected (out iter);
				return (ProcessInfo) store.GetValue (iter, 0);
			}
		}
		
		public IDebuggerEngine SelectedDebugger {
			get {
				return currentDebEngines [comboDebs.Active];
			}
		}
	}
}
