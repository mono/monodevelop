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
using System.Collections.Generic;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class WordCountDialog : Dialog
	{
		ScrolledWindow scrolledwindow;
		TreeView resultListView;
		TreeStore store;
		ComboBox locationComboBox;
		IList<Report> items;
		Report total;
		
		class Report
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
			
			public static Report operator+(Report r, Report s)
			{
				Report tmpReport = new Report (GettextCatalog.GetString("Total"), s.chars, s.words, s.lines);
				
				tmpReport.chars += r.chars;
				tmpReport.words += r.words;
				tmpReport.lines += r.lines;
				return tmpReport;
			}
		}
		
		static Report GetReport(string filename)
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
		
		void StartEvent(object sender, System.EventArgs e)
		{
			items = new List<Report>();
			total = null;
			
			switch (locationComboBox.Active) {
			case 0: {// current file
				Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null) {
					if (doc.FileName == null) {
						Services.MessageService.ShowWarning (GettextCatalog.GetString ("You must save the file"));
					} else {
						Report r = GetReport (doc.FileName);
						if (r != null) items.Add(r);
					}
				}
				break;
			}
			case 1: {// all open files
				if (IdeApp.Workbench.Documents.Count > 0) {
					bool dirty = false;
					
					total = new Report (GettextCatalog.GetString ("total"), 0, 0, 0);
					foreach (Document doc in IdeApp.Workbench.Documents) {
						if (doc.FileName == null) {
							Services.MessageService.ShowWarning (GettextCatalog.GetString ("You must save the file"));
							continue;
						} else {
							Report r = GetReport (doc.FileName);
							if (r != null) {
								if (doc.IsDirty) dirty = true;
								total += r;
								items.Add(r);
							}
						}
					}
					
					if (dirty) {
						Services.MessageService.ShowWarning (GettextCatalog.GetString ("Unsaved changed to open files were not included in counting"));
					}
				}
				break;
			}
			case 2: {// whole project
				if (ProjectService.Solution == null) {
					Services.MessageService.ShowError (GettextCatalog.GetString ("You must be in project mode"));
					break;
				}
				total = new Report (GettextCatalog.GetString ("total"), 0, 0, 0);
				CountCombine (ProjectService.Solution, ref total);
				break;
			}
			}
			
			UpdateList();
		}
		
		void CountCombine(Solution combine, ref Report all)
		{
			foreach (IProject project in combine.AllProjects) {
				foreach (ProjectItem item in project.Items) {
					ProjectFile finfo = item as ProjectFile;
					if (finfo == null)
						continue;
					if (finfo.FileType == FileType.Compile) {
						Report r = GetReport (finfo.FullPath);
						all += r;
						items.Add(r);
					}
				}
			}
		}
		
		void UpdateList()
		{
			if (items == null) {
				return;
			}

			// clear it here
			store = new TreeStore (typeof (string), typeof (long), typeof (long), typeof (long));
			
			if (items.Count == 0) {
				return;
			}
			
			foreach (Report report in items) {
				store.AppendValues (System.IO.Path.GetFileName(report.name), report.chars, report.words, report.lines);
			}
			
			if (total != null) {
				store.AppendValues (System.IO.Path.GetFileName(total.name), total.chars, total.words, total.lines);						
			}
			
			resultListView.Model = store;
		}				
		
		public WordCountDialog ()
		{
			this.BorderWidth = 6;
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.HasSeparator = false;
			InitializeComponents();
			this.ShowAll ();
		}
		
		void InitializeComponents()
		{
			this.SetDefaultSize (300, 300);
			this.Title = GettextCatalog.GetString ("Word Count");
			Button startButton = new Button (Gtk.Stock.Execute);
			startButton.Clicked += new EventHandler (StartEvent);

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
			fileColumn.SortIndicator = true;
			fileColumn.SortColumnId = 0;
			resultListView.AppendColumn (fileColumn);
			
			TreeViewColumn charsColumn = new TreeViewColumn (GettextCatalog.GetString ("Chars"), new CellRendererText (), "text", 1);
			charsColumn.SortIndicator = true;
			charsColumn.SortColumnId = 1;
			resultListView.AppendColumn (charsColumn);
			
			TreeViewColumn wordsColumn = new TreeViewColumn (GettextCatalog.GetString ("Words"), new CellRendererText (), "text", 2);
			wordsColumn.SortIndicator = true;
			wordsColumn.SortColumnId = 2;
			resultListView.AppendColumn (wordsColumn);
			
			TreeViewColumn linesColumn = new TreeViewColumn (GettextCatalog.GetString ("Lines"), new CellRendererText (), "text", 3);
			linesColumn.SortIndicator = true;
			linesColumn.SortColumnId = 3;
			resultListView.AppendColumn (linesColumn);
			
			this.Icon = Services.Resources.GetIcon ("gtk-find");
			this.TransientFor = IdeApp.Workbench.RootWindow;
			
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

