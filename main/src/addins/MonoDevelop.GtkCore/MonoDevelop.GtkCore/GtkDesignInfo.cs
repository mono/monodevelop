//
// GtkDesignInfo.cs
//
// Authors:
//   Lluis Sanchez Gual
//   Mike Kestner
//
// Copyright (C) 2006-2008 Novell, Inc (http://www.novell.com)
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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core.Serialization;

using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.GtkCore.NodeBuilders;

namespace MonoDevelop.GtkCore
{
	public class GtkDesignInfo: IDisposable
	{
		DotNetProject project;
		GuiBuilderProject builderProject;
		IDotNetLanguageBinding binding;
		ProjectResourceProvider resourceProvider;
		bool updatingVersion;
		
		static string[] GtkSharpAssemblies = new string [] { 
			"art-sharp", "atk-sharp", "gconf-sharp", "gdk-sharp", "glade-sharp","glib-sharp","gnome-sharp",
			"gnome-vfs-sharp", "gtk-dotnet", "gtkhtml-sharp", "gtk-sharp", "pango-sharp", "rsvg-sharp", "vte-sharp"
		};
		
		[ItemProperty (DefaultValue=true)]
		bool generateGettext = true;
		
		[ItemProperty (DefaultValue="Mono.Unix.Catalog")]
		string gettextClass = "Mono.Unix.Catalog";
		
		[ItemProperty]
		string gtkVersion;
		
		public GtkDesignInfo ()
		{
		}
		
		public GtkDesignInfo (DotNetProject project)
		{
			IExtendedDataItem item = (IExtendedDataItem) project;
			item.ExtendedProperties ["GtkDesignInfo"] = this;
			Project = project;
		}
		
		DotNetProject Project {
			get { return project; }
			set {
				if (project == value)
					return;

				if (project != null) {
					project.FileAddedToProject -= OnFileEvent;
					project.FileChangedInProject -= OnFileEvent;
					project.FileRemovedFromProject -= OnFileEvent;
					project.ReferenceAddedToProject -= OnReferenceAdded;
					project.ReferenceRemovedFromProject -= OnReferenceRemoved;
					binding = null;
				}
				project = value;
				if (project != null) {
					binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
					project.FileAddedToProject += OnFileEvent;
					project.FileChangedInProject += OnFileEvent;
					project.FileRemovedFromProject += OnFileEvent;
					project.ReferenceAddedToProject += OnReferenceAdded;
					project.ReferenceRemovedFromProject += OnReferenceRemoved;
				}
			}
		}

		void OnFileEvent (object o, ProjectFileEventArgs args)
		{
			if (!IdeApp.Workspace.IsOpen || !File.Exists (ObjectsFile))
				return;

			UpdateObjectsFile ();
		}

		void OnReferenceAdded (object o, ProjectReferenceEventArgs args)
		{
			if (!updatingVersion && IsGtkReference (args.ProjectReference))
				builderProject = null;
		}

		void OnReferenceRemoved (object o, ProjectReferenceEventArgs args)
		{
			if (!updatingVersion && IsGtkReference (args.ProjectReference)) {
				if (MonoDevelop.Core.Gui.MessageService.Confirm (GettextCatalog.GetString ("The Gtk# User Interface designer will be disabled by removing the gtk-sharp reference."), new MonoDevelop.Core.Gui.AlertButton (GettextCatalog.GetString ("Disable Designer")))) {
					StringCollection saveFiles = new StringCollection ();
					saveFiles.AddRange (new string[] {ObjectsFile, SteticFile});
					CleanGtkFolder (saveFiles);
					project.Files.Remove (ObjectsFile);
					project.Files.Remove (SteticFile);
					if (builderProject != null)
						builderProject = new GuiBuilderProject (project, null);
					ProjectNodeBuilder.OnSupportChanged (project);
				} else {
					project.References.Add (new ProjectReference (ReferenceType.Gac, "gtk-sharp, " + GtkAsmVersion));
				}
			}
		}

		public void Dispose ()
		{
			if (resourceProvider != null)
				System.Runtime.Remoting.RemotingServices.Disconnect (resourceProvider);
			resourceProvider = null;
			if (builderProject != null)
				builderProject.Dispose ();
			builderProject = null;
			Project = null;
		}
		
		public GuiBuilderProject GuiBuilderProject {
			get {
				if (builderProject == null) {
					if (SupportsDesigner (project)) {
						if (!File.Exists (SteticFile)) {
							UpdateGtkFolder ();
							ProjectNodeBuilder.OnSupportChanged (project);
						}
						builderProject = new GuiBuilderProject (project, SteticFile);
					} else
						builderProject = new GuiBuilderProject (project, null);
				}
				return builderProject;
			}
		}
		
