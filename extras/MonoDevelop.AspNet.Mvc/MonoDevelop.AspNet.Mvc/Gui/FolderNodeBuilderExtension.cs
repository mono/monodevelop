// 
// FolderNodeBuilderExtension.cs
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.AspNet.Mvc.TextTemplating;

namespace MonoDevelop.AspNet.Mvc.Gui
{
	
	class FolderNodeBuilderExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (ProjectFolder).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof (FolderCommandHandler); }
		}
	}
	
	class FolderCommandHandler : NodeCommandHandler
	{
		[CommandUpdateHandler (AspMvcCommands.AddController)]
		public void AddControllerUpdate (CommandInfo info)
		{
			ProjectFolder pf = (ProjectFolder)CurrentNode.DataItem;
			string rootName = System.IO.Path.Combine (pf.Project.BaseDirectory, "Controllers");
			info.Enabled = info.Visible = pf.Path.StartsWith (rootName)
				&& (pf.Path.Length == rootName.Length || pf.Path[rootName.Length] == System.IO.Path.DirectorySeparatorChar);
		}
		
		[CommandHandler (AspMvcCommands.AddController)]
		public void AddController ()
		{
			AddFile ("AspMvcController");
		}
		
		[CommandUpdateHandler (AspMvcCommands.AddView)]
		public void AddViewUpdate (CommandInfo info)
		{
			ProjectFolder pf = (ProjectFolder)CurrentNode.DataItem;
			string rootName = System.IO.Path.Combine (pf.Project.BaseDirectory, "Views");
			info.Enabled = info.Visible = pf.Path.StartsWith (rootName)
				&& (pf.Path.Length == rootName.Length || pf.Path[rootName.Length] == System.IO.Path.DirectorySeparatorChar);
		}
		
		[CommandHandler (AspMvcCommands.AddView)]
		public void AddView ()
		{
			AspMvcProject project = CurrentNode.GetParentDataItem (typeof(AspMvcProject), true) as AspMvcProject;
			if (project == null)
				return;
			
			object currentItem = CurrentNode.DataItem;
				
			ProjectFolder folder = CurrentNode.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			string path = folder != null? folder.Path : project.BaseDirectory;
			
			AddView (project, path, null);
			
			ITreeNavigator nav = Tree.GetNodeAtObject (currentItem);
			if (nav != null)
				nav.Expanded = true;
		}
		
		public static void AddView (AspMvcProject project, string path, string name)
		{
			string outputFile = null;
			MvcTextTemplateHost host = null;
			AppDomain domain = null;
			AddViewDialog dialog = null;
			
			try {
				dialog = new AddViewDialog (project);
				dialog.ViewName = name;
				
				bool fileGood = false;
				while (!fileGood) {
					Gtk.ResponseType resp = (Gtk.ResponseType) MessageService.ShowCustomDialog (dialog);
					dialog.Hide ();
					if (resp != Gtk.ResponseType.Ok || ! dialog.IsValid ())
						return;
				
					outputFile = System.IO.Path.Combine (path, dialog.ViewName) + (dialog.IsPartialView? ".ascx" : ".aspx");
					
					if (System.IO.File.Exists (outputFile)) {
						fileGood = MessageService.AskQuestion ("Overwrite file?", "The file '{0}' already exists.\n" +
								"Would you like to overwrite it?", AlertButton.OverwriteFile, AlertButton.Cancel)
							!= AlertButton.Cancel;
					} else
						break;
				}	
				
				AppDomainSetup info = new AppDomainSetup ();
				info.ApplicationBase  = System.IO.Path.GetDirectoryName (typeof (MvcTextTemplateHost).Assembly.Location);
				domain = AppDomain.CreateDomain ("AspMvcGenerationDomain", null, info);
				host = MvcTextTemplateHost.Create (domain);
				
				if (dialog.HasMaster) {
					host.IsViewContentPage = true;
					host.ContentPlaceholder = dialog.PrimaryPlaceHolder;
					host.MasterPage = dialog.MasterFile;
					host.ContentPlaceHolders = dialog.ContentPlaceHolders;
				}
				else if (dialog.IsPartialView)
					host.IsViewUserControl = true;
				else
					host.IsViewPage = true;
				
				if (dialog.IsStronglyTyped) {
					//TODO: use dialog.ViewDataType to construct 
					// host.ViewDataTypeGenericString and host.ViewDataType
				}
				
				host.ProcessTemplate (dialog.TemplateFile, outputFile);
				MonoDevelop.TextTemplating.TextTemplatingService.ShowTemplateHostErrors (host.Errors);
				
			} finally {
				if (domain != null)
					AppDomain.Unload (domain);
				if (dialog != null)
					dialog.Destroy ();
			}
			
			if (System.IO.File.Exists (outputFile)) {
				project.AddFile (outputFile);
				MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.Save (project);
			}
		}
		
		//adapted from GtkCore
		void AddFile (string id)
		{
			AspMvcProject project = CurrentNode.GetParentDataItem (typeof(AspMvcProject), true) as AspMvcProject;
			if (project == null)
				return;
			
			object currentItem = CurrentNode.DataItem;
				
			ProjectFolder folder = CurrentNode.GetParentDataItem (typeof(ProjectFolder), true) as ProjectFolder;
			string path = folder != null? folder.Path : project.BaseDirectory;

			MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.CreateProjectFile (project, path, id);
			MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.Save (project);
			
			ITreeNavigator nav = Tree.GetNodeAtObject (currentItem);
			if (nav != null)
				nav.Expanded = true;
		}
	}
}
