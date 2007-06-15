//
// GuiBuilderProject.cs
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
using System.Xml;
using System.IO;
using System.Reflection;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderProject
	{
		ArrayList formInfos;
		Stetic.Project gproject;
		IProject project;
		string fileName;
		bool hasError;
		bool needsUpdate = true;
		
		FileSystemWatcher watcher;
		DateTime lastSaveTime;
		object fileSaveLock = new object ();
		bool disposed;
		
		public event WindowEventHandler WindowAdded;
		public event WindowEventHandler WindowRemoved;
		public event EventHandler Reloaded;
		public event EventHandler Unloaded;
		public event EventHandler Changed;

		public GuiBuilderProject (IProject project, string fileName)
		{
			this.fileName = fileName;
			this.project = project;
		}
		
		void Load ()
		{
			if (gproject != null || disposed)
				return;
			
			gproject = GuiBuilderService.SteticApp.CreateProject ();
			formInfos = new ArrayList ();
			
			if (!System.IO.File.Exists (fileName)) {
				// Regenerate the gtk-gui folder if the stetic project
				// doesn't exist.
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
				info.UpdateGtkFolder ();
			}

			try {
				gproject.Load (fileName);
			} catch (Exception ex) {
				IdeApp.Services.MessageService.ShowError (ex, GettextCatalog.GetString ("The GUI designer project file '{0}' could not be loaded.", fileName));
				hasError = true;
			}
			
			// Sync project libraries
			UpdateLibraries ();

			gproject.ResourceProvider = GtkCoreService.GetGtkInfo (project).ResourceProvider;
			gproject.ComponentAdded += new Stetic.ComponentEventHandler (OnAddWidget);
			gproject.ComponentRemoved += new Stetic.ComponentRemovedEventHandler (OnRemoveWidget);
			gproject.ActionGroupsChanged += OnGroupsChanged;
// TODO: Project Conversion 
//			project.FileRemovedFromProject += new ProjectFileEventHandler (OnFileRemoved);
//			project.ReferenceAddedToProject += OnReferenceAdded;
//			project.ReferenceRemovedFromProject += OnReferenceRemoved;
//			
			foreach (Stetic.WidgetComponent ob in gproject.GetComponents ())
				RegisterWindow (ob, false);
				
			// Monitor changes in the file
			lastSaveTime = System.IO.File.GetLastWriteTime (fileName);
			watcher = new FileSystemWatcher ();
			if (System.IO.File.Exists (fileName)) {
				watcher.Path = Path.GetDirectoryName (fileName);
				watcher.Filter = Path.GetFileName (fileName);
				watcher.Changed += (FileSystemEventHandler) IdeApp.Services.DispatchService.GuiDispatch (new FileSystemEventHandler (OnSteticFileChanged));
				watcher.EnableRaisingEvents = true;
			}
		}	
	
		void Unload ()
		{
			if (gproject == null)
				return;

			if (Unloaded != null)
				Unloaded (this, EventArgs.Empty);

			foreach (GuiBuilderWindow win in formInfos)
				win.Dispose ();

			gproject.ComponentAdded -= new Stetic.ComponentEventHandler (OnAddWidget);
			gproject.ComponentRemoved -= new Stetic.ComponentRemovedEventHandler (OnRemoveWidget);
			gproject.ActionGroupsChanged -= OnGroupsChanged;
// TODO: Project Conversion 
//			project.FileRemovedFromProject -= new ProjectFileEventHandler (OnFileRemoved);
//			project.ReferenceAddedToProject -= OnReferenceAdded;
//			project.ReferenceRemovedFromProject -= OnReferenceRemoved;
			gproject.Dispose ();
			gproject = null;
			formInfos = null;
			needsUpdate = true;
			hasError = false;
			GuiBuilderService.SteticApp.UpdateWidgetLibraries (false);
			
			watcher.Dispose ();
			watcher = null;
			NotifyChanged ();
		}
		
		void OnSteticFileChanged (object s, FileSystemEventArgs args)
		{
			lock (fileSaveLock) {
				if (lastSaveTime == System.IO.File.GetLastWriteTime (fileName))
					return;
			}
			
			if (GuiBuilderService.HasOpenDesigners (project, true)) {
				if (!IdeApp.Services.MessageService.AskQuestion (GettextCatalog.GetString ("The project '{0}' has been modified by an external application. Do you want to reload it? Unsaved changes in the open GTK designers will be lost.", project.Name)))
					return;
			}
			if (!disposed)
				Reload ();
		}
		
		public void Reload ()
		{
			if (disposed)
				return;
			Unload ();
			if (Reloaded != null)
				Reloaded (this, EventArgs.Empty);
			NotifyChanged ();
		}
		
		public bool HasError {
			get { return hasError; }
		}
		
		public bool IsEmpty {
			get {
				// If the project is not loaded, assume not empty
				return gproject != null && Windows.Count == 0; 
			}
		}
		
		public void Save (bool saveMdProject)
		{
			if (disposed)
				return;

			if (gproject != null && !hasError) {
				lock (fileSaveLock) {
					gproject.Save (fileName);
					lastSaveTime = System.IO.File.GetLastWriteTime (fileName);
				}
			}
				
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info.UpdateGtkFolder () && saveMdProject)
				ProjectService.SaveProject (project);
			GuiBuilderService.StoreConfiguration ();
		}
		
		public string File {
			get { return fileName; }
		}
		
		public Stetic.Project SteticProject {
			get {
				Load ();
				return gproject;
			}
		}
		
		public ICollection Windows {
			get {
				Load ();
				return formInfos; 
			}
		}
		
		public IProject Project {
			get { return project; }
		}
		
		public void Dispose ()
		{
			disposed = true;
			if (watcher != null)
				watcher.Dispose ();
			Unload ();
		}
		
		public bool IsActive ()
		{
			return GuiBuilderService.SteticApp.ActiveProject == SteticProject && !disposed;
		}
		
		public Stetic.WidgetComponent AddNewComponent (Stetic.ComponentType type, string name)
		{
			Stetic.WidgetComponent c = SteticProject.AddNewComponent (type, name);
			RegisterWindow (c, true);
			return c;
		}
		
		public Stetic.WidgetComponent AddNewComponent (XmlElement element)
		{
			Stetic.WidgetComponent c = SteticProject.AddNewComponent (element);
			// Register the window now, don't wait for the WidgetAdded event since
			// it may take some time, and the GuiBuilderWindow object is needed
			// just after this call
			RegisterWindow (c, true);
			return c;
		}
	
		void RegisterWindow (Stetic.WidgetComponent widget, bool notify)
		{
			foreach (GuiBuilderWindow w in formInfos)
				if (w.RootWidget == widget)
					return;

			GuiBuilderWindow win = new GuiBuilderWindow (this, gproject, widget);
			formInfos.Add (win);
			
			if (notify) {
				if (WindowAdded != null)
					WindowAdded (this, new WindowEventArgs (win));
				NotifyChanged ();
			}
		}
	
		void UnregisterWindow (GuiBuilderWindow win)
		{
			if (!formInfos.Contains (win))
				return;

			formInfos.Remove (win);

			if (WindowRemoved != null)
				WindowRemoved (this, new WindowEventArgs (win));

			win.Dispose ();
			NotifyChanged ();
		}
		
		public void Remove (GuiBuilderWindow win)
		{
			gproject.RemoveComponent (win.RootWidget);
			UnregisterWindow (win);
		}
	
		public void RemoveActionGroup (Stetic.ActionGroupComponent group)
		{
			gproject.RemoveActionGroup (group);
		}
	
		void OnAddWidget (object s, Stetic.ComponentEventArgs args)
		{
			if (!disposed)
				RegisterWindow ((Stetic.WidgetComponent)args.Component, true);
		}
		
		void OnRemoveWidget (object s, Stetic.ComponentRemovedEventArgs args)
		{
			if (disposed)
				return;
			foreach (GuiBuilderWindow form in Windows) {
				if (form.RootWidget.Name == args.ComponentName) {
					UnregisterWindow (form);
					break;
				}
			}
		}
		
