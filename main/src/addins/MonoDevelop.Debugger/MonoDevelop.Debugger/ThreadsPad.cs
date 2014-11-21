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


namespace MonoDevelop.Debugger
{
	public class ThreadsPad : Gtk.ScrolledWindow, IPadContent
	{
		TreeViewState treeViewState;
		PadTreeView tree;
		TreeStore store;
		bool needsUpdate;
		IPadWindow window;
		
		enum Columns
		{
			Icon,
			Id,
			Name,
			Object,
			Weight,
			Location
		}
		
		public ThreadsPad ()
		{
			this.ShadowType = ShadowType.None;

			store = new TreeStore (typeof(string), typeof (string), typeof(string), typeof(object), typeof(int), typeof(string));

			tree = new PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			treeViewState = new TreeViewState (tree, (int)Columns.Object);
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererImage ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", (int) Columns.Icon);
			tree.AppendColumn (col);
				
			TreeViewColumn FrameCol = new TreeViewColumn ();
			FrameCol.Title = GettextCatalog.GetString ("Id");
			FrameCol.PackStart (tree.TextRenderer, true);
			FrameCol.AddAttribute (tree.TextRenderer, "text", (int) Columns.Id);
			FrameCol.AddAttribute (tree.TextRenderer, "weight", (int) Columns.Weight);
			FrameCol.Resizable = true;
			FrameCol.Alignment = 0.0f;
			tree.AppendColumn (FrameCol);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Name");
			col.Resizable = true;
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", (int) Columns.Name);
			col.AddAttribute (tree.TextRenderer, "weight", (int) Columns.Weight);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Location");
			col.Resizable = true;
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", (int) Columns.Location);
			col.AddAttribute (tree.TextRenderer, "weight", (int) Columns.Weight);
			tree.AppendColumn (col);
			
			Add (tree);
			ShowAll ();
			
			UpdateDisplay ();
			
			tree.RowActivated += OnRowActivated;
			DebuggingService.CallStackChanged += OnStackChanged;
			DebuggingService.PausedEvent += OnDebuggerPaused;
			DebuggingService.ResumedEvent += OnDebuggerResumed;
			DebuggingService.StoppedEvent += OnDebuggerStopped;
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
		
		void IPadContent.Initialize (IPadWindow window)
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

		void Update ()
		{
			if (tree.IsRealized)
				tree.ScrollToPoint (0, 0);

			treeViewState.Save ();
			
			store.Clear ();

			if (!DebuggingService.IsPaused)
				return;

			try {
				var processes = DebuggingService.DebuggerSession.GetProcesses ();
				
				if (processes.Length == 1) {
					AppendThreads (TreeIter.Zero, processes[0]);
				} else {
					foreach (var process in processes) {
						TreeIter iter = store.AppendValues (null, process.Id.ToString (), process.Name, process, (int) Pango.Weight.Normal, "");
						AppendThreads (iter, process);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
			
			tree.ExpandAll ();
			
			treeViewState.Load ();
		}

		void AppendThreads (TreeIter iter, ProcessInfo process)
		{
			var threads = process.GetThreads ();

			Array.Sort (threads, (ThreadInfo t1, ThreadInfo t2) => t1.Id.CompareTo (t2.Id));

			DebuggingService.DebuggerSession.FetchFrames (threads);

			foreach (var thread in threads) {
				ThreadInfo activeThread = DebuggingService.DebuggerSession.ActiveThread;
				var name = thread.Name == null && thread.Id == 1 ? "Main Thread" : thread.Name;
				var weight = thread == activeThread ? Pango.Weight.Bold : Pango.Weight.Normal;
				var icon = thread == activeThread ? Gtk.Stock.GoForward : null;

				if (iter.Equals (TreeIter.Zero))
					store.AppendValues (icon, thread.Id.ToString (), name, thread, (int) weight, thread.Location);
				else
					store.AppendValues (iter, icon, thread.Id.ToString (), name, thread, (int) weight, thread.Location);
			}
		}

		void UpdateThread (TreeIter iter, ThreadInfo thread, ThreadInfo activeThread)
		{
			var weight = thread == activeThread ? Pango.Weight.Bold : Pango.Weight.Normal;
			var icon = thread == activeThread ? Gtk.Stock.GoForward : null;

			store.SetValue (iter, (int) Columns.Weight, (int) weight);
			store.SetValue (iter, (int) Columns.Icon, icon);
		}

		void UpdateThreads (ThreadInfo activeThread)
		{
			TreeIter iter;

			if (!store.GetIterFirst (out iter))
				return;

			do {
				var thread = store.GetValue (iter, (int) Columns.Object) as ThreadInfo;

				if (thread == null) {
					// this is a process... descend into our children
					TreeIter child;

					if (store.IterChildren (out child)) {
						do {
							thread = store.GetValue (iter, (int) Columns.Object) as ThreadInfo;
							UpdateThread (child, thread, activeThread);
						} while (store.IterNext (ref child));
					}
				} else {
					UpdateThread (iter, thread, activeThread);
				}
			} while (store.IterNext (ref iter));
		}
		
		void OnRowActivated (object s, RowActivatedArgs args)
		{
			TreeIter iter, selected;

			if (!tree.Selection.GetSelected (out selected))
				return;

			var thread = store.GetValue (selected, (int) Columns.Object) as ThreadInfo;

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

		public Widget Control {
			get { return this; }
		}

		public string Id {
			get { return "MonoDevelop.Debugger.ThreadsPad"; }
		}

		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public void RedrawContent ()
		{
			UpdateDisplay ();
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
