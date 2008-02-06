using Gtk;
using System;
using System.IO;
using System.Xml.Serialization;
using Mono.Unix;

namespace Stetic {

	public class UIManager : Gtk.UIManager {
	
		RecentFiles recentFiles;
		Menu recentFilesMenu;
		WidgetDesigner lastDesigner;

		static string menuXml =
		"<ui>" +
		"    <menubar>" +
		"	<menu action='FileMenu'>" +
		"	    <menuitem action='Open' />" +
		"	    <menuitem action='RecentFiles' />" +
		"	    <separator />" +
		"	    <menuitem action='Save' />" +
		"	    <menuitem action='SaveAs' />" +
		"	    <separator />" +
		"	    <menuitem action='ImportGlade' />" +
		"	    <menuitem action='ExportGlade' />" +
		"	    <separator />" +
		"	    <menuitem action='Close' />" +
		"	    <menuitem action='Quit' />" +
		"	</menu>" +
		"	<menu action='EditMenu'>" +
		"	    <menuitem action='Undo' />" +
		"	    <menuitem action='Redo' />" +
		"	    <separator />" +
		"	    <menuitem action='Cut' />" +
		"	    <menuitem action='Copy' />" +
		"	    <menuitem action='Paste' />" +
		"	    <menuitem action='Delete' />" +
		"	    <separator />" +
		"	    <menuitem action='LibraryManager' />" +
		"	</menu>" +
		"	<menu action='ProjectMenu'>" +
		"	    <menuitem action='EditProjectIcons' />" +
		"	</menu>" +
		"	<menu action='HelpMenu'>" +
		"	    <menuitem action='Contents' />" +
		"	    <menuitem action='About' />" +
		"	</menu>" +
		"    </menubar>" +
		"    <toolbar>" +
		"	    <toolitem action='Open' />" +
		"	    <toolitem action='Save' />" +
		"	    <separator />" +
		"	    <toolitem action='Undo' />" +
		"	    <toolitem action='Redo' />" +
		"	    <separator />" +
		"	    <toolitem action='Cut' />" +
		"	    <toolitem action='Copy' />" +
		"	    <toolitem action='Paste' />" +
		"	    <toolitem action='Delete' />" +
		"    </toolbar>" +
		"</ui>";

		public UIManager (Project project)
		{
			ActionEntry[] entries = new ActionEntry[] {
				new ActionEntry ("FileMenu", null, Catalog.GetString ("_File"), null, null, null),
				new ActionEntry ("Open", Stock.Open, null, "<control>O", Catalog.GetString ("Open a file"), OpenFile),
				new ActionEntry ("RecentFiles", null, Catalog.GetString ("Recent files"), null, null, null),
				new ActionEntry ("Save", Stock.Save, null, "<control>S", Catalog.GetString ("Save"), SaveFile),
				new ActionEntry ("SaveAs", Stock.SaveAs, null, "<control><shift>S", Catalog.GetString ("Save As"), SaveFileAs),
				new ActionEntry ("ImportGlade", null, Catalog.GetString ("_Import from Glade File..."), null, Catalog.GetString ("Import UI from a Glade file"), ImportGlade),
				new ActionEntry ("ExportGlade", null, Catalog.GetString ("_Export to Glade File..."), null, Catalog.GetString ("Export UI to a Glade file"), ExportGlade),
				new ActionEntry ("Close", Stock.Close, null, "<control>W", Catalog.GetString ("Close"), Close),
				new ActionEntry ("Quit", Stock.Quit, null, "<control>Q", Catalog.GetString ("Quit"), Quit),

				new ActionEntry ("EditMenu", null, "_Edit", null, null, null),
				new ActionEntry ("Undo", Stock.Undo, null, "<control>Z", Catalog.GetString ("Undo previous action"), Undo),
				new ActionEntry ("Redo", Stock.Redo, null, "<control><shift>Z", Catalog.GetString ("Redo previously-undone action"), Redo),
				new ActionEntry ("Cut", Stock.Cut, null, "<control>X", Catalog.GetString ("Cut selection to clipboard"), Cut),
				new ActionEntry ("Copy", Stock.Copy, null, "<control>C", Catalog.GetString ("Copy selection to clipboard"), Copy),
				new ActionEntry ("Paste", Stock.Paste, null, "<control>V", Catalog.GetString ("Paste from clipboard"), Paste),
				new ActionEntry ("Delete", Stock.Delete, null, "Delete", Catalog.GetString ("Delete selection"), Delete),
				new ActionEntry ("LibraryManager", null, Catalog.GetString ("Widget libraries..."), null, null, ShowLibraryManager),

				new ActionEntry ("ProjectMenu", null, Catalog.GetString ("Project"), null, null, null),
				new ActionEntry ("EditProjectIcons", null, Catalog.GetString ("Project Icons..."), null, null, EditIcons),

				new ActionEntry ("HelpMenu", null, Catalog.GetString ("_Help"), null, null, null),
				new ActionEntry ("Contents", Stock.Help, Catalog.GetString ("_Contents"), "F1", Catalog.GetString ("Help"), Help),
				new ActionEntry ("About", Stock.About, null, null, Catalog.GetString ("About Stetic"), About),
			};

			ActionGroup actions = new ActionGroup ("group");
			actions.Add (entries);

			InsertActionGroup (actions, 0);
			AddUiFromString (menuXml);

			// Not yet implemented
			GetAction ("/menubar/HelpMenu/Contents").Sensitive = false;
			
			GetAction ("/menubar/FileMenu/Open").IsImportant = true;
			GetAction ("/menubar/FileMenu/Save").IsImportant = true;

			// Set up Edit menu sensitivity hackery
			Gtk.MenuItem editMenuItem = (Gtk.MenuItem) GetWidget ("/menubar/EditMenu");
			Gtk.Menu editMenu = (Gtk.Menu)editMenuItem.Submenu;
			editMenu.Shown += EditShown;
			editMenu.Hidden += EditHidden;
			
			Gtk.MenuItem recentMenuItem = (Gtk.MenuItem) GetWidget ("/menubar/FileMenu/RecentFiles");
			recentFilesMenu = new Gtk.Menu ();
			recentMenuItem.Submenu = recentFilesMenu;
			recentMenuItem.ShowAll ();
			
			ReadRecentFiles ();

			SteticMain.CurrentDesignerChanged += OnDesignerChanged;
			Project = project;
		}

