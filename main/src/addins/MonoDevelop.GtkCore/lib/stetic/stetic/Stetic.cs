using Gtk;
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Xml.Serialization;
using Mono.Unix;

namespace Stetic {

	public class SteticMain  {

		static Gnome.Program Program;
		public static Stetic.Application SteticApp;

		static Stetic.Palette Palette;
		static Stetic.Project Project;
		public static WindowListWidget ProjectView;
		static SignalsEditor Signals;
		static Gtk.Notebook WidgetNotebook; 
		static WidgetPropertyTree propertyTree;
		static WidgetTree widgetTree;

		public static Stetic.UIManager UIManager;
		public static Gtk.Window MainWindow;
		
		static string language = "C#";
		static ArrayList libraries = new ArrayList ();
		
		static Hashtable openWindows = new Hashtable ();
		public static Configuration Configuration;
		
		public static event EventHandler CurrentDesignerChanged;
		

		public static int Main (string[] args)
		{
			int n = 0;
			IsolationMode mode = IsolationMode.None;
			bool usePartial = false;
			bool useGettext = false;
			bool genEmpty = false;
			bool useMultifile = false;
			
			while (n < args.Length) {
				string arg = args[n];
				if (arg.StartsWith ("--language:"))
					language = arg.Substring (11);
				else if (arg.StartsWith ("-l:"))
					language = arg.Substring (3);
				else if (arg.StartsWith ("-lib:"))
					libraries.Add (arg.Substring (5));
				else if (arg.StartsWith ("--library:"))
					libraries.Add (arg.Substring (10));
				else if (arg == "--generate" || arg == "-g")
					break;
				else if (arg == "--noisolation")
					mode = IsolationMode.None;
				else if (arg == "--gen-partial")
					usePartial = true;
				else if (arg == "--gen-gettext")
					useGettext = true;
				else if (arg == "--gen-multifile")
					useMultifile = true;
				else if (arg == "--gen-empty")
					genEmpty = true;
				else
					break;
				n++;
			}
			
			if (args.Length == 1 && args [0] == "--help") {
				Console.WriteLine (Catalog.GetString ("Stetic - A GTK User Interface Builder")); 
				Console.WriteLine (Catalog.GetString ("Usage:"));
				Console.WriteLine ("\tstetic [<file>]");
				Console.WriteLine ("\tstetic [--language:<language>] [-lib:<library>...] --generate <sourceFile> <projectFile> ...");
				return 0;
			}
			
			Program = new Gnome.Program ("Stetic", "0.0", Gnome.Modules.UI, args);
			
			int ret;
			
			if (args.Length - n > 2 && ((args [n] == "--generate" || args [n] == "-g"))) {
				SteticApp = Stetic.ApplicationFactory.CreateApplication (IsolationMode.None);
				GenerationOptions ops = new GenerationOptions ();
				ops.UsePartialClasses = usePartial;
				ops.GenerateEmptyBuildMethod = genEmpty;
				ops.UseGettext = useGettext;
				ops.GenerateSingleFile = !useMultifile;
				ret = GenerateCode (args [n+1], args, n+2, ops);
			}
			else {
				SteticApp = Stetic.ApplicationFactory.CreateApplication (mode);
				SteticApp.AllowInProcLibraries = false;
				ret = RunApp (args, n);
			}
			
			SteticApp.Dispose ();
			return ret;
		}
		
		static int GenerateCode (string file, string[] args, int n, GenerationOptions ops)
		{
			foreach (string lib in libraries)
				SteticApp.AddWidgetLibrary (lib);
			
			SteticApp.UpdateWidgetLibraries (false);
	
			Project[] projects = new Project [args.Length - n];
			for (int i=n; i<args.Length; i++)
				projects [i - n] = SteticApp.LoadProject (args [i]);

			CodeDomProvider provider = GetProvider (language);
			CodeGenerationResult res = SteticApp.GenerateProjectCode (file, "Stetic", provider, ops, projects);
			foreach (SteticCompilationUnit f in res.Units)
				Console.WriteLine ("Generated file: " + f.Name);
			foreach (string s in res.Warnings)
				Console.WriteLine ("WARNING: " + s);
			return 0;
		}
		
		static CodeDomProvider GetProvider (string language)
		{
			return new Microsoft.CSharp.CSharpCodeProvider ();
		}
		
