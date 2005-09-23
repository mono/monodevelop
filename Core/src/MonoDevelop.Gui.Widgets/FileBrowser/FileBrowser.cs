//
// Author: John Luke  <jluke@cfl.rr.com>
// Author: Inigo Illan <kodeport@terra.es>
// License: LGPL
//
// Copyright 2004 John Luke
//

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Gtk;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Utils;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Widgets
{
	public delegate void DirectoryChangedEventHandler (string path);

	enum PerformingTask
	{
		None,
		Renaming,
		CreatingNew
	}

	public class FileBrowser : VBox
	{
		public DirectoryChangedEventHandler DirectoryChangedEvent;

		private static Tooltips tips = new Tooltips ();
		private Gtk.TreeView tv;
		private Gtk.ScrolledWindow scrolledwindow;
		private Gtk.ToolButton goUp, goHome;
		private ToolbarEntry entry;
		private IMessageService messageService;
		private Gtk.CellRendererText text_render;
		private ListStore store;
		private string currentDir;
		private bool ignoreHidden = true;
		private bool init = false;

		private PerformingTask performingtask = PerformingTask.None;
		private ArrayList files = new ArrayList ();
		private ArrayList hiddenfolders = new ArrayList ();

		PropertyService PropertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));

		public FileBrowser ()
		{
			messageService = (IMessageService) ServiceManager.GetService (typeof (IMessageService));

			scrolledwindow = new ScrolledWindow ();
			scrolledwindow.VscrollbarPolicy = PolicyType.Automatic;
			scrolledwindow.HscrollbarPolicy = PolicyType.Automatic;
			scrolledwindow.ShadowType = ShadowType.In;

			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;

			goUp = new ToolButton (Stock.GoUp);
			goUp.Clicked += new EventHandler (OnGoUpClicked);
			goUp.SetTooltip (tips, GettextCatalog.GetString ("Go up one level"), "Go up one level");
			toolbar.Insert (goUp, -1);

			goHome = new ToolButton (Stock.Home);
			goHome.Clicked += new EventHandler (OnGoHomeClicked);
			goHome.SetTooltip (tips, GettextCatalog.GetString ("Home"), "Home");
			toolbar.Insert (goHome, -1);

			entry = new ToolbarEntry ();
			entry.Activated += new EventHandler (OnEntryActivated);
			entry.SetTooltip (tips, GettextCatalog.GetString ("Location"), "");
			toolbar.Insert (entry, -1);
			toolbar.ShowAll ();
			this.PackStart (toolbar, false, true, 0);

			IProperties p = (IProperties) PropertyService.GetProperty ("SharpDevelop.UI.SelectStyleOptions", new DefaultProperties ());
			ignoreHidden = !p.GetProperty ("MonoDevelop.Gui.FileScout.ShowHidden", false);

			tv = new Gtk.TreeView ();
			tv.RulesHint = true;

			TreeViewColumn directorycolumn = new TreeViewColumn ();
			directorycolumn.Title = "Directories";
			
			CellRendererPixbuf pix_render = new CellRendererPixbuf ();
			directorycolumn.PackStart (pix_render, false);
			directorycolumn.AddAttribute (pix_render, "pixbuf", 0);

			text_render = new CellRendererText ();
			text_render.Edited += new EditedHandler (OnDirEdited);
			directorycolumn.PackStart (text_render, false);
			directorycolumn.AddAttribute (text_render, "text", 1);
			
			tv.AppendColumn (directorycolumn);

			store = new ListStore (typeof (Gdk.Pixbuf), typeof (string));
			CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
			tv.Model = store;

			tv.RowActivated += new RowActivatedHandler (OnRowActivated);
			tv.ButtonReleaseEvent += new ButtonReleaseEventHandler (OnButtonRelease);			
			tv.PopupMenu += new PopupMenuHandler (OnPopupMenu);			

			scrolledwindow.Add (tv);
			this.Homogeneous = false;
			this.PackEnd (scrolledwindow);
			this.ShowAll ();
			init = true;
		}

		// FIXME: we should watch the PropertyChanged event instead
		// of exposing the set part
		public bool IgnoreHidden
		{
			get { return ignoreHidden; }
			set {
				if (ignoreHidden != value) {
					ignoreHidden = value; 
					// redraw folder list
					Populate ();
				}
			}
		}

		public string CurrentDir
		{
			get { return currentDir; }
			set { 
					currentDir = System.IO.Path.GetFullPath (value);
					GetListOfHiddenFolders ();
					Populate ();

					if (DirectoryChangedEvent != null) {
						DirectoryChangedEvent (CurrentDir);
					}
				}
		}

		public string[] Files
		{
			get {
				return (string[]) files.ToArray (typeof (string)); 
			}
		}

		public void SelectFirst ()
		{
			tv.Selection.SelectPath (new TreePath ("0"));
		}

		void Populate ()
		{
			store.Clear ();

			if (System.IO.Path.GetPathRoot (CurrentDir) == CurrentDir)
				goUp.Sensitive = false;
			else if (goUp.Sensitive == false)
				goUp.Sensitive = true;

			DirectoryInfo di = new DirectoryInfo (CurrentDir);
			DirectoryInfo[] dirs = di.GetDirectories ();
			
			foreach (DirectoryInfo d in dirs)
			{
				if (ignoreHidden)
				{
					if (!d.Name.StartsWith (".") && NotHidden (d.Name))
						store.AppendValues (FileIconLoader.GetPixbufForFile (System.IO.Path.Combine (CurrentDir, d.Name), 24), d.Name);
				}
				else
				{
					store.AppendValues (FileIconLoader.GetPixbufForFile (System.IO.Path.Combine (CurrentDir, d.Name), 24), d.Name);
				}
			}

			if (init == true)
				tv.Selection.SelectPath (new Gtk.TreePath ("0"));

			entry.Text = CurrentDir;
			string[] filesaux = Directory.GetFiles (CurrentDir);

			files.Clear ();
			for (int cont = 0; cont < filesaux.Length; cont++)
			{
				if (ignoreHidden)
				{
					if (NotHidden (System.IO.Path.GetFileName (filesaux[cont])))
					{
						files.Add (filesaux[cont]);
					}
				}
				else
				{
					files.Add (filesaux[cont]);
				}
			}
		}

		private void OnRowActivated (object o, RowActivatedArgs args)
		{
			TreeIter iter;
			if (store.GetIter (out iter, args.Path))
			{
				string newDir = System.IO.Path.Combine (currentDir, (string) store.GetValue (iter, 1));
				if (Directory.Exists (newDir))
					CurrentDir = newDir;
			}
		}
		
		private void OnButtonRelease (object o, ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowPopup ();
		}

		private void OnPopupMenu (object o, PopupMenuArgs args)
		{
			ShowPopup ();
		}

		private void ShowPopup ()
		{
			// FIXME: port to Action API
			Menu menu = new Menu ();
			MenuItem openfilebrowser = new MenuItem (GettextCatalog.GetString ("Open with file browser"));
			openfilebrowser.Activated += new EventHandler (OpenFileBrowser);

			MenuItem openterminal = new MenuItem (GettextCatalog.GetString ("Open with terminal"));
			openterminal.Activated += new EventHandler (OpenTerminal);

			MenuItem rename = new MenuItem (GettextCatalog.GetString ("Rename"));
			rename.Activated += new EventHandler (OnDirRename);

			MenuItem delete = new MenuItem (GettextCatalog.GetString ("Delete"));
			delete.Activated += new EventHandler (OnDirDelete);

			MenuItem newfolder = new MenuItem (GettextCatalog.GetString ("Create new folder"));
			newfolder.Activated += new EventHandler (OnNewDir);

			menu.Append (newfolder);
			menu.Append (new MenuItem ());
			menu.Append (delete);
			menu.Append (rename);
			menu.Append (new MenuItem ());
			menu.Append (openterminal);
			menu.Append (openfilebrowser);
			menu.Popup ();
			menu.ShowAll ();
		}
		
		private void OpenFileBrowser (object o, EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			// FIXME: look in GConf for the settings
			// but strangely there is not one
			string commandline = "nautilus \"";

			if (tv.Selection.GetSelected (out model, out iter))
			{
				string selection = (string) store.GetValue (iter, 1);
				commandline += System.IO.Path.Combine (currentDir, selection) + "\"";
				Process.Start (commandline);
			}
		}
		
		private void OpenTerminal (object o, EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			// FIXME: look in GConf for the settings
			// but the args will be terminal dependent
			// leaving as hardcoded for now
			string commandline = "gnome-terminal --working-directory=\"";
			if (tv.Selection.GetSelected (out model, out iter))
			{
				string selection = (string) store.GetValue (iter, 1);
				commandline += System.IO.Path.Combine (currentDir, selection) + "\"";
				Process.Start (commandline);
			}
		}
		
		private void OnGoUpClicked (object o, EventArgs args)
		{
			if (System.IO.Path.GetPathRoot (CurrentDir) != CurrentDir)
				CurrentDir = System.IO.Path.Combine (CurrentDir, "..");
		}
		
		private void OnGoHomeClicked (object o, EventArgs args)
		{
			CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
		}

		private void OnEntryActivated (object sender, EventArgs args)
		{
			if (Directory.Exists (entry.Text.Trim ()))
				CurrentDir = entry.Text.Trim ();
			else
    			messageService.ShowError (null, String.Format (GettextCatalog.GetString ("Cannot enter '{0}' folder"), entry.Text));
		}

		private void OnDirRename (object o, EventArgs args)
		{
			TreePath treepath;
			TreeViewColumn column;

			performingtask = PerformingTask.Renaming;
			text_render.Editable = true;

			tv.GetCursor (out treepath, out column);

			tv.SetCursor (treepath, column, true);
		}

		private void OnDirEdited (object o, EditedArgs args)
		{
			text_render.Editable = false;

			switch (performingtask)
			{
				case PerformingTask.Renaming:
					TreeIter iter;
					tv.Model.IterNthChild (out iter, Int32.Parse (args.Path));
					string oldpath = (string) store.GetValue (iter, 1);

					if (oldpath != args.NewText)
					{
    					try
    					{
    						System.IO.Directory.Move (System.IO.Path.Combine(CurrentDir, oldpath), System.IO.Path.Combine(CurrentDir, args.NewText));
    					}
    					catch (Exception ex)
    					{
    						messageService.ShowError (ex, String.Format (GettextCatalog.GetString ("Could not rename folder '{0}' to '{1}'"), oldpath, args.NewText));
    					}
    					finally
    					{
    						Populate ();
    					}
					}

					break;

				case PerformingTask.CreatingNew:
					System.IO.DirectoryInfo dirinfo = new DirectoryInfo (CurrentDir);
					try
					{
						dirinfo.CreateSubdirectory(args.NewText);
					}
					catch (Exception ex)
					{
    					messageService.ShowError (ex, String.Format (GettextCatalog.GetString ("Could not create new folder '{0}'"), args.NewText));
					}
					finally
					{
						Populate ();
					}

					break;
											
				default:
					Console.WriteLine ("This should not be happening");
					break;
			}

			performingtask = PerformingTask.None;
		}

		private void OnDirDelete (object o, EventArgs args)
		{
			TreeIter iter;
			TreeModel model;

			if (messageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to delete this folder?"), GettextCatalog.GetString ("Delete folder")))
			{
				if (tv.Selection.GetSelected (out model, out iter))
				{
					try
					{
						Directory.Delete (System.IO.Path.Combine (CurrentDir, (string) store.GetValue (iter, 1)), true);
					}
					catch (Exception ex)
					{
						messageService.ShowError (ex, String.Format (GettextCatalog.GetString ("Could not delete folder '{0}'"), System.IO.Path.Combine (CurrentDir, (string) store.GetValue (iter, 1))));
					}
					finally
					{
						Populate ();
					}
				}
			}
		}

		// FIXME: When the scrollbars of the directory list
		// are shown, and we perform a new dir action
		// the column is never edited, but Populate is called
		private void OnNewDir (object o, EventArgs args)
		{
			TreeIter iter;
			TreePath treepath;
			TreeViewColumn column;

			performingtask = PerformingTask.CreatingNew;
			text_render.Editable = true;

			tv.Reorderable = false;
			iter = store.AppendValues (FileIconLoader.GetPixbufForFile (CurrentDir, 24), "folder name");
			treepath = tv.Model.GetPath(iter);

			column = tv.GetColumn (0);
			tv.SetCursor (treepath, column, true);
		}

		private void GetListOfHiddenFolders ()
		{
			hiddenfolders.Clear ();

			try {
				if (System.IO.File.Exists (CurrentDir + System.IO.Path.DirectorySeparatorChar + ".hidden"))
				{
					using (StreamReader stream =  new StreamReader (System.IO.Path.Combine (CurrentDir, ".hidden"))) {
						string foldertohide;
						while ((foldertohide = stream.ReadLine ()) != null) {
							hiddenfolders.Add (foldertohide);
							foldertohide = stream.ReadLine ();
						}
					}
				}
			} catch (UnauthorizedAccessException){
				// Ignore
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}

		private bool NotHidden (string folder)
		{
			return !hiddenfolders.Contains (folder);
		} 
	}

	public class ToolbarEntry : ToolItem
	{
		Entry entry;

		public ToolbarEntry () : base ()
		{
			entry = new Entry ();
			entry.Activated += new EventHandler (OnActivated);
			this.Add (entry);
			entry.Show ();
			this.ShowAll ();
		}

		protected void OnActivated (object sender, EventArgs a)
		{
			if (Activated != null)
				Activated (this, EventArgs.Empty);
		}

		public string Text {
			get {
				return entry.Text;
			}
			set {
				entry.Text = value;
			}
		}

		public event EventHandler Activated;
	}
}

