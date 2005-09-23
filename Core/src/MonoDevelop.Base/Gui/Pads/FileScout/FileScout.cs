using System;

using MonoDevelop.Gui.Widgets;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Gui.Pads
{
	public class FileScout : Gtk.VPaned, IPadContent
	{
		public string Id {
			get { return "MonoDevelop.Gui.Pads.FileScout"; }
		}
		
		public string DefaultPlacement {
			get { return "Left"; }
		}

		public Gtk.Widget Control {
			get {
				return this;
			}
		}
		
		public string Title {
			get {
				return GettextCatalog.GetString ("Files");
			}
		}
		
		public string Icon {
			get {
				//return MonoDevelop.Gui.Stock.OpenFolderBitmap;
				return Gtk.Stock.Open;
			}
		}
		
		public void RedrawContent()
		{
		}
		
		FileList filelister = new FileList ();
		FileBrowser fb = new FileBrowser ();

		public FileScout()
		{
			fb.DirectoryChangedEvent += new DirectoryChangedEventHandler (OnDirChanged);
			filelister.RowActivated += new Gtk.RowActivatedHandler (FileSelected);
			Runtime.ProjectService.CombineOpened += (CombineEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEventHandler(OnCombineOpened));
			Runtime.ProjectService.CombineClosed += (CombineEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEventHandler(OnCombineClosed));

			Gtk.Frame treef  = new Gtk.Frame ();
			treef.Add (fb);

			Gtk.ScrolledWindow listsw = new Gtk.ScrolledWindow ();
			listsw.ShadowType = Gtk.ShadowType.In;
			listsw.Add (filelister);
			
			this.Pack1 (treef, true, true);
			this.Pack2 (listsw, true, true);

			fb.SelectFirst ();
			
			OnDirChanged (fb.CurrentDir);
			this.ShowAll ();
		}

		void OnDirChanged (string path) 
		{
			filelister.Clear ();

			bool ignoreHidden = !Runtime.Properties.GetProperty ("MonoDevelop.Gui.FileScout.ShowHidden", false);
			fb.IgnoreHidden = ignoreHidden;

			foreach (string f in fb.Files)
			{
				if (System.IO.File.Exists(f)) {
					if (!(System.IO.Path.GetFileName (f)).StartsWith ("."))
					{
						FileListItem it = new FileListItem (f);
						filelister.ItemAdded (it);
					}
					else
					{
						if (!ignoreHidden)
						{
							FileListItem it = new FileListItem (f);
							filelister.ItemAdded (it);
						
						}
					}
				}
			}
		}

		void FileSelected (object sender, Gtk.RowActivatedArgs e)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;

			// we are not using SelectMultiple
			// nor can more than one be activated here
			if (filelister.Selection.GetSelected (out model, out iter))
			{
				FileListItem item = (FileListItem) filelister.Model.GetValue (iter, 3);

				//FIXME: use mimetypes not extensions
				// also change to Project tab when its a project
				if (Runtime.ProjectService.IsCombineEntryFile (item.FullName))
					Runtime.ProjectService.OpenCombine (item.FullName);
				else
					Runtime.FileService.OpenFile (item.FullName);
			}
		}

		void OnCombineOpened(object sender, CombineEventArgs args)
		{
			try {
				if (args.Combine.StartupEntry != null)
					fb.CurrentDir = args.Combine.StartupEntry.BaseDirectory;
			} catch {
				fb.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			}
		}

		void OnCombineClosed(object sender, CombineEventArgs args)
		{
			fb.CurrentDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		}

		public event EventHandler TitleChanged {
			add {}
			remove {}
		}
		
		public event EventHandler IconChanged {
			add {}
			remove {}
		}
	}
}
