// ThreadsPad.cs
//
// Author:
//   Alfonso Santos Luaces <asantosluaces@gmail.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Alfonso Santos Luaces
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


using Gtk;

using System;

using Stock = MonoDevelop.Ide.Gui.Stock;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Mono.Debugging.Client;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Components.AutoTest;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

namespace MonoDevelop.Debugger
{
	public class ThreadsPad : PadContent
	{
		ThreadsPadWidget control = new ThreadsPadWidget ();

		protected override void Initialize (IPadWindow window)
		{
			Id = "MonoDevelop.Debugger.ThreadsPad";
			control.Initialize (window);
		}

		public override Control Control {
			get {
				return control;
			}
		}
	}

	public class ThreadsPadWidget : Gtk.ScrolledWindow
	{
		TreeViewState treeViewState;
		PadTreeView tree;
		TreeStore store;
		bool needsUpdate;
		IPadWindow window;
		Clipboard clipboard;

		enum Columns
		{
			Icon,
			Id,
			Name,
			Object,
			Weight,
			Location,
			Session
		}

		public ThreadsPadWidget ()
		{
			this.ShadowType = ShadowType.None;

			store = new TreeStore (typeof (string), typeof (string), typeof (string), typeof (object), typeof (int), typeof (string), typeof (object));
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("store__Icon", "store__Id", "store_Name",
				"store_Object", "store_Weight", "store_Location");
			TypeDescriptor.AddAttributes (store, modelAttr);

			tree = new PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			treeViewState = new TreeViewState (tree, (int)Columns.Object);

			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererImage ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", (int)Columns.Icon);
			tree.AppendColumn (col);

			TreeViewColumn FrameCol = new TreeViewColumn ();
			FrameCol.Title = GettextCatalog.GetString ("Id");
			FrameCol.PackStart (tree.TextRenderer, true);
			FrameCol.AddAttribute (tree.TextRenderer, "text", (int)Columns.Id);
			FrameCol.AddAttribute (tree.TextRenderer, "weight", (int)Columns.Weight);
			FrameCol.Resizable = true;
			FrameCol.Alignment = 0.0f;
			tree.AppendColumn (FrameCol);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Name");
			col.Resizable = true;
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", (int)Columns.Name);
			col.AddAttribute (tree.TextRenderer, "weight", (int)Columns.Weight);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Location");
			col.Resizable = true;
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", (int)Columns.Location);
			col.AddAttribute (tree.TextRenderer, "weight", (int)Columns.Weight);
			tree.AppendColumn (col);

			Add (tree);
			ShowAll ();

			UpdateDisplay ();

			tree.RowActivated += OnRowActivated;
			tree.DoPopupMenu = ShowPopup;
			DebuggingService.CallStackChanged += OnStackChanged;
			DebuggingService.PausedEvent += OnDebuggerPaused;
			DebuggingService.ResumedEvent += OnDebuggerResumed;
			DebuggingService.StoppedEvent += OnDebuggerStopped;
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			TreeIter selected;

			if (!tree.Selection.GetSelected (out selected))
				return;

			var context_menu = new ContextMenu ();
			var copyMenuItem = new ContextMenuItem (GettextCatalog.GetString ("_Copy"));
			copyMenuItem.Sensitive = true;
			copyMenuItem.Clicked += CopyExecution_Clicked;
			context_menu.Items.Add (copyMenuItem);

			var process = store.GetValue (selected, (int)Columns.Object) as ProcessInfo;
			if (process != null) { //User right-clicked on thread and not process
				context_menu.Items.Add (new SeparatorContextMenuItem ());
				var session = store.GetValue (selected, (int)Columns.Session) as DebuggerSession;
				var continueExecution = new ContextMenuItem (GettextCatalog.GetString ("Resume"));
				continueExecution.Sensitive = !session.IsRunning;
				continueExecution.Clicked += delegate {
					session.Continue ();
				};
				context_menu.Items.Add (continueExecution);
				var pauseExecution = new ContextMenuItem (GettextCatalog.GetString ("Pause"));
				pauseExecution.Sensitive = session.IsRunning;
				pauseExecution.Clicked += delegate {
					session.Stop ();
				};
				context_menu.Items.Add (pauseExecution);
			}
			context_menu.Show (this, evt);
		}

