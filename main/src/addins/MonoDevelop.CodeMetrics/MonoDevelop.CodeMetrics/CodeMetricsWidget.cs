//
// CodeMetricsWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Mono.TextEditor;
using MonoDevelop.Ide.StandardHeaders;

namespace MonoDevelop.CodeMetrics
{
	[System.ComponentModel.Category("MonoDevelop.CodeMetrics")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CodeMetricsWidget : Gtk.Bin
	{
		List<string> files = new List<string> ();
		TreeStore store = new TreeStore (typeof (string), // file name
		                                 typeof (string), // line count (real lines)
		                                 typeof (string),  // license
		                                 typeof (int) // line count number
		                                 );
		
		public CodeMetricsWidget()
		{
			this.Build();
			treeviewMetrics.RulesHint = true;
			treeviewMetrics.Model = store;
			
			CellRendererText crt = new CellRendererText ();
			crt.Ellipsize = Pango.EllipsizeMode.Start;
			TreeViewColumn col = new TreeViewColumn (GettextCatalog.GetString ("File"), crt, "text", 0);
			col.SortIndicator = true;
			col.SortColumnId = 0;
			col.Expand = true;
			col.Resizable = true;
			treeviewMetrics.AppendColumn (col);
			
			col = new TreeViewColumn (GettextCatalog.GetString ("Lines (real)"), new CellRendererText (), "text", 1);
			col.SortIndicator = true;
			col.SortColumnId = 3;
			treeviewMetrics.AppendColumn (col);
			
			col = new TreeViewColumn (GettextCatalog.GetString ("License"), new CellRendererText (), "text", 2);
			col.SortIndicator = true;
			col.SortColumnId = 2;
			treeviewMetrics.AppendColumn (col);
			this.treeviewMetrics.RowActivated += delegate {
				Gtk.TreeIter selectedIter;
				if (treeviewMetrics.Selection.GetSelected (out selectedIter)) {
					string fileName = (string)store.GetValue (selectedIter, 0);
					MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (fileName);
				}
			};
		}
		
		class MetricsWorkerThread : WorkerThread
		{
			Dictionary<string, Mono.TextEditor.Document> headers = new Dictionary<string, Mono.TextEditor.Document> ();
			int longestHeader = -1;
				
			CodeMetricsWidget widget;
			
			//int[,] num;
			public MetricsWorkerThread (CodeMetricsWidget widget)
			{
				this.widget = widget;
				foreach (KeyValuePair<string, string> header in StandardHeaderService.HeaderTemplates) {
					Mono.TextEditor.Document newDoc = new Mono.TextEditor.Document ();
					newDoc.Text = header.Value;
					headers[header.Key] = newDoc;
					longestHeader = Math.Max (longestHeader, header.Value.Length);
				}
			//	num = new int [longestHeader, longestHeader];
			}
			/* real lcs takes too long, but we can fake it.
			int LongestCommonSubstring (string str1, string str2)
			{
				if (String.IsNullOrEmpty (str1) || String.IsNullOrEmpty (str2))
					return 0;
				
				for (int i = 0; i < str1.Length; i++) {
					num [i, 0] = 0;
				}
				for (int j = 0; j < str2.Length; j++) {
					num [0, j] = 0;
				}
				
				for (int i = 1; i < str1.Length; i++) {
					for (int j = 1; j < str2.Length; j++) {
						if (str1[i] == str2[j])
							num [i, j] = num [i - 1, j - 1] + 1;
						else 
							num [i, j] = Math.Max (num [i, j - 1], num [i - 1, j]);
					}
				}
				return num [str1.Length - 1, str2.Length - 1];
			}*/
			
			int FakeLongestCommonSubstring (Mono.TextEditor.Document doc, string header)
			{
				int lcs = 0;
				int i = 0;
				
				int j = Math.Max (0, header.LastIndexOf ('}') + 1);
				while (j < header.Length && i < doc.Length) {
					if (doc.GetCharAt (i) == header[j]) {
						i++;
						j++;
						lcs++;
						continue;
					} 
					if (Char.IsWhiteSpace (header[j])) {
						j++;
						continue;
					}
					if (header[j] == '[') {
						while (j < header.Length && header[j] != ']') {
							j++;
						}
						j++;
						continue;
					}
					if (Char.IsWhiteSpace (doc.GetCharAt (i))) {
						i++;
						continue;
					}
					i++;
				}
				return lcs;
			}
			
			string last = null;
			
			string GetLicense (Mono.TextEditor.Document document)
			{
				string result = GettextCatalog.GetString ("Unknown");
				//string possibleHeader = document.GetTextAt (0, Math.Min (this.longestHeader, document.Length));
				
				if (!String.IsNullOrEmpty (last)) {
					foreach (KeyValuePair<string, string> header in StandardHeaderService.HeaderTemplates) {
						if (header.Key == last) {
							int match = FakeLongestCommonSubstring (document, header.Value);//;LongestCommonSubstring (header.Value, possibleHeader);
							if (match > header.Value.Length / 2) 
								return header.Key;
							break;
						}
					}
				}
				
				foreach (KeyValuePair<string, string> header in StandardHeaderService.HeaderTemplates) {
					if (header.Key == last)
						continue;
					int match = FakeLongestCommonSubstring (document, header.Value);
//					int match = LongestCommonSubstring (header.Value, possibleHeader);
					if (match > header.Value.Length / 2) {
						last = header.Key;
						return header.Key;
					}
				}
				return result;
			}
			Dictionary<string, int> licenseStats = new Dictionary<string,int> ();
			protected override void InnerRun ()
			{
				ulong totalLines = 0, totalRealLines = 0, totalCommentedLines = 0;
				Mono.TextEditor.Document doc = new Mono.TextEditor.Document ();
				for (int i = 0; i < widget.files.Count; i++) {
					string file = widget.files [i];
					if (base.IsStopping)
						return;
					try {
						doc.Text = System.IO.File.ReadAllText (file);
					} catch (Exception e) {
						DispatchService.GuiSyncDispatch (delegate {
						MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction (i / (double)widget.files.Count);
							widget.store.AppendValues (file,
							                           e.Message,
							                           e.Message);
						});
						continue;
					}
					int realLines = 0;
					foreach (LineSegment segment in doc.Lines) {
						string text = doc.GetTextAt (segment).Trim ();
						bool isComment = text.StartsWith ("//");
						if (isComment)
							totalCommentedLines++;
						if (text.Length > 0 && !isComment)
							realLines++;
					}
					string license = GetLicense (doc);
					if (!licenseStats.ContainsKey (license))
						licenseStats [license] = 0;
					totalLines     += (ulong)doc.LineCount;
					totalRealLines += (ulong)realLines;
					licenseStats[license]++;
					DispatchService.GuiSyncDispatch (delegate {
						MonoDevelop.Ide.Gui.IdeApp.Workbench.StatusBar.SetProgressFraction (i / (double)widget.files.Count);
						widget.store.AppendValues (file,
						                           doc.LineCount + "(" + realLines + ")",
						                           license,
						                           doc.LineCount);
					});
				}
				DispatchService.GuiSyncDispatch (delegate {
					IdeApp.Workbench.StatusBar.EndProgress ();
					widget.ShowResults (totalLines, totalRealLines, totalCommentedLines, licenseStats);
				});
				base.Stop ();
			}
				
		}
		
		public void ShowResults (ulong lines, ulong realLines, ulong commentedLines, Dictionary<string, int> licenseStats)
		{
			textviewReport.Buffer.Text = GettextCatalog.GetString ("Results:"); 
			textviewReport.Buffer.Text += Environment.NewLine; 
			textviewReport.Buffer.Text += GettextCatalog.GetString ("lines: {0} (real:{1}), commented:{2} ({3:0.00}%), blank:{4} ({5:0.00}%))",
			                                                       lines,
			                                                       realLines,
			                                                       commentedLines,
			                                                       commentedLines * 100.0 / lines,
			                                                       lines - realLines - commentedLines,
			                                                       (lines - realLines - commentedLines) * 100.0 / lines);
			textviewReport.Buffer.Text += Environment.NewLine; 
			textviewReport.Buffer.Text += Environment.NewLine; 
			textviewReport.Buffer.Text += GettextCatalog.GetString ("Licenses:"); 
			textviewReport.Buffer.Text += Environment.NewLine; 
			foreach (KeyValuePair<string, int> license in licenseStats) {
				textviewReport.Buffer.Text += "\t"; 
				textviewReport.Buffer.Text += license.Key;
				textviewReport.Buffer.Text += Environment.NewLine; 
				textviewReport.Buffer.Text +=  String.Format ("\t\t{0} ({1:0.00}%)",
				                                              license.Value,
				                                              license.Value * 100.0 / this.files.Count); 
				textviewReport.Buffer.Text += Environment.NewLine; 
			}
		}
		
		public void Run ()
		{
			MetricsWorkerThread thread = new MetricsWorkerThread (this);
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Scanning files..."));
			textviewReport.Buffer.Text = GettextCatalog.GetString ("Scanning files...");
			thread.Start ();
		}
		
		public void Add (string fileName)
		{
			files.Add (fileName);
		}
		
		public void Add (ProjectFile projectFile)
		{
			if (projectFile.BuildAction == BuildAction.Compile) 
				Add (projectFile.FilePath);
		}
		
		public void Add (Project project)
		{
			foreach (ProjectFile projectFile in project.Files) {
				Add (projectFile);
			}
		}
		
		public void Add (SolutionFolder combine)
		{
			foreach (Project project in combine.GetAllProjects ()) {
				Add (project);
			}
		}
		
		public void Add (WorkspaceItem item)
		{
			foreach (Project project in item.GetAllProjects ()) {
				Add (project);
			}
		}
	}
}