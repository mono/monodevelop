// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Text;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Project;

using Gtk;

namespace MonoDevelop.Gui.Pads
{
	public class SearchResultPad : IPadContent
	{
		ScrolledWindow sw;
		Gtk.TreeView view;
		ListStore store;
		string basePath;
		IAsyncOperation asyncOperation;
		string markupTitle;
		string title;
		string id;
		int matchCount;
		string statusText;
		
		const int COL_TYPE = 0, COL_LINE = 1, COL_COLUMN = 2, COL_DESC = 3, COL_FILE = 4, COL_PATH = 5, COL_FULLPATH = 6, COL_READ = 7, COL_READ_WEIGHT = 8, COL_ISFILE = 9;
		
		Gtk.TextBuffer logBuffer;
		Gtk.TextView logTextView;
		Gtk.ScrolledWindow logScroller;
		ToggleToolButton buttonOutput;
		ToolButton buttonStop;
		Label status;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		StringBuilder log = new StringBuilder ();
		Widget control;
		
		public SearchResultPad ()
		{
			// Toolbar
			
			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			toolbar.Orientation = Orientation.Vertical;
			toolbar.ToolbarStyle = ToolbarStyle.Icons;

			buttonStop = new ToolButton ("gtk-stop");
			buttonStop.Clicked += new EventHandler (OnButtonStopClick);
			buttonStop.SetTooltip (tips, "Stop", "Stop");
			toolbar.Insert (buttonStop, -1);

			ToolButton buttonClear = new ToolButton ("gtk-clear");
			buttonClear.Clicked += new EventHandler (OnButtonClearClick);
			buttonClear.SetTooltip (tips, "Clear results", "Clear results");
			toolbar.Insert (buttonClear, -1);
			
			buttonOutput = new ToggleToolButton (MonoDevelop.Gui.Stock.OutputIcon);
			buttonOutput.Clicked += new EventHandler (OnButtonOutputClick);
			buttonOutput.SetTooltip (tips, "Show output", "Show output");
			toolbar.Insert (buttonOutput, -1);
			
			// Results list
			
			store = new Gtk.ListStore (
				typeof (Gdk.Pixbuf), // image
				typeof (int),        // line
				typeof (int),        // column
				typeof (string),     // desc
				typeof (string),     // file
				typeof (string),     // path
				typeof (string),     // full path
				typeof (bool),       // read?
				typeof (int),       // read? -- use Pango weight
				typeof (bool));       // is file

			view = new Gtk.TreeView (store);
			view.RulesHint = true;
			AddColumns ();
			
			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			sw.Add (view);
			
			// Log view
			
			logBuffer = new Gtk.TextBuffer (new Gtk.TextTagTable ());
			logTextView = new Gtk.TextView (logBuffer);
			logTextView.Editable = false;
			logScroller = new Gtk.ScrolledWindow ();
			logScroller.ShadowType = ShadowType.In;
			logScroller.Add (logTextView);

			// HPaned
			
			Gtk.HPaned paned = new Gtk.HPaned ();
			paned.Pack1 (sw, true, true);
			paned.Pack2 (logScroller, true, true);
			
			// HBox
			
			status = new Label ();
			status.Xalign = 0.0f;
			
			VBox vbox = new VBox ();
			vbox.PackStart (paned, true, true, 0);
			vbox.PackStart (status, false, false, 3);
			
			HBox hbox = new HBox ();
			hbox.PackStart (vbox, true, true, 0);
			hbox.PackStart (toolbar, false, false, 0);
			
			control = hbox;
			
			Control.ShowAll ();
			
			logScroller.Hide ();
			
			view.RowActivated += new RowActivatedHandler (OnRowActivated);
		}
		
		public void BeginProgress (string title)
		{
			this.title = title;
			this.markupTitle = "<span foreground=\"blue\">" + title + "</span>";
			
			matchCount = 0;
			store.Clear ();
			logBuffer.Clear ();
			if (!logScroller.Visible)
				log = new StringBuilder ();
				
			OnTitleChanged (null);
			buttonStop.Sensitive = true;
		}
		
		public void EndProgress ()
		{
			markupTitle = title;
			OnTitleChanged (null);
			buttonStop.Sensitive = false;
			status.Text = " " + statusText;
		}
		
		public bool AllowReuse {
			get { return !buttonStop.Sensitive; }
		}
		
