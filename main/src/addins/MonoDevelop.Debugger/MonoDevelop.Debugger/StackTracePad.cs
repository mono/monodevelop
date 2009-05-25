using GLib;
using Gtk;
using GtkSharp;
using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;

using Stock = MonoDevelop.Core.Gui.Stock;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public class StackTracePad : Gtk.ScrolledWindow, IPadContent
	{
		Backtrace current_backtrace;

		MonoDevelop.Ide.Gui.Components.PadTreeView tree;
		Gtk.TreeStore store;
		bool needsUpdate;
		IPadWindow window;

		public StackTracePad ()
		{
			this.ShadowType = ShadowType.None;

			store = new TreeStore (typeof(string), typeof (string), typeof(string), typeof(string), typeof(string));

			tree = new MonoDevelop.Ide.Gui.Components.PadTreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;

			TreeViewColumn col = new TreeViewColumn ();
			CellRenderer crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", 0);
			tree.AppendColumn (col);
			
			TreeViewColumn FrameCol = new TreeViewColumn ();
			FrameCol.Title = GettextCatalog.GetString ("Name");
			FrameCol.PackStart (tree.TextRenderer, true);
			FrameCol.AddAttribute (tree.TextRenderer, "text", 1);
			FrameCol.Resizable = true;
			FrameCol.Alignment = 0.0f;
			tree.AppendColumn (FrameCol);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("File");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", 2);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Language");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", 3);
			tree.AppendColumn (col);

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Address");
			col.PackStart (tree.TextRenderer, false);
			col.AddAttribute (tree.TextRenderer, "text", 4);
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
			window.Title = GettextCatalog.GetString ("Call Stack");
			window.Icon = Stock.OutputIcon;
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
				
				StringBuilder met = new StringBuilder (fr.SourceLocation.Method);
				met.Append (" (");
				ObjectValue[] args = fr.GetParameters ();
				for (int n=0; n<args.Length; n++) {
					if (n > 0)
						met.Append (", ");
					met.Append (args[n].Name).Append ("=").Append (args[n].Value);
				}
				met.Append (")");
				
				string file;
				if (!string.IsNullOrEmpty (fr.SourceLocation.Filename)) {
					file = fr.SourceLocation.Filename;
					if (fr.SourceLocation.Line != -1)
						file += ":" + fr.SourceLocation.Line;
				} else
					file = string.Empty;
				
				store.AppendValues (icon, met.ToString (), file, fr.Language, fr.Address.ToString ("x"));
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
			DebuggingService.CurrentFrameIndex = args.Path.Indices [0];
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
	}
}
