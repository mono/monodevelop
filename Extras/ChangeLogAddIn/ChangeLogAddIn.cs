/*
Copyright (C) 2006  Jacob Ilsø Christensen

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using System;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Content;

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
			Document document = GetActiveChangeLogDocument(true);
			if (document == null) return;
			
			InsertHeader(document);
			InsertEntry(document);					
		}

		protected override void Update(CommandInfo info)
		{
			info.Enabled = GetSelectedProjectFile() != null;
		}

        private ProjectFile GetSelectedProjectFile()
		{
			Pad pad = IdeApp.Workbench.Pads[typeof(SolutionPad)];
			if (pad == null) return null;

			SolutionPad solutionPad = pad.Content as SolutionPad;
			if (solutionPad == null) return null;

			ITreeNavigator navigator = solutionPad.GetSelectedNode();
			if (navigator == null) return null;
			MonoDevelop.Ide.Gui.Pads.SolutionViewPad.FileNode file = navigator.DataItem as MonoDevelop.Ide.Gui.Pads.SolutionViewPad.FileNode;
			if (file != null) {
				return file.Project.Project.GetFile (file.FileName);
			}
			return null;			
		}
		
		private void InsertEntry(Document document)
		{
			IEditableTextBuffer textBuffer = document.GetContent<IEditableTextBuffer>();					
			if (textBuffer == null) return;

			ProjectFile projectFile = GetSelectedProjectFile();
			if (projectFile == null) return;

			string changeLogFileName = document.FileName;
			string changeLogFileNameDirectory = Path.GetDirectoryName(changeLogFileName);
			string selectedFileName = projectFile.FullPath;
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
		
		private void InsertHeader(Document document)
		{
			IEditableTextBuffer textBuffer = document.GetContent<IEditableTextBuffer>();					
			if (textBuffer == null)	return;
		
			string name = Runtime.Properties.GetProperty("ChangeLogAddIn.Name", "Full Name");
			string email = Runtime.Properties.GetProperty("ChangeLogAddIn.Email", "Email Address");
			string date = DateTime.Now.ToString("yyyy-MM-dd");
			string text = date + "  " + name + " <" + email + ">" 
			    + Environment.NewLine + Environment.NewLine;

            // Read the first line and compare it with the header: if they are
            // the same don't insert a new header.
            
            int pos = GetHeaderEndPosition(document);
            string line = textBuffer.GetText(0, pos+2);
            
            if (line != text)
    			textBuffer.InsertText(0, text);			
        }
        
        private int GetHeaderEndPosition(Document document)
        {
			IEditableTextBuffer textBuffer = document.GetContent<IEditableTextBuffer>();					
			if (textBuffer == null)	return 0;
			
			// This is less than optimal, we simply read 1024 chars hoping to
			// find a newline there: if we don't find it we just return 0.
            string text = textBuffer.GetText(0, Math.Min(textBuffer.Length-1, 1023));
            int pos = text.IndexOf(Environment.NewLine + Environment.NewLine);
            return pos >= 0 ? pos : 0;
        }
        
		private Document GetActiveChangeLogDocument(bool create)
		{
		    // We look for the ChangeLog file in different places: first of all
		    // at the top-level directory of the current Project and then at
		    // the top-level directory of the enclosing combine, going up to
		    // the topmost one.
		    
            Document document = null;
            
            IProject project = ProjectService.ActiveProject.Project;
		    if (project != null && project.BasePath != null) {
    		    string changelog = Path.Combine(project.BasePath, "ChangeLog");
    		    if (File.Exists(changelog))
    		        document = IdeApp.Workbench.OpenDocument(changelog, false);
            }
                        
            if (document == null && project != null) {
//                Solution combine = ProjectService.Solution;
				string baseDirectory = Path.GetDirectoryName (ProjectService.SolutionFileName);
				string changelog = Path.Combine(baseDirectory, "ChangeLog");
				if (File.Exists(changelog)) {
                   document = IdeApp.Workbench.OpenDocument(changelog, false);
				}
            }
            
            // If no ChangeLog has been found, we create one in the current
            // project's base directory.
            
			if (document == null && create && project != null && project.BasePath != null) {
    		    string changelog = Path.Combine(project.BasePath, "ChangeLog");
                document = IdeApp.Workbench.NewDocument(changelog, "text/plain", "");
                document.Save();
			}
			
			return document;		
		}			
	}
}
	