		static int RunApp (string[] args, int n)
		{
			Project = SteticApp.CreateProject ();
			
			Project.WidgetAdded += OnWidgetAdded;
			Project.WidgetRemoved += OnWidgetRemoved;
			Project.ModifiedChanged += OnProjectModified;
			Project.ProjectReloaded += OnProjectReloaded;

			Palette = SteticApp.PaletteWidget;
			widgetTree = SteticApp.WidgetTreeWidget;
			Signals = SteticApp.SignalsWidget;
			propertyTree = SteticApp.PropertiesWidget;
			ProjectView = new WindowListWidget ();
			
			UIManager = new Stetic.UIManager (Project);

			Glade.XML.CustomHandler = CustomWidgetHandler;
			Glade.XML glade = new Glade.XML ("stetic.glade", "MainWindow");
			glade.Autoconnect (typeof (SteticMain));

			if (ProjectView.Parent is Gtk.Viewport &&
			    ProjectView.Parent.Parent is Gtk.ScrolledWindow) {
				Gtk.Viewport viewport = (Gtk.Viewport)ProjectView.Parent;
				Gtk.ScrolledWindow scrolled = (Gtk.ScrolledWindow)viewport.Parent;
				viewport.Remove (ProjectView);
				scrolled.Remove (viewport);
				scrolled.AddWithViewport (ProjectView);
			}

			foreach (Gtk.Widget w in glade.GetWidgetPrefix ("")) {
				Gtk.Window win = w as Gtk.Window;
				if (win != null) {
					win.AddAccelGroup (UIManager.AccelGroup);
					win.ShowAll ();
				}
			}
			MainWindow = (Gtk.Window)Palette.Toplevel;
			WidgetNotebook = (Gtk.Notebook) glade ["notebook"];
			WidgetNotebook.SwitchPage += OnPageChanged;
			ProjectView.ComponentActivated += OnWidgetActivated;
			widgetTree.SelectionChanged += OnSelectionChanged;

#if GTK_SHARP_2_6
			// This is needed for both our own About dialog and for ones
			// the user constructs
			Gtk.AboutDialog.SetUrlHook (ActivateUrl);
#endif

			if (n < args.Length) {
				LoadProject (args [n]);
			}

			ReadConfiguration ();
			
			foreach (string s in Configuration.WidgetLibraries) {
				SteticApp.AddWidgetLibrary (s);
			}
			SteticApp.UpdateWidgetLibraries (false);
			
			ProjectView.Fill (Project);
			
			Program.Run ();
			return 0;
		}

		static Gtk.Widget CustomWidgetHandler (Glade.XML xml, string func_name,
						       string name, string string1, string string2,
						       int int1, int int2)
		{
			if (name == "Palette")
				return Palette;
			else if (name == "ProjectView")
				return ProjectView;
			else if (name == "PropertyGrid")
				return propertyTree;
			else if (name == "SignalsEditor")
				return Signals;
			else if (name == "MenuBar")
				return UIManager.MenuBar;
			else if (name == "Toolbar")
				return UIManager.Toolbar;
			else if (name == "WidgetTree")
				return widgetTree;
			else
				return null;
		}

#if GTK_SHARP_2_6
		static void ActivateUrl (Gtk.AboutDialog about, string url)
		{
			Gnome.Url.Show (url);
		}
#endif

		internal static void Window_Delete (object obj, DeleteEventArgs args) {
			args.RetVal = true;
			Quit ();
		}
		
		static void OnWidgetAdded (object s, WidgetInfoEventArgs args)
		{
			OpenWindow (args.WidgetInfo);
		}
		
		static void OnWidgetRemoved (object s, WidgetInfoEventArgs args)
		{
			foreach (WidgetInfo c in openWindows.Keys) {
				if (c.Name == args.WidgetInfo.Name) {
					CloseWindow (c);
					return;
				}
			}
		}
		
		static void OnSelectionChanged (object s, ComponentEventArgs args)
		{
			if (args.Component == null)
				return;
			WidgetInfo wi = Project.GetWidget (args.Component.Name);
			if (wi != null && IsWindowOpen (wi))
				OpenWindow (wi);
		}
		
		static void OnWidgetActivated (object s, EventArgs args)
		{
			ProjectItemInfo wi = ProjectView.Selection;
			OpenWindow (wi);
		}
		
		static void OnProjectModified (object s, EventArgs a)
		{
			string title = "Stetic - " + Path.GetFileName (Project.FileName);
			if (Project.Modified)
				title += "*";
			MainWindow.Title = title;
		}
		