		Project project;
		Project Project {
			get {
				return project;
			}
			set {
				project = value;
				UpdateEdit ();
				GetAction ("/menubar/FileMenu/ImportGlade").Sensitive = (project != null);
				GetAction ("/menubar/FileMenu/ExportGlade").Sensitive = (project != null);
			}
		}
		
		void OnDesignerChanged (object s, EventArgs args)
		{
			if (lastDesigner != null)
				lastDesigner.SelectionChanged -= Selected;
			lastDesigner = SteticMain.CurrentDesigner;
			if (lastDesigner != null)
				lastDesigner.SelectionChanged += Selected;
			UpdateEdit ();
		}

		void OpenFile (object obj, EventArgs e)
		{
			FileChooserDialog dialog =
				new FileChooserDialog (Catalog.GetString (Catalog.GetString ("Open Stetic File")), null, FileChooserAction.Open,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Open, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok) {
				SteticMain.LoadProject (dialog.Filename);
				AddRecentFile (dialog.Filename);
			}
			dialog.Hide ();
		}
		
		void SaveFile (object obj, EventArgs e)
		{
			SteticMain.SaveProject ();
		}
		
		void SaveFileAs (object obj, EventArgs e)
		{
			SteticMain.SaveProjectAs ();
		}
		
		void ImportGlade (object obj, EventArgs e)
		{
			FileChooserDialog dialog =
				new FileChooserDialog (Catalog.GetString ("Import from Glade File"), null, FileChooserAction.Open,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Open, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok)
				project.ImportGlade (dialog.Filename);
			dialog.Hide ();
		}

		void ExportGlade (object obj, EventArgs e)
		{
			FileChooserDialog dialog =
				new FileChooserDialog (Catalog.GetString ("Export to Glade File"), null, FileChooserAction.Save,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Save, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok)
				project.ExportGlade (dialog.Filename);
			dialog.Hide ();
		}

		void Undo (object obj, EventArgs e)
		{
			SteticMain.GetUndoQueue().Undo ();
		}

		void Redo (object obj, EventArgs e)
		{
			SteticMain.GetUndoQueue().Redo ();
		}

		void Cut (object obj, EventArgs e)
		{
			Gtk.Editable focus = Focus;
			if (focus != null)
				focus.CutClipboard ();
			else {
				WidgetDesigner wd = SteticMain.CurrentDesigner;
				if (wd != null)
					wd.CutSelection ();
			}
		}

		void Copy (object obj, EventArgs e)
		{
			Gtk.Editable focus = Focus;
			if (focus != null)
				focus.CopyClipboard ();
			else {
				WidgetDesigner wd = SteticMain.CurrentDesigner;
				if (wd != null)
					wd.CopySelection ();
			}
		}

		void Paste (object obj, EventArgs e)
		{
			Gtk.Editable focus = Focus;
			if (focus != null)
				focus.PasteClipboard ();
			else {
				WidgetDesigner wd = SteticMain.CurrentDesigner;
				if (wd != null)
					wd.PasteToSelection ();
			}
		}

		void Delete (object obj, EventArgs e)
		{
			Gtk.Editable focus = Focus;
			if (focus != null)
				focus.DeleteSelection ();
			else {
				WidgetDesigner wd = SteticMain.CurrentDesigner;
				if (wd != null && wd.Selection != null)
					wd.DeleteSelection ();
			}
		}

		void Help (object obj, EventArgs e)
		{
			Console.WriteLine ("Help");
		}

