using GLib;
using Gtk;
using GtkSharp;
using System;
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

		Gtk.TreeView tree;
		Gtk.TreeStore store;

		public StackTracePad ()
		{
			this.ShadowType = ShadowType.In;

			store = new TreeStore (typeof (string));

			tree = new TreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;

			TreeViewColumn FrameCol = new TreeViewColumn ();
			CellRenderer FrameRenderer = new CellRendererText ();
			FrameCol.Title = "Frame";
			FrameCol.PackStart (FrameRenderer, true);
			FrameCol.AddAttribute (FrameRenderer, "text", 0);
			FrameCol.Resizable = true;
			FrameCol.Alignment = 0.0f;
			tree.AppendColumn (FrameCol);

			Add (tree);
			ShowAll ();

			current_backtrace = IdeApp.Services.DebuggingService.CurrentBacktrace;
			UpdateDisplay ();
			
			IdeApp.Services.DebuggingService.PausedEvent += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnPausedEvent));
			IdeApp.Services.DebuggingService.ResumedEvent += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnResumedEvent));
			IdeApp.Services.DebuggingService.StoppedEvent += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnStoppedEvent));
		}
		
		void IPadContent.Initialize (IPadWindow window)
		{
			window.Title = "Call Stack";
			window.Icon = Stock.OutputIcon;
		}

		public void UpdateDisplay ()
		{
			store.Clear ();

			if (current_backtrace == null)
				return;

			string[] trace = IdeApp.Services.DebuggingService.Backtrace;
			foreach (string frame in trace)
				store.AppendValues (frame);
		}

		protected void OnStoppedEvent (object o, EventArgs args)
		{
			current_backtrace = null;
			UpdateDisplay ();
		}

		protected void OnResumedEvent (object o, EventArgs args)
		{
			current_backtrace = null;
			UpdateDisplay ();
		}

		protected void OnPausedEvent (object o, EventArgs args)
		{
			current_backtrace = IdeApp.Services.DebuggingService.CurrentBacktrace;
			UpdateDisplay ();
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
