//
// GtkDesignInfo.cs
//
// Authors:
//   Lluis Sanchez Gual
//   Mike Kestner
//   Krzysztof Marecki
//
// Copyright (C) 2006-2008 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Serialization;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.GtkCore.NodeBuilders;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore
{
	public class GtkDesignInfo: IDisposable, Stetic.IProjectDesignInfo
	{
		DotNetProject project;
		GuiBuilderProject builderProject;
		IDotNetLanguageBinding binding;
		ProjectResourceProvider resourceProvider;
		ReferenceManager referenceManager;
		string langExtension;
		
		[ItemProperty (DefaultValue = true)]
		bool generateGettext = true;
		
		[ItemProperty (DefaultValue = "Mono.Unix.Catalog")]
		string gettextClass = "Mono.Unix.Catalog";
		
		[ItemProperty (DefaultValue = "Designer")]
		string steticFolderName = "Designer";
		
		[ItemProperty (DefaultValue = true)]
		bool hideGtkxFiles = true;
		
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
					
					CodeDomProvider provider = binding.GetCodeDomProvider ();
					if (provider == null)
						throw new UserException ("Code generation not supported for language: " + project.LanguageName);
					langExtension = "." + provider.FileExtension;
					
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
						builderProject = new GuiBuilderProject (project, SteticFolder.FullPath.FullPath);
					} else
						builderProject = new GuiBuilderProject (project, null);
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
		
		public bool NeedsConversion {
			get { return project.IsFileInProject (SteticFile); }
		}
		
		FilePath ObjectsFile {
			get { return SteticFolder.Combine ("objects.xml"); }
		}
		
		[Obsolete]
		public FilePath SteticGeneratedFile {
			get { return SteticFolder.Combine (binding.GetFileName ("generated")); }
		}
		
		[Obsolete]
		public FilePath SteticFile {
			get { return SteticFolder.Combine ("gui.stetic"); }
		}
		
		public string BuildFileExtension {
			get { return ".designer"; }
		}
		
		public string DesignerFileExtension {
			get { return ".gtkx"; }
		}
		
		public FilePath SteticFolder {
			get { 
				FilePath oldfolder = project.BaseDirectory.Combine ("gtk-gui");
				if (Directory.Exists (oldfolder))
					return oldfolder;
				
				return project.BaseDirectory.Combine (steticFolderName); 
			}
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
		
		public string SteticFolderName {
			get { return steticFolderName; }
			set { steticFolderName = value; }
		}
		
		public bool HideGtkxFiles { 
			get { return hideGtkxFiles; }
			set { hideGtkxFiles = value; }
		}
		
		public static bool HasDesignedObjects (Project project)
		{
			if (project == null)
				return false;

			return SupportsDesigner (project);
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
				project.SetNeedsBuilding (true);
			} catch {
				// Ignore errors here
			}
		}
		
		public void CheckGtkFolder ()
		{
			foreach (string designerFile in GetDesignerFiles ()) {
				string componentFile = GetComponentFileFromDesigner (designerFile);
				if (componentFile != null) {
					string buildFile = GetBuildFileFromComponent (componentFile);
					if (buildFile != null) {
						// if build file is missing, force to generate it again
						if ((project.GetProjectFile (buildFile) == null) || !File.Exists (buildFile)) {
							FileInfo fi = new FileInfo (designerFile);
							fi.LastWriteTime = DateTime.Now;
						}
					}
				}
			}
		}
		
		bool CleanGtkFolder (StringCollection remaining_files)
		{
			bool projectModified = false;
			// Remove all project files which are not in the generated list
			foreach (ProjectFile pf in project.Files.GetFilesInPath (SteticFolder)) {
				if (remaining_files.Contains (pf.FilePath))
					continue;

				project.Files.Remove (pf);
				FileService.DeleteFile (pf.FilePath);
				projectModified = true;
			}

			if (remaining_files.Count == 0)
				FileService.DeleteDirectory (SteticFolder);

			return projectModified;
		}

		public void ConvertGtkFolder (string guiFolderName, bool makeBackup)
		{	
			foreach (ProjectFile pf in project.Files.GetFilesInPath (SteticFolder)) {
				FilePath path = pf.FilePath;
				
				if (path != SteticGeneratedFile) {
					project.Files.Remove (path);
					
					if (path != SteticFile)
						FileService.DeleteFile (path.FullPath);
				}
			}
			
			string oldGuiFolder = SteticFolder.FullPath;
			string oldSteticFile = SteticFile;
			string oldGeneratedFile = SteticGeneratedFile;
			SteticFolderName = guiFolderName;
	
			if (!Directory.Exists (SteticFolder))
				FileService.CreateDirectory (SteticFolder);
			
			if (makeBackup && File.Exists (oldSteticFile)) {
				string backupFile = SteticFolder.Combine ("old.stetic");
				FileService.MoveFile (oldSteticFile, backupFile);
			}
			
			if (File.Exists (oldGeneratedFile))
				FileService.DeleteFile (oldGeneratedFile);
				
			if (Directory.Exists (oldGuiFolder))
				FileService.DeleteDirectory (oldGuiFolder);
		}
		
		public bool UpdateGtkFolder ()
		{
			if (!SupportsDesigner (project))
				return false;
			
			// This method synchronizes the current gtk project configuration info
			// with the needed support files in the gtk-gui folder.

			FileService.CreateDirectory (SteticFolder);
			bool projectModified = false;
			
			foreach (string filename in GetDesignerFiles ()) {
				ProjectFile pf = project.AddFile (filename, BuildAction.EmbeddedResource);
				pf.ResourceId = Path.GetFileName (filename);
	
				string componentFile = GetComponentFileFromDesigner (filename);
				
				if (componentFile != null && File.Exists (componentFile)) { 
					pf.DependsOn = componentFile;	
				
					string buildFile = GetBuildFileFromComponent (componentFile);
					if (buildFile != null && File.Exists (buildFile)) {
						ProjectFile pf2 = project.AddFile (buildFile, BuildAction.Compile);
						pf2.ResourceId = Path.GetFileName (buildFile);
						pf2.DependsOn = componentFile;
					}
				}
				
				projectModified = true;
			}
			
			StringCollection files = GuiBuilderProject.GenerateFiles (SteticFolder);
			foreach (string filename in files) {
				if (!project.IsFileInProject (filename)) {
					project.AddFile (filename, BuildAction.Compile);
					projectModified = true;
				}
				
			}
			
			UpdateObjectsFile ();
			
			return ReferenceManager.Update () || projectModified;
		}
		
//		public bool UpdateGtkFolder ()
//		{
//			if (!SupportsDesigner (project))
//				return false;
//
//			// This method synchronizes the current gtk project configuration info
//			// with the needed support files in the gtk-gui folder.
//
//			FileService.CreateDirectory (GtkGuiFolder);
//			bool projectModified = false;
//			bool initialGeneration = false;
//			
//			if (!File.Exists (SteticFile)) {
//				initialGeneration = true;
//				StreamWriter sw = new StreamWriter (SteticFile);
//				sw.WriteLine ("<stetic-interface />");
//				sw.Close ();
//				FileService.NotifyFileChanged (SteticFile);
//			}
//				
//			if (!project.IsFileInProject (SteticFile)) {
//				ProjectFile pf = project.AddFile (SteticFile, BuildAction.EmbeddedResource);
//				pf.ResourceId = "gui.stetic";
//				projectModified = true;
//			}
//
//			StringCollection files = GuiBuilderProject.GenerateFiles (GtkGuiFolder);
//			DateTime generatedTime = File.GetLastWriteTime (SteticFile).Subtract (TimeSpan.FromSeconds (2));
//
//			foreach (string filename in files) {
//				if (initialGeneration) {
//					// Ensure that the generation date of this file is < the date of the .stetic file
//					// In this way the code will be properly regenerated when building the project.
//					File.SetLastWriteTime (filename, generatedTime);
//				}
//				if (!project.IsFileInProject (filename)) {
//					project.AddFile (filename, BuildAction.Compile);
//					projectModified = true;
//				}
//			}
//
//			UpdateObjectsFile ();
//			files.Add (ObjectsFile);
//			files.Add (SteticFile);
//
//			if (CleanGtkFolder (files))
//				projectModified = true;
//
//			return ReferenceManager.Update () || projectModified;
//			return true;
//		}
		
		public void RenameComponentFile (ProjectFile pfComponent) 
		{
			foreach (ProjectFile pf in pfComponent.DependentChildren) {
				if (pf.FilePath.FileName.Contains (BuildFileExtension)) {
					FileService.RenameFile (pf.FilePath, 
					                        GetBuildFileNameFromComponent (pfComponent.FilePath));
				}
				if (pf.FilePath.FileName.Contains (DesignerFileExtension)) {
					FileService.RenameFile (pf.FilePath, 
					                        GetDesignerFileNameFromComponent (pfComponent.FilePath));
				}
			}
		}
			         
		public string GetComponentFile (string componentName)
		{
			IType type = GuiBuilderProject.FindClass (componentName);
			if (type != null) {
				foreach (IType part in type.Parts) {
					string componentFile = part.CompilationUnit.FileName.FullPath;
					if (componentFile.Contains (BuildFileExtension))
						componentFile = componentFile.Replace (BuildFileExtension, string.Empty);
					
					return componentFile;
				}
			}
			
			//If ProjectDom does not exist, assume that project is being created
			//and return component file path that is located in the project root folder
			ProjectDom ctx = ProjectDomService.GetProjectDom (project);
			if (ctx == null) {
				string componentFile = Path.Combine (project.BaseDirectory, componentName + langExtension);
				if (File.Exists (componentFile))
					return componentFile;
			}
			
			return null;
		}
		
		public string GetBuildFileInSteticFolder (string componentName)
		{
			string name = string.Format ("{0}{1}{2}", componentName, BuildFileExtension, langExtension);
			string buildFile = Path.Combine (SteticFolder, name);
			
			return buildFile;
		}

		public string GetBuildFileFromComponent (string componentFile)
		{
			if (componentFile != null) {
				ProjectFile pf = project.Files.GetFile (componentFile);
				if (pf != null) {
					foreach (var child in pf.DependentChildren) {
						if (child.Name.Contains (BuildFileExtension))
							return child.FilePath;
					}
				}
				return GetBuildFileNameFromComponent (componentFile);
			}
			return null;
		}
		
		public string GetBuildFileNameFromComponent (string componentFile)
		{
			string buildFile = componentFile.Replace 
					(langExtension, BuildFileExtension + langExtension);
			return buildFile;
		}
		
		public string GetDesignerFileFromComponent (string componentFile)
		{
			if (componentFile != null) {
				ProjectFile pf = project.Files.GetFile (componentFile);
				if (pf != null) {
					foreach (var child in pf.DependentChildren) {
						if (child.Name.Contains (DesignerFileExtension))
							return child.FilePath;
					}
				}
				return GetDesignerFileNameFromComponent (componentFile);
			}
			return null;
		}
		
		public string GetDesignerFileNameFromComponent (string componentFile)
		{
			string gtkxFile = componentFile.Replace (langExtension, DesignerFileExtension);
			return gtkxFile;
		}
		
		public string GetComponentFileFromDesigner (string gtkxFile)
		{
			if (gtkxFile != null) { 
				ProjectFile pf = project.Files.GetFile (gtkxFile);
				if (pf != null) {
					if (pf.DependsOn != null)
						return pf.DependsOn;
				}
				string componentFile = gtkxFile.Replace (DesignerFileExtension, langExtension);
				return componentFile;
			}
			
			return null;
		}
		
//		public string GetBuildFileFromDesigner (string gtkxFile)
//		{
//			string buildFile = gtkxFile.Replace (DesignerFileExtension, BuildFileExtension + langExtension);
//			return buildFile;
//		}
		
		
		public string GetComponentFolder (string componentName)
		{
			IType type = GuiBuilderProject.FindClass (componentName);
			
			if (type != null) {
				FilePath folder = type.CompilationUnit.FileName.ParentDirectory;
				return folder.FullPath.ToString ();
			}
			
			return null;
		}
		
		public string[] GetComponentFolders ()
		{
			List<string> folders = new List<string> ();
			ProjectDom ctx = GuiBuilderProject.GetParserContext ();
			
			if (ctx != null) {
				foreach (IType type in ctx.Types)
					foreach (IType part in type.Parts) {
						FilePath folder = part.CompilationUnit.FileName.ParentDirectory;
						string folderName = folder.FullPath.ToString ();
					
						if (!folders.Contains (folderName))
							folders.Add (folder);
				}
			}
			
			return folders.ToArray ();
		}
		
		public string[] GetDesignerFiles ()
		{
			List<string> files = new List<string> ();
			
			foreach (string folder in GetComponentFolders ()) {
				DirectoryInfo dir = new DirectoryInfo (folder);
				
				foreach (FileInfo file in dir.GetFiles ()) 
					if (file.Extension == DesignerFileExtension) 
						files.Add (file.ToString ());
			}
			
			return files.ToArray ();
		}

		public bool HasComponentFile (string componentName)
		{
			return (GetComponentFile (componentName) != null);
		}
		
		public bool ComponentNeedsCodeGeneration (string componentName)
		{
			string componentFile = GetComponentFile (componentName);
			string gtkxFile = GetDesignerFileFromComponent (componentFile);
			string buildFile = GetBuildFileFromComponent (componentFile);
			FileInfo gtkxFileInfo = File.Exists (gtkxFile) ? new FileInfo (gtkxFile) : null;
			FileInfo buildFileInfo = File.Exists (buildFile) ? new FileInfo (buildFile) : null;
			if (gtkxFileInfo == null)
				return false;
			//file does not exist
			if (buildFileInfo == null)
				return true;
			
			return gtkxFileInfo.LastWriteTime > buildFileInfo.LastWriteTime;
		}

		void UpdateObjectsFile ()
		{
			if (!File.Exists (ObjectsFile))
				return;

			ObjectsDocument doc = new ObjectsDocument (ObjectsFile);
			doc.Update (GuiBuilderProject.WidgetParser, GuiBuilderProject.SteticProject, IdeApp.Workspace.GetCodeRefactorer (project.ParentSolution));
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
