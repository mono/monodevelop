//
// GuiBuilderService.cs
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
using System.IO;
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

using MonoDevelop.GtkCore.WidgetLibrary;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	class GuiBuilderService
	{
		static GuiBuilderProjectPad widgetTreePad;
		internal static Stetic.Project EmptyProject;
		static string GuiBuilderLayout = "GUI Builder";
		static string defaultLayout;
		static Stetic.Project activeProject;
	
		static ProjectReferenceEventHandler referencesChangedHandler;
		static ProjectCompileEventHandler projectCompileHandler;
		static Hashtable assemblyLibs = new Hashtable ();
		
		static GuiBuilderService ()
		{
			referencesChangedHandler = (ProjectReferenceEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new ProjectReferenceEventHandler (OnReferencesChanged));
			projectCompileHandler = (ProjectCompileEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new ProjectCompileEventHandler (OnProjectCompiled));
			
			EmptyProject = new Stetic.Project ();
			
			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new CombineEventHandler (OnOpenCombine));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCloseCombine));
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
			IdeApp.ProjectOperations.EndBuild += projectCompileHandler;
			IdeApp.ProjectOperations.ParserDatabase.AssemblyInformationChanged += (AssemblyInformationEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new AssemblyInformationEventHandler (OnAssemblyInfoChanged));
		}
		
		internal static GuiBuilderProjectPad WidgetTreePad {
			get { return widgetTreePad; }
			set { widgetTreePad = value; }
		}
		
		static void OnActiveDocumentChanged (object s, EventArgs args)
		{
			NotifyWidgetLibraryChange ();

			if (IdeApp.Workbench.ActiveDocument == null) {
				ActiveProject = EmptyProject;
				if (widgetTreePad != null)
					widgetTreePad.Fill (null);
				RestoreLayout ();
				return;
			}

			GuiBuilderView view = IdeApp.Workbench.ActiveDocument.Content as GuiBuilderView;
			if (view != null) {
				ActiveProject = view.EditSession.SteticProject;
				if (widgetTreePad != null)
					widgetTreePad.Fill (view.EditSession.SteticProject);
				SetDesignerLayout ();
			}
			else {
				ActiveProject = EmptyProject;
				if (widgetTreePad != null)
					widgetTreePad.Fill (null);
				RestoreLayout ();
			}
		}
		
		static void SetDesignerLayout ()
		{
			if (IdeApp.Workbench.CurrentLayout != GuiBuilderLayout) {
				bool exists = Array.IndexOf (IdeApp.Workbench.Layouts, GuiBuilderLayout) != -1;
				defaultLayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = GuiBuilderLayout;
				if (!exists) {
					Pad p = IdeApp.Workbench.Pads [typeof(GuiBuilderPalettePad)];
					if (p != null) p.Visible = true;
					p = IdeApp.Workbench.Pads [typeof(GuiBuilderPropertiesPad)];
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
		
		static void OnOpenCombine (object s, CombineEventArgs args)
		{
			args.Combine.ReferenceAddedToProject += referencesChangedHandler;
			args.Combine.ReferenceRemovedFromProject += referencesChangedHandler;
			
			UpdateWidgetRegistry ();
		}
		
		static void OnCloseCombine (object s, CombineEventArgs args)
		{
			args.Combine.ReferenceAddedToProject -= referencesChangedHandler;
			args.Combine.ReferenceRemovedFromProject -= referencesChangedHandler;
			
			UpdateWidgetRegistry ();
		}
		
		static void OnReferencesChanged (object sender, ProjectReferenceEventArgs e)
		{
			UpdateWidgetRegistry ();
			NotifyWidgetLibraryChange ();
			CleanUnusedAssemblyLibs ();
		}
		
		static void OnProjectCompiled (bool success)
		{
			// After compiling, discard the cached data, since it may have changed
			foreach (Project p in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects (true)) {
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (p);
				if (info != null && info.IsWidgetLibrary)
					info.ProjectWidgetLibrary.ClearCachedInfo ();
			}
			UpdateWidgetRegistry ();
		}
		
		public static void UpdateWidgetRegistry ()
		{
			ArrayList list = new ArrayList ();
			
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				foreach (Project p in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects (true)) {
					GtkDesignInfo info = GtkCoreService.GetGtkInfo (p);
					if (info != null) {
						if (info.IsWidgetLibrary)
							list.Add (info.ProjectWidgetLibrary);
						foreach (Stetic.WidgetLibrary lib in info.GetReferencedWidgetLibraries ())
							if (!list.Contains (lib))
								list.Add (lib);
					}
				}
			}
			
			foreach (Stetic.WidgetLibrary lib in list) {
				if (Stetic.Registry.IsRegistered (lib))
					Stetic.Registry.ReloadWidgetLibrary (lib);
				else
					Stetic.Registry.RegisterWidgetLibrary (lib);
			}
			
			foreach (Stetic.WidgetLibrary lib in Stetic.Registry.RegisteredWidgetLibraries)
				if (!list.Contains (lib))
					Stetic.Registry.UnregisterWidgetLibrary (lib);
		}
		
		public static AssemblyReferenceWidgetLibrary GetAssemblyLibrary (string assemblyReference)
		{
			AssemblyReferenceWidgetLibrary lib = assemblyLibs [assemblyReference] as AssemblyReferenceWidgetLibrary;
			if (lib == null) {
				string aname = IdeApp.ProjectOperations.ParserDatabase.LoadAssembly (assemblyReference);
				lib = new AssemblyReferenceWidgetLibrary (assemblyReference, aname);
				assemblyLibs [assemblyReference] = lib;
			}
			
			// We are registering here all assembly references. Not all of them are widget libraries.
			if (!lib.ExportsWidgets)
				return null;
			else
				return lib;
		}
		
		static void CleanUnusedAssemblyLibs ()
		{
			ArrayList toDelete = new ArrayList ();
			CombineEntryCollection col = IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ();
			foreach (string assemblyReference in assemblyLibs.Keys) {
				bool found = false;
				foreach (Project p in col) {
					foreach (ProjectReference pref in p.ProjectReferences) {
						if (pref.Reference == assemblyReference) {
							found = true;
							break;
						}
					}
					if (found) break;
				}
				if (!found)
					toDelete.Add (assemblyReference);
			}
			
			foreach (string name in toDelete) {
				IdeApp.ProjectOperations.ParserDatabase.UnloadAssembly (name);
				assemblyLibs.Remove (name);
			}
		}
		
		static void OnAssemblyInfoChanged (object s, AssemblyInformationEventArgs args)
		{
			// Update the widget registry if a widget library has changed.
			
			bool changed = false;
			
			foreach (AssemblyReferenceWidgetLibrary lib in assemblyLibs.Values) {
				if (lib.AssemblyName == args.AssemblyName) {
					bool oldExport = lib.ExportsWidgets;
					lib.LoadInfo ();
					if (oldExport || lib.ExportsWidgets)
						changed = true;
				}
			}
			
			if (changed)
				UpdateWidgetRegistry ();
		}

		public static Stetic.Project ActiveProject {
			get { return activeProject; }
			set {
				activeProject = value;
				if (activeProject == null)
					activeProject = EmptyProject;

				if (ActiveProjectChanged != null)
					ActiveProjectChanged (null, null);
			}
		}
		
		public static Stetic.WidgetLibrary[] ActiveWidgetLibraries {
			get {
				Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null && IdeApp.ProjectOperations.CurrentOpenCombine != null) {
					Project p = IdeApp.ProjectOperations.CurrentOpenCombine.GetProjectEntryContaining (doc.FileName);
					GtkDesignInfo info = GtkCoreService.GetGtkInfo (p);
					if (info != null)
						return info.GetReferencedWidgetLibraries ();
				}
				return new Stetic.WidgetLibrary [0];
			}
		}
		
		public static void NotifyWidgetLibraryChange ()
		{
			if (WidgetLibrariesChanged != null)
				WidgetLibrariesChanged (null, null);
		}
		
		internal static void AddCurrentWidgetToClass ()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				GuiBuilderView view = IdeApp.Workbench.ActiveDocument.Content as GuiBuilderView;
				if (view != null)
					view.AddCurrentWidgetToClass ();
			}
		}
		
		internal static void JumpToSignalHandler (Stetic.Signal signal)
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				GuiBuilderView view = IdeApp.Workbench.ActiveDocument.Content as GuiBuilderView;
				if (view != null)
					view.JumpToSignalHandler (signal);
			}
		}
		
		public static void ImportGladeFile (Project project)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null) info = GtkCoreService.EnableGtkSupport (project);
			info.GuiBuilderProject.ImportGladeFile ();
		}
		
		internal static event EventHandler ActiveProjectChanged; 
		internal static event EventHandler WidgetLibrariesChanged; 
	}
}
