// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.IO;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Core.Properties;

using MonoDevelop.Core.Services;
using MonoDevelop.TextEditor.Util;

using MonoDevelop.Internal.Project;

namespace MonoDevelop.Gui.Dialogs
{
	public class ExportProjectToHtmlDialog //: Form
	{/*
		TextBox pathTextBox  = new TextBox();
		Label   pathLabel    = new Label();
		Button  okButton     = new Button();
		Button  cancelButton = new Button();
		Button  browseButton = new Button();
		
		ProgressBar progressBar = new ProgressBar();
		
		FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.Services.GetService(typeof(FileUtilityService));
		IconService iconService = (IconService)ServiceManager.Services.GetService(typeof(IconService));
		ResourceService resourceService = (ResourceService)ServiceManager.Services.GetService(typeof(IResourceService));	
		IProject project;
		int filesExported = 0;
		
		int FilesExported {
			get {
				return filesExported;
			}
			set {
				progressBar.Value = filesExported = value;
			}
		}
		
		void StartExporting()
		{
			Controls.Clear();
			
			progressBar.Location = new Point(8, 8);
			progressBar.Size     = new Size(Width - 16, 16);
			progressBar.Minimum  = 0;
			progressBar.Maximum  = project.ProjectFiles.Count;
			
			Controls.Add(progressBar);
			
			cancelButton = new Button();
			cancelButton.Location = new Point(188 + 80, 36);
			cancelButton.Size     = new Size(74, 23);
			cancelButton.Text     = resourceService.GetString("Global.CancelButtonText");
			cancelButton.Click   += new EventHandler(StopThread);
			Controls.Add(cancelButton);
		}
		public ExportProjectToHtmlDialog(IProject project)
		{
			PropertyService propertyService = (PropertyService)ServiceManager.Services.GetService(typeof(PropertyService));
			
			this.project = project;
			
			pathLabel.Location = new Point(8, 8);
			pathLabel.Size     = new Size(60,16);
			pathLabel.Text = resourceService.GetString("Dialog.ExportProjectToHtml.FolderLabel");
			Controls.Add(pathLabel);
			
			pathTextBox.Location = new Point(70, 8);
			pathTextBox.Size     = new Size(230,16);
			Controls.Add(pathTextBox);
			
			browseButton.Location = new Point(310, 8);
			browseButton.Size     = new Size(32,23);
			browseButton.Text     = "...";
			browseButton.Click   += new EventHandler(BrowseDirectories);
			browseButton.FlatStyle = FlatStyle.System;
			Controls.Add(browseButton);
			
			okButton.Location = new Point(188, 36);
			okButton.Size     = new Size(74, 23);
			okButton.Text     = resourceService.GetString("Global.OKButtonText");
			okButton.Click   += new EventHandler(ExportProject);
			okButton.FlatStyle = FlatStyle.System;
			Controls.Add(okButton);
			
			cancelButton.Location = new Point(188 + 80, 36);
			cancelButton.Size     = new Size(74, 23);
			cancelButton.Text     = resourceService.GetString("Global.CancelButtonText");
			cancelButton.DialogResult = DialogResult.Cancel;
			cancelButton.FlatStyle = FlatStyle.System;
			Controls.Add(cancelButton);
			
			Icon = null;
			FormBorderStyle = FormBorderStyle.FixedDialog;
			Size = new Size(350, 88 + 6);
			StartPosition = FormStartPosition.CenterParent;
			ShowInTaskbar = false;
			MinimizeBox = MaximizeBox = false;
			
			Text = resourceService.GetString("Dialog.ExportProjectToHtml.DialogName");
		}
		
		void BrowseDirectories(object sender, EventArgs e)
		{
			FolderDialog fd = new FolderDialog();

			if(fd.DisplayDialog(resourceService.GetString("Dialog.ExportProjectToHtml.SelectTargetDirInfo")) == DialogResult.OK) {
				pathTextBox.Text = fd.Path;
			}
		}
		
		Hashtable projectTable = new Hashtable();
		Hashtable bitmapTable  = new Hashtable();
		int       bitmapIconIndex = 0;
		int       indexFileIndex  = 0;
		
		public Hashtable GetPath(string filename, Hashtable table, bool create)
		{
			string directory    = Path.GetDirectoryName(filename);
			string[] treepath   = directory.Split(new char[] { Path.DirectorySeparatorChar });
			Hashtable curTable = table;
			
			foreach (string path in treepath) {
				if (path.Length == 0 || path[0] == '.')
					continue;
				
				object node = curTable[path];
				
				if (node == null) {
					if (create) {
						Hashtable newTable = new Hashtable();
						curTable[path] = newTable;
						curTable = newTable;
						continue;
					} else {
						return null;
					}
				} 
				curTable = (Hashtable)node;
			}
			return curTable;
		}
		int GetImageIndex(string filename)
		{
			if (filename != null) {
				return iconService.GetImageIndexForFile(filename);
			}
			return -1;
		}
		
		class Descriptor
		{
			public string title;
			public string url;
			public Descriptor(string title, string url)
			{
				this.title = title;
				this.url = url;
			}
		}
		
		StreamWriter curFileStream  = null;
		Stack        curIndexStreamStack = new Stack();
		int          curSpanNumber  = 0;
		Hashtable    Spans          = new Hashtable();
		
		string ExportFile(string fileName, string targetPath)
		{
			string targetFile = fileUtilityService.AbsoluteToRelativePath(project.BaseDirectory, fileName).Substring(2).Replace(Path.DirectorySeparatorChar.ToString(), "") + ".html";
			
			IDocument document = new DocumentFactory().CreateDocument();
			document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategyForFile(fileName);
			StreamReader stream = File.OpenText(fileName);
			document.TextContent = stream.ReadToEnd();
			stream.Close();
			
			curFileStream = File.CreateText(targetPath + Path.DirectorySeparatorChar + targetFile);
			curFileStream.Write("<html>\r\n");
			curFileStream.Write("<head>\r\n");
			curFileStream.Write("<link rel=\"stylesheet\" type=\"text/css\" href=\"sdcss.css\">\r\n");
			curFileStream.Write("<title>" + Path.GetFileName(fileName) + "</title>\r\n");
			curFileStream.Write("</head>\r\n");
			curFileStream.Write("<body>\r\n");
			
			curFileStream.Write("<div class=\"code\"><TABLE SUMMARY=\"SourceCode\" BORDER=0 CELLSPACING=0 CELLPADDING=2 WIDTH=\"100%\">\r\n");
			curFileStream.Write("  <TR BGCOLOR=\"#FFFFFF\">\r\n");
			curFileStream.Write("    <TH WIDTH=\"50\" NOWRAP ALIGN=LEFT></TH>\r\n");
			curFileStream.Write("    <TH WIDTH=\"1%\" NOWRAP ALIGN=LEFT></TH>\r\n");
			curFileStream.Write("    <TH VALIGN=TOP ALIGN=LEFT>&nbsp; \r\n");
			curFileStream.Write("    </TH>\r\n");
			
			curFileStream.Write("  </TR>\r\n");
			int i = 0;
			foreach (LineSegment line in document.LineSegmentCollection) {
				curFileStream.Write("  <TR BGCOLOR=\"#FFFFFF\" VALIGN=TOP>\r\n");
				curFileStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=RIGHT>" + ++i + ":</TD>\r\n");
				curFileStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=LEFT>&nbsp;\r\n");
				bool spanOpen = false;
				Color curColor = Color.Black;
				bool  oldItalic = false;
				bool  oldBold   = false;
				bool firstSpan = true;
				foreach (TextWord word in line.Words) {
					switch (word.Type) {
						case TextWordType.Space:
							curFileStream.Write("&nbsp;");
						break;
						
						case TextWordType.Tab:
							for (int k = 0; k < document.TextEditorProperties.TabIndent; ++k) {
								curFileStream.Write("&nbsp;");
							}
						break;
						
						case TextWordType.Word:
							Color c = word.Color;
							string colorstr = c.R + ", " + c.G + ", " + c.B;
							
							if (word.Font.Italic) {
								colorstr = "i" + colorstr;
							}
							if (word.Font.Bold) {
								colorstr = "b" + colorstr;
							}
							if (Spans[colorstr] == null) {
								Spans[colorstr] = "span" + ++curSpanNumber;
							}
							bool newColor = c != curColor || oldItalic != word.Font.Italic || oldBold != word.Font.Bold;
							if (newColor) {
								if (!firstSpan) {
									curFileStream.Write("</span>" );
								}
								
								curFileStream.Write("<span class=\"" + Spans[colorstr].ToString() + "\">" );
								spanOpen  = true;
								firstSpan = false;
							}
							curFileStream.Write(HtmlLize(word.Word));
							
							if (newColor) {
								curColor = c;
								oldItalic = word.Font.Italic;
								oldBold = word.Font.Bold;
							}
						break;
					}
				}
				if (spanOpen) {
					curFileStream.Write("</span>" );
				}
				curFileStream.Write("</TD>\r\n");
				curFileStream.Write("</TR>\r\n");
			}
			curFileStream.Write("</TABLE></div>\r\n");
			
			curFileStream.Write("<P>\r\n");
			curFileStream.Write("This page was automatically generated by \r\n");
			curFileStream.Write("<A TARGET=\"_blank\" HREF=\"http://www.icsharpcode.net/OpenSource/SD\">SharpDevelop</A>.\r\n");
			curFileStream.Write("</p>\r\n");
			curFileStream.Write("</body>\r\n");
			curFileStream.Write("</html>\r\n");
			curFileStream.Close();
			return targetFile;
		}
		
		string HtmlLize(string str)
		{
			return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
		}
		
		void WriteIndexTable(string fileName, string name, ArrayList nameList, Hashtable table)
		{
			curIndexStreamStack.Push(File.CreateText(fileName));
			StreamWriter curIndexStream = (StreamWriter)curIndexStreamStack.Peek();
			
			curIndexStream.Write("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.0 Transitional//EN\">\r\n");
			curIndexStream.Write("<html>\r\n");
			curIndexStream.Write("<head>\r\n");
			curIndexStream.Write("<title>" + project.Name + "</title>\r\n");
			curIndexStream.Write("</head>\r\n");
			
			curIndexStream.Write("<body>\r\n");
			curIndexStream.Write("<p>Location : ");
			foreach (Descriptor d in nameList) {
				curIndexStream.Write("<a href=\""+ d.url + "\">" + d.title + "</a>/");
			}
			curIndexStream.Write(name);
			curIndexStream.Write("</p>\r\n");
			
			nameList.Add(new Descriptor(name, Path.GetFileName(fileName)));
					
			curIndexStream.Write("<TABLE SUMMARY=\"Directory\" BORDER=0 CELLSPACING=0 CELLPADDING=2 WIDTH=\"100%\">\r\n");
			curIndexStream.Write("  <TR BGCOLOR=\"#F38C00\">\r\n");
			curIndexStream.Write("    <TH WIDTH=\"1%\">&nbsp;</TH>\r\n");
			curIndexStream.Write("    <TH WIDTH=\"1%\" NOWRAP ALIGN=LEFT>Name</TH>\r\n");
			curIndexStream.Write("    <TH WIDTH=\"1%\" NOWRAP ALIGN=RIGHT>Size</TH>\r\n");
			curIndexStream.Write("    <TH WIDTH=\"1%\" NOWRAP ALIGN=LEFT>Date</TH>\r\n");
			curIndexStream.Write("    <TH VALIGN=TOP ALIGN=LEFT>&nbsp; \r\n");
			curIndexStream.Write("    </TH>\r\n");
			curIndexStream.Write("  </TR>\r\n");
			
			bool coloring = false;
			foreach (DictionaryEntry entry in table) {
				if (entry.Value is Hashtable) {
					string filename = "index" + ++indexFileIndex + ".html";
					WriteIndexTable(Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar + filename, entry.Key.ToString(), nameList, (Hashtable)entry.Value);
					nameList.RemoveAt(nameList.Count - 1);
					curIndexStream.Write("  <TR BGCOLOR=\"" + (coloring ? "#FFFFFF" : "#EEEEEE") + "\" VALIGN=TOP>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=RIGHT><a href=\"" + filename + "\"><IMG ALIGN=ABSBOTTOM BORDER=0 WIDTH=16 HEIGHT=16 SRC=\"folderbitmap.png\"></a></TD>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=LEFT><a href=\""+ filename + "\">" + entry.Key.ToString() + "</a> </TD>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=RIGHT>-</TD>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=RIGHT>-</TD>\r\n");
					curIndexStream.Write("    <TD VALIGN=TOP ALIGN=LEFT>&nbsp; \r\n");
					curIndexStream.Write("    </TD>\r\n");
					curIndexStream.Write("  </TR>\r\n");
				} else if (entry.Value is ProjectFile) {
					ProjectFile fInfo = (ProjectFile)entry.Value;
					DateTime time = Directory.GetLastAccessTime(fInfo.Name);
					FileStream reader = File.OpenRead(fInfo.Name);
					long size = reader.Length;
					reader.Close();
					
					int idx  = GetImageIndex(fInfo.Name);
					if (bitmapTable[idx] == null) {
						string filename = "fileicon" + ++bitmapIconIndex + ".png";
						
						Bitmap bmp = (Bitmap)iconService.ImageList.Images[idx];
						bmp.Save(Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar + filename);
						bitmapTable[idx] = filename;
					}
					string outFile = ExportFile(fInfo.Name, Path.GetDirectoryName(fileName));
					++FilesExported;
					curIndexStream.Write("  <TR BGCOLOR=\"" + (coloring ? "#FFFFFF" : "#EEEEEE") + "\" VALIGN=TOP>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=RIGHT><a href=\"" + outFile + "\"><IMG ALIGN=ABSBOTTOM BORDER=0 WIDTH=16 HEIGHT=16 SRC=\"" + bitmapTable[idx].ToString() +"\"></a></TD>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=LEFT><a href=\"" + outFile + "\">" + Path.GetFileName(fInfo.Name) + "</a> </TD>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=RIGHT>" + size + "</TD>\r\n");
					curIndexStream.Write("    <TD NOWRAP VALIGN=TOP ALIGN=RIGHT>" + time.ToString("d") + "</TD>\r\n");
					curIndexStream.Write("    <TD VALIGN=TOP ALIGN=LEFT>&nbsp; \r\n");
					curIndexStream.Write("    </TD>\r\n");
					curIndexStream.Write("  </TR>\r\n");
					
				}
				coloring = !coloring;
			}
			
			curIndexStream.Write("</TABLE>\r\n");
			
			curIndexStream.Write("<p>\r\n");
			curIndexStream.Write("This page was automatically generated by \r\n");
			curIndexStream.Write("<A TARGET=\"_blank\" HREF=\"http://www.icsharpcode.net/OpenSource/SD\">SharpDevelop</A>.\r\n");
			curIndexStream.Write("</p>\r\n");
			curIndexStream.Write("</body>\r\n");
			curIndexStream.Write("</html>\r\n");
			
			lock (this) {
				curIndexStream.Close();
				curIndexStreamStack.Pop();
			}
		}
		
		delegate void MyD();
		
		void QuitDialog()
		{
			DialogResult = DialogResult.OK;
		}
		
		Thread exportFilesThread; 
		void ExportFilesThread()
		{
			resourceService.GetBitmap("Icons.16x16.ClosedFolderBitmap").Save(fileUtilityService.GetDirectoryNameWithSeparator(pathTextBox.Text) + "folderbitmap.png");
			WriteIndexTable(fileUtilityService.GetDirectoryNameWithSeparator(pathTextBox.Text) + "index.html", "[ROOT]", new ArrayList(), projectTable);
			CreateCSS(pathTextBox.Text);
			Invoke(new MyD(QuitDialog));
		}
		
		void StopThread(object sender, EventArgs e)
		{
			lock (this) {
				exportFilesThread.Abort();
				curFileStream.Close();
				while (curIndexStreamStack.Count > 0) {
					((StreamWriter)curIndexStreamStack.Pop()).Close();
				}
				QuitDialog();
			}
		}
		
		void CreateCSS(string targetPath)
		{
			lock (this) {
				StreamWriter sw = File.CreateText(targetPath + Path.DirectorySeparatorChar + "sdcss.css");
				sw.Write("div.code\r\n");
				sw.Write("{\r\n");
				sw.Write("	background-color: rgb(255,255,255);\r\n");
				sw.Write("	font-family: \"Lucida Console\", \"courier new\", courier;\r\n");
				sw.Write("	color: rgb(0,0,0);\r\n");
				sw.Write("	font-size: x-small;\r\n");
				sw.Write("	padding: 1em;\r\n");
				sw.Write("	margin: 1em;\r\n");
				sw.Write("}\r\n");
				
				foreach (DictionaryEntry entry in Spans) {
					string color = entry.Key.ToString();
					string name  = entry.Value.ToString();
					bool bold   = color.StartsWith("b");
					if (bold) {
						color = color.Substring(1);
					}
					bool italic = color.StartsWith("i");
					if (italic) {
						color = color.Substring(1);
					}
					
					sw.Write("div.code span." + name +"\r\n");
					sw.Write("{\r\n");
					sw.Write("	color: rgb("+ color + ");\r\n");
					if (bold) {
						sw.Write("	font-weight: bold;\r\n");
					} else
					if (italic) {
						sw.Write("	font-weight: italic;\r\n");
					} else {
						sw.Write("	font-weight: normal;\r\n");
					}
					
					sw.Write("}\r\n");
				}
				sw.Close();
			}
		
		}
		
		void ExportProject(object sender, EventArgs e)
		{
			if (!Directory.Exists(pathTextBox.Text)) {
				MessageBox.Show("Directory doesn't exists", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			
			foreach (ProjectFile fInfo in project.ProjectFiles) {
				if (fInfo.Subtype != Subtype.Directory) {
					if (fInfo.BuildAction == BuildAction.Compile ||
					    fInfo.BuildAction == BuildAction.Nothing) {
						string relativefile = fileUtilityService.AbsoluteToRelativePath(project.BaseDirectory, fInfo.Name);
						Hashtable table = GetPath(relativefile, projectTable, true);
						table[Path.GetFileName(fInfo.Name)] = fInfo;
				    }
				}
			}
			
			StartExporting();
			exportFilesThread = new Thread(new ThreadStart(ExportFilesThread));
			exportFilesThread.IsBackground  = true;
			exportFilesThread.Start();
		}*/
	}
}
