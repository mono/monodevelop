//
// ProjectNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual, Krzysztof Marecki
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// Copyright (C) 2010 Krzysztof Marecki
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
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.GtkCore.Dialogs;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public class ProjectNodeBuilder: NodeBuilderExtension
	{
		static ProjectNodeBuilder instance;

		public override bool CanBuildNode (Type dataType)
		{
			return typeof(DotNetProject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get {
				return typeof (UserInterfaceCommandHandler);
			}
		}
		
		protected override void Initialize ()
		{
			lock (typeof (ProjectNodeBuilder))
				instance = this;
		}
		
		public override void Dispose ()
		{
			lock (typeof (ProjectNodeBuilder))
				instance = null;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Project project = dataObject as Project;
			
			if (project is DotNetProject) {
				GtkDesignInfo info = GtkDesignInfo.FromProject (project);
				
				if (info.NeedsConversion) {
					ProjectConversionDialog dialog = new ProjectConversionDialog (project, info.SteticFolderName);
					
					try
					{
						if (dialog.Run () == (int)ResponseType.Yes) {
							info.GuiBuilderProject.Convert (dialog.GuiFolderName, dialog.MakeBackup);
							IdeApp.ProjectOperations.Save (project);
						}
					} finally {
						dialog.Destroy ();
					}
				}
				
				project.FileAddedToProject += HandleProjectFileAddedToProject;
			}
		}
		
		void HandleProjectFileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			Project project = e.Project;
			ProjectFile pf = e.ProjectFile;
			string fileName = pf.FilePath.FullPath.ToString ();
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			
			string buildFile = info.GetBuildFileFromComponent (fileName);
			if (!project.IsFileInProject(buildFile) && File.Exists (buildFile)) {
				ProjectFile pf2 = project.AddFile (buildFile, BuildAction.Compile);
				pf2.DependsOn = pf.FilePath.FileName;
			}
			
			string gtkxFile = info.GetDesignerFileFromComponent (fileName);
			if (!project.IsFileInProject(gtkxFile) && File.Exists (gtkxFile)) {
				ProjectFile pf3 = project.AddFile (gtkxFile, BuildAction.EmbeddedResource);
				pf3.DependsOn = pf.FilePath.FileName;
			}
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project)dataObject;
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			
			if (GtkDesignInfo.HasDesignedObjects (project)) {
				GuiProjectFolder folder = new GuiProjectFolder(info.SteticFolder.FullPath, project, null);
				builder.AddChild (folder);
			}
		}
		
		public static void OnSupportChanged (Project p)
		{
			if (instance == null)
				return;

			ITreeBuilder tb = instance.Context.GetTreeBuilder (p);
			if (tb != null)
				tb.UpdateAll ();
		}
	}
}