		public IAsyncOperation AsyncOperation {
			get {
				return asyncOperation;
			}
			set {
				asyncOperation = value;
			}
		}
		
		public void SetBasePath (string path)
		{
			basePath = path;
		}
		
		public Gtk.Widget Control {
			get {
				return control;
			}
		}

		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public string Title {
			get {
				return markupTitle;
			}
		}
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string Icon {
			get {
				return MonoDevelop.Gui.Stock.FindIcon;
			}
		}
		
		public void RedrawContent()
		{
		}

		void OnButtonClearClick (object sender, EventArgs e)
		{
			matchCount = 0;
			store.Clear ();
			logBuffer.Clear ();
			if (log != null) log = new StringBuilder ();
		}

		void OnButtonStopClick (object sender, EventArgs e)
		{
			asyncOperation.Cancel ();
		}

		void OnButtonOutputClick (object sender, EventArgs e)
		{
			if (buttonOutput.Active) {
				if (log != null) {
					logBuffer.Text = log.ToString ();
					log = null;
				}
				logScroller.Show ();
			} else {
				logScroller.Hide ();
			}
		}

		public void WriteText (string text)
		{
			if (log != null)
				log.Append (text);
			else {
				TextIter it = logBuffer.EndIter;
				logBuffer.Insert (ref it, text);

				if (text.EndsWith ("\n"))
					logTextView.ScrollMarkOnscreen (logBuffer.InsertMark);
			}
		}

		void AddColumns ()
		{
			Gtk.CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf ();
			
			Gtk.CellRendererText line = new Gtk.CellRendererText (), desc = new Gtk.CellRendererText () , path = new Gtk.CellRendererText (),
			file = new Gtk.CellRendererText ();
			
			TreeViewColumn col;
			col = view.AppendColumn ("", iconRender, "pixbuf", COL_TYPE);
			view.AppendColumn (GettextCatalog.GetString ("Line"), line, "text", COL_LINE, "weight", COL_READ_WEIGHT, "visible", COL_ISFILE);
			col = view.AppendColumn (GettextCatalog.GetString ("File"), file, "text", COL_FILE, "weight", COL_READ_WEIGHT, "visible", COL_ISFILE);
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("Text"), desc, "text", COL_DESC, "weight", COL_READ_WEIGHT);
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("Path"), path, "text", COL_PATH, "weight", COL_READ_WEIGHT, "visible", COL_ISFILE);
			col.Resizable = true;
		}
		
		public void Dispose ()
		{
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIter (out iter, args.Path)) {
				if (!(bool) store.GetValue (iter, COL_ISFILE))
					return;
				store.SetValue (iter, COL_READ, true);
				store.SetValue (iter, COL_READ_WEIGHT, (int) Pango.Weight.Normal);
				
				string path = (string) store.GetValue (iter, COL_FULLPATH);
				int line = (int) store.GetValue (iter, COL_LINE);
				/*int line = (int)*/ store.GetValue (iter, COL_COLUMN);

				Runtime.FileService.OpenFile (path, line, 1, true);
			}
		}
		
		public void AddResult (string file, int line, int column, string text)
		{
			if (file == null) {
				statusText = text;
			} else {
				matchCount++;
				
				Gdk.Pixbuf stock;
				stock = sw.RenderIcon (Runtime.Gui.Icons.GetImageForFile (file), Gtk.IconSize.SmallToolbar, "");
	
				string tmpPath = file;
				if (basePath != null)
					tmpPath = Runtime.FileUtilityService.AbsoluteToRelativePath (basePath, file);
				
				string fileName = tmpPath;
				string path = tmpPath;
				
				fileName = Path.GetFileName (file);
				
				try {
					path = Path.GetDirectoryName (tmpPath);
				} catch (Exception) {}
				
				store.AppendValues (stock, line, column, text, fileName, path, file, false, (int) Pango.Weight.Bold, file != null);
			}
			
			status.Text = " " + statusText + " - " + matchCount + " matches";
		}
		
		protected virtual void OnTitleChanged (EventArgs e)
		{
			if (TitleChanged != null)
				TitleChanged(this, e);
		}
		
		protected virtual void OnIconChanged (EventArgs e)
		{
			if (IconChanged != null)
				IconChanged (this, e);
		}
		
		public event EventHandler TitleChanged, IconChanged;
	}
}
