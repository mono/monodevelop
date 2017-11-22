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
using System.Linq;
using MonoDevelop.Components.AutoTest;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace MonoDevelop.Debugger
{
	public class StackTracePad : PadContent
	{
		StackTracePadWidget control;

		public StackTracePad ()
		{
			Id = "MonoDevelop.Debugger.StackTracePad";
			control = new StackTracePadWidget ();
		}

		protected override void Initialize (IPadWindow window)
		{
			control.Initialize (window);
		}

		public override Control Control {
			get {
				return control;
			}
		}
	}

	public class StackTracePadWidget : ScrolledWindow
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

		readonly PadTreeView tree;
		readonly ListStore store;
		IPadWindow window;
		bool needsUpdate;

		static Xwt.Drawing.Image pointerImage = ImageService.GetIcon ("md-stack-pointer", IconSize.Menu);

		public StackTracePadWidget ()
		{
			this.ShadowType = ShadowType.None;

			store = new ListStore (typeof (bool), typeof (string), typeof (string), typeof (string), typeof (string), typeof (string), typeof (Pango.Style), typeof (object), typeof (int), typeof (bool));
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("store__Icon", "store__Method", "store_File",
				"store_Lang", "store_Addr", "store_Foreground", "store_Style", "store_Frame", "store_FrameIndex");
			TypeDescriptor.AddAttributes (store, modelAttr);

			tree = new PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			tree.Selection.Mode = SelectionMode.Multiple;
			tree.SearchEqualFunc = Search;
			tree.EnableSearch = true;
			tree.SearchColumn = 1;
			tree.DoPopupMenu = ShowPopup;

			var col = new TreeViewColumn ();
			var crp = new CellRendererImage ();
			col.PackStart (crp, false);
			crp.Image = pointerImage;
			col.AddAttribute (crp, "visible", IconColumn);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Name");
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
			col.Visible = false;//By default Language column is hidden
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Address");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", AddrColumn);
			col.AddAttribute (tree.TextRenderer, "foreground", ForegroundColumn);
			col.Visible = false;//By default Address column is hidden
			tree.AppendColumn (col);

			Add (tree);

			LoadColumnsVisibility ();

			ShowAll ();
			UpdateDisplay ();

			DebuggingService.CallStackChanged += OnClassStackChanged;
			DebuggingService.CurrentFrameChanged += OnFrameChanged;
			DebuggingService.StoppedEvent += OnDebuggingServiceStopped;

			tree.RowActivated += OnRowActivated;
		}

		void LoadColumnsVisibility ()
		{
			var columns = PropertyService.Get ("Monodevelop.StackTrace.ColumnsVisibility", "");
			var tokens = columns.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (tree.Columns.Length == tokens.Length) {
				for (int i = 0; i < tokens.Length; i++) {
					bool visible;
					if (bool.TryParse (tokens [i], out visible))
						tree.Columns [i].Visible = visible;
				}
			}
		}

		void StoreColumnsVisibility ()
		{
			PropertyService.Set ("Monodevelop.StackTrace.ColumnsVisibility", string.Join (";", tree.Columns.Select (c => c.Visible ? "TRUE" : "FALSE")));
		}

		void OnDebuggingServiceStopped (object sender, EventArgs e)
		{
			TreeIter iter;

			if (store != null && store.GetIterFirst (out iter) && (store.GetValue (iter, FrameColumn) as StackFrame)?.DebuggerSession == sender)
				store.Clear ();
		}

		static bool Search (TreeModel model, int column, string key, TreeIter iter)
		{
			string value = (string)model.GetValue (iter, column);

			return !value.Contains (key);
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

		bool GetExternalCodeValue (DebuggerSessionOptions options)
		{
			return options.EvaluationOptions.StackFrameFormat.ExternalCode ?? options.ProjectAssembliesOnly;
		}

		static List<(StackFrame frame, string text)> GetStackFrames ()
		{
			var backtrace = DebuggingService.CurrentCallStack;
			var result = new List<(StackFrame frame, string text)> ();
			for (int i = 0; i < backtrace.FrameCount; i++) {
				var frame = backtrace.GetFrame (i);
				result.Add ((frame, frame.FullStackframeText));
			}
			return result;
		}

		CancellationTokenSource cancelUpdate = new CancellationTokenSource ();

		async void Update ()
		{
			if (tree.IsRealized)
				tree.ScrollToPoint (0, 0);

			needsUpdate = false;
			store.Clear ();

			if (!DebuggingService.IsPaused)
				return;

			cancelUpdate.Cancel ();
			cancelUpdate = new CancellationTokenSource ();
			var token = cancelUpdate.Token;

			List<(StackFrame frame, string text)> stackFrames;
			try {
				stackFrames = await Task.Run (() => GetStackFrames ());
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
				return;
			}
			// Another fetch of all data already in progress, return
			if (token.IsCancellationRequested)
				return;

			var externalCodeIter = TreeIter.Zero;
			for (int i = 0; i < stackFrames.Count; i++) {
				bool icon = i == DebuggingService.CurrentFrameIndex;
				StackFrame frame = stackFrames [i].frame;
				if (frame.IsDebuggerHidden)
					continue;

				if (!GetExternalCodeValue (frame.DebuggerSession.Options) && frame.IsExternalCode) {
					if (externalCodeIter.Equals (TreeIter.Zero)) {
						externalCodeIter = store.AppendValues (icon, GettextCatalog.GetString ("[External Code]"), string.Empty, string.Empty, string.Empty, null, Pango.Style.Italic, null, -1);
					} else if (icon) {
						//Set IconColumn value to true if any of hidden frames is current frame
						store.SetValue (externalCodeIter, IconColumn, true);
					}
					continue;
				}
				externalCodeIter = TreeIter.Zero;
				var method = stackFrames [i].text;

				string file;
				if (!string.IsNullOrEmpty (frame.SourceLocation.FileName)) {
					file = frame.SourceLocation.FileName;
					if (frame.SourceLocation.Line != -1 && frame.DebuggerSession.EvaluationOptions.StackFrameFormat.Line)
						file += ":" + frame.SourceLocation.Line;
				} else {
					file = string.Empty;
				}

				var style = frame.IsExternalCode ? Pango.Style.Italic : Pango.Style.Normal;

				store.AppendValues (icon, method, file, frame.Language, "0x" + frame.Address.ToString ("x"), null,
					style, frame, i);
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

		public void UpdateCurrentFrame ()
		{
			TreeIter iter;

			if (!store.GetIterFirst (out iter))
				return;

			do {
				int frame = (int)store.GetValue (iter, FrameIndexColumn);

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

		void ShowPopup (Gdk.EventButton evt)
		{
			var context_menu = new ContextMenu ();
			context_menu.Items.Add (new SeparatorContextMenuItem ());
			var selectAllItem = new ContextMenuItem (GettextCatalog.GetString ("Select All"));
			selectAllItem.Clicked += delegate {
				OnSelectAll ();
			};
			context_menu.Items.Add (selectAllItem);
			var copyItem = new ContextMenuItem (GettextCatalog.GetString ("Copy"));
			copyItem.Clicked += delegate {
				OnCopy ();
			};
			context_menu.Items.Add (copyItem);
			context_menu.Items.Add (new SeparatorContextMenuItem ());
			var showExternalCodeCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show External Code"));
			showExternalCodeCheckbox.Clicked += delegate {
				var opts = DebuggingService.GetUserOptions ();
				opts.EvaluationOptions.StackFrameFormat.ExternalCode = showExternalCodeCheckbox.Checked = !GetExternalCodeValue(opts);
				DebuggingService.SetUserOptions (opts);
				UpdateDisplay ();
			};
			var userOptions = DebuggingService.GetUserOptions ();
			var frameOptions = userOptions.EvaluationOptions.StackFrameFormat;
			showExternalCodeCheckbox.Checked = GetExternalCodeValue (userOptions);
			context_menu.Items.Add (showExternalCodeCheckbox);

			context_menu.Items.Add (new SeparatorContextMenuItem ());

			var assemblyCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Module Name"));
			assemblyCheckbox.Clicked += delegate {
				var opts = DebuggingService.GetUserOptions ();
				opts.EvaluationOptions.StackFrameFormat.Module = assemblyCheckbox.Checked = !opts.EvaluationOptions.StackFrameFormat.Module;
				DebuggingService.SetUserOptions (opts);
				UpdateDisplay ();
			};
			assemblyCheckbox.Checked = frameOptions.Module;
			context_menu.Items.Add (assemblyCheckbox);
			var typeCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Parameter Type"));
			typeCheckbox.Clicked += delegate {
				var opts = DebuggingService.GetUserOptions ();
				opts.EvaluationOptions.StackFrameFormat.ParameterTypes = typeCheckbox.Checked = !opts.EvaluationOptions.StackFrameFormat.ParameterTypes;
				DebuggingService.SetUserOptions (opts);
				UpdateDisplay ();
			};
			typeCheckbox.Checked = frameOptions.ParameterTypes;
			context_menu.Items.Add (typeCheckbox);
			var nameCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Parameter Name"));
			nameCheckbox.Clicked += delegate {
				var opts = DebuggingService.GetUserOptions ();
				opts.EvaluationOptions.StackFrameFormat.ParameterNames = nameCheckbox.Checked = !opts.EvaluationOptions.StackFrameFormat.ParameterNames;
				DebuggingService.SetUserOptions (opts);
				UpdateDisplay ();
			};
			nameCheckbox.Checked = frameOptions.ParameterNames;
			context_menu.Items.Add (nameCheckbox);
			var valueCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Parameter Value"));
			valueCheckbox.Clicked += delegate {
				var opts = DebuggingService.GetUserOptions ();
				opts.EvaluationOptions.StackFrameFormat.ParameterValues = valueCheckbox.Checked = !opts.EvaluationOptions.StackFrameFormat.ParameterValues;
				DebuggingService.SetUserOptions (opts);
				UpdateDisplay ();
			};
			valueCheckbox.Checked = frameOptions.ParameterValues;
			context_menu.Items.Add (valueCheckbox);
			var lineCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Line Number"));
			lineCheckbox.Clicked += delegate {
				var opts = DebuggingService.GetUserOptions ();
				opts.EvaluationOptions.StackFrameFormat.Line = lineCheckbox.Checked = !opts.EvaluationOptions.StackFrameFormat.Line;
				DebuggingService.SetUserOptions (opts);
				UpdateDisplay ();
			};
			lineCheckbox.Checked = frameOptions.Line;
			context_menu.Items.Add (lineCheckbox);

			context_menu.Items.Add (new SeparatorContextMenuItem ());

			var columnsVisibilitySubMenu = new ContextMenu ();
			var nameColumnVisibilityCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Name"));
			nameColumnVisibilityCheckbox.Clicked += delegate {
				nameColumnVisibilityCheckbox.Checked = tree.Columns [MethodColumn].Visible = !tree.Columns [MethodColumn].Visible;
				StoreColumnsVisibility ();
			};
			nameColumnVisibilityCheckbox.Checked = tree.Columns [MethodColumn].Visible;
			columnsVisibilitySubMenu.Items.Add (nameColumnVisibilityCheckbox);
			var fileColumnVisibilityCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("File"));
			fileColumnVisibilityCheckbox.Clicked += delegate {
				fileColumnVisibilityCheckbox.Checked = tree.Columns [FileColumn].Visible = !tree.Columns [FileColumn].Visible;
				StoreColumnsVisibility ();
			};
			fileColumnVisibilityCheckbox.Checked = tree.Columns [FileColumn].Visible;
			columnsVisibilitySubMenu.Items.Add (fileColumnVisibilityCheckbox);
			var languageColumnVisibilityCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Language"));
			languageColumnVisibilityCheckbox.Clicked += delegate {
				languageColumnVisibilityCheckbox.Checked = tree.Columns [LangColumn].Visible = !tree.Columns [LangColumn].Visible;
				StoreColumnsVisibility ();
			};
			languageColumnVisibilityCheckbox.Checked = tree.Columns [LangColumn].Visible;
			columnsVisibilitySubMenu.Items.Add (languageColumnVisibilityCheckbox);
			var addressColumnVisibilityCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Address"));
			addressColumnVisibilityCheckbox.Clicked += delegate {
				addressColumnVisibilityCheckbox.Checked = tree.Columns [AddrColumn].Visible = !tree.Columns [AddrColumn].Visible;
				StoreColumnsVisibility ();
			};
			addressColumnVisibilityCheckbox.Checked = tree.Columns [AddrColumn].Visible;
			columnsVisibilitySubMenu.Items.Add (addressColumnVisibilityCheckbox);
			context_menu.Items.Add (new ContextMenuItem (GettextCatalog.GetString ("Columns")) { SubMenu = columnsVisibilitySubMenu });


			context_menu.Show (this, evt);
		}

		void ActivateFrame ()
		{
			var selected = tree.Selection.GetSelectedRows ();
			TreeIter iter;

			if (selected.Length > 0 && store.GetIter (out iter, selected [0])) {
				var frameIndex = (int)store.GetValue (iter, FrameIndexColumn);
				if (frameIndex == -1) {
					var opts = DebuggingService.GetUserOptions ();
					opts.EvaluationOptions.StackFrameFormat.ExternalCode = true;
					DebuggingService.SetUserOptions (opts);
					UpdateDisplay ();
					return;
				}
				DebuggingService.CurrentFrameIndex = frameIndex;
			}
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

				string method = (string)model.GetValue (iter, MethodColumn);
				string file = (string)model.GetValue (iter, FileColumn);

				if (txt.Length > 0)
					txt.Append (Environment.NewLine);

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