		static void OnProjectReloaded (object sender, EventArgs a)
		{
			if (WidgetNotebook.Page == -1)
				return;
			
			// Get the opened components
			
			int active = WidgetNotebook.Page;
			ArrayList pages = new ArrayList ();
			while (WidgetNotebook.NPages > 0) {
				DesignerView view = (DesignerView) WidgetNotebook.GetNthPage (0);
				pages.Add (view.Component.Name);
				WidgetNotebook.Remove (view);
				view.Dispose ();
			}
			openWindows.Clear ();
			
			// Reopen the components
			foreach (string s in pages) {
				WidgetInfo w = Project.GetWidget (s);
				if (w != null)
					OpenWindow (w);
			}
			WidgetNotebook.Page = active;
		}
		
		public static WidgetDesigner CurrentDesigner {
			get {
				if (WidgetNotebook == null)
					return null;
				DesignerView view = WidgetNotebook.CurrentPageWidget as DesignerView;
				if (view == null)
					return null;
				else
					return view.Designer;
			}
		}
		
		static bool IsWindowOpen (WidgetInfo component)
		{
			Gtk.Widget w = openWindows [component] as Gtk.Widget;
			return w != null && w.Visible;
		}
		
		static void OpenWindow (ProjectItemInfo item)
		{
			Gtk.Widget page = (Gtk.Widget) openWindows [item];
			if (page != null) {
				page.Show ();
				WidgetNotebook.Page = WidgetNotebook.PageNum (page);
			}
			else {
				DesignerView view = new DesignerView (Project, item);
				
				// Tab label
				
				HBox tabLabel = new HBox ();
				tabLabel.PackStart (new Gtk.Image (item.Component.Type.Icon), true, true, 0);
				tabLabel.PackStart (new Label (item.Name), true, true, 3);
				Button b = new Button (new Gtk.Image ("gtk-close", IconSize.Menu));
				b.Relief = Gtk.ReliefStyle.None;
				b.WidthRequest = b.HeightRequest = 24;
				
				b.Clicked += delegate (object s, EventArgs a) {
					view.Hide ();
					WidgetNotebook.QueueResize ();
				};
				
				tabLabel.PackStart (b, false, false, 0);
				tabLabel.ShowAll ();
				
				// Notebook page
				
				int p = WidgetNotebook.AppendPage (view, tabLabel);
				view.ShowAll ();
				openWindows [item] = view;
				WidgetNotebook.Page = p;
			}
		}
		
		static void CloseWindow (WidgetInfo widget)
		{
			if (widget != null) {
				Gtk.Widget page = (Gtk.Widget) openWindows [widget];
				if (page != null) {
					WidgetNotebook.Remove (page);
					openWindows.Remove (widget);
					page.Dispose ();
					if (openWindows.Count == 0)
						SteticApp.ActiveDesigner = null;
				}
			}
		}
		
		static void OnPageChanged (object s, EventArgs a)
		{
			if (WidgetNotebook != null) {
				DesignerView view = WidgetNotebook.CurrentPageWidget as DesignerView;
				if (view != null) {
					ProjectView.Selection = view.ProjectItem;
					SteticApp.ActiveDesigner = view.Designer;
				}
			}
			
			if (CurrentDesignerChanged != null)
				CurrentDesignerChanged (null, a);
		}
		
		public static UndoQueue GetUndoQueue ()
		{
			DesignerView view = (DesignerView) WidgetNotebook.CurrentPageWidget;
			return view.UndoQueue;
		}
		
		public static void LoadProject (string file)
		{
			try {
				if (!CloseProject ())
					return;

				Project.Load (file);
				
				string title = "Stetic - " + Path.GetFileName (file);
				MainWindow.Title = title;
				ProjectView.Fill (Project);
				
			} catch (Exception ex) {
				Console.WriteLine (ex);
				string msg = string.Format ("The file '{0}' could not be loaded.", file);
				msg += " " + ex.Message;
				Gtk.MessageDialog dlg = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, ButtonsType.Close, msg);
				dlg.Run ();
				dlg.Destroy ();
			}
		}
		
		public static bool SaveProject ()
		{
			if (Project.FileName == null)
				return SaveProjectAs ();
			else {
				try {
					Project.Save (Project.FileName);
					Project.Modified = false;
					return true;
				} catch (Exception ex) {
					ReportError (Catalog.GetString ("The project could not be saved."), ex);
					return false;
				}
			}
		}
		
