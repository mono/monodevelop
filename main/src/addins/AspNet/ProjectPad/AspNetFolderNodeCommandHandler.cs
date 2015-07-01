//
// AspNetFolderNodeCommandHandler.cs
//
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Ide;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.Commands;

namespace MonoDevelop.AspNet.ProjectPad
{
	class AspNetFolderNodeCommandHandler : NodeCommandHandler
	{
		[CommandUpdateHandler (AspNetCommands.AddController)]
		public void AddControllerUpdate (CommandInfo info)
		{
			var project = CurrentNode.GetParentDataItem (typeof (AspNetAppProject), true) as AspNetAppProject;
			if (project == null || !project.IsAspMvcProject) {
				info.Enabled = info.Visible = false;
				return;
			}

			var pf = (ProjectFolder)CurrentNode.DataItem;
			FilePath rootName = pf.Project.BaseDirectory.Combine ("Controllers");
			info.Enabled = info.Visible = (pf.Path == rootName || pf.Path.IsChildPathOf (rootName));
		}
		
		[CommandHandler (AspNetCommands.AddController)]
		public void AddController ()
		{
			var project = (AspNetAppProject) CurrentNode.GetParentDataItem (typeof (AspNetAppProject), true);

			object currentItem = CurrentNode.DataItem;

			var folder = CurrentNode.GetParentDataItem (typeof (ProjectFolder), true) as ProjectFolder;
			string path = folder != null ? folder.Path : project.BaseDirectory;

			AddController (project, path, null);

			ITreeNavigator nav = Tree.GetNodeAtObject (currentItem);
			if (nav != null)
				nav.Expanded = true;
		}

		public static void AddController (AspNetAppProject project, string path, string name)
		{
			var provider = project.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				throw new InvalidOperationException ("Project language has null CodeDOM provider");

			string outputFile = null;
			MvcTextTemplateHost host = null;
			AddControllerDialog dialog = null;

			try {
				dialog = new AddControllerDialog (project);
				if (!String.IsNullOrEmpty (name))
					dialog.ControllerName = name;

				bool fileGood = false;
				while (!fileGood) {
					var resp = (Gtk.ResponseType) MessageService.RunCustomDialog (dialog);
					dialog.Hide ();
					if (resp != Gtk.ResponseType.Ok || !dialog.IsValid ())
						return;

					outputFile = System.IO.Path.Combine (path, dialog.ControllerName) + ".cs";

					if (System.IO.File.Exists (outputFile)) {
						fileGood = MessageService.AskQuestion ("Overwrite file?",
								String.Format ("The file '{0}' already exists.\n", dialog.ControllerName) +
								"Would you like to overwrite it?", AlertButton.OverwriteFile, AlertButton.Cancel)
							!= AlertButton.Cancel;
					} else
						break;
				}

				host = new MvcTextTemplateHost {
					LanguageExtension = provider.FileExtension,
					ItemName = dialog.ControllerName,
					NameSpace = project.DefaultNamespace + ".Controllers"
				};

				host.ProcessTemplate (dialog.TemplateFile, outputFile);
				MonoDevelop.TextTemplating.TextTemplatingService.ShowTemplateHostErrors (host.Errors);

			} finally {
				if (host != null)
					host.Dispose ();
				if (dialog != null) {
					dialog.Destroy ();
					dialog.Dispose ();
				}
			}

			if (System.IO.File.Exists (outputFile)) {
				project.AddFile (outputFile);
				IdeApp.ProjectOperations.Save (project);
			}
		}
		
		[CommandUpdateHandler (AspNetCommands.AddView)]
		public void AddViewUpdate (CommandInfo info)
		{
			var project = CurrentNode.GetParentDataItem (typeof (AspNetAppProject), true) as AspNetAppProject;
			if (project == null || !project.IsAspMvcProject) {
				info.Enabled = info.Visible = false;
				return;
			}

			var pf = (ProjectFolder)CurrentNode.DataItem;
			FilePath rootName = pf.Project.BaseDirectory.Combine ("Views");
			info.Enabled = info.Visible =  (pf.Path == rootName || pf.Path.IsChildPathOf (rootName));
		}
		
		[CommandHandler (AspNetCommands.AddView)]
		public void AddView ()
		{
			var project = (AspNetAppProject) CurrentNode.GetParentDataItem (typeof (AspNetAppProject), true);

			object currentItem = CurrentNode.DataItem;
				
			var folder = CurrentNode.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			string path = folder != null? folder.Path : project.BaseDirectory;
			
			AddViewFromControllerCommandHandler.AddView (project, path, null);
			
			ITreeNavigator nav = Tree.GetNodeAtObject (currentItem);
			if (nav != null)
				nav.Expanded = true;
		}
	}
}
