using GLib;
using Gtk;
using GtkSharp;
using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using Mono.Debugger;
using Mono.Debugger.Languages;

using Stock = MonoDevelop.Gui.Stock;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui;

namespace MonoDevelop.Debugger
{
	public class ThreadPad : Gtk.ScrolledWindow, IPadContent
	{
		Gtk.TreeView tree;
		Gtk.TreeStore store;
		Hashtable thread_rows;

		public ThreadPad ()
		{
			thread_rows = new Hashtable ();

			this.ShadowType = ShadowType.In;

			store = new TreeStore (typeof (int),
					       typeof (int),
					       typeof (string),
					       typeof (string));

			tree = new TreeView (store);
			tree.RulesHint = true;
			tree.HeadersVisible = true;

			TreeViewColumn Col;
			CellRenderer ThreadRenderer;

			Col = new TreeViewColumn ();
			ThreadRenderer = new CellRendererText ();
			Col.Title = "Id";
			Col.PackStart (ThreadRenderer, true);
			Col.AddAttribute (ThreadRenderer, "text", 0);
			Col.Resizable = true;
			Col.Alignment = 0.0f;
			tree.AppendColumn (Col);

			Col = new TreeViewColumn ();
			ThreadRenderer = new CellRendererText ();
			Col.Title = "PID";
			Col.PackStart (ThreadRenderer, true);
			Col.AddAttribute (ThreadRenderer, "text", 1);
			Col.Resizable = true;
			Col.Alignment = 0.0f;
			tree.AppendColumn (Col);

			Col = new TreeViewColumn ();
			ThreadRenderer = new CellRendererText ();
			Col.Title = "State";
			Col.PackStart (ThreadRenderer, true);
			Col.AddAttribute (ThreadRenderer, "text", 2);
			Col.Resizable = true;
			Col.Alignment = 0.0f;
			tree.AppendColumn (Col);

			Col = new TreeViewColumn ();
			ThreadRenderer = new CellRendererText ();
			Col.Title = "Current Location";
			Col.PackStart (ThreadRenderer, true);
			Col.AddAttribute (ThreadRenderer, "text", 3);
			Col.Resizable = true;
			Col.Alignment = 0.0f;
			tree.AppendColumn (Col);

			Add (tree);
			ShowAll ();

			((DebuggingService)Runtime.DebuggingService).ThreadStateEvent += (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnThreadEvent));
		}

		void AddThread (Process thread)
		{
			TreeIter iter;
			store.Append (out iter);
			store.SetValue (iter, 0, new GLib.Value (thread.ID));
			store.SetValue (iter, 1, new GLib.Value (thread.PID));
			store.SetValue (iter, 2, new GLib.Value (thread.State.ToString()));
			if (thread.IsStopped)
				store.SetValue (iter, 3, new GLib.Value (thread.GetBacktrace().Frames[0].SourceAddress.Name));
			else
				store.SetValue (iter, 3, new GLib.Value (""));
			thread_rows.Add (thread, new TreeRowReference (store, store.GetPath (iter)));
		}

		void UpdateThread (Process thread)
		{
			TreeRowReference row = (TreeRowReference)thread_rows[thread];
			TreeIter iter;

			if (row != null && store.GetIter (out iter, row.Path)) {
				store.SetValue (iter, 0, thread.ID);
				store.SetValue (iter, 1, thread.PID);
				store.SetValue (iter, 2, thread.State.ToString());

				string location;
				if (thread.IsStopped)
					location = thread.GetBacktrace().Frames[0].SourceAddress.Name;
				else
					location = "";

				store.SetValue (iter, 3, location);
			}
			else {
				AddThread (thread);
			}
		}

		void RemoveThread (Process thread)
		{
			TreeRowReference row = (TreeRowReference)thread_rows[thread];
			TreeIter iter;

			if (row != null && store.GetIter (out iter, row.Path))
				store.Remove (ref iter);

			thread_rows.Remove (thread);
		}

		public void UpdateDisplay ()
		{
			Hashtable threads_to_remove = new Hashtable();

			foreach (Process thread in thread_rows.Keys) {
				threads_to_remove.Add (thread, thread);
			}

			foreach (Process t in ((DebuggingService)Runtime.DebuggingService).Threads) {
				if (t.State != TargetState.NO_TARGET && !t.IsDaemon) {
					UpdateThread (t);
					threads_to_remove.Remove (t);
				}
			}

			foreach (Process t in threads_to_remove.Keys) {
				RemoveThread (t);
			}
		}

		public void CleanDisplay ()
		{
			UpdateDisplay ();
		}

		public void RedrawContent ()
		{
			UpdateDisplay ();
		}

		protected void OnThreadEvent (object o, EventArgs args)
		{
			UpdateDisplay ();
		}

		public Gtk.Widget Control {
			get {
				return this;
			}
		}

		public string Id {
			get { return "MonoDevelop.Debugger.ThreadPad"; }
		}

		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public string Title {
			get {
				return "Threads";
			}
		}

		public string Icon {
			get {
				return Stock.OutputIcon;
			}
		}

                protected virtual void OnTitleChanged(EventArgs e)
                {
                        if (TitleChanged != null) {
                                TitleChanged(this, e);
                        }
                }
                protected virtual void OnIconChanged(EventArgs e)
                {
                        if (IconChanged != null) {
                                IconChanged(this, e);
                        }
                }
                public event EventHandler TitleChanged;
                public event EventHandler IconChanged;
	  
	}
}