		public void ReloadGuiBuilderProject ()
		{
			if (builderProject != null)
				builderProject.Reload ();
		}
		
		public ProjectResourceProvider ResourceProvider {
			get {
				if (resourceProvider == null) {
					resourceProvider = new ProjectResourceProvider (project);
					System.Runtime.Remoting.RemotingServices.Marshal (resourceProvider, null, typeof(Stetic.IResourceProvider));
				}
				return resourceProvider;
			}
		}
		
		string ObjectsFile {
			get { return Path.Combine (GtkGuiFolder, "objects.xml"); }
		}
		
		public string SteticGeneratedFile {
			get { return Path.Combine (GtkGuiFolder, binding.GetFileName ("generated")); }
		}
		
		public string SteticFile {
			get { return Path.Combine (GtkGuiFolder, "gui.stetic"); }
		}
		
		public string GtkGuiFolder {
			get { return Path.Combine (project.BaseDirectory, "gtk-gui"); }
		}
		
		public bool GenerateGettext {
			get { return generateGettext; }
			set {
				generateGettext = value;
				// Set to default value if gettext is not enabled
				if (!generateGettext) 
					gettextClass = "Mono.Unix.Catalog";
			}
		}
		
		public string GettextClass {
			get { return gettextClass; }
			set { gettextClass = value; }
		}
		
		public static bool HasDesignedObjects (Project project)
		{
			return SupportsDesigner (project) && File.Exists (FromProject (project).SteticFile);
		}

		public string TargetGtkVersion {
			get {
				if (gtkVersion != null)
					return gtkVersion;
				else
					return GtkCoreService.DefaultGtkVersion;
			}
			set {
				if (gtkVersion != value) {
					gtkVersion = value;
					if (HasDesignedObjects (project)) {
						GuiBuilderProject.SteticProject.TargetGtkVersion = TargetGtkVersion;
						GuiBuilderProject.Save (false);
					}
				}
			}
		}
		
		public static bool SupportsDesigner (Project project)
		{
			DotNetProject dnp = project as DotNetProject;
			return dnp != null && HasGtkReference (dnp) && SupportsRefactoring (dnp);
		}

		static bool SupportsRefactoring (DotNetProject project)
		{
			if (project.LanguageBinding == null || project.LanguageBinding.GetCodeDomProvider () == null)
				return false;
			
			RefactorOperations ops = RefactorOperations.AddField | RefactorOperations.AddMethod | RefactorOperations.RenameField | RefactorOperations.AddAttribute;
			CodeRefactorer cref = IdeApp.Workspace.GetCodeRefactorer (project.ParentSolution);
			return cref.LanguageSupportsOperation (project.LanguageBinding.Language, ops); 
		}
		
		static bool IsGtkReference (ProjectReference pref)
		{
			if (pref.ReferenceType != ReferenceType.Gac)
				return false;

			int idx = pref.StoredReference.IndexOf (",");
			if (idx == -1)
				return false;

			string name = pref.StoredReference.Substring (0, idx).Trim ();
			return name == "gtk-sharp";
		}

		static bool HasGtkReference (DotNetProject project)
		{
			foreach (ProjectReference pref in project.References)
				if (IsGtkReference (pref))
					return true;
			return false;
		}
		
		public void ForceCodeGenerationOnBuild ()
		{
			if (!SupportsDesigner (project))
				return;
			try {
				FileInfo fi = new FileInfo (SteticFile);
				fi.LastWriteTime = DateTime.Now;
				fi = new FileInfo (SteticGeneratedFile);
				fi.LastWriteTime = DateTime.Now;
			} catch {
				// Ignore errors here
			}
		}
		
		bool CleanGtkFolder (StringCollection remaining_files)
		{
			bool projectModified = false;

			// Remove all project files which are not in the generated list
			foreach (ProjectFile pf in project.Files.GetFilesInPath (GtkGuiFolder)) {
				if (remaining_files.Contains (pf.FilePath))
					continue;

				project.Files.Remove (pf);
				FileService.DeleteFile (pf.FilePath);
				projectModified = true;
			}

			if (remaining_files.Count == 0)
				FileService.DeleteDirectory (GtkGuiFolder);

			return projectModified;
		}