		void CopyExecution_Clicked (object sender, ContextMenuItemClickedEventArgs e)
		{
			TreeIter selected;
			tree.Selection.GetSelected (out selected);

			var infoObject = tree.Model.GetValue (selected, (int)Columns.Object);

			string bufferText = string.Empty;
			if (infoObject is ThreadInfo threadInfo)
				bufferText = $"{threadInfo.Id} {threadInfo.Name} {threadInfo.Location}";

			if (infoObject is ProcessInfo processInfo) 
				bufferText = $"{processInfo.Id} {processInfo.Name} {processInfo.Description}";

			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = bufferText;
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = bufferText;
		}

		public override void Dispose ()
		{
			base.Dispose ();
			DebuggingService.CallStackChanged -= OnStackChanged;
			DebuggingService.PausedEvent -= OnDebuggerPaused;
			DebuggingService.ResumedEvent -= OnDebuggerResumed;
			DebuggingService.StoppedEvent -= OnDebuggerStopped;
		}

		void OnStackChanged (object s, EventArgs a)
		{
			UpdateDisplay ();
		}

		public void Initialize (IPadWindow window)
		{
			this.window = window;
			window.PadContentShown += delegate {
				if (needsUpdate)
					Update ();
			};
		}

		public void UpdateDisplay ()
		{
			if (window != null && window.ContentVisible)
				Update ();
			else
				needsUpdate = true;
		}

		List<(DebuggerSession session, ThreadInfo activeThread, List<(ProcessInfo process, ThreadInfo [] threads)> processes)> PreFetchSessionsWithProcessesAndThreads ()
		{
			var result = new List<(DebuggerSession, ThreadInfo activeThread, List<(ProcessInfo, ThreadInfo [])>)> ();
			foreach (var session in DebuggingService.GetSessions ()) {
				var processList = new List<(ProcessInfo process, ThreadInfo [] threads)> ();
				result.Add ((session, session.ActiveThread, processList));
				foreach (var process in session.GetProcesses ()) {
					processList.Add ((process, process.GetThreads ()));
				}
			}
			return result;
		}

		CancellationTokenSource cancelUpdate = new CancellationTokenSource ();

		async void Update ()
		{
			cancelUpdate.Cancel ();
			cancelUpdate = new CancellationTokenSource ();
			var token = cancelUpdate.Token;
			List<(DebuggerSession session, ThreadInfo activeThread, List<(ProcessInfo process, ThreadInfo [] threads)> processes)> sessions = null;
			try {
				sessions = await Task.Run (() => PreFetchSessionsWithProcessesAndThreads ());
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
				return;
			}
			// Another fetch of all data already in progress, return
			if (token.IsCancellationRequested)
				return;

			if (tree.IsRealized)
				tree.ScrollToPoint (0, 0);

			treeViewState.Save ();

			store.Clear ();

			try {
				if (sessions.SelectMany (s => s.processes).Count () > 1) {
					foreach (var sessionWithProcesses in sessions) {
						foreach (var processWithThreads in sessionWithProcesses.processes) {
							var iter = store.AppendValues (
								sessionWithProcesses.session.IsRunning ? "md-continue-debug" : "md-pause-debug",
								processWithThreads.process.Id.ToString (),
								processWithThreads.process.Name,
								processWithThreads.process,
								sessionWithProcesses.session == DebuggingService.DebuggerSession ? (int)Pango.Weight.Bold : (int)Pango.Weight.Normal,
								"",
								sessionWithProcesses);
							if (sessionWithProcesses.session.IsRunning)
								continue;
							AppendThreads (iter, processWithThreads.threads, sessionWithProcesses.session, sessionWithProcesses.activeThread);
						}
					}
				} else {
					if (!DebuggingService.IsPaused)
						return;
					AppendThreads (TreeIter.Zero, sessions [0].processes [0].threads, sessions [0].session, sessions [0].activeThread);
				}
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}

			tree.ExpandAll ();

			treeViewState.Load ();
		}

