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
using MonoDevelop.Components;

namespace MonoDevelop.Debugger
{
	public class StackTracePad : ScrolledWindow, IPadContent
	{
		const int IconColumn = 0;
		const int MethodColumn = 1;
		const int FileColumn = 2;
		const int LangColumn = 3;
		const int AddrColumn = 4;
		const int ForegroundColumn = 5;
		const int StyleColumn = 6;
		const int FrameColumn = 7;
		const int FrameIndexColumn = 8;
		const int CanRefreshColumn = 9;

		readonly CellRendererImage refresh;
		readonly CommandEntrySet menuSet;
		readonly PadTreeView tree;
		readonly ListStore store;
		IPadWindow window;
		bool needsUpdate;

		static Xwt.Drawing.Image pointerImage = Xwt.Drawing.Image.FromResource ("stack-pointer-light-16.png");

		public StackTracePad ()
		{
			this.ShadowType = ShadowType.None;

			var evalCmd = new ActionCommand ("StackTracePad.EvaluateMethodParams", GettextCatalog.GetString ("Evaluate Method Parameters"));
			var gotoCmd = new ActionCommand ("StackTracePad.ActivateFrame", GettextCatalog.GetString ("Activate Stack Frame"));
			
			menuSet = new CommandEntrySet ();
			menuSet.Add (evalCmd);
			menuSet.Add (gotoCmd);
			menuSet.AddSeparator ();
			menuSet.AddItem (EditCommands.SelectAll);
			menuSet.AddItem (EditCommands.Copy);
			
			store = new ListStore (typeof (bool), typeof (string), typeof (string), typeof (string), typeof (string), typeof (string), typeof (Pango.Style), typeof (object), typeof (int), typeof (bool));

			tree = new PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			tree.Selection.Mode = SelectionMode.Multiple;
			tree.SearchEqualFunc = Search;
			tree.EnableSearch = true;
			tree.SearchColumn = 1;
			tree.ButtonPressEvent += HandleButtonPressEvent;
			tree.DoPopupMenu = ShowPopup;

			var col = new TreeViewColumn ();
			var crp = new CellRendererImage ();
			col.PackStart (crp, false);
			crp.Image = pointerImage;
			col.AddAttribute (crp, "visible", IconColumn);
			tree.AppendColumn (col);
			
			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Name");
			refresh = new CellRendererImage ();
			refresh.Image = ImageService.GetIcon (Gtk.Stock.Refresh).WithSize (12, 12);
			col.PackStart (refresh, false);
			col.AddAttribute (refresh, "visible", CanRefreshColumn);
			col.PackStart (tree.TextRenderer, true);
			col.AddAttribute (tree.TextRenderer, "text", MethodColumn);
			col.AddAttribute (tree.TextRenderer, "foreground", ForegroundColumn);
			col.AddAttribute (tree.TextRenderer, "style", StyleColumn);
			col.Resizable = true;
			col.Alignment = 0.0f;
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("File");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", FileColumn);
			col.AddAttribute (tree.TextRenderer, "foreground", ForegroundColumn);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Language");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", LangColumn);
			col.AddAttribute (tree.TextRenderer, "foreground", ForegroundColumn);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Address");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", AddrColumn);
			col.AddAttribute (tree.TextRenderer, "foreground", ForegroundColumn);
			tree.AppendColumn (col);
			
			Add (tree);
			ShowAll ();
			UpdateDisplay ();
			
			DebuggingService.CallStackChanged += OnClassStackChanged;
			DebuggingService.CurrentFrameChanged += OnFrameChanged;
			DebuggingService.StoppedEvent += OnDebuggingServiceStopped;

