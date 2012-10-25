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
using System.IO;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.GtkCore.NodeBuilders;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.GtkCore
{
	public class GtkDesignInfo: IDisposable
	{
		DotNetProject project;
		GuiBuilderProject builderProject;
		IDotNetLanguageBinding binding;
		ProjectResourceProvider resourceProvider;
		ReferenceManager referenceManager;
		
		[ItemProperty (DefaultValue=true)]
		bool generateGettext = true;
		
		[ItemProperty (DefaultValue="Mono.Unix.Catalog")]
		string gettextClass = "Mono.Unix.Catalog";
		
		GtkDesignInfo ()
		{
		}
		
		GtkDesignInfo (DotNetProject project)
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
					binding = null;
					if (referenceManager != null)
						referenceManager.Dispose ();
					referenceManager = null;
				}
				project = value;
				if (project != null) {
					binding = LanguageBindingService.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
					project.FileAddedToProject += OnFileEvent;
					project.FileChangedInProject += OnFileEvent;
					project.FileRemovedFromProject += OnFileEvent;
				}
			}
		}

		void OnFileEvent (object o, ProjectFileEventArgs args)
		{
			if (!IdeApp.Workspace.IsOpen || !File.Exists (ObjectsFile))
				return;

			UpdateObjectsFile ();
		}

		public void Dispose ()
		{
			if (resourceProvider != null)
				System.Runtime.Remoting.RemotingServices.Disconnect (resourceProvider);
			resourceProvider = null;
			if (builderProject != null)
				builderProject.Dispose ();
			builderProject = null;
			if (referenceManager != null)
				referenceManager.Dispose ();
			referenceManager = null;
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
						builderProject = GuiBuilderService.CreateBuilderProject (project, SteticFile);
					} else
						builderProject = GuiBuilderService.CreateBuilderProject (project, null);
				}
				return builderProject;
			}
		}

		public ReferenceManager ReferenceManager {
			get {
				if (referenceManager == null)
					referenceManager = new ReferenceManager (project);
				return referenceManager;
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
		
		FilePath ObjectsFile {
			get { return GtkGuiFolder.Combine ("objects.xml"); }
		}
		
		public FilePath SteticGeneratedFile {
			get { return GtkGuiFolder.Combine (binding.GetFileName ("generated")); }
		}
		
		public FilePath SteticFile {
			get { return GtkGuiFolder.Combine ("gui.stetic"); }
		}
		
		public FilePath GtkGuiFolder {
			get { return project.BaseDirectory.Combine ("gtk-gui"); }
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
			if (project == null)
				return false;

			string stetic_file = Path.Combine (Path.Combine (project.BaseDirectory, "gtk-gui"), "gui.stetic");
			return SupportsDesigner (project) && File.Exists (stetic_file);
		}

		public static bool SupportsDesigner (Project project)
		{
			DotNetProject dnp = project as DotNetProject;
			return dnp != null && HasGtkReference (dnp) && SupportsRefactoring (dnp);
		}

		public static bool SupportsRefactoring (DotNetProject project)
		{
			if (project == null || project.LanguageBinding == null || project.LanguageBinding.GetCodeDomProvider () == null)
				return false;
			var testFileName = project.LanguageBinding.GetFileName ("test");
			return CodeGenerator.HasGenerator (DesktopService.GetMimeTypeForUri (testFileName));
		}
		
		static bool IsGtkReference (ProjectReference pref)
		{
			if (pref.ReferenceType != ReferenceType.Package)
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
				project.SetNeedsBuilding (true);
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
			bool initialGeneration = false;
			
			if (!File.Exists (SteticFile)) {
				initialGeneration = true;
				StreamWriter sw = new StreamWriter (SteticFile);
				sw.WriteLine ("<stetic-interface />");
				sw.Close ();
				FileService.NotifyFileChanged (SteticFile);
			}
				
			if (!project.IsFileInProject (SteticFile)) {
				ProjectFile pf = project.AddFile (SteticFile, BuildAction.EmbeddedResource);
				pf.ResourceId = "gui.stetic";
				projectModified = true;
			}

			StringCollection files = GuiBuilderProject.GenerateFiles (GtkGuiFolder);
			DateTime generatedTime = File.GetLastWriteTime (SteticFile).Subtract (TimeSpan.FromSeconds (2));

			foreach (string filename in files) {
				if (initialGeneration) {
					// Ensure that the generation date of this file is < the date of the .stetic file
					// In this way the code will be properly regenerated when building the project.
					File.SetLastWriteTime (filename, generatedTime);
				}
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

			return ReferenceManager.Update () || projectModified;
		}

		void UpdateObjectsFile ()
		{
			if (!File.Exists (ObjectsFile))
				return;

			ObjectsDocument doc = new ObjectsDocument (ObjectsFile);
			doc.Update (GuiBuilderProject.WidgetParser, GuiBuilderProject.SteticProject);
		}

		public static void DisableProject (Project project)
		{
			if (HasDesignedObjects (project))
				return;

			GtkDesignInfo info = FromProject (project);
			StringCollection saveFiles = new StringCollection ();
			saveFiles.AddRange (new string[] {info.ObjectsFile, info.SteticFile});
			info.CleanGtkFolder (saveFiles);
			project.Files.Remove (info.ObjectsFile);
			project.Files.Remove (info.SteticFile);
			IExtendedDataItem item = (IExtendedDataItem) project;
			item.ExtendedProperties.Remove ("GtkDesignInfo");
			info.Dispose ();
			ProjectNodeBuilder.OnSupportChanged (project);
		}

		public static GtkDesignInfo FromProject (Project project)
		{
			if (!(project is DotNetProject))
				return new GtkDesignInfo ();

			IExtendedDataItem item = (IExtendedDataItem) project;
			GtkDesignInfo info = item.ExtendedProperties ["GtkDesignInfo"] as GtkDesignInfo;
			if (info == null)
				info = new GtkDesignInfo ((DotNetProject) project);
			else
				info.Project = (DotNetProject) project;
			return info;
		}
	}	
}
