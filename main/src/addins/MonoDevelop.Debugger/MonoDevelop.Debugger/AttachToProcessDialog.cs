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
using System.Threading;

namespace MonoDevelop.Debugger
{
	public partial class AttachToProcessDialog : Gtk.Dialog
	{
		List<DebuggerEngine> currentDebEngines;
		Dictionary<long, List<DebuggerEngine>> procEngines;
		List<ProcessInfo> procs;
		Gtk.ListStore store;
		TreeViewState state;
		bool closed;

		public AttachToProcessDialog()
		{
			this.Build();

			store = new Gtk.ListStore (typeof(ProcessInfo), typeof(string), typeof(string));
			tree.Model = store;
			tree.AppendColumn ("PID", new Gtk.CellRendererText (), "text", 1);
			tree.AppendColumn (GettextCatalog.GetString ("Process Name"), new Gtk.CellRendererText (), "text", 2);
			tree.RowActivated += OnRowActivated;

			state = new TreeViewState (tree, 1);

			var refreshThread = new Thread (new ThreadStart (Refresh));
			refreshThread.IsBackground = true;
			refreshThread.Start ();

			comboDebs.Sensitive = false;
			buttonOk.Sensitive = false;
			tree.Selection.UnselectAll ();
			tree.Selection.Changed += OnSelectionChanged;

			Gtk.TreeIter it;
			if (store.GetIterFirst (out it))
				tree.Selection.SelectIter (it);
		}

		public override void Destroy ()
		{
			closed = true;
			base.Destroy ();
		}

		void Refresh ()
		{
			while (!closed) {
				var procEngines = new Dictionary<long, List<DebuggerEngine>> ();
				var procs = new List<ProcessInfo> ();

				foreach (DebuggerEngine de in DebuggingService.GetDebuggerEngines ()) {
					if ((de.SupportedFeatures & DebuggerFeatures.Attaching) == 0)
						continue;
					try {
						var infos = de.GetAttachableProcesses ();
						foreach (ProcessInfo pi in infos) {
							List<DebuggerEngine> engs;
							if (!procEngines.TryGetValue (pi.Id, out engs)) {
								engs = new List<DebuggerEngine> ();
								procEngines [pi.Id] = engs;
								procs.Add (pi);
							}
							engs.Add (de);
						}
					} catch (Exception ex) {
						LoggingService.LogError ("Could not get attachable processes.", ex);
					}
				}
				this.procEngines = procEngines;
				this.procs = procs;
				Runtime.RunInMainThread (new Action(FillList)).Ignore ();
				Thread.Sleep (3000);
			}
		}

		void FillList ()
		{
			state.Save ();
			tree.Model = null;
			store.Clear ();
			string filter = entryFilter.Text;
			foreach (ProcessInfo pi in procs) {
				if (filter.Length == 0 || pi.Id.ToString().Contains (filter) || pi.Name.Contains (filter))
					store.AppendValues (pi, pi.Id.ToString (), pi.Name);
			}
			tree.Model = store;
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
				foreach (DebuggerEngine de in currentDebEngines) {
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

		void OnRowActivated (object o, Gtk.RowActivatedArgs args)
		{
			Respond (Gtk.ResponseType.Ok);
		}

		public ProcessInfo SelectedProcess {
			get {
				Gtk.TreeIter iter;
				tree.Selection.GetSelected (out iter);
				return (ProcessInfo) store.GetValue (iter, 0);
			}
		}

		public DebuggerEngine SelectedDebugger {
			get {
				return currentDebEngines [comboDebs.Active];
			}
		}
	}
}
