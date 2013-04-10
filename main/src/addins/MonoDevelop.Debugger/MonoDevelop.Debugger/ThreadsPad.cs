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
		PadTreeView tree;
		TreeStore store;
		
		TreeViewState treeViewState;
		
		enum Columns
		{
			Icon,
			Id,
			Name,
			Object,
			Weight,
			Location
		}
		
		public ThreadsPad()
		{
			this.ShadowType = ShadowType.None;

			store = new TreeStore (typeof(string), typeof (string), typeof(string), typeof(object), typeof(int), typeof(string));

			tree = new PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			treeViewState = new TreeViewState (tree, (int)Columns.Object);
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererIcon ();
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
		}
		
		public void UpdateDisplay ()
		{
			if (tree.IsRealized)
				tree.ScrollToPoint (0, 0);

			treeViewState.Save ();
			
			store.Clear ();

			if (!DebuggingService.IsPaused)
				return;
			
			try {
				ProcessInfo[] currentProcesses = DebuggingService.DebuggerSession.GetProcesses ();
				
				if (currentProcesses.Length == 1) {
					AppendThreads (TreeIter.Zero, currentProcesses [0]);
				}
				else {
					foreach (ProcessInfo p in currentProcesses) {
						TreeIter it = store.AppendValues (null, p.Id.ToString (), p.Name, p, (int) Pango.Weight.Normal, "");
						AppendThreads (it, p);
					}
				}
			} catch (Exception ex) {
				MessageService.ShowException (ex);
			}
			
			tree.ExpandAll ();
			
			treeViewState.Load ();
		}
		
		void AppendThreads (TreeIter it, ProcessInfo p)
		{
			ThreadInfo[] threads = p.GetThreads ();
			Array.Sort (threads, delegate (ThreadInfo t1, ThreadInfo t2) {
				return t1.Id.CompareTo (t2.Id);
			});
			foreach (ThreadInfo t in threads) {
				ThreadInfo activeThread = DebuggingService.DebuggerSession.ActiveThread;
				Pango.Weight wi = t == activeThread ? Pango.Weight.Bold : Pango.Weight.Normal;
				string icon = t == activeThread ? Gtk.Stock.GoForward : null;
				if (it.Equals (TreeIter.Zero))
					store.AppendValues (icon, t.Id.ToString (), t.Name, t, (int) wi, t.Location);
				else
					store.AppendValues (it, icon, t.Id.ToString (), t.Name, t, (int) wi, t.Location);
			}
		}
		
		void OnRowActivated (object s, Gtk.RowActivatedArgs args)
		{
			TreeIter it;
			tree.Selection.GetSelected (out it);
			ThreadInfo t = store.GetValue (it, (int)Columns.Object) as ThreadInfo;
			if (t != null)
				DebuggingService.ActiveThread = t;
		}
		
		public Gtk.Widget Control {
			get {
				return this;
			}
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
			Sensitive = true;
			UpdateDisplay ();
		}
		
		void OnDebuggerResumed (object s, EventArgs a)
		{
			Sensitive = false;
			UpdateDisplay ();
		}
		
		void OnDebuggerStopped (object s, EventArgs a)
		{
			Sensitive = false;
			UpdateDisplay ();
		}
	}
}