		void AppendThreads (TreeIter iter, ThreadInfo [] threads, DebuggerSession session, ThreadInfo activeThread)
		{
			Array.Sort (threads, (ThreadInfo t1, ThreadInfo t2) => t1.Id.CompareTo (t2.Id));

			session.FetchFrames (threads);

			foreach (var thread in threads) {
				var name = thread.Name == null && thread.Id == 1 ? GettextCatalog.GetString ("Main Thread") : thread.Name;
				var weight = thread == activeThread ? Pango.Weight.Bold : Pango.Weight.Normal;
				var icon = thread == activeThread ? Gtk.Stock.GoForward : null;

				if (iter.Equals (TreeIter.Zero))
					store.AppendValues (icon, thread.Id.ToString (), name, thread, (int)weight, thread.Location, session);
				else
					store.AppendValues (iter, icon, thread.Id.ToString (), name, thread, (int)weight, thread.Location, session);
			}
		}

		void UpdateThread (TreeIter iter, ThreadInfo thread, ThreadInfo activeThread)
		{
			var weight = thread == activeThread ? Pango.Weight.Bold : Pango.Weight.Normal;
			var icon = thread == activeThread ? Gtk.Stock.GoForward : null;

			store.SetValue (iter, (int)Columns.Weight, (int)weight);
			store.SetValue (iter, (int)Columns.Icon, icon);
		}

		void UpdateThreads (ThreadInfo activeThread)
		{
			TreeIter iter;

			if (!store.GetIterFirst (out iter))
				return;

			do {
				var thread = store.GetValue (iter, (int)Columns.Object) as ThreadInfo;

				if (thread == null) {
					store.SetValue (iter, (int)Columns.Weight, (int)(((ProcessInfo)store.GetValue (iter, (int)Columns.Object)).GetThreads ().Contains (activeThread) ? Pango.Weight.Bold : Pango.Weight.Normal));
					var sessionActiveThread = ((DebuggerSession)store.GetValue (iter, (int)Columns.Session)).ActiveThread;
					// this is a process... descend into our children
					TreeIter child;

					if (store.IterChildren (out child, iter)) {
						do {
							thread = store.GetValue (child, (int)Columns.Object) as ThreadInfo;
							UpdateThread (child, thread, sessionActiveThread);
						} while (store.IterNext (ref child));
					}
				} else {
					UpdateThread (iter, thread, activeThread);
				}
			} while (store.IterNext (ref iter));
		}

		void OnRowActivated (object s, RowActivatedArgs args)
		{
			TreeIter selected;

			if (!tree.Selection.GetSelected (out selected))
				return;

			var thread = store.GetValue (selected, (int)Columns.Object) as ThreadInfo;

			if (thread != null) {
				DebuggingService.CallStackChanged -= OnStackChanged;

				try {
					// Note: setting the active thread causes CallStackChanged to be emitted, but we don't want to refresh our thread list.
					DebuggingService.ActiveThread = thread;
					UpdateThreads (thread);
				} finally {
					DebuggingService.CallStackChanged += OnStackChanged;
				}
			}
		}

		void OnDebuggerPaused (object s, EventArgs a)
		{
			UpdateDisplay ();
		}

		void OnDebuggerResumed (object s, EventArgs a)
		{
			UpdateDisplay ();
		}

		void OnDebuggerStopped (object s, EventArgs a)
		{
			UpdateDisplay ();
		}
	}
}
