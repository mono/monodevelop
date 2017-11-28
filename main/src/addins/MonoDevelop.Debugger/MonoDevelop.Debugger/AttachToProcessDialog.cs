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
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	public partial class AttachToProcessDialog : Gtk.Dialog
	{
		List<DebuggerEngine> debugEngines = new List<DebuggerEngine> ();
		DebuggerEngine selectedEngine;
		ProcessInfo [] procs;
		Gtk.ListStore store;
		TreeViewState state;
		ProcessAttacher processAttacher;
		CancellationTokenSource refreshLoopTokenSource = new CancellationTokenSource ();

		public AttachToProcessDialog ()
		{
			this.Build ();

			store = new Gtk.ListStore (typeof (ProcessInfo), typeof (string), typeof (string), typeof (string));
			tree.Model = store;
			tree.AppendColumn ("PID", new Gtk.CellRendererText (), "text", 1);
			tree.AppendColumn (GettextCatalog.GetString ("Name"), new Gtk.CellRendererText (), "text", 2);
			tree.AppendColumn (GettextCatalog.GetString ("Description"), new Gtk.CellRendererText (), "text", 3);
			tree.RowActivated += OnRowActivated;

			state = new TreeViewState (tree, 1);

			buttonOk.Sensitive = false;
			tree.Selection.UnselectAll ();
			tree.Selection.Changed += OnSelectionChanged;

			Gtk.TreeIter it;
			if (store.GetIterFirst (out it))
				tree.Selection.SelectIter (it);

			//Logic below tries to CreateExecutionCommand which is used to determine default debug engine
			var startupConfig = IdeApp.ProjectOperations.CurrentSelectedSolution?.StartupConfiguration as SingleItemSolutionRunConfiguration;
			ExecutionCommand executionCommand = null;
			if (startupConfig?.Item is DotNetProject dnp) {
				var config = dnp.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
				var runProjectConfiguration = startupConfig.RunConfiguration as ProjectRunConfiguration ?? dnp.GetDefaultRunConfiguration () as ProjectRunConfiguration;
				if (config != null) {
					executionCommand = dnp.CreateExecutionCommand (IdeApp.Workspace.ActiveConfiguration, config, runProjectConfiguration);
				}
			}
			DebuggerEngine defaultEngine = null;
			foreach (DebuggerEngine de in DebuggingService.GetDebuggerEngines ()) {
				if ((de.SupportedFeatures & DebuggerFeatures.Attaching) == 0)
					continue;
				if (executionCommand != null && de.CanDebugCommand (executionCommand))
					defaultEngine = de;
				debugEngines.Add (de);
				comboDebs.AppendText (de.Name);
			}
			if (!debugEngines.Any ())
				return;
			if (defaultEngine == null)
				defaultEngine = debugEngines.First ();
			comboDebs.Active = debugEngines.IndexOf (defaultEngine);
			ChangeEngine (defaultEngine);
			comboDebs.Changed += delegate {
				ChangeEngine (debugEngines [comboDebs.Active]);
			};
		}

		private void ChangeEngine (DebuggerEngine newEngine)
		{
			if (selectedEngine == newEngine)
				return;
			selectedEngine = newEngine;

			refreshLoopTokenSource.Cancel ();
			refreshLoopTokenSource = new CancellationTokenSource ();

			if (processAttacher != null) {
				processAttacher.AttachableProcessesChanged -= ProcessAttacher_AttachableProcessesChanged;
				processAttacher.Dispose ();
			}

			processAttacher = selectedEngine.GetProcessAttacher ();
			if (processAttacher != null) {
				processAttacher.AttachableProcessesChanged += ProcessAttacher_AttachableProcessesChanged;
				this.procs = processAttacher.GetAttachableProcesses ();
				Runtime.RunInMainThread (new Action (FillList)).Ignore ();
			} else {
				var refreshThread = new Thread (new ParameterizedThreadStart (Refresh));
				refreshThread.IsBackground = true;
				refreshThread.Start (refreshLoopTokenSource.Token);
			}
		}

		void ProcessAttacher_AttachableProcessesChanged (Debugger.ProcessAttacher sender)
		{
			this.procs = sender.GetAttachableProcesses ();
			Runtime.RunInMainThread (new Action (FillList)).Ignore ();
		}

		protected override void OnDestroyed ()
		{
			if (processAttacher != null) {
				processAttacher.AttachableProcessesChanged -= ProcessAttacher_AttachableProcessesChanged;
				processAttacher.Dispose ();
			}
			refreshLoopTokenSource.Cancel ();
			base.OnDestroyed ();
		}

		void Refresh (object tokenObject)
		{
			var token = (CancellationToken)tokenObject;
			var engine = selectedEngine;
			while (!token.IsCancellationRequested) {
				try {
					this.procs = engine.GetAttachableProcesses ();
				} catch (Exception ex) {
					LoggingService.LogError ("Could not get attachable processes.", ex);
				}
				Runtime.RunInMainThread (new Action (FillList)).Ignore ();
				Thread.Sleep (3000);
			}
		}

		void FillList ()
		{
			state.Save ();
			tree.Model = null;
			store.Clear ();
			string filter = entryFilter.Text;
			bool anyPidSet = false;
			bool anyDescriptionSet = false;
			foreach (ProcessInfo pi in procs) {
				if (pi.Id != 0)
					anyPidSet = true;
				if (pi.Description != null)
					anyDescriptionSet = true;
				if (filter.Length == 0 || (pi.Id != 0 && pi.Id.ToString ().Contains (filter)) || pi.Name.Contains (filter) || (pi.Description?.Contains (filter) ?? false))
					store.AppendValues (pi, pi.Id.ToString (), pi.Name, pi.Description);
			}
			tree.Columns [0].Visible = anyPidSet;
			tree.Columns [2].Visible = anyDescriptionSet;
			tree.Model = store;
			state.Load ();
			tree.ColumnsAutosize ();
			if (tree.Selection.CountSelectedRows () == 0) {
				Gtk.TreeIter it;
				if (store.GetIterFirst (out it))
					tree.Selection.SelectIter (it);
			}
		}

		void OnSelectionChanged (object s, EventArgs args)
		{
			Gtk.TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				buttonOk.Sensitive = true;
			} else {
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
				return (ProcessInfo)store.GetValue (iter, 0);
			}
		}

		public DebuggerEngine SelectedDebugger {
			get {
				return selectedEngine;
			}
		}
	}
}
