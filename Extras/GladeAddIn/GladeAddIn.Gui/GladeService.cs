//
// GladeService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Reflection;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;

namespace GladeAddIn.Gui
{
	class GladeService
	{
		static Gladeui.App gladeApp;
		static Hashtable projects = new Hashtable ();
		static ProjectFileEventHandler fileAddedHandler;
		static CombineEntryEventHandler entryRemovedHander;
		static GladeWidgetTreePad widgetTreePad;
		internal static Gladeui.Project EmptyProject;
		static string GuiBuilderLayout = "GUI Builder";
		static string defaultLayout;
	
		static GladeService ()
		{
			gladeApp = new Gladeui.App ();
			Gladeui.App.SetDefaultApp (gladeApp);
			gladeApp.Window = IdeApp.Workbench.RootWindow;
			gladeApp.TransientParent = IdeApp.Workbench.RootWindow;
			
			fileAddedHandler = (ProjectFileEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new ProjectFileEventHandler (OnFileAdded));
			entryRemovedHander = (CombineEntryEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new CombineEntryEventHandler (OnEntryRemoved));
			
			EmptyProject = new Gladeui.Project (true);
			gladeApp.AddProject (EmptyProject);
			gladeApp.Project = EmptyProject;
			
			IdeApp.ProjectOperations.CombineOpened += new CombineEventHandler (OnOpenCombine);
			IdeApp.ProjectOperations.CombineClosed += new CombineEventHandler (OnCloseCombine);
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
		}
		
		internal static GladeWidgetTreePad WidgetTreePad {
			get { return widgetTreePad; }
			set { widgetTreePad = value; }
		}
		
		static void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				gladeApp.Project = EmptyProject;
				if (widgetTreePad != null)
					widgetTreePad.Fill (null);
//				RestoreLayout ();
				return;
			}

			GuiBuilderView view = IdeApp.Workbench.ActiveDocument.Content as GuiBuilderView;
			if (view != null) {
				gladeApp.Project = view.EditSession.GladeProject;
				if (widgetTreePad != null)
					widgetTreePad.Fill (view.EditSession.GladeWidget);
//				SetDesignerLayout ();
			}
			else {
				gladeApp.Project = EmptyProject;
				if (widgetTreePad != null)
					widgetTreePad.Fill (null);
//				RestoreLayout ();
			}
		}
		
		static void SetDesignerLayout ()
		{
			if (IdeApp.Workbench.CurrentLayout != GuiBuilderLayout) {
				bool exists = Array.IndexOf (IdeApp.Workbench.Layouts, GuiBuilderLayout) == -1;
				defaultLayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = GuiBuilderLayout;
				if (!exists) {
					Pad p = IdeApp.Workbench.Pads [typeof(GladePalettePad)];
					if (p != null) p.Visible = true;
					p = IdeApp.Workbench.Pads [typeof(GladeWidgetTreePad)];
					if (p != null) p.Visible = true;
					p = IdeApp.Workbench.Pads [typeof(GladePropertiesPad)];
					if (p != null) p.Visible = true;
				}
			}
		}
		
		static void RestoreLayout ()
		{
			if (defaultLayout != null) {
				IdeApp.Workbench.CurrentLayout = defaultLayout;
				defaultLayout = null;
			}
		}
		
		public static GuiBuilderProject[] GetGuiBuilderProjects (Project project)
		{
			ArrayList list = (ArrayList) projects [project];
			if (list == null) {
				list = new ArrayList ();
				projects [project] = list;
				
				foreach (ProjectFile file in project.ProjectFiles) {
					RegisterGuiBuilderProject (project, file.Name);
				}
				gladeApp.Project = EmptyProject;
			}
			return (GuiBuilderProject[]) list.ToArray (typeof(GuiBuilderProject));
		}
		
		static GuiBuilderProject RegisterGuiBuilderProject (Project project, string fileName)
		{
			if (!fileName.EndsWith (".glade"))
				return null;

			ArrayList list = (ArrayList) projects [project];
			if (list == null)
				return null;
			
			GuiBuilderProject fp = new GuiBuilderProject (gladeApp, project, fileName);
			list.Add (fp);
			return fp;
		}
		
		static void OnOpenCombine (object s, CombineEventArgs args)
		{
			args.Combine.EntryRemoved += entryRemovedHander;
			args.Combine.FileAddedToProject += fileAddedHandler;
		}
		
		static void OnCloseCombine (object s, CombineEventArgs args)
		{
			args.Combine.EntryRemoved -= entryRemovedHander;
			args.Combine.FileAddedToProject -= fileAddedHandler;
			CloseEntry (args.Combine);
		}
		
		static void OnEntryRemoved (object s, CombineEntryEventArgs args)
		{
			CloseEntry (args.CombineEntry);
		}
		
		static void CloseEntry (CombineEntry entry)
		{
			if (entry is Project) {
				Project project = (Project) entry;
				ArrayList list = (ArrayList) projects [project];
				if (list != null) {
					foreach (GuiBuilderProject gproject in list) {
						gladeApp.RemoveProject (gproject.GladeProject);
						gproject.Dispose ();
					}
					projects.Remove (project);
				}
			} else if (entry is Combine) {
				foreach (CombineEntry e in ((Combine)entry).Entries)
					CloseEntry (e);
			}
		}
		
		static void OnFileAdded (object ob, ProjectFileEventArgs args)
		{
			// If it's a new glade file, register it
			RegisterGuiBuilderProject (args.Project, args.ProjectFile.Name);
		}
		
		internal static void SetActiveProject (Gladeui.Project gproject)
		{
			gladeApp.Project = gproject;
		}
		
		internal static void AddCurrentWidgetToClass ()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				GuiBuilderView view = IdeApp.Workbench.ActiveDocument.Content as GuiBuilderView;
				if (view != null)
					view.AddCurrentWidgetToClass ();
			}
		}
		
		public static Gladeui.App App {
			get { return gladeApp; }
		}
	}
}
