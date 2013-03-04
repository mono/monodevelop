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
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	public class NewIdeViMode : Mono.TextEditor.Vi.NewViEditMode
	{
		public NewIdeViMode (ExtensibleTextEditor editor)
		{
			this.editor = editor;
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			base.HandleKeypress (key, unicodeKey, modifier);
			IdeApp.Workbench.StatusBar.ShowMessage (ViEditor.Message);
		}
	}
	
	public class IdeViMode : Mono.TextEditor.Vi.ViEditMode
	{
		new ExtensibleTextEditor editor;
		TabAction tabAction;
		
		public IdeViMode (ExtensibleTextEditor editor)
		{
			this.editor = editor;
			tabAction = new TabAction (editor);
		}

		protected override Action<TextEditorData> GetInsertAction (Gdk.Key key, Gdk.ModifierType modifier)
		{
			if (modifier == Gdk.ModifierType.None) {
				switch (key) {
				case Gdk.Key.BackSpace:
					return EditActions.AdvancedBackspace;
				case Gdk.Key.Tab:
					return tabAction.Action;
				}
			}
			return base.GetInsertAction (key, modifier);
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
						editor.View.WorkbenchWindow.Document.Save ();
						Gtk.Application.Invoke (delegate {
							editor.View.WorkbenchWindow.CloseWindow (false/*, true, -1*/);
						});
						return "Saved and closed file.";
					case '!':	// :w!
						editor.View.Save ();
						break;
					default:
						return base.RunExCommand (command);
					}
				}
				else editor.View.WorkbenchWindow.Document.Save ();
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
					editor.View.WorkbenchWindow.CloseWindow (force/*, true, -1*/);
				});
				return force? "Closed file without saving.": "Closed file.";
				
				
			case 'm':
				if (!Regex.IsMatch (command, "^:mak[e!]", RegexOptions.Compiled))
					break;
				MonoDevelop.Projects.Project proj = editor.View.Project;
				if (proj != null) {
					IdeApp.ProjectOperations.Build (proj);
					return string.Format ("Building project {0}", proj.Name);
				}
				return "File is not part of a project";
			case 'c':
				// Error manipulation
				if (3 == command.Length) {
					switch (command[2]) {
					case 'n':
						// :cn - jump to next error
						IdeApp.CommandService.DispatchCommand (MonoDevelop.Ide.Commands.ViewCommands.ShowNext);
						return string.Empty;
					case 'N':
					case 'p':
						// :c[pN] - jump to previous error
						IdeApp.CommandService.DispatchCommand (MonoDevelop.Ide.Commands.ViewCommands.ShowPrevious);
						return string.Empty;
					}
				}
				break;
			}
			
			return base.RunExCommand (command);
		}
		
		protected override void HandleKeypress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			if (0 != (Gdk.ModifierType.ControlMask & modifier)) {
				switch (key) {
				case Gdk.Key.bracketright:
					// ctrl-] => Go to declaration	
					// HACK: since the SourceEditor can't link the Refactoring addin the command is provided as string.
					IdeApp.CommandService.DispatchCommand ("MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration");
					return;
				}
			}// ctrl+key		
			
			base.HandleKeypress (key, unicodeKey, modifier);
		}
	}
}
