using Gtk;
using GtkSharp;
using Pango;

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.Shell
{

	public class MonoPad
	{
		TextEditorControl textEditor;
		Window window;
		
		public MonoPad() {
			Application.Init ();
			
			Window win = new Gtk.Window ("MonoPad");
			window = win;
			win.DeleteEvent += new DeleteEventHandler (Main_Closed);
			textEditor = new TextEditorControl ();
			textEditor.ShowEOLMarkers = false;
			textEditor.ShowLineNumbers = true;
			textEditor.ShowInvalidLines = true;
			textEditor.ShowTabs = false;
			textEditor.ShowVRuler = false;
			
			win.RequestSize = new Size (400, 400);
			VBox box = new VBox (false, 2);
			box.PackStart(BuildMenuBar(), false, false, 0);
			//box.PackStart(BuildToolBar(), false, false, 0);
			box.PackStart(textEditor, true, true, 0);
			
			win.Add(box);
			win.ShowAll ();
			Application.Run ();
			
		}

		private MenuBar BuildMenuBar() {
			MenuBar mb = new MenuBar ();
			
			Menu file_menu = new Menu ();
			MenuItem new_item = new MenuItem("New Window");
			MenuItem open_item = new MenuItem("Open");
			MenuItem save_item = new MenuItem("Save");
			MenuItem save_as_item = new MenuItem("Save As");
			MenuItem exit_item = new MenuItem("Exit");
			new_item.Activated += new EventHandler (OnNew);
			open_item.Activated += new EventHandler (OnOpen);
			save_item.Activated += new EventHandler (OnSave);
			save_as_item.Activated += new EventHandler (OnSaveAs);
			exit_item.Activated += new EventHandler (OnExit);
			file_menu.Append (new_item);
			file_menu.Append (new MenuItem());
			file_menu.Append (open_item);
			file_menu.Append (save_item);
			file_menu.Append (save_as_item);
			file_menu.Append (new MenuItem());
			file_menu.Append (exit_item);
			
			Menu view_menu = new Menu();
			CheckMenuItem lines_item = new CheckMenuItem("Line numbers");
			CheckMenuItem eol_item = new CheckMenuItem("EOL marker");
			CheckMenuItem tab_item = new CheckMenuItem("Tab marker");
			CheckMenuItem ruler_item = new CheckMenuItem("Vertical ruler");
			CheckMenuItem invalid_item = new CheckMenuItem("Invalid lines");
			lines_item.Active = textEditor.ShowLineNumbers;
			eol_item.Active = textEditor.ShowEOLMarkers;
			tab_item.Active = textEditor.ShowTabs;
			ruler_item.Active = textEditor.ShowVRuler;
			invalid_item.Active = textEditor.ShowInvalidLines;			
			lines_item.Toggled += new EventHandler (OnLineNumbersToggled);
			eol_item.Toggled += new EventHandler (OnEOLToggled);
			tab_item.Toggled += new EventHandler (OnTabToggled);
			ruler_item.Toggled += new EventHandler (OnRulerToggled);
			invalid_item.Toggled += new EventHandler (OnInvalidToggled);
			view_menu.Append(lines_item);
			view_menu.Append(invalid_item);
			view_menu.Append(tab_item);
			view_menu.Append(eol_item);
			view_menu.Append(ruler_item);

			Menu tools_menu = new Menu();
			MenuItem font_item = new MenuItem("Font");
			font_item.Activated += new EventHandler(OnFont);
			tools_menu.Append(font_item);
			
			MenuItem file_item = new MenuItem("File");
			file_item.Submenu = file_menu;
			
			MenuItem view_item = new MenuItem("View");
			view_item.Submenu = view_menu;
			
			MenuItem tools_item = new MenuItem("Tools");
			tools_item.Submenu = tools_menu;
			
			mb.Append (file_item);
			mb.Append (view_item);
			mb.Append (tools_item);
			return mb;
		}

		private Toolbar BuildToolBar() {
			Toolbar t = new Toolbar();
			Gtk.Widget new_ = BuildToolbarButton("gtk-new");
			Gtk.Widget open = BuildToolbarButton("gtk-open");
			Gtk.Widget save = BuildToolbarButton("gtk-save");
			Gtk.Widget save_as = BuildToolbarButton("gtk-save-as");
			
			new_.ButtonPressEvent += new ButtonPressEventHandler(OnNew);
			open.ButtonPressEvent += new ButtonPressEventHandler(OnOpen);
			save.ButtonPressEvent += new ButtonPressEventHandler(OnSave);
			save_as.ButtonPressEvent += new ButtonPressEventHandler(OnSaveAs);
			
			t.AppendWidget(new_, "New", "New");
			t.AppendSpace();
			t.AppendWidget(open, "Open", "Open");
			t.AppendWidget(save, "Save", "Save");
			t.AppendWidget(save_as, "Save as", "Save as");
			
			return t;
		}
		
		private Gtk.Widget BuildToolbarButton(string stock) {
			Gtk.Button but = new Gtk.Button();
			Gtk.Image ret = new Gtk.Image();
			ret.Sensitive = true;
			ret.Stock = stock;
			ret.Xpad = 10;
			ret.Ypad = 10;
			but.Add(ret);
			return but;
		}
		
		private void OnNew (object o, ButtonPressEventArgs args) {
			New();
		}
		private void OnNew (object o, EventArgs args) {
			New();
		}
		private void New() {
			new MonoPad();
		}

		private void OnFont (object o, EventArgs args) {
			FontSelectionDialog fs = new FontSelectionDialog("Choose font");
			int response = fs.Run();
			fs.Destroy();
			if (response == (int)Gtk.ResponseType.Cancel 
					|| response == (int)Gtk.ResponseType.None 
					|| response == (int)Gtk.ResponseType.DeleteEvent)
			{
				return;
			}
			try {
				textEditor.Font = FontDescription.FromString(fs.FontName);
			} catch (Exception e) {
				ShowError("Error setting font");
			}
		}

		private void OnOpen (object o, ButtonPressEventArgs args) {
			Open();
		}
		
		private void OnOpen (object o, EventArgs args) {
			Open();
		}
		private void Open() {
			FileSelection fs = new FileSelection("File to open");
			int response = fs.Run();
			string fileName = fs.Filename;
			fs.Destroy();
			
			if (response == (int)Gtk.ResponseType.Cancel 
					|| response == (int)Gtk.ResponseType.None 
					|| response == (int)Gtk.ResponseType.DeleteEvent
					|| fileName == null
					|| fileName.Trim() == "")
			{
				return;
			}
			try {
				textEditor.LoadFile(fileName, true);
			} catch (Exception e) {
				ShowError("Unable to open the file");
			}
		}
		
		private void OnSave (object o, ButtonPressEventArgs args) {
			Save();
		}
		private void OnSave (object o, EventArgs args) {
			Save();
		}
		private void Save() {
			if (textEditor.FileName == null || textEditor.FileName.Trim() == "") {
				SaveAs();
				return;
			} 
			textEditor.SaveFile(textEditor.FileName);
			ShowMessage("File saved");
		}

		private void OnSaveAs (object o, ButtonPressEventArgs args) {
			SaveAs();
		}
		private void OnSaveAs (object o, EventArgs args) {
			SaveAs();
		}
		private void SaveAs() {
			FileSelection fs = new FileSelection("File to open");
			int response = fs.Run();
			string fileName = fs.Filename;
			fs.Destroy();
			
			if (response == (int)Gtk.ResponseType.Cancel 
					|| response == (int)Gtk.ResponseType.None 
					|| response == (int)Gtk.ResponseType.DeleteEvent
					|| fileName == null
					|| fileName.Trim() == "")
			{
				return;
			}
			if (File.Exists(fileName)) {
				if (AskQuestion("File exists. Overrite it?") == false) {
					return;
				}
			}
			try {
				textEditor.SaveFile(fileName);
			} catch (Exception e) {
				ShowError("Unable to save the file");
			}
			ShowMessage("File saved");
		}

		private void OnLineNumbersToggled (object o, EventArgs args) {
			textEditor.ShowLineNumbers = !textEditor.ShowLineNumbers;
		}
		
		private void OnEOLToggled (object o, EventArgs args) {
			textEditor.ShowEOLMarkers = !textEditor.ShowEOLMarkers;
		}
		
		private void OnTabToggled (object o, EventArgs args) {
			textEditor.ShowTabs = !textEditor.ShowTabs;
		}
		
		private void OnRulerToggled (object o, EventArgs args) {
			textEditor.ShowVRuler = !textEditor.ShowVRuler;
		}
		
		private void OnInvalidToggled (object o, EventArgs args) {
			textEditor.ShowInvalidLines = !textEditor.ShowInvalidLines;
		}
		
		
		private void OnExit (object o, EventArgs args) {
			Quit();
		}

		private void Main_Closed (object o, DeleteEventArgs e)
		{
			Quit ();
		}
		
		private void Quit() {
			Application.Quit();
		}
		
		private void ShowError(string msg) {
			MessageDialog md = new MessageDialog (window,
				DialogFlags.DestroyWithParent, MessageType.Error,
				ButtonsType.Close, msg);	
			int result = md.Run ();
			md.Destroy();
		}
		
		private void ShowMessage(string msg) {
			MessageDialog md = new MessageDialog (window,
				DialogFlags.DestroyWithParent, MessageType.Info,
				ButtonsType.Close, msg);
			int result = md.Run ();
			md.Destroy();
		}
		
		private bool AskQuestion(string question) {
			MessageDialog md = new MessageDialog (window,
				DialogFlags.DestroyWithParent, MessageType.Question,
				ButtonsType.YesNo, question);	
			int result = md.Run ();
			md.Destroy();
			return (result == (int)Gtk.ResponseType.Yes);
		}
		
		public static int Main (string[] args)
		{
			MonoPad shell = new MonoPad();
			return 0;
		}
	}
}

