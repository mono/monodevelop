// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;
using Gtk;
using System.Collections;

using MonoDevelop.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Dialogs
{
	internal class WordCountDialog : Dialog
	{
		ScrolledWindow scrolledwindow;
		TreeView resultListView;
		TreeStore store;
		ComboBox locationComboBox;
		ArrayList items;
		Report total;
		
		internal class Report
		{
			public string name;
			public long chars;
			public long words;
			public long lines;
			
			public Report(string name, long chars, long words, long lines)
			{
				this.name  = name;
				this.chars = chars;
				this.words = words;
				this.lines = lines;
			}
			
			public string[] ToListItem()
			{
				return new string[] {System.IO.Path.GetFileName (name), chars.ToString (), words.ToString (), lines.ToString ()};
			}
			
			public static Report operator+(Report r, Report s)
			{
				Report tmpReport = new Report (GettextCatalog.GetString("total"), s.chars, s.words, s.lines);
				
				tmpReport.chars += r.chars;
				tmpReport.words += r.words;
				tmpReport.lines += r.lines;
				return tmpReport;
			}
		}
		
		Report GetReport(string filename)
		{
			long numLines = 0;
			long numWords = 0;
			long numChars = 0;
			
			if (!System.IO.File.Exists(filename)) return null;
			
			FileStream istream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
			StreamReader sr = new StreamReader(istream);
			string line = sr.ReadLine();
			while (line != null) {
				++numLines;
				numChars += line.Length;
				string[] words = line.Split(null);
				numWords += words.Length;
				line = sr.ReadLine();
			}
			
			sr.Close();
			return new Report(filename, numChars, numWords, numLines);
		}
		
		void startEvent(object sender, System.EventArgs e)
		{
			items = new ArrayList();
			total = null;
			
			switch (locationComboBox.Active) {
			case 0: {// current file
				IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
				if (window != null) {
					if (window.ViewContent.ContentName == null) {
						Runtime.MessageService.ShowWarning (GettextCatalog.GetString ("You must save the file"));
					} else {
						Report r = GetReport(window.ViewContent.ContentName);
						if (r != null) items.Add(r);
					}
				}
				break;
			}
			case 1: {// all open files
				if (WorkbenchSingleton.Workbench.ViewContentCollection.Count > 0) {
					bool dirty = false;
					
					total = new Report (GettextCatalog.GetString ("total"), 0, 0, 0);
					foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
						if (content.ContentName == null) {
							Runtime.MessageService.ShowWarning (GettextCatalog.GetString ("You must save the file"));
							continue;
						} else {
							Report r = GetReport(content.ContentName);
							if (r != null) {
								if (content.IsDirty) dirty = true;
								total += r;
								items.Add(r);
							}
						}
					}
					
					if (dirty) {
						Runtime.MessageService.ShowWarning (GettextCatalog.GetString ("Unsaved changed to open files were not included in counting"));
					}
				}
				break;
			}
			case 2: {// whole project
				if (Runtime.ProjectService.CurrentOpenCombine == null) {
					Runtime.MessageService.ShowError (GettextCatalog.GetString ("You must be in project mode"));
					break;
				}
				total = new Report (GettextCatalog.GetString ("total"), 0, 0, 0);
				CountCombine (Runtime.ProjectService.CurrentOpenCombine, ref total);
				break;
			}
			}
			
			UpdateList(0);
		}
		
		void CountCombine(Combine combine, ref Report all)
		{
			foreach (CombineEntry entry in combine.Entries) {
				if (entry is Project) {
					foreach (ProjectFile finfo in ((Project)entry).ProjectFiles) {
						if (finfo.Subtype != Subtype.Directory && 
						    finfo.BuildAction == BuildAction.Compile) {
							Report r = GetReport(finfo.Name);
							all += r;
							items.Add(r);
						}
					}
				} else
					CountCombine ((Combine)entry, ref all);
			}
		}
		
		void UpdateList(int SortKey)
		{
			if (items == null) {
				return;
			}
			// clear it here
			store = new TreeStore (typeof (string), typeof (string), typeof (string), typeof (string));
			
			if (items.Count == 0) {
				return;
			}
			
			ReportComparer rc = new ReportComparer(SortKey);
			items.Sort(rc);
			
			for (int i = 0; i < items.Count; ++i) {
				string[] tmp = ((Report)items[i]).ToListItem();
				store.AppendValues (tmp[0], tmp[1], tmp[2], tmp[3]);
			}
			
			if (total != null) {
				store.AppendValues ("", "", "", "");
				string[] tmp = total.ToListItem();
				store.AppendValues (tmp[0], tmp[1], tmp[2], tmp[3]);
			}
			
			resultListView.Model = store;
			resultListView.HeadersClickable = true;
		}		
		
		internal class ReportComparer : IComparer
		{
			int sortKey;
		
			public ReportComparer(int SortKey)
			{
				sortKey = SortKey;
			}
			
			public int Compare(object x, object y)
			{
				Report xr = x as Report;
				Report yr = y as Report;
				
				if (x == null || y == null) return 1;
				
				switch (sortKey) {
				case 0:  // files
					return String.Compare(xr.name, yr.name);
				case 1:  // chars
					return xr.chars.CompareTo(yr.chars);
				case 2:  // words
					return xr.words.CompareTo(yr.words);
				case 3:  // lines
					return xr.lines.CompareTo(yr.lines);
				default:
					return 1;
				}
			}
		}
		
		private SortType ReverseSort (SortType st)
		{
			//Runtime.LoggingService.Info (st);
			if (st == SortType.Ascending)
				return SortType.Descending;
			else
				return SortType.Ascending;
		}
		
		void SortEvt (object sender, EventArgs e)
		{
			TreeViewColumn col = (TreeViewColumn) sender;
			string file = GettextCatalog.GetString ("File");
			string chars = GettextCatalog.GetString ("Chars");
			string words = GettextCatalog.GetString ("Words");
			string lines = GettextCatalog.GetString ("Lines");
			
			if (file == col.Title)
				store.SetSortColumnId (0, ReverseSort (col.SortOrder));
			else if (chars == col.Title)
				store.SetSortColumnId (1, ReverseSort (col.SortOrder));
			else if (words == col.Title)
				store.SetSortColumnId (2, ReverseSort (col.SortOrder));
			else if (lines == col.Title)
				store.SetSortColumnId (3, ReverseSort (col.SortOrder));
			
			//UpdateList ((TreeViewColumn)e.Column);
		}
		
		public WordCountDialog ()
		{
			this.BorderWidth = 6;
			this.TransientFor = (Window) WorkbenchSingleton.Workbench;
			this.HasSeparator = false;
			InitializeComponents();
			this.ShowAll ();
		}
		
		void InitializeComponents()
		{
			this.SetDefaultSize (300, 300);
			this.Title = GettextCatalog.GetString ("Word Count");
			Button startButton = new Button (Gtk.Stock.Execute);
			startButton.Clicked += new EventHandler (startEvent);

			// dont emit response
			this.ActionArea.PackStart (startButton);
			
			this.AddButton (Gtk.Stock.Cancel, (int) ResponseType.Cancel);
			
			scrolledwindow = new ScrolledWindow();
			scrolledwindow.VscrollbarPolicy = PolicyType.Automatic;
			scrolledwindow.HscrollbarPolicy = PolicyType.Never;
			scrolledwindow.ShadowType = ShadowType.In;
			
			resultListView = new TreeView ();
			resultListView.RulesHint = true;

			TreeViewColumn fileColumn = new TreeViewColumn (GettextCatalog.GetString ("File"), new CellRendererText (), "text", 0);
			fileColumn.Clicked += new EventHandler (SortEvt);
			resultListView.AppendColumn (fileColumn);
			
			TreeViewColumn charsColumn = new TreeViewColumn (GettextCatalog.GetString ("Chars"), new CellRendererText (), "text", 1);
			charsColumn.Clicked += new EventHandler (SortEvt);
			resultListView.AppendColumn (charsColumn);
			
			TreeViewColumn wordsColumn = new TreeViewColumn (GettextCatalog.GetString ("Words"), new CellRendererText (), "text", 2);
			wordsColumn.Clicked += new EventHandler (SortEvt);
			resultListView.AppendColumn (wordsColumn);
			
			TreeViewColumn linesColumn = new TreeViewColumn (GettextCatalog.GetString ("Lines"), new CellRendererText (), "text", 3);
			linesColumn.Clicked += new EventHandler (SortEvt);
			resultListView.AppendColumn (linesColumn);
			
			this.Icon = Runtime.Gui.Resources.GetIcon ("gtk-find");
			this.TransientFor = (Window) WorkbenchSingleton.Workbench;
			
			HBox hbox = new HBox (false, 0);
			Label l = new Label (GettextCatalog.GetString ("_Count where"));
			hbox.PackStart (l);
			
			locationComboBox = ComboBox.NewText ();
			locationComboBox.AppendText (GettextCatalog.GetString ("Current file"));
			locationComboBox.AppendText (GettextCatalog.GetString ("All open files"));
			locationComboBox.AppendText (GettextCatalog.GetString ("Whole solution"));
			locationComboBox.Active = 0;
			hbox.PackStart (locationComboBox);
			
			scrolledwindow.Add(resultListView);
			this.VBox.PackStart (hbox, false, true, 0);
			this.VBox.PackStart (scrolledwindow, true, true, 6);
		}
	}
}

