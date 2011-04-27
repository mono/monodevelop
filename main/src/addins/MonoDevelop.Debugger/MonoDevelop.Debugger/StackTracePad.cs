
using System;
using System.Text;
using Gtk;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using Stock = MonoDevelop.Ide.Gui.Stock;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.Debugger
{
	public class StackTracePad : Gtk.ScrolledWindow, IPadContent
	{
		Backtrace current_backtrace;

		MonoDevelop.Ide.Gui.Components.PadTreeView tree;
		Gtk.ListStore store;
		bool needsUpdate;
		IPadWindow window;
		CommandEntrySet menuSet;

		public StackTracePad ()
		{
			this.ShadowType = ShadowType.None;

			ActionCommand gotoCmd = new ActionCommand ("StackTracePad.ActivateFrame", GettextCatalog.GetString ("Activate Stack Frame"));
			
			menuSet = new CommandEntrySet ();
			menuSet.Add (gotoCmd);
			menuSet.AddSeparator ();
			menuSet.AddItem (EditCommands.SelectAll);
			menuSet.AddItem (EditCommands.Copy);
			
			store = new ListStore (typeof(string), typeof (string), typeof(string), typeof(string), typeof(string), typeof(string), typeof (Pango.Style));

			tree = new MonoDevelop.Ide.Gui.Components.PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;
			tree.Selection.Mode = SelectionMode.Multiple;
			tree.ButtonPressEvent += HandleTreeButtonPressEvent;;
			tree.PopupMenu += HandleTreePopupMenu;

			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererIcon ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", 0);
			tree.AppendColumn (col);
			
			TreeViewColumn FrameCol = new TreeViewColumn ();
			FrameCol.Title = GettextCatalog.GetString ("Name");
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
		
		void Update ()
		{
			needsUpdate = false;
			store.Clear ();

			if (current_backtrace == null)
				return;

			for (int i = 0; i < current_backtrace.FrameCount; i++) {
				string icon;
				if (i == DebuggingService.CurrentFrameIndex)
					icon = Gtk.Stock.GoForward;
				else
					icon = null;
				
				StackFrame fr = current_backtrace.GetFrame (i);
				
				StringBuilder met = new StringBuilder (fr.SourceLocation.MethodName);
				ObjectValue[] args = fr.GetParameters ();
				if (args.Length != 0 || !fr.SourceLocation.MethodName.StartsWith ("[")) {
					met.Append (" (");
					for (int n=0; n<args.Length; n++) {
						if (n > 0)
							met.Append (", ");
						met.Append (args[n].Name).Append ("=").Append (args[n].Value);
					}
					met.Append (")");
				}
				
				string file;
				if (!string.IsNullOrEmpty (fr.SourceLocation.FileName)) {
					file = fr.SourceLocation.FileName;
					if (fr.SourceLocation.Line != -1)
						file += ":" + fr.SourceLocation.Line;
				} else
					file = string.Empty;
				
				store.AppendValues (icon, met.ToString (), file, fr.Language, "0x" + fr.Address.ToString ("x"), null,
				                    fr.IsExternalCode? Pango.Style.Italic : Pango.Style.Normal);
			}
		}
		
		public void UpdateCurrentFrame ()
		{
			TreeIter it;
			if (store.GetIterFirst (out it)) {
				int n=0;
				do {
					if (n == DebuggingService.CurrentFrameIndex)
						store.SetValue (it, 0, Gtk.Stock.GoForward);
					else
						store.SetValue (it, 0, null);
					n++;
				} while (store.IterNext (ref it));
			}
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

		[GLib.ConnectBefore]
		void HandleTreeButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowPopup ();
		}

		[GLib.ConnectBefore]
		void HandleTreePopupMenu (object o, PopupMenuArgs args)
		{
			ShowPopup ();
		}

		internal void ShowPopup ()
		{
			IdeApp.CommandService.ShowContextMenu (menuSet, tree);
		}
		
		[CommandHandler ("StackTracePad.ActivateFrame")]
		void ActivateFrame ()
		{
			TreePath[] sel = tree.Selection.GetSelectedRows ();
			if (sel.Length > 0) {
				DebuggingService.CurrentFrameIndex = sel[0].Indices [0];
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
