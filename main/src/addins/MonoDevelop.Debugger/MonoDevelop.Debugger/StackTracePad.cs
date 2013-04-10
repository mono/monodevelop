// StackTracePad.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text;

using Gtk;

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components.Commands;
using Stock = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Debugger
{
	public class StackTracePad : Gtk.ScrolledWindow, IPadContent
	{
		Backtrace current_backtrace;

		PadTreeView tree;
		Gtk.ListStore store;
		CellRendererIcon refresh;
		bool needsUpdate;
		IPadWindow window;
		CommandEntrySet menuSet;

		public StackTracePad ()
		{
			this.ShadowType = ShadowType.None;

			ActionCommand evalCmd = new ActionCommand ("StackTracePad.EvaluateMethodParams", GettextCatalog.GetString ("Evaluate Method Parameters"));
			ActionCommand gotoCmd = new ActionCommand ("StackTracePad.ActivateFrame", GettextCatalog.GetString ("Activate Stack Frame"));
			
			menuSet = new CommandEntrySet ();
			menuSet.Add (evalCmd);
			menuSet.Add (gotoCmd);
			menuSet.AddSeparator ();
			menuSet.AddItem (EditCommands.SelectAll);
			menuSet.AddItem (EditCommands.Copy);
			
			store = new ListStore (typeof (string), typeof (string), typeof (string), typeof (string), typeof (string), typeof (string), typeof (Pango.Style), typeof (object), typeof (bool));

			tree = new PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			tree.Selection.Mode = SelectionMode.Multiple;
			tree.EnableSearch = true;
			tree.SearchColumn = 1;
			tree.ButtonPressEvent += HandleButtonPressEvent;
			tree.DoPopupMenu = ShowPopup;

			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererIcon ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", 0);
			tree.AppendColumn (col);
			
			TreeViewColumn FrameCol = new TreeViewColumn ();
			FrameCol.Title = GettextCatalog.GetString ("Name");
			refresh = new CellRendererIcon ();
			refresh.Pixbuf = ImageService.GetPixbuf (Gtk.Stock.Refresh).ScaleSimple (12, 12, Gdk.InterpType.Hyper);
			FrameCol.PackStart (refresh, false);
			FrameCol.AddAttribute (refresh, "visible", 8);
			FrameCol.PackStart (tree.TextRenderer, true);
			FrameCol.AddAttribute (tree.TextRenderer, "text", 1);
			FrameCol.AddAttribute (tree.TextRenderer, "foreground", 5);
			FrameCol.AddAttribute (tree.TextRenderer, "style", 6);
			FrameCol.Resizable = true;
			FrameCol.Alignment = 0.0f;
			tree.AppendColumn (FrameCol);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("File");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", 2);
			col.AddAttribute (tree.TextRenderer, "foreground", 5);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Language");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", 3);
			col.AddAttribute (tree.TextRenderer, "foreground", 5);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Address");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", 4);
			col.AddAttribute (tree.TextRenderer, "foreground", 5);
			tree.AppendColumn (col);
			
			Add (tree);
			ShowAll ();

			current_backtrace = DebuggingService.CurrentCallStack;
			UpdateDisplay ();
			
			DebuggingService.CallStackChanged += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnClassStackChanged));
			DebuggingService.CurrentFrameChanged += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnFrameChanged));
			tree.RowActivated += OnRowActivated;
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

		string EvaluateMethodName (StackFrame frame, EvaluationOptions options)
		{
			StringBuilder method = new StringBuilder (frame.SourceLocation.MethodName);
			ObjectValue[] args = frame.GetParameters (options);

			if (args.Length != 0 || !frame.SourceLocation.MethodName.StartsWith ("[", StringComparison.Ordinal)) {
				method.Append (" (");
				for (int n = 0; n < args.Length; n++) {
					if (n > 0)
						method.Append (", ");
					method.Append (args[n].Name).Append ("=").Append (args[n].Value);
				}
				method.Append (")");
			}

			return method.ToString ();
		}

		void Update ()
		{
			if (tree.IsRealized)
				tree.ScrollToPoint (0, 0);

			needsUpdate = false;
			store.Clear ();

			if (current_backtrace == null)
				return;

			var options = DebuggingService.DebuggerSession.Options.EvaluationOptions;

			for (int i = 0; i < current_backtrace.FrameCount; i++) {
				string icon;
				if (i == DebuggingService.CurrentFrameIndex)
					icon = Gtk.Stock.GoForward;
				else
					icon = null;
				
				StackFrame frame = current_backtrace.GetFrame (i);
				if (frame.IsDebuggerHidden)
					continue;
				
				var method = EvaluateMethodName (frame, options);
				
				string file;
				if (!string.IsNullOrEmpty (frame.SourceLocation.FileName)) {
					file = frame.SourceLocation.FileName;
					if (frame.SourceLocation.Line != -1)
						file += ":" + frame.SourceLocation.Line;
				} else
					file = string.Empty;

				var style = frame.IsExternalCode ? Pango.Style.Italic : Pango.Style.Normal;
				
				store.AppendValues (icon, method, file, frame.Language, "0x" + frame.Address.ToString ("x"), null,
				                    style, frame, !options.AllowDisplayStringEvaluation);
			}
		}

		bool GetCellAtPos (int x, int y, out TreePath path, out TreeViewColumn col, out CellRenderer cellRenderer)
		{
			int cx, cy;

			if (tree.GetPathAtPos (x, y, out path, out col, out cx, out cy)) {
				tree.GetCellArea (path, col);
				foreach (CellRenderer cr in col.CellRenderers) {
					int xo, w;

					col.CellGetPosition (cr, out xo, out w);
					if (cr.Visible && cx >= xo && cx < xo + w) {
						cellRenderer = cr;
						return true;
					}
				}
			}

			cellRenderer = null;
			return false;
		}

		[GLib.ConnectBefore]
		void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			TreeViewColumn col;
			CellRenderer cr;
			TreePath path;
			TreeIter iter;

			if (args.Event.Button != 1 || !GetCellAtPos ((int) args.Event.X, (int) args.Event.Y, out path, out col, out cr))
				return;

			if (!store.GetIter (out iter, path))
				return;

			if (cr == refresh) {
				var options = DebuggingService.DebuggerSession.Options.EvaluationOptions.Clone ();
				options.AllowMethodEvaluation = true;
				options.AllowToStringCalls = true;

				var frame = (StackFrame) store.GetValue (iter, 7);
				var method = EvaluateMethodName (frame, options);

				store.SetValue (iter, 1, method);
				store.SetValue (iter, 8, false);
			}
		}
		
		public void UpdateCurrentFrame ()
		{
			TreeIter it;
			int n = 0;

			if (!store.GetIterFirst (out it))
				return;

			do {
				if (n == DebuggingService.CurrentFrameIndex)
					store.SetValue (it, 0, Gtk.Stock.GoForward);
				else
					store.SetValue (it, 0, null);
				n++;
			} while (store.IterNext (ref it));
		}

		protected void OnFrameChanged (object o, EventArgs args)
		{
			UpdateCurrentFrame ();
		}

		protected void OnClassStackChanged (object o, EventArgs args)
		{
			current_backtrace = DebuggingService.CurrentCallStack;
			UpdateDisplay ();
		}
		
		void OnRowActivated (object o, Gtk.RowActivatedArgs args)
		{
			ActivateFrame ();
		}

		public Gtk.Widget Control {
			get {
				return this;
			}
		}

		public string Id {
			get { return "MonoDevelop.Debugger.StackTracePad"; }
		}

		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public void RedrawContent ()
		{
			UpdateDisplay ();
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			IdeApp.CommandService.ShowContextMenu (tree, evt, menuSet, tree);
		}

		[CommandHandler ("StackTracePad.EvaluateMethodParams")]
		void EvaluateMethodParams ()
		{
			TreeIter iter;

			if (!store.GetIterFirst (out iter))
				return;

			var options = DebuggingService.DebuggerSession.Options.EvaluationOptions.Clone ();
			options.AllowMethodEvaluation = true;
			options.AllowToStringCalls = true;

			do {
				if ((bool) store.GetValue (iter, 8)) {
					var frame = (StackFrame) store.GetValue (iter, 7);
					var method = EvaluateMethodName (frame, options);

					store.SetValue (iter, 1, method);
					store.SetValue (iter, 8, false);
				}
			} while (store.IterNext (ref iter));
		}
		
		[CommandHandler ("StackTracePad.ActivateFrame")]
		void ActivateFrame ()
		{
			TreePath[] selected = tree.Selection.GetSelectedRows ();
			if (selected.Length > 0)
				DebuggingService.CurrentFrameIndex = selected[0].Indices [0];
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		internal void OnSelectAll ()
		{
			tree.Selection.SelectAll ();
		}
		
		[CommandHandler (EditCommands.Copy)]
		internal void OnCopy ()
		{
			TreeModel model;
			StringBuilder txt = new StringBuilder ();
			foreach (Gtk.TreePath p in tree.Selection.GetSelectedRows (out model)) {
				TreeIter it;
				if (!model.GetIter (out it, p))
					continue;
				string met = (string) model.GetValue (it, 1);
				string file = (string) model.GetValue (it, 2);
				if (txt.Length > 0)
					txt.Append ('\n');
				txt.AppendFormat ("{0} in {1}", met, file);
			}
			Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = txt.ToString ();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = txt.ToString ();
		}		
	}
}
