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

		readonly PadTreeView tree;
		readonly ListStore store;
		IPadWindow window;
		bool needsUpdate;

		static Xwt.Drawing.Image pointerImage = Xwt.Drawing.Image.FromResource ("stack-pointer-16.png");

		public StackTracePad ()
		{
			this.ShadowType = ShadowType.None;

			store = new ListStore (typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(Pango.Style), typeof(object), typeof(int), typeof(bool));

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
			LoadSettings ();

			ShowAll ();
			UpdateDisplay ();
			
			DebuggingService.CallStackChanged += OnClassStackChanged;
			DebuggingService.CurrentFrameChanged += OnFrameChanged;
			DebuggingService.StoppedEvent += OnDebuggingServiceStopped;

			tree.RowActivated += OnRowActivated;
		}

		bool ShowModuleName;
		bool ShowParameterType;
		bool ShowParameterName;
		bool ShowParameterValue;
		bool ShowLineNumber;

		void LoadSettings ()
		{
			ShowModuleName = PropertyService.Get ("Monodevelop.StackTrace.ShowModuleName", false);
			ShowParameterType = PropertyService.Get ("Monodevelop.StackTrace.ShowParameterType", true);
			ShowParameterName = PropertyService.Get ("Monodevelop.StackTrace.ShowParameterName", true);
			ShowParameterValue = PropertyService.Get ("Monodevelop.StackTrace.ShowParameterValue", false);
			ShowLineNumber = PropertyService.Get ("Monodevelop.StackTrace.ShowLineNumber", true);
		}

		void StoreSettings ()
		{
			PropertyService.Set ("Monodevelop.StackTrace.ShowModuleName", ShowModuleName);
			PropertyService.Set ("Monodevelop.StackTrace.ShowParameterType", ShowParameterType);
			PropertyService.Set ("Monodevelop.StackTrace.ShowParameterName", ShowParameterName);
			PropertyService.Set ("Monodevelop.StackTrace.ShowParameterValue", ShowParameterValue);
			PropertyService.Set ("Monodevelop.StackTrace.ShowLineNumber", ShowLineNumber);
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
			if (store != null)
				store.Clear ();
		}

		static bool Search (TreeModel model, int column, string key, TreeIter iter)
		{
			string value = (string)model.GetValue (iter, column);

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

		string EvaluateMethodName (StackFrame frame)
		{
			var methodNameBuilder = new StringBuilder (frame.SourceLocation.MethodName);
			var options = DebuggingService.DebuggerSession.Options.EvaluationOptions.Clone ();
			if (ShowParameterValue) {
				options.AllowMethodEvaluation = true;
				options.AllowToStringCalls = true;
				options.AllowTargetInvoke = true;
			} else {
				options.AllowMethodEvaluation = false;
				options.AllowToStringCalls = false;
				options.AllowTargetInvoke = false;
			}

			var args = frame.GetParameters ();

			//MethodName starting with "["... it's something like [ExternalCode]
			if (!frame.SourceLocation.MethodName.StartsWith ("[", StringComparison.Ordinal)) {
				if (ShowModuleName && !string.IsNullOrEmpty (frame.FullModuleName)) {
					methodNameBuilder.Insert (0, System.IO.Path.GetFileName (frame.FullModuleName) + "!");
				}
				if (ShowParameterType || ShowParameterName || ShowParameterValue) {
					methodNameBuilder.Append ("(");
					for (int n = 0; n < args.Length; n++) {
						if (n > 0)
							methodNameBuilder.Append (", ");
						if (ShowParameterType) {
							methodNameBuilder.Append (args [n].TypeName);
							if (ShowParameterName)
								methodNameBuilder.Append (" ");
						}
						if (ShowParameterName)
							methodNameBuilder.Append (args [n].Name);
						if (ShowParameterValue) {
							if (ShowParameterType || ShowParameterName)
								methodNameBuilder.Append (" = ");
							var val = args [n].Value ?? "";
							methodNameBuilder.Append (val.Replace ("\r\n", " ").Replace ("\n", " "));
						}
					}
					methodNameBuilder.Append (")");
				}
			}

			return methodNameBuilder.ToString ();
		}

		void Update ()
		{
			if (tree.IsRealized)
				tree.ScrollToPoint (0, 0);

			needsUpdate = false;
			store.Clear ();

			if (!DebuggingService.IsPaused)
				return;

			var backtrace = DebuggingService.CurrentCallStack;

			for (int i = 0; i < backtrace.FrameCount; i++) {
				bool icon = i == DebuggingService.CurrentFrameIndex;

				StackFrame frame = backtrace.GetFrame (i);
				if (frame.IsDebuggerHidden)
					continue;
				
				var method = EvaluateMethodName (frame);
				
				string file;
				if (!string.IsNullOrEmpty (frame.SourceLocation.FileName)) {
					file = frame.SourceLocation.FileName;
					if (frame.SourceLocation.Line != -1 && ShowLineNumber)
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

			var assemblyCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Module Name"));
			assemblyCheckbox.Clicked += delegate {
				assemblyCheckbox.Checked = ShowModuleName = !ShowModuleName;
				StoreSettings ();
				UpdateDisplay ();
			};
			assemblyCheckbox.Checked = ShowModuleName;
			context_menu.Items.Add (assemblyCheckbox);
			var typeCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Parameter Type"));
			typeCheckbox.Clicked += delegate {
				typeCheckbox.Checked = ShowParameterType = !ShowParameterType;
				StoreSettings ();
				UpdateDisplay ();
			};
			typeCheckbox.Checked = ShowParameterType;
			context_menu.Items.Add (typeCheckbox);
			var nameCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Parameter Name"));
			nameCheckbox.Clicked += delegate {
				nameCheckbox.Checked = ShowParameterName = !ShowParameterName;
				StoreSettings ();
				UpdateDisplay ();
			};
			nameCheckbox.Checked = ShowParameterName;
			context_menu.Items.Add (nameCheckbox);
			var valueCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Parameter Value"));
			valueCheckbox.Clicked += delegate {
				valueCheckbox.Checked = ShowParameterValue = !ShowParameterValue;
				StoreSettings ();
				UpdateDisplay ();
			};
			valueCheckbox.Checked = ShowParameterValue;
			context_menu.Items.Add (valueCheckbox);
			var lineCheckbox = new CheckBoxContextMenuItem (GettextCatalog.GetString ("Show Line Number"));
			lineCheckbox.Clicked += delegate {
				lineCheckbox.Checked = ShowLineNumber = !ShowLineNumber;
				StoreSettings ();
				UpdateDisplay ();
			};
			lineCheckbox.Checked = ShowLineNumber;
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
			context_menu.Items.Add (new ContextMenuItem (GettextCatalog.GetString ("Columns")){ SubMenu = columnsVisibilitySubMenu });


			context_menu.Show (this, evt);
		}

		void ActivateFrame ()
		{
			var selected = tree.Selection.GetSelectedRows ();
			TreeIter iter;

			if (selected.Length > 0 && store.GetIter (out iter, selected [0]))
				DebuggingService.CurrentFrameIndex = (int)store.GetValue (iter, FrameIndexColumn);
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
