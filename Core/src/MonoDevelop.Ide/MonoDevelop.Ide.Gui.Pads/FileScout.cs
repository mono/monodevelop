using System;
using System.IO;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Pads
{
	internal class FileScout : Gtk.VPaned, IPadContent
	{
		void IPadContent.Initialize (IPadWindow window)
		{
		}
		
		public string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.FileScout"; }
		}
		
		public string DefaultPlacement {
			get { return "Left"; }
		}

		public Gtk.Widget Control {
			get {
				return this;
			}
		}
		
		public void RedrawContent()
		{
		}
		
		FileList filelister = new FileList ();
		FileBrowser fb = new FileBrowser ();

		public FileScout()
		{
			string path = PropertyService.Get("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", 
									System.Environment.GetEnvironmentVariable("HOME")).ToString();

			if (Directory.Exists(path))
			{
				fb.CurrentDir = path;
			}

			fb.DirectoryChangedEvent += new DirectoryChangedEventHandler (OnDirChanged);
			filelister.RowActivated += new Gtk.RowActivatedHandler (FileSelected);
			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) DispatchService.GuiDispatch (new CombineEventHandler(OnCombineOpened));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) DispatchService.GuiDispatch (new CombineEventHandler(OnCombineClosed));

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

			bool ignoreHidden = !PropertyService.Get ("MonoDevelop.Core.Gui.FileScout.ShowHidden", false);
			fb.IgnoreHidden = ignoreHidden;

			foreach (string f in fb.Files)
			{
				try
				{
					if (System.IO.File.Exists(f))
					{
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
				catch (IOException) {} // Avoid crash on file existence check error
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
				if (Services.ProjectService.IsCombineEntryFile (item.FullName))
					IdeApp.ProjectOperations.OpenCombine (item.FullName);
				else
					IdeApp.Workbench.OpenDocument (item.FullName);
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
	}
}
