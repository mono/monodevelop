// 
// IdeViMode.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using System.Text.RegularExpressions;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.SourceEditor
{
	
	
	public class IdeViMode : Mono.TextEditor.Vi.ViEditMode
	{
		ExtensibleTextEditor editor;
		
		public IdeViMode (ExtensibleTextEditor editor)
		{
			this.editor = editor;
		}
		
		public override string Status {
			get { return base.Status; }
			protected set {
				base.Status = value;
				IdeApp.Workbench.StatusBar.ShowMessage (value);
			}
		}
		
		protected override string RunExCommand (string command)
		{
			if (':' != command[0] || 2 > command.Length)
				return base.RunExCommand (command);

			switch (command[1]) {
			case 'w':
				if (2 < command.Length) { 
					switch (command[2]) {
					case 'q':	// :wq
						editor.View.Save ();
						Gtk.Application.Invoke (delegate {
							editor.View.WorkbenchWindow.CloseWindow (false, true, -1);
						});
						return "Saved and closed file.";
					case '!':	// :w!
						editor.View.Save ();
						break;
					default:
						return base.RunExCommand (command);
					}
				}
				else editor.View.Save ();
				return "Saved file.";
				
			case 'q':
				bool force = false;
				if (2 < command.Length) {
					switch (command[2]) {
					case '!':	// :q!
						force = true;
						break;
					default:
						return base.RunExCommand (command);
					}
				}
				
				if (!force && editor.View.IsDirty)
					return "Document has not been saved!";

				Gtk.Application.Invoke (delegate {
					editor.View.WorkbenchWindow.CloseWindow (force, true, -1);
				});
				return force? "Closed file without saving.": "Closed file.";
				
				
			case 'm':
				if (!Regex.IsMatch (command, "^:mak[e!]", RegexOptions.Compiled))
					break;
				MonoDevelop.Projects.Project proj = editor.View.Project;
				if (proj != null) {
					IdeApp.ProjectOperations.Build (proj);
					return string.Format ("Building project {0}", proj.Name);
				} else {
					return "File is not part of a project";
				}
			}
			
			return base.RunExCommand (command);
		}
	}
}
