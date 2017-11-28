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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderProject
	{
		internal object MemoryProbe = Counters.GuiProjectsInMemory.CreateMemoryProbe ();
		
		List<GuiBuilderWindow> formInfos;
		Stetic.Project gproject;
		DotNetProject project;
		string fileName;
		bool hasError;
		bool needsUpdate = true;
		
		FileSystemWatcher watcher;
		DateTime lastSaveTime;
		object fileSaveLock = new object ();
		bool disposed;
		bool librariesUpdated;
		
		public event WindowEventHandler WindowAdded;
		public event WindowEventHandler WindowRemoved;
		public event EventHandler Reloaded;
		public event EventHandler Unloaded;
		public event EventHandler Changed;

		public GuiBuilderProject (DotNetProject project, string fileName)
		{
			this.fileName = fileName;
			this.project = project;
			Counters.GuiProjectsLoaded++;
			GuiBuilderService.NotifyGuiProjectLoaded ();
		}
		
		void Load ()
		{
			if (gproject != null || disposed || fileName == null)
				return;
			
			gproject = GuiBuilderService.SteticApp.CreateProject ();
			formInfos = new List<GuiBuilderWindow> ();
			
			if (!System.IO.File.Exists (fileName)) {
				// Regenerate the gtk-gui folder if the stetic project
				// doesn't exist.
				GtkDesignInfo.FromProject (project).UpdateGtkFolder ();
			}

			try {
				gproject.Load (fileName);
			} catch (Exception ex) {
				MessageService.ShowError (GettextCatalog.GetString ("The GUI designer project file '{0}' could not be loaded.", fileName), ex);
				hasError = true;
			}

			Counters.SteticProjectsLoaded++;
			gproject.ResourceProvider = GtkDesignInfo.FromProject (project).ResourceProvider;
			gproject.WidgetAdded += OnAddWidget;
			gproject.WidgetRemoved += OnRemoveWidget;
			gproject.ActionGroupsChanged += OnGroupsChanged;
			project.FileAddedToProject += OnFileAdded;
			project.FileRemovedFromProject += OnFileRemoved;
			project.ReferenceAddedToProject += OnReferenceAdded;
			project.ReferenceRemovedFromProject += OnReferenceRemoved;
			
			foreach (Stetic.WidgetInfo ob in gproject.Widgets)
				RegisterWindow (ob, false);
				
			// Monitor changes in the file
			lastSaveTime = System.IO.File.GetLastWriteTime (fileName);
			watcher = new FileSystemWatcher ();
			if (System.IO.File.Exists (fileName)) {
				watcher.Path = Path.GetDirectoryName (fileName);
				watcher.Filter = Path.GetFileName (fileName);
				watcher.Changed += OnSteticFileChanged;
				watcher.EnableRaisingEvents = true;
			}
		}	
	
		void Unload ()
		{
			if (gproject == null)
				return;

			Counters.SteticProjectsLoaded--;
			
			if (Unloaded != null)
				Unloaded (this, EventArgs.Empty);
			if (formInfos != null) {
				foreach (GuiBuilderWindow win in formInfos)
					win.Dispose ();
				formInfos = null;
			}
			if (gproject != null) {
				gproject.WidgetAdded -= OnAddWidget;
				gproject.WidgetRemoved -= OnRemoveWidget;
				gproject.ActionGroupsChanged -= OnGroupsChanged;
				gproject.Dispose ();
				gproject = null;
			}
			if (project != null) {
				project.FileAddedToProject -= OnFileAdded;
				project.FileRemovedFromProject -= OnFileRemoved;
				project.ReferenceAddedToProject -= OnReferenceAdded;
				project.ReferenceRemovedFromProject -= OnReferenceRemoved;
			}
			needsUpdate = true;
			hasError = false;
			librariesUpdated = false;
			if (watcher != null) {
				watcher.Dispose ();
				watcher = null;
			}
			NotifyChanged ();
		}
		
		void OnSteticFileChanged (object s, FileSystemEventArgs args)
		{
			Runtime.RunInMainThread (() => {
				lock (fileSaveLock) {
					if (lastSaveTime == System.IO.File.GetLastWriteTime (fileName))
						return;
				}

				if (GuiBuilderService.HasOpenDesigners (project, true)) {
					if (MessageService.AskQuestion (GettextCatalog.GetString ("The project '{0}' has been modified by an external application. Do you want to reload it?", project.Name), GettextCatalog.GetString ("Unsaved changes in the open GTK designers will be lost."), AlertButton.Cancel, AlertButton.Reload) != AlertButton.Reload)
						return;
				}
				if (!disposed)
					Reload ();
			});
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
				return gproject != null && Windows != null && Windows.Count == 0; 
			}
		}
		
		public void SaveAll (bool saveMdProject)
		{
			if (gproject != null)
				gproject.SetAllWidgetsModified ();
			SaveProject (saveMdProject);
		}

		public void SaveWindow (bool saveMdProject, string modifiedWindow)
		{
			if (gproject != null)
				gproject.SetWidgetModified (modifiedWindow);
			SaveProject (saveMdProject);
		}

		public void SaveProject (bool saveMdProject)
		{
			if (disposed)
				return;

			if (gproject != null && !hasError) {
				lock (fileSaveLock) {
					gproject.Save (fileName);
					lastSaveTime = System.IO.File.GetLastWriteTime (fileName);
				}
			}
				
			if (GtkDesignInfo.FromProject (project).UpdateGtkFolder () && saveMdProject)
				IdeApp.ProjectOperations.SaveAsync (project);
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
		
		public ICollection<GuiBuilderWindow> Windows {
			get {
				Load ();
				return formInfos; 
			}
		}
		
		public DotNetProject Project {
			get { return project; }
		}
		
		public void Dispose ()
		{
			if (disposed)
				return;
			GuiBuilderService.NotifyGuiProjectUnloaded ();
			Counters.GuiProjectsLoaded--;
			disposed = true;
			if (watcher != null)
				watcher.Dispose ();
			Unload ();
		}
		
		public Stetic.WidgetInfo AddNewComponent (Stetic.ComponentType type, string name)
		{
			Stetic.WidgetInfo c = SteticProject.AddNewComponent (type, name);
			RegisterWindow (c, true);
			return c;
		}
		
		public Stetic.WidgetInfo AddNewComponent (XmlElement element)
		{
			Stetic.WidgetInfo c = SteticProject.AddNewComponent (element);
			// Register the window now, don't wait for the WidgetAdded event since
			// it may take some time, and the GuiBuilderWindow object is needed
			// just after this call
			RegisterWindow (c, true);
			return c;
		}
	
		void RegisterWindow (Stetic.WidgetInfo widget, bool notify)
		{
			if (formInfos != null) {
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
	
		public void RemoveActionGroup (Stetic.ActionGroupInfo group)
		{
			gproject.RemoveActionGroup (group);
		}
	
		void OnAddWidget (object s, Stetic.WidgetInfoEventArgs args)
		{
			if (!disposed)
				RegisterWindow (args.WidgetInfo, true);
		}
		
		void OnRemoveWidget (object s, Stetic.WidgetInfoEventArgs args)
		{
			if (disposed || Windows == null)
				return;
			foreach (GuiBuilderWindow form in Windows) {
				if (form.RootWidget.Name == args.WidgetInfo.Name) {
					UnregisterWindow (form);
					break;
				}
			}
		}
		
		void OnFileAdded (object sender, ProjectFileEventArgs e)
		{
			foreach (ProjectFileEventInfo args in e) {
				var docId = TypeSystemService.GetDocumentId (args.Project, args.ProjectFile.Name);
				if (docId == null)
					continue;
				var doc = TypeSystemService.GetCodeAnalysisDocument (docId);
				if (doc == null)
					continue;
	
				string dir = Path.Combine (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "stetic"), "deleted-designs");
				if (!Directory.Exists (dir) || !Directory.EnumerateFiles (dir).Any ())
					continue;
				var semanticModel = doc.GetSemanticModelAsync ().Result;
				if (semanticModel == null)
					continue;

				foreach (var classDeclaration in semanticModel.SyntaxTree.GetRoot ().DescendantNodesAndSelf (child => !(child is BaseTypeDeclarationSyntax)).OfType<ClassDeclarationSyntax> ()) {
					var c = semanticModel.GetDeclaredSymbol (classDeclaration);
					string path = Path.Combine (dir, c.ToDisplayString (Microsoft.CodeAnalysis.SymbolDisplayFormat.CSharpErrorMessageFormat) + ".xml");
					if (!System.IO.File.Exists (path))
						continue;
					XmlDocument xmldoc = new XmlDocument ();
					xmldoc.Load (path);
					AddNewComponent (xmldoc.DocumentElement);
					System.IO.File.Delete (path);
				}
			}
		}

		void OnFileRemoved (object sender, ProjectFileEventArgs e)
		{
			ArrayList toDelete = new ArrayList ();

			foreach (ProjectFileEventInfo args in e) {

				var docId = TypeSystemService.GetDocumentId (args.Project, args.ProjectFile.Name);
				if (docId == null)
					continue;
				var doc = TypeSystemService.GetCodeAnalysisDocument (docId);
				if (doc == null)
					continue;
				var semanticModel = doc.GetSemanticModelAsync ().Result;
				if (semanticModel == null)
					continue;

	
				foreach (var classDeclaration in semanticModel.SyntaxTree.GetRoot ().DescendantNodesAndSelf (child => !(child is BaseTypeDeclarationSyntax)).OfType<ClassDeclarationSyntax> ()) {
					var c = semanticModel.GetDeclaredSymbol (classDeclaration);
					GuiBuilderWindow win = GetWindowForClass (c.ToDisplayString (Microsoft.CodeAnalysis.SymbolDisplayFormat.MinimallyQualifiedFormat));
					if (win != null)
						toDelete.Add (win);
				}
			}
			
			foreach (GuiBuilderWindow win in toDelete)
				Remove (win);
		}

		void OnGroupsChanged (object s, EventArgs a)
		{
			if (!disposed)
				NotifyChanged ();
		}

		void OnReferenceAdded (object ob, ProjectReferenceEventArgs args)
		{
			if (disposed || !librariesUpdated)
				return;
			string pref = GetReferenceLibraryPath (args.ProjectReference);
			if (pref != null) {
				gproject.AddWidgetLibrary (pref);
				SaveProject (false);
			}
		}
		
		void OnReferenceRemoved (object ob, ProjectReferenceEventArgs args)
		{
			if (disposed || !librariesUpdated)
				return;
			string pref = GetReferenceLibraryPath (args.ProjectReference);
			if (pref != null) {
				gproject.RemoveWidgetLibrary (pref);
				SaveProject (false);
			}
		}

		string GetReferenceLibraryPath (MonoDevelop.Projects.ProjectReference pref)
		{
			string path = null;
			
			if (pref.ReferenceType == ReferenceType.Project) {
				DotNetProject p = pref.ResolveProject (project.ParentSolution) as DotNetProject;
				if (p != null)
					path = p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
			} else if (pref.ReferenceType == ReferenceType.Assembly) {
				path = pref.HintPath;
			} else if (pref.ReferenceType == ReferenceType.Package) {
				path = pref.Reference;
			}
			if (path != null && GuiBuilderService.SteticApp.IsWidgetLibrary (path))
				return path;
			else
				return null;
		}
		
		public void ImportGladeFile ()
		{
			var dlg = new MonoDevelop.Components.SelectFileDialog (GettextCatalog.GetString ("Open Glade File"));
			dlg.AddFilter (GettextCatalog.GetString ("Glade files"), "*.glade");
			dlg.AddAllFilesFilter ();
			if (dlg.Run ()) {
				SteticProject.ImportGlade (dlg.SelectedFile);
				SaveAll (true);
			}
		}
		
		public GuiBuilderWindow GetWindowForClass (string className)
		{
			if (Windows != null) {
				foreach (GuiBuilderWindow form in Windows) {
					if (CodeBinder.GetObjectName (form.RootWidget) == className)
						return form;
				}
			}
			return null;
		}

		public GuiBuilderWindow GetWindowForFile (FilePath fileName)
		{
			if (Windows != null) {
				foreach (GuiBuilderWindow win in Windows) {
					if (fileName == win.SourceCodeFile)
						return win;
				}
			}
			return null;
		}
		
		public GuiBuilderWindow GetWindow (string name)
		{
			if (Windows != null) {
				foreach (GuiBuilderWindow win in Windows) {
					if (name == win.Name)
						return win;
				}
			}
			return null;
		}

		public Stetic.ActionGroupInfo GetActionGroupForFile (FilePath fileName)
		{
			foreach (Stetic.ActionGroupInfo group in SteticProject.ActionGroups) {
				if (fileName == GetSourceCodeFile (group, true))
					return group;
			}
			return null;
		}
		
		public Stetic.ActionGroupInfo GetActionGroup (string name)
		{
			return SteticProject.GetActionGroup (name);
		}

		public FilePath GetSourceCodeFile (Stetic.ProjectItemInfo obj)
		{
			return GetSourceCodeFile (obj, true);
		}

		public FilePath GetSourceCodeFile (Stetic.ProjectItemInfo obj, bool getUserClass)
		{
			var cls = GetClass (obj, getUserClass);
			if (cls != null)
				return cls.Locations.First ().SourceTree.FilePath;
			return null;
		}
		
		INamedTypeSymbol GetClass (Stetic.ProjectItemInfo obj, bool getUserClass)
		{
			string name = CodeBinder.GetClassName (obj);
			return FindClass (name, getUserClass);
		}
		
		public INamedTypeSymbol FindClass (string className)
		{
			return FindClass (className, true);
		}
		
		public INamedTypeSymbol FindClass (string className, bool getUserClass)
		{
			FilePath gui_folder = GtkDesignInfo.FromProject (project).GtkGuiFolder;
			var ctx = GetParserContext ();
			if (ctx == null)
				return null;
			foreach (var cls in ctx.GetAllTypesInMainAssembly ()) {
				if (cls.GetFullName() == className) {
					if (getUserClass) {
						// Return this class only if it is declared outside the gtk-gui
						// folder. Generated partial classes will be ignored.
						foreach (var part in cls.Locations) {
							var filePath = part.SourceTree.FilePath;
							if (!string.IsNullOrEmpty (filePath) && !((FilePath)filePath).IsChildPathOf (gui_folder)) {
								return cls;
							}
						}
						continue;
					}
					return cls;
				}
			}
			return null;
		}
		
		public Compilation GetParserContext ()
		{
			System.Threading.Tasks.Task<Compilation> task;
			do {
				task = TypeSystemService.GetCompilationAsync (Project);
				task.Wait (500);
			} while (!task.IsCompleted);

			var dom = task.Result;
			if (dom != null && needsUpdate) {
				needsUpdate = false;
			}
			return dom;
		}
		
		public WidgetParser WidgetParser {
			get {
				return new WidgetParser (GetParserContext ());
			}
		}

		public void UpdateLibraries ()
		{
			if (hasError || disposed || gproject == null)
				return;

			bool needsSave = false;
			librariesUpdated = true;
			
			string[] oldLibs = gproject.WidgetLibraries;
			
			ArrayList libs = new ArrayList ();
			string[] internalLibs;
			
			foreach (var pref in project.References) {
				string wref = GetReferenceLibraryPath (pref);
				if (wref != null)
					libs.Add (wref);
			}
			
			ReferenceManager refmgr = new ReferenceManager (project);
			string target_version = refmgr.TargetGtkVersion;
			refmgr.Dispose ();
			
			// Make sure the target gtk version is properly set
			if (gproject.TargetGtkVersion != target_version) {
				gproject.TargetGtkVersion = target_version;
				needsSave = true;
			}

			string outLib = project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
			if (!string.IsNullOrEmpty (outLib))
				internalLibs = new string [] { outLib };
			else
				internalLibs = new string [0];

			string[] newLibs = (string[]) libs.ToArray (typeof(string));
			
			// See if something has changed
			if (LibrariesChanged (oldLibs, internalLibs, newLibs)) {
				gproject.SetWidgetLibraries (newLibs, internalLibs);
				needsSave = true;
			} else {
				GuiBuilderService.SteticApp.UpdateWidgetLibraries (false);
			}
			
			if (needsSave)
				SaveAll (true);
		}
		
		bool LibrariesChanged (string[] oldLibs, string[] internalLibs, string[] newLibs)
		{
			if (oldLibs.Length == newLibs.Length + internalLibs.Length) {
				foreach (string s in newLibs) {
					if (!((IList)oldLibs).Contains (s))
						return true;
				}
				foreach (string s in internalLibs) {
					if (!((IList)oldLibs).Contains (s))
						return true;
				}
				return false;
			} else
				return true;
		}
		
		void NotifyChanged ()
		{
			if (Changed != null && !disposed)
				Changed (this, EventArgs.Empty);
		}

		public StringCollection GenerateFiles (string guiFolder)
		{
			StringCollection files = new StringCollection ();

			if (hasError)
				return files;

			var binding = LanguageBindingService.GetBindingPerLanguageName (project.LanguageName);
			string path = Path.Combine (guiFolder, binding.GetFileName ("generated"));
			if (!System.IO.File.Exists (path)) {
				// Generate an empty build class
				CodeDomProvider provider = binding.GetCodeDomProvider ();
				if (provider == null)
					throw new UserException ("Code generation not supported for language: " + project.LanguageName);
				GuiBuilderService.SteticApp.GenerateProjectCode (path, "Stetic", provider, null);
			}
			files.Add (path);

			if (Windows != null) {
				foreach (GuiBuilderWindow win in Windows)
					files.Add (GuiBuilderService.GenerateSteticCodeStructure (project, win.RootWidget, true, false));
			}
					
			foreach (Stetic.ActionGroupInfo ag in SteticProject.ActionGroups)
				files.Add (GuiBuilderService.GenerateSteticCodeStructure (project, ag, true, false));

			return files;
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