//		void OnFileRemoved (object sender, ProjectFileEventArgs args)
//		{
			// Disable for now since it may have issues when moving files.
			
/*			ArrayList toDelete = new ArrayList ();

			foreach (GuiBuilderWindow win in formInfos) {
				if (win.SourceCodeFile == args.ProjectFile.Name)
					toDelete.Add (win);
			}
			
			foreach (GuiBuilderWindow win in toDelete)
				Remove (win);
*/         //}

		void OnGroupsChanged (object s, EventArgs a)
		{
			if (!disposed)
				NotifyChanged ();
		}

// TODO: Project Conversion
//		void OnReferenceAdded (object ob, ProjectReferenceEventArgs args)
//		{
//			if (disposed)
//				return;
//			string pref = GetReferenceLibraryPath (args.ProjectReference);
//			if (pref != null) {
//				gproject.AddWidgetLibrary (pref);
//				Save (false);
//			}
//		}
//		
//		void OnReferenceRemoved (object ob, ProjectReferenceEventArgs args)
//		{
//			if (disposed)
//				return;
//			string pref = GetReferenceLibraryPath (args.ProjectReference);
//			if (pref != null) {
//				gproject.RemoveWidgetLibrary (pref);
//				Save (false);
//			}
//		}

		string GetReferenceLibraryPath (ReferenceProjectItem pref)
		{
			// Assume everything is a widget library. Stetic will discard it if it is not.
			string path = Path.GetFullPath (Path.Combine (pref.Project.BasePath, pref.HintPath));
			
			if (path != null && GuiBuilderService.SteticApp.IsWidgetLibrary (path))
				return path;
			else
				return null;
		}
		string GetReferenceLibraryPath (ProjectReferenceProjectItem pref)
		{
			string path = null;
			
			MSBuildProject p = ProjectService.FindProject (pref.Name).Project as MSBuildProject;
			if (p != null) {
				GtkDesignInfo info = GtkCoreService.GetGtkInfo (p);
				if (info != null && info.IsWidgetLibrary)
					path = p.AssemblyName;
			}
			
			if (path != null && GuiBuilderService.SteticApp.IsWidgetLibrary (path))
				return path;
			else
				return null;
		}
		
		public void ImportGladeFile ()
		{
			Gtk.FileChooserDialog dialog =
				new Gtk.FileChooserDialog ("Open Glade File", null, Gtk.FileChooserAction.Open,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Open, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok) {
				SteticProject.ImportGlade (dialog.Filename);
				Save (true);
			}
			dialog.Destroy ();
		}
		
		public GuiBuilderWindow GetWindowForWidget (Stetic.Component w)
		{
			foreach (GuiBuilderWindow form in Windows) {
				if (form.RootWidget.Name == w.Name)
					return form;
			}
			return null;
		}
		
		public GuiBuilderWindow GetWindowForClass (string className)
		{
			foreach (GuiBuilderWindow form in Windows) {
				if (CodeBinder.GetObjectName (form.RootWidget) == className)
					return form;
			}
			return null;
		}
		
		public GuiBuilderWindow GetWindowForFile (string fileName)
		{
			foreach (GuiBuilderWindow win in Windows) {
				if (fileName == win.SourceCodeFile)
					return win;
			}
			return null;
		}
		
		public GuiBuilderWindow GetWindow (string name)
		{
			foreach (GuiBuilderWindow win in Windows) {
				if (name == win.Name)
					return win;
			}
			return null;
		}
		
		public Stetic.ActionGroupComponent GetActionGroupForFile (string fileName)
		{
			foreach (Stetic.ActionGroupComponent group in SteticProject.GetActionGroups ()) {
				if (fileName == GetSourceCodeFile (group))
					return group;
			}
			return null;
		}
		
		public Stetic.ActionGroupComponent GetActionGroup (string name)
		{
			foreach (Stetic.ActionGroupComponent group in SteticProject.GetActionGroups ()) {
				if (name == group.Name)
					return group;
			}
			return null;
		}
		
		public string GetSourceCodeFile (Stetic.Component obj)
		{
			IClass cls = GetClass (obj);
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			
			if (cls != null) {
				// Ignore partial classes located at the gtk-gui folder
				foreach (IClass pc in cls.Parts) {
					if (!pc.Region.FileName.StartsWith (info.GtkGuiFolder)) {
						return pc.Region.FileName;
					}
				}
			}
			return null;
		}
		
		IClass GetClass (Stetic.Component obj)
		{
			string name = CodeBinder.GetClassName (obj);
			return FindClass (name);
		}
		
		public IClass FindClass (string className)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			IParserContext ctx = GetParserContext ();
			IClass[] classes = ctx.GetProjectContents ();
			foreach (IClass cls in classes) {
				if (cls.FullyQualifiedName == className) {
					// Return this class only if it is declared outside the gtk-gui
					// folder. Generated partial classes will be ignored.
					foreach (IClass part in cls.Parts) {
						if (!part.Region.FileName.StartsWith (info.GtkGuiFolder))
							return part;
					}
				}
			}
			return null;
		}
		
		public IParserContext GetParserContext ()
		{
// TODO: Project Conversion
//			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (Project);
//			if (needsUpdate) {
//				needsUpdate = false;
//				ctx.UpdateDatabase ();
//			}
//			return ctx;
			return null;
		}
		
		public void UpdateLibraries ()
		{
			if (hasError || disposed || gproject == null)
				return;

			string[] oldLibs = gproject.WidgetLibraries;
			
			ArrayList libs = new ArrayList ();
			
			foreach (ProjectItem item in project.Items) {
				string wref = null;
				if (item is ProjectReferenceProjectItem) {
					wref = GetReferenceLibraryPath (item as ProjectReferenceProjectItem);
				} else if (item is ReferenceProjectItem) {
					wref = GetReferenceLibraryPath (item as ReferenceProjectItem);
				} 
				if (wref != null)
					libs.Add (wref);
			}
			
			// If the project is a library, add itself as a widget source
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info != null && info.IsWidgetLibrary)
				libs.Add (ProjectService.GetOutputFileName (project));

			string[] newLibs = (string[]) libs.ToArray (typeof(string));
			
			// See if something has changed
			if (oldLibs.Length == newLibs.Length) {
				bool found = false;
				foreach (string s in newLibs) {
					if (!((IList)oldLibs).Contains (s)) {
						found = true;
						break;
					}
				}
				if (!found)	// Arrays are the same
					return;
			}
			gproject.WidgetLibraries = newLibs;
			Save (true);
		}
		
		void NotifyChanged ()
		{
			if (Changed != null && !disposed)
				Changed (this, EventArgs.Empty);
		}
	}
	
	public delegate void WindowEventHandler (object s, WindowEventArgs args);
	
	public class WindowEventArgs: EventArgs
	{
		GuiBuilderWindow win;
		
		public WindowEventArgs (GuiBuilderWindow win)
		{
			this.win = win;
		}
		
		public GuiBuilderWindow Window {
			get { return win; }
		}
	}
}
