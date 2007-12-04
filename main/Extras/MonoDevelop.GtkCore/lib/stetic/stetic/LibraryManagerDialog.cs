
using System;
using Gtk;
using Mono.Unix;

namespace Stetic
{
	public class LibraryManagerDialog: IDisposable
	{
		[Glade.Widget] Gtk.TreeView libraryList;
		[Glade.Widget ("LibraryManagerDialog")] Gtk.Dialog dialog;
		Gtk.ListStore store;
		
		public LibraryManagerDialog ()
		{
			Glade.XML xml = new Glade.XML (null, "stetic.glade", "LibraryManagerDialog", null);
			xml.Autoconnect (this);
			
			store = new Gtk.ListStore (typeof(string));
			libraryList.HeadersVisible = false;
			libraryList.Model = store;
			libraryList.AppendColumn ("Assembly", new Gtk.CellRendererText (), "text", 0);

			LoadList ();
		}
		
		void LoadList ()
		{
			store.Clear ();
			foreach (string s in SteticMain.SteticApp.GetWidgetLibraries ())
				AddLibrary (s);
		}
		
		void AddLibrary (string lib)
		{
			store.AppendValues (lib);
		}
		
		public int Run ()
		{
			return dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		protected void OnClose (object s, EventArgs args)
		{
			SteticMain.SteticApp.UpdateWidgetLibraries (false);
			SteticMain.SaveConfiguration ();
		}
		
		protected void OnAdd (object s, EventArgs args)
		{
			FileChooserDialog dialog = new FileChooserDialog (Catalog.GetString (Catalog.GetString ("Add Widget Library")), null, FileChooserAction.Open,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Open, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok) {
				SteticMain.SteticApp.AddWidgetLibrary (dialog.Filename);
				AddLibrary (dialog.Filename);
				LoadList ();
			}
			dialog.Hide ();
		}
		
		protected void OnRemove (object se, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			if (libraryList.Selection.GetSelected (out model, out iter)) {
				string s = (string) store.GetValue (iter, 0);
				store.Remove (ref iter);
				SteticMain.SteticApp.RemoveWidgetLibrary (s);
			}
		}
		
		protected void OnReload (object s, EventArgs args)
		{
			SteticMain.SteticApp.UpdateWidgetLibraries (true);
		}
	}
}