		void EditIcons (object obj, EventArgs e)
		{
			project.EditIcons ();
		}
		
		void ShowLibraryManager (object obj, EventArgs e)
		{
			SteticMain.ShowLibraryManager ();
		}
		
		const string AppName = "Stetic";
		const string AppVersion = "0.1.0";
		static readonly string AppComments = Catalog.GetString ("A GNOME and Gtk GUI designer");
		static string[] AppAuthors = new string[] { "Dan Winship", "Lluís Sánchez" };
		const string AppCopyright = "Copyright 2004, 2005, 2006 Novell, Inc.";

		void About (object obj, EventArgs e)
		{
			Gtk.AboutDialog about = new Gtk.AboutDialog ();
			about.Name = AppName;
			about.Version = AppVersion;
			about.Comments = AppComments;
			about.Authors = AppAuthors;
			about.Copyright = AppCopyright;
			about.Website = "http://mono-project.com/Stetic";
			about.Run ();
		}

		void Quit (object obj, EventArgs e)
		{
			SteticMain.Quit ();
		}

		void Close (object obj, EventArgs e)
		{
			SteticMain.CloseProject ();
		}

		public Gtk.MenuBar MenuBar {
			get {
				return (Gtk.MenuBar) GetWidget ("/menubar");
			}
		}

		public Gtk.Toolbar Toolbar {
			get {
				return (Gtk.Toolbar) GetWidget ("/toolbar");
			}
		}

		Gtk.Editable Focus {
			get {
				if (SteticMain.MainWindow.HasToplevelFocus)
					return SteticMain.MainWindow.Focus as Gtk.Editable;
				else
					return null;
			}
		}

		void EditShown (object obj, EventArgs args)
		{
			Gtk.Editable focus = Focus;
			if (focus != null) {
				int selStart, selEnd;
				bool hasSelection, editable;
				hasSelection = focus.GetSelectionBounds (out selStart, out selEnd);
				editable = focus.IsEditable;
				UpdateEdit (hasSelection && editable, hasSelection, editable);
			} else
				UpdateEdit ();
		}

		void EditHidden (object obj, EventArgs args)
		{
			UpdateEdit (true, true, true);
		}

		public void UpdateEdit (bool cut, bool copy, bool paste)
		{
			GetAction ("/menubar/EditMenu/Cut").Sensitive = cut;
			GetAction ("/menubar/EditMenu/Copy").Sensitive = copy;
			GetAction ("/menubar/EditMenu/Paste").Sensitive = paste;
			GetAction ("/menubar/EditMenu/Delete").Sensitive = cut;
		}

		void UpdateEdit ()
		{
			WidgetDesigner wd = SteticMain.CurrentDesigner;
			if (wd != null) {
				UpdateEdit (wd.CanCutSelection, wd.CanCopySelection, wd.CanPasteToSelection);
			}
			else
				UpdateEdit (false, false, false);
		}

		void Selected (object s, EventArgs args)
		{
			UpdateEdit ();
		}
		
		void ReadRecentFiles ()
		{
			string file = GetConfigFile ();
			recentFiles = null;
			
			if (File.Exists (file)) {
				try {
					using (StreamReader sr = new StreamReader (file)) {
						XmlSerializer ser = new XmlSerializer (typeof (RecentFiles));
						recentFiles = (RecentFiles) ser.Deserialize (sr);
					}
				} catch {
					// Ignore exceptions while reading the recents file
				}
			}
			
			if (recentFiles == null)
				recentFiles = new RecentFiles ();
			
			BuildRecentMenu ();
		}
		
		public void AddRecentFile (string steticFile)
		{
			recentFiles.Files.Remove (steticFile);
			recentFiles.Files.Insert (0, steticFile);
			if (recentFiles.Files.Count > 10)
				recentFiles.Files.RemoveAt (10);

			string file = GetConfigFile ();
			try {
				if (!Directory.Exists (Path.GetDirectoryName (file)))
					Directory.CreateDirectory (Path.GetDirectoryName (file));

				using (StreamWriter sw = new StreamWriter (file)) {
					XmlSerializer ser = new XmlSerializer (typeof (RecentFiles));
					ser.Serialize (sw, recentFiles);
				}
			} catch {
				// Ignore exceptions while writing the recents file
			}
			BuildRecentMenu ();
		}
		
		void BuildRecentMenu ()
		{
			foreach (Gtk.Widget w in recentFilesMenu)
				recentFilesMenu.Remove (w);

			int n = 0;
			foreach (string f in recentFiles.Files) {
				MenuItem m = new MenuItem ("_" + n + " " + Path.GetFileName (f));
				string fname = f;
				m.Activated += delegate {
					SteticMain.LoadProject (fname);
					AddRecentFile (fname);
				};
				recentFilesMenu.Append (m);
				n++;
			}
			recentFilesMenu.ShowAll ();
		}
		
		string GetConfigFile ()
		{
			return Path.Combine (SteticMain.ConfigDir, "recent-files.xml");
		}
	}
}
