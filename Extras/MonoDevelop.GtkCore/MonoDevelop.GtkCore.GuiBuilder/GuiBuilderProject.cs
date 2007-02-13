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
using MonoDevelop.Projects;
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
		Project project;
		string fileName;
		internal bool UpdatingWindow;
		bool hasError;
		bool needsUpdate = true;
		
		public event WindowEventHandler WindowAdded;
		public event WindowEventHandler WindowRemoved;
		public event EventHandler Reloaded;
		public event EventHandler Disposed;
	
		public GuiBuilderProject (Project project, string fileName)
		{
			this.fileName = fileName;
			this.project = project;
		}
		
		void Load ()
		{
			if (gproject != null)
				return;
			
			gproject = GuiBuilderService.SteticApp.CreateProject ();
			formInfos = new ArrayList ();

			try {
				gproject.Load (fileName);
			} catch (Exception ex) {
				IdeApp.Services.MessageService.ShowError (ex, "The GUI designer project file '" + fileName + "' could not be loaded.");
				hasError = true;
			}
			
			// Sync project libraries
			UpdateLibraries ();

			gproject.ResourceProvider = GtkCoreService.GetGtkInfo (project).ResourceProvider;
			gproject.ComponentAdded += new Stetic.ComponentEventHandler (OnAddWidget);
			gproject.ComponentRemoved += new Stetic.ComponentRemovedEventHandler (OnRemoveWidget);
			project.FileRemovedFromProject += new ProjectFileEventHandler (OnFileRemoved);
			project.ReferenceAddedToProject += OnReferenceAdded;
			project.ReferenceRemovedFromProject += OnReferenceRemoved;
			
			foreach (Stetic.WidgetComponent ob in gproject.GetComponents ())
				RegisterWindow (ob);
		}
		
		void Unload ()
		{
			if (gproject == null)
				return;

			foreach (GuiBuilderWindow win in formInfos)
				win.Dispose ();

			gproject.ComponentAdded -= new Stetic.ComponentEventHandler (OnAddWidget);
			gproject.ComponentRemoved -= new Stetic.ComponentRemovedEventHandler (OnRemoveWidget);
			project.FileRemovedFromProject -= new ProjectFileEventHandler (OnFileRemoved);
			project.ReferenceAddedToProject -= OnReferenceAdded;
			project.ReferenceRemovedFromProject -= OnReferenceRemoved;
			gproject.Dispose ();
			gproject = null;
			formInfos = null;
			needsUpdate = true;
			hasError = false;
			GuiBuilderService.SteticApp.UpdateWidgetLibraries (false);
		}
		
		public void Reload ()
		{
			Unload ();
			if (Reloaded != null)
				Reloaded (this, EventArgs.Empty);
		}
		
		public bool IsEmpty {
			get {
				// If the project is not loaded, assume not empty
				return gproject == null || Windows.Count == 0; 
			}
		}
		
		public void Save (bool saveMdProject)
		{
			if (gproject != null && !hasError)
				gproject.Save (fileName);
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info.UpdateGtkFolder () && saveMdProject)
				IdeApp.ProjectOperations.SaveProject (project);
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
		
		public Project Project {
			get { return project; }
		}
		
		public void Dispose ()
		{
			if (Disposed != null)
				Disposed (this, EventArgs.Empty);
			Unload ();
		}
		
		public bool IsActive ()
		{
			return GuiBuilderService.SteticApp.ActiveProject == SteticProject;
		}
		
		public Stetic.WidgetComponent AddNewComponent (Stetic.ComponentType type, string name)
		{
			Stetic.WidgetComponent c = SteticProject.AddNewComponent (type, name);
			RegisterWindow (c);
			return c;
		}
		
		public Stetic.WidgetComponent AddNewComponent (XmlElement element)
		{
			Stetic.WidgetComponent c = SteticProject.AddNewComponent (element);
			// Register the window now, don't wait for the WidgetAdded event since
			// it may take some time, and the GuiBuilderWindow object is needed
			// just after this call
			RegisterWindow (c);
			return c;
		}
	
		void RegisterWindow (Stetic.WidgetComponent widget)
		{
			foreach (GuiBuilderWindow w in formInfos)
				if (w.RootWidget == widget)
					return;

			GuiBuilderWindow win = new GuiBuilderWindow (this, gproject, widget);
			formInfos.Add (win);
			
			if (WindowAdded != null)
				WindowAdded (this, new WindowEventArgs (win));
		}
	
		void UnregisterWindow (GuiBuilderWindow win)
		{
			if (!formInfos.Contains (win))
				return;

			formInfos.Remove (win);

			if (WindowRemoved != null)
				WindowRemoved (this, new WindowEventArgs (win));

			win.Dispose ();
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
			if (UpdatingWindow)
				return;
			RegisterWindow ((Stetic.WidgetComponent)args.Component);
		}
		
		void OnRemoveWidget (object s, Stetic.ComponentRemovedEventArgs args)
		{
			if (UpdatingWindow)
				return;
			
			foreach (GuiBuilderWindow form in Windows) {
				if (form.RootWidget.Name == args.ComponentName) {
					UnregisterWindow (form);
					break;
				}
			}
		}
		
		void OnFileRemoved (object sender, ProjectFileEventArgs args)
		{
			// Disable for now since it may have issues when moving files.
			
/*			ArrayList toDelete = new ArrayList ();

			foreach (GuiBuilderWindow win in formInfos) {
				if (win.SourceCodeFile == args.ProjectFile.Name)
					toDelete.Add (win);
			}
			
			foreach (GuiBuilderWindow win in toDelete)
				Remove (win);
*/		}

		void OnReferenceAdded (object ob, ProjectReferenceEventArgs args)
		{
			string pref = GetReferenceLibraryPath (args.ProjectReference);
			if (pref != null) {
				gproject.AddWidgetLibrary (pref);
				Save (false);
			}
		}
		
		void OnReferenceRemoved (object ob, ProjectReferenceEventArgs args)
		{
			string pref = GetReferenceLibraryPath (args.ProjectReference);
			if (pref != null) {
				gproject.RemoveWidgetLibrary (pref);
				Save (false);
			}
		}

		string GetReferenceLibraryPath (ProjectReference pref)
		{
			string path = null;
			
			if (pref.ReferenceType == ReferenceType.Project) {
				DotNetProject p = project.RootCombine.FindProject (pref.Reference) as DotNetProject;
				if (p != null) {
					GtkDesignInfo info = GtkCoreService.GetGtkInfo (p);
					if (info != null && info.IsWidgetLibrary)
						path = p.GetOutputFileName ();
				}
			} else if (pref.ReferenceType == ReferenceType.Gac || 
					pref.ReferenceType == ReferenceType.Assembly) {
				
				// Assume everything is a widget library. Stetic will discard it if it is not.
				path = pref.Reference;
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
		
		public Stetic.ActionGroupComponent GetActionGroupForFile (string fileName)
		{
			foreach (Stetic.ActionGroupComponent group in SteticProject.GetActionGroups ()) {
				if (fileName == GetSourceCodeFile (group))
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
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (Project);
			if (needsUpdate) {
				needsUpdate = false;
				ctx.UpdateDatabase ();
			}
			return ctx;
		}
		
		public void UpdateLibraries ()
		{
			string[] oldLibs = gproject.WidgetLibraries;
			
			ArrayList libs = new ArrayList ();
			
			foreach (ProjectReference pref in project.ProjectReferences) {
				string wref = GetReferenceLibraryPath (pref);
				if (wref != null)
					libs.Add (wref);
			}
			
			// If the project is a library, add itself as a widget source
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info != null && info.IsWidgetLibrary)
				libs.Add (project.GetOutputFileName ());

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
