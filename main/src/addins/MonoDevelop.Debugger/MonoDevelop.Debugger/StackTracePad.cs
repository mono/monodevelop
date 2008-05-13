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
			try {
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
	
				IdeApp.Services.DebuggingService.PausedEvent += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnPausedEvent));
				/*Services.DebuggingService.ResumedEvent += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnResumedEvent));
				Services.DebuggingService.StoppedEvent += (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnStoppedEvent));*/
			} catch (Exception e) {
				Console.WriteLine ("new StackTracePad, e - {0}", e.ToString ());
			}
		}
		
		void IPadContent.Initialize (IPadWindow window)
		{
			window.Title = "Call Stack";
			window.Icon = Stock.OutputIcon;
		}

		public void UpdateDisplay ()
		{
			Console.WriteLine ("** UpdateDisplay");
			TreeIter it;

			//if ((current_frame == null) /*|| (current_frame.Method == null)*/) {
			if (current_backtrace == null) {
				if (store.GetIterFirst (out it))
					do { } while (store.Remove (ref it));

				return;
			}

			string [] trace = IdeApp.Services.DebuggingService.Backtrace;
			if (!store.GetIterFirst (out it)) {
				foreach (string frame in trace) {
					store.Append (out it);
					store.SetValue (it, 0, frame);
				}
			}
			else {
				for (int i = 0; i < trace.Length; i ++) {
					store.SetValue (it, 0, trace[i]);
					if (i < trace.Length - 1 && !store.IterNext (ref it))
						store.Append (out it);
				}
				/* clear any remaining rows */
				if (store.IterNext (ref it))
					do { } while (store.Remove (ref it));
			}
		}

		protected void OnStoppedEvent (object o, EventArgs args)
		{
			//current_frame = null;
			current_backtrace = null;
			UpdateDisplay ();
		}

		protected void OnResumedEvent (object o, EventArgs args)
		{
		}

		protected void OnPausedEvent (object o, EventArgs args)
		{
			try {
				current_backtrace = IdeApp.Services.DebuggingService.CurrentBacktrace;
				UpdateDisplay ();
			} catch (Exception e) {
				LoggingService.LogError ("Error updating stack trace display", e);
				throw;
			}
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