			tree.RowActivated += OnRowActivated;
		}

		void OnDebuggingServiceStopped(object sender, EventArgs e)
		{
			if (store != null)
				store.Clear();
		}

		static bool Search (TreeModel model, int column, string key, TreeIter iter)
		{
			string value = (string) model.GetValue (iter, column);

			return !value.Contains (key);
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

		static string EvaluateMethodName (StackFrame frame, EvaluationOptions options)
		{
			var method = new StringBuilder (frame.SourceLocation.MethodName);
			var args = frame.GetParameters (options);

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

			if (!DebuggingService.IsPaused)
				return;

			var options = DebuggingService.DebuggerSession.Options.EvaluationOptions;
			var backtrace = DebuggingService.CurrentCallStack;

			for (int i = 0; i < backtrace.FrameCount; i++) {
				bool icon = i == DebuggingService.CurrentFrameIndex;

				StackFrame frame = backtrace.GetFrame (i);
				if (frame.IsDebuggerHidden)
					continue;
				
				var method = EvaluateMethodName (frame, options);
				
				string file;
				if (!string.IsNullOrEmpty (frame.SourceLocation.FileName)) {
					file = frame.SourceLocation.FileName;
					if (frame.SourceLocation.Line != -1)
						file += ":" + frame.SourceLocation.Line;
				} else {
					file = string.Empty;
				}

				var style = frame.IsExternalCode ? Pango.Style.Italic : Pango.Style.Normal;
				
				store.AppendValues (icon, method, file, frame.Language, "0x" + frame.Address.ToString ("x"), null,
				                    style, frame, i, !options.AllowDisplayStringEvaluation);
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

				var frame = (StackFrame) store.GetValue (iter, FrameColumn);
				var method = EvaluateMethodName (frame, options);

				store.SetValue (iter, MethodColumn, method);
				store.SetValue (iter, CanRefreshColumn, false);
			}
		}
		
		public void UpdateCurrentFrame ()
		{
			TreeIter iter;

			if (!store.GetIterFirst (out iter))
				return;

			do {
				int frame = (int) store.GetValue (iter, FrameIndexColumn);

				if (frame == DebuggingService.CurrentFrameIndex)
					store.SetValue (iter, IconColumn, true);
				else
					store.SetValue (iter, IconColumn, false);
			} while (store.IterNext (ref iter));
		}

		protected void OnFrameChanged (object o, EventArgs args)
		{
			UpdateCurrentFrame ();
		}

		protected void OnClassStackChanged (object o, EventArgs args)
		{
			UpdateDisplay ();
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			ActivateFrame ();
		}

		public Widget Control {
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
				if ((bool) store.GetValue (iter, CanRefreshColumn)) {
					var frame = (StackFrame) store.GetValue (iter, FrameColumn);
					var method = EvaluateMethodName (frame, options);

					store.SetValue (iter, MethodColumn, method);
					store.SetValue (iter, CanRefreshColumn, false);
				}
			} while (store.IterNext (ref iter));
		}

		[CommandHandler ("StackTracePad.ActivateFrame")]
		void ActivateFrame ()
		{
			var selected = tree.Selection.GetSelectedRows ();
			TreeIter iter;

			if (selected.Length > 0 && store.GetIter (out iter, selected[0]))
				DebuggingService.CurrentFrameIndex = (int) store.GetValue (iter, FrameIndexColumn);
		}
		
		[CommandHandler (EditCommands.SelectAll)]
		internal void OnSelectAll ()
		{
			tree.Selection.SelectAll ();
		}
		
		[CommandHandler (EditCommands.Copy)]
		internal void OnCopy ()
		{
			var txt = new StringBuilder ();
			TreeModel model;
			TreeIter iter;

			foreach (TreePath path in tree.Selection.GetSelectedRows (out model)) {
				if (!model.GetIter (out iter, path))
					continue;

				string method = (string) model.GetValue (iter, MethodColumn);
				string file = (string) model.GetValue (iter, FileColumn);

				if (txt.Length > 0)
					txt.Append ('\n');

				txt.AppendFormat ("{0} in {1}", method, file);
			}

			Clipboard clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = txt.ToString ();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = txt.ToString ();
		}

		protected override void OnDestroyed ()
		{
			DebuggingService.CallStackChanged -= OnClassStackChanged;
			DebuggingService.CurrentFrameChanged -= OnFrameChanged;
			DebuggingService.StoppedEvent -= OnDebuggingServiceStopped;
			base.OnDestroyed ();
		}
	}
}
