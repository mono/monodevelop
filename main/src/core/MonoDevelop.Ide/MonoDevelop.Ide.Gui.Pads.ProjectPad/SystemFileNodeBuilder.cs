//
// SystemFileNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class SystemFileNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(SystemFile); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SystemFileNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return Path.GetFileName (((SystemFile)dataObject).Name);
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SystemFile file = (SystemFile) dataObject;
			label = file.Name;
			
			icon = DesktopService.GetPixbufForFile (file.Path, Gtk.IconSize.Menu);
			
			if (file.ShowTransparent) {
				Gdk.Pixbuf gicon = Context.GetComposedIcon (icon, "fade");
				if (gicon == null) {
					gicon = ImageService.MakeTransparent (icon, 0.5);
					Context.CacheComposedIcon (icon, "fade", gicon);
				}
				icon = gicon;
				label = "<span foreground='dimgrey'>" + label + "</span>";
			}
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectFolder)
				return 1;
			else
				return DefaultSort;
		}
	}
	
	public class SystemFileNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			string oldname = file.Path;

			string newname = Path.Combine (Path.GetDirectoryName (oldname), newName);
			if (newname != oldname) {
				try {
					if (!FileService.IsValidPath (newname)) {
						MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
					} else if (File.Exists (newname) || Directory.Exists (newname)) {
						MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
					} else {
						FileService.RenameFile (oldname, newname);
					}
				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
				} catch (System.IO.IOException ex) { 
					MessageService.ShowException (ex, GettextCatalog.GetString ("There was an error renaming the file."));
				}
			}
		}
		
		public override void ActivateItem ()
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			IdeApp.Workbench.OpenDocument (file.Path);
		}
		
		public override void DeleteItem ()
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			
			bool yes = MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to permanently delete the file {0}?", file.Path), AlertButton.Delete);
			if (!yes) return;

			try {
				FileService.DeleteFile (file.Path);
			} catch {
				MessageService.ShowError (GettextCatalog.GetString ("The file {0} could not be deleted", file.Path));
			}
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
		
		[CommandHandler (ProjectCommands.IncludeToProject)]
		[AllowMultiSelection]
		public void IncludeFileToProject ()
		{
			Set<SolutionEntityItem> projects = new Set<SolutionEntityItem> ();
			Set<Solution> solutions = new Set<Solution> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				SystemFile file = (SystemFile) node.DataItem;
				Project project = node.GetParentDataItem (typeof(Project), true) as Project;
				if (project != null) {
					project.AddFile (file.Path);
					projects.Add (project);
				}
				else {
					SolutionFolder folder = node.GetParentDataItem (typeof(SolutionFolder), true) as SolutionFolder;
					if (folder != null) {
						folder.Files.Add (file.Path);
						solutions.Add (folder.ParentSolution);
					}
					else {
						Solution sol = node.GetParentDataItem (typeof(Solution), true) as Solution;
						sol.RootFolder.Files.Add (file.Path);
						solutions.Add (sol);
					}
				}
			}
			IdeApp.ProjectOperations.Save (projects);
			foreach (Solution sol in solutions)
				IdeApp.ProjectOperations.Save (sol);
		}
		
		[CommandUpdateHandler (ProjectCommands.IncludeToProject)]
		public void UpdateIncludeFileToProject (CommandInfo info)
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			if (project != null)
				return;
			if (CurrentNode.GetParentDataItem (typeof(Solution), true) != null) {
				info.Text = GettextCatalog.GetString ("Include to Solution");
				return;
			}
			info.Visible = false;
		}
		
		[CommandHandler (ViewCommands.OpenWithList)]
		public void OnOpenWith (object ob)
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			((FileViewer)ob).OpenFile (file.Path);
		}
		
		[CommandUpdateHandler (ViewCommands.OpenWithList)]
		public void OnOpenWithUpdate (CommandArrayInfo info)
		{
			ProjectFileNodeCommandHandler.PopulateOpenWithViewers (info, ((SystemFile) CurrentNode.DataItem).Path);
		}
	}
}