		public bool UpdateGtkFolder ()
		{
			if (!SupportsDesigner (project))
				return false;

			// This method synchronizes the current gtk project configuration info
			// with the needed support files in the gtk-gui folder.

			FileService.CreateDirectory (GtkGuiFolder);
			bool projectModified = false;
			
			if (!File.Exists (SteticFile)) {
				StreamWriter sw = new StreamWriter (SteticFile);
				sw.WriteLine ("<stetic-interface />");
				sw.Close ();
				FileService.NotifyFileChanged (SteticFile);
			}
				
			if (!project.IsFileInProject (SteticFile)) {
				ProjectFile pf = project.AddFile (SteticFile, BuildAction.EmbedAsResource);
				pf.ResourceId = "gui.stetic";
				projectModified = true;
			}

			StringCollection files = GuiBuilderProject.GenerateFiles (GtkGuiFolder);

			foreach (string filename in files) {
				if (!project.IsFileInProject (filename)) {
					project.AddFile (filename, BuildAction.Compile);
					projectModified = true;
				}
			}

			UpdateObjectsFile ();
			files.Add (ObjectsFile);
			files.Add (SteticFile);

			if (CleanGtkFolder (files))
				projectModified = true;

			return UpdateReferences () || projectModified;
		}

		string GtkAsmVersion {
			get {
				string gtkAsmVersion = "";
			
				if (gtkVersion != null) {
					foreach (SystemPackage p in Runtime.SystemAssemblyService.GetPackages ()) {
						if (p.Name == "gtk-sharp-2.0" && p.Version == TargetGtkVersion) {
							string fn = Runtime.SystemAssemblyService.GetAssemblyFullName (p.Assemblies[0]);
							int i = fn.IndexOf (',');
							gtkAsmVersion = fn.Substring (i+1).Trim ();
							break;
						}
					}
				}	
				return gtkAsmVersion;
			}
		}

		bool UpdateReferences ()
		{
			string gtkAsmVersion = GtkAsmVersion;
			
			bool changed = false, gdk=false, posix=false;
			
			foreach (ProjectReference r in new List<ProjectReference> (project.References)) {
				if (r.ReferenceType != ReferenceType.Gac)
					continue;
				int i = r.StoredReference.IndexOf (',');
				string name;
				if (i == -1)
					name = r.StoredReference;
				else
					name = r.StoredReference.Substring (0,i).Trim ();
				if (name == "gdk-sharp")
					gdk = true;
				else if (name == "Mono.Posix")
					posix = true;
				
				// Is a gtk-sharp-2.0 assembly?
				if (Array.IndexOf (GtkSharpAssemblies, name) == -1)
					continue;
				
				// Check and correct the assembly version only if a version is set
				if (!string.IsNullOrEmpty (gtkAsmVersion) && r.StoredReference.Substring (i+1).Trim() != gtkAsmVersion) {
					updatingVersion = true;
					project.References.Remove (r);
					project.References.Add (new ProjectReference (ReferenceType.Gac, name + ", " + gtkAsmVersion));
					updatingVersion = false;
					changed = true;
				}
			}
			if (!gdk) {
				project.References.Add (new ProjectReference (ReferenceType.Gac, typeof(Gdk.Window).Assembly.FullName));
				changed = true;
			}
				
			if (!posix && GenerateGettext && GettextClass == "Mono.Unix.Catalog") {
				// Add a reference to Mono.Posix. Use the version for the selected project's runtime version.
				string aname = Runtime.SystemAssemblyService.FindInstalledAssembly ("Mono.Posix, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");
				aname = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (aname, project.ClrVersion);
				project.References.Add (new ProjectReference (ReferenceType.Gac, aname));
				changed = true;
			}
			
			return changed;
		}
		
		void UpdateObjectsFile ()
		{
			if (!File.Exists (ObjectsFile))
				return;

			ObjectsDocument doc = new ObjectsDocument (ObjectsFile);
			doc.Update (GuiBuilderProject.WidgetParser, GuiBuilderProject.SteticProject, IdeApp.Workspace.GetCodeRefactorer (project.ParentSolution));
		}

		public static GtkDesignInfo FromProject (Project project)
		{
			if (!(project is DotNetProject))
				return new GtkDesignInfo ();

			IExtendedDataItem item = (IExtendedDataItem) project;
			GtkDesignInfo info = item.ExtendedProperties ["GtkDesignInfo"] as GtkDesignInfo;
			if (info == null) {
				info = new GtkDesignInfo ((DotNetProject) project);
				info.TargetGtkVersion = GtkCoreService.DefaultGtkVersion;
			} else
				info.Project = (DotNetProject) project;
			return info;
		}
	}	
}
