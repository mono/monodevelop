// ChangeLogAddIn.cs
//
// Author:
//   Jacob Ilsø Christensen
//
// Copyright (C) 2006  Jacob Ilsø Christensen
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace MonoDevelop.ChangeLogAddIn
{
	public enum ChangeLogCommands
	{
		InsertEntry
	}

	public class InsertEntryHandler : CommandHandler
	{
		protected override void Run()
		{
			Document document = GetActiveChangeLogDocument();
			if (document == null) return;
			
			if (InsertHeader(document))
				InsertEntry(document);					
		}

		protected override void Update(CommandInfo info)
		{
			string file = GetSelectedFile ();
			if (file != null) {
				string clog = ChangeLogService.GetChangeLogForFile (null, file);
				info.Enabled = !string.IsNullOrEmpty (clog);
			} else
				info.Enabled = false;
		}

        private string GetSelectedFile()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				string fn = IdeApp.Workbench.ActiveDocument.FileName;
				if (fn != null && Path.GetFileName (fn) != "ChangeLog")
					return fn;
			}
			ProjectFile file = IdeApp.ProjectOperations.CurrentSelectedItem as ProjectFile;
			if (file != null)
				return file.FilePath;
			SystemFile sf = IdeApp.ProjectOperations.CurrentSelectedItem as SystemFile;
			if (sf != null)
				return sf.Path;
			return null;
		}
		
		private void InsertEntry(Document document)
		{
			IEditableTextBuffer textBuffer = document.GetContent<IEditableTextBuffer>();					
			if (textBuffer == null) return;

			string changeLogFileName = document.FileName;
			string changeLogFileNameDirectory = Path.GetDirectoryName(changeLogFileName);
			string selectedFileName = GetSelectedFile();
			string selectedFileNameDirectory = Path.GetDirectoryName(selectedFileName);
	
	        int pos = GetHeaderEndPosition(document);
	        
			if (selectedFileNameDirectory.StartsWith(changeLogFileNameDirectory)) {
				string text = "\t* " 
				    + selectedFileName.Substring(changeLogFileNameDirectory.Length + 1) + ": "
					+ Environment.NewLine + Environment.NewLine;
				textBuffer.InsertText(pos+2, text);
				
				pos += text.Length;
                textBuffer.Select(pos, pos);
			    textBuffer.CursorPosition = pos;
			    
			    document.Select();	    
	        }
		}
		
		private bool InsertHeader(Document document)
		{
			IEditableTextBuffer textBuffer = document.GetContent<IEditableTextBuffer>();					
			if (textBuffer == null) return false;
		
			string name = PropertyService.Get("ChangeLogAddIn.Name", string.Empty);
			string email = PropertyService.Get("ChangeLogAddIn.Email", string.Empty);
			
			if (string.IsNullOrEmpty (name) || string.IsNullOrEmpty (email)) {
				string title = GettextCatalog.GetString ("ChangeLog entries can't be generated");
				string detail = GettextCatalog.GetString ("The name or e-mail of the user has not been configured.");
				MessageService.ShowError (title, detail);
				return false;
			}
			
			string date = DateTime.Now.ToString("yyyy-MM-dd");
			string text = date + "  " + name + "  <" + email + ">" 
			    + Environment.NewLine + Environment.NewLine;

            // Read the first line and compare it with the header: if they are
            // the same don't insert a new header.
            
            int pos = GetHeaderEndPosition(document);
            string line = textBuffer.GetText(0, pos+2);
            
            if (line != text)
    			textBuffer.InsertText(0, text);			
    			return true;
        }
        
        private int GetHeaderEndPosition(Document document)
        {
			IEditableTextBuffer textBuffer = document.GetContent<IEditableTextBuffer>();					
			if (textBuffer == null) return 0;
			
			// This is less than optimal, we simply read 1024 chars hoping to
			// find a newline there: if we don't find it we just return 0.
            string text = textBuffer.GetText(0, Math.Min(textBuffer.Length-1, 1023));
            int pos = text.IndexOf(Environment.NewLine + Environment.NewLine);
            return pos >= 0 ? pos : 0;
        }
        
		private Document GetActiveChangeLogDocument()
		{
			string file = GetSelectedFile ();
			if (file == null)
				return null;
			
			string clog = ChangeLogService.GetChangeLogForFile (null, file);
			if (string.IsNullOrEmpty (clog))
				return null;
			
			Project project = IdeApp.ProjectOperations.CurrentSelectedProject;			
			if (project == null)
				return null;
			
			if (File.Exists (clog))
				return IdeApp.Workbench.OpenDocument (clog, false);
			
			Document document = IdeApp.Workbench.NewDocument (clog, "text/plain", "");
			document.Save();
			return document;				
		}
	}
}