		public static bool SaveProjectAs ()
		{
			FileChooserDialog dialog =
				new FileChooserDialog (Catalog.GetString ("Save Stetic File As"), null, FileChooserAction.Save,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Save, Gtk.ResponseType.Ok);

			if (Project.FileName != null)
				dialog.CurrentName = Project.FileName;

			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok) {
				string name = dialog.Filename;
				if (System.IO.Path.GetExtension (name) == "")
					name = name + ".stetic";
				try {
					Project.Save (name);
					Project.Modified = false;
					SteticMain.UIManager.AddRecentFile (name);
				} catch (Exception ex) {
					ReportError (Catalog.GetString ("The project could not be saved."), ex);
					return false;
				}
			}
			dialog.Hide ();
			return true;
		}
		
		
		public static bool CloseProject ()
		{
			if (Project.Modified) {
				string msg = Catalog.GetString ("Do you want to save the project before closing?");
				Gtk.MessageDialog dlg = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal, Gtk.MessageType.Question, ButtonsType.None, msg);
				dlg.AddButton (Catalog.GetString ("Close without saving"), Gtk.ResponseType.No);
				dlg.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
				dlg.AddButton (Gtk.Stock.Save, Gtk.ResponseType.Yes);
				Gtk.ResponseType res = (Gtk.ResponseType) dlg.Run ();
				dlg.Destroy ();
				
				if (res == Gtk.ResponseType.Cancel)
					return false;
					
				if (res == Gtk.ResponseType.Yes) {
					if (!SaveProject ())
						return false;
				}
			}
			
			object[] obs = new object [openWindows.Count];
			openWindows.Values.CopyTo (obs, 0);
			foreach (Gtk.Widget page in obs) {
				WidgetNotebook.Remove (page);
				page.Destroy ();
			}
				
			openWindows.Clear ();

			Project.Close ();
			MainWindow.Title = "Stetic";
			ProjectView.Clear ();
			return true;
		}
		
		public static void Quit ()
		{
			if (!CloseProject ())
				return;
			SaveConfiguration ();
			Palette.Destroy ();
			Program.Quit ();
		}
		
		public static void ShowLibraryManager ()
		{
			using (LibraryManagerDialog dlg = new LibraryManagerDialog ()) {
				dlg.Run ();
			}
		}
		
		static void ReportError (string message, Exception ex)
		{
			string msg = message + " " + ex.Message;
			Gtk.MessageDialog dlg = new Gtk.MessageDialog (MainWindow, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, ButtonsType.Close, msg);
			dlg.Run ();
			dlg.Destroy ();
		}
		
		static void ReadConfiguration ()
		{
			string file = Path.Combine (SteticMain.ConfigDir, "configuration.xml");
			Configuration = null;
			
			if (File.Exists (file)) {
				try {
					using (StreamReader sr = new StreamReader (file)) {
						XmlSerializer ser = new XmlSerializer (typeof (Configuration));
						Configuration = (Configuration) ser.Deserialize (sr);
					}
				} catch {
					// Ignore exceptions while reading the recents file
				}
			}
			
			if (Configuration != null) {
				MainWindow.Move (Configuration.WindowX, Configuration.WindowY);
				MainWindow.Resize (Configuration.WindowWidth, Configuration.WindowHeight);
				if (Configuration.WindowState == Gdk.WindowState.Maximized)
					MainWindow.Maximize ();
				else if (Configuration.WindowState == Gdk.WindowState.Iconified)
					MainWindow.Iconify ();
				SteticApp.ShowNonContainerWarning = Configuration.ShowNonContainerWarning;
			}
			else {
				Configuration = new Configuration ();
			}
			
		}
		
		public static void SaveConfiguration ()
		{
			SteticMain.Configuration.WidgetLibraries.Clear ();
			SteticMain.Configuration.WidgetLibraries.AddRange (SteticApp.GetWidgetLibraries ());
					
			MainWindow.GetPosition (out Configuration.WindowX, out Configuration.WindowY);
			MainWindow.GetSize (out Configuration.WindowWidth, out Configuration.WindowHeight);
			Configuration.WindowState = MainWindow.GdkWindow.State;
			
			Configuration.ShowNonContainerWarning = SteticApp.ShowNonContainerWarning;
			
			string file = Path.Combine (SteticMain.ConfigDir, "configuration.xml");
			try {
				if (!Directory.Exists (SteticMain.ConfigDir))
					Directory.CreateDirectory (SteticMain.ConfigDir);

				using (StreamWriter sw = new StreamWriter (file)) {
					XmlSerializer ser = new XmlSerializer (typeof (Configuration));
					ser.Serialize (sw, Configuration);
				}
			} catch (Exception ex) {
				// Ignore exceptions while writing the recents file
				Console.WriteLine (ex);
			}
		}
		
		public static string ConfigDir {
			get { 
				string file = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".config");
				return Path.Combine (file, "stetic");
			}
		}
	}
}
