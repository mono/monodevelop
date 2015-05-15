// 
// SharpDevelopProjectSystem.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Formats.MSBuild;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class SharpDevelopProjectSystem : PhysicalFileSystem, IProjectSystem
	{
		IDotNetProject project;
		ProjectTargetFramework targetFramework;
		IPackageManagementFileService fileService;
		Action<MessageHandler> guiSyncDispatcher;

		public SharpDevelopProjectSystem(DotNetProject project)
			: this (
				new DotNetProjectProxy (project),
				new PackageManagementFileService (),
				PackageManagementServices.ProjectService,
				DispatchService.GuiSyncDispatch)
		{
		}
		
		public SharpDevelopProjectSystem (
			IDotNetProject project,
			IPackageManagementFileService fileService,
			IPackageManagementProjectService projectService,
			Action<MessageHandler> guiSyncDispatcher)
			: base (AppendTrailingSlashToDirectory (project.BaseDirectory))
		{
			this.project = project;
			this.fileService = fileService;
			this.guiSyncDispatcher = guiSyncDispatcher;
		}
		
		static string AppendTrailingSlashToDirectory(string directory)
		{
			return directory + Path.DirectorySeparatorChar.ToString();
		}
		
		public bool IsBindingRedirectSupported { get; set; }
		
		public FrameworkName TargetFramework {
			get {
				return GuiSyncDispatch (() => GetTargetFramework ());
			}
		}
		
		FrameworkName GetTargetFramework()
		{
			if (targetFramework == null) {
				targetFramework = new ProjectTargetFramework(project);
			}
			return targetFramework.TargetFrameworkName;
		}
		
		public string ProjectName {
			get {
				return GuiSyncDispatch (() => project.Name);
			}
		}

		public dynamic GetPropertyValue (string propertyName)
		{
			return GuiSyncDispatch (() => {
				if ("RootNamespace".Equals(propertyName, StringComparison.OrdinalIgnoreCase)) {
					return project.DefaultNamespace;
				}
				return String.Empty;
			});
		}

		public void AddReference(string referencePath, Stream stream)
		{
			GuiSyncDispatch (() => {
				ProjectReference assemblyReference = CreateReference (referencePath);
				AddReferenceToProject (assemblyReference);
			});
		}
		
		ProjectReference CreateReference(string referencePath)
		{
			string fullPath = GetFullPath(referencePath);
			return ProjectReference.CreateAssemblyFileReference (fullPath);
		}
		
		void AddReferenceToProject(ProjectReference assemblyReference)
		{
			project.References.Add (assemblyReference);
			project.Save ();
			LogAddedReferenceToProject(assemblyReference);
		}
		
		void LogAddedReferenceToProject(ProjectReference referenceProjectItem)
		{
			LogAddedReferenceToProject(referenceProjectItem.Reference, ProjectName);
		}
		
		protected virtual void LogAddedReferenceToProject(string referenceName, string projectName)
		{
			DebugLogFormat("Added reference '{0}' to project '{1}'.", referenceName, projectName);
		}
		
		void DebugLogFormat(string format, params object[] args)
		{
			Logger.Log(MessageLevel.Debug, format, args);
		}
		
		public bool ReferenceExists(string name)
		{
			return GuiSyncDispatch (() => {
				ProjectReference referenceProjectItem = FindReference (name);
				if (referenceProjectItem != null) {
					return true;
				}
				return false;
			});
		}
		
		ProjectReference FindReference(string name)
		{
			string referenceName = GetReferenceName(name);
			foreach (ProjectReference referenceProjectItem in project.References) {
				string projectReferenceName = GetProjectReferenceName(referenceProjectItem.Reference);
				if (IsMatchIgnoringCase(projectReferenceName, referenceName)) {
					return referenceProjectItem;
				}
			}
			return null;
		}
		
		string GetReferenceName(string name)
		{
			if (HasDllOrExeFileExtension(name)) {
				return Path.GetFileNameWithoutExtension(name);
			}
			return name;
		}
		
		string GetProjectReferenceName(string name)
		{
			string referenceName = GetReferenceName(name);
			return GetAssemblyShortName(referenceName);
		}
		
		string GetAssemblyShortName(string name)
		{
			string[] parts = name.Split(',');
			return parts[0];
		}
		
		bool HasDllOrExeFileExtension(string name)
		{
			string extension = Path.GetExtension(name);
			return
				IsMatchIgnoringCase(extension, ".dll") ||
				IsMatchIgnoringCase(extension, ".exe");
		}
		
		bool IsMatchIgnoringCase(string lhs, string rhs)
		{
			return String.Equals(lhs, rhs, StringComparison.InvariantCultureIgnoreCase);
		}
		
		public void RemoveReference(string name)
		{
			GuiSyncDispatch (() => {
				ProjectReference referenceProjectItem = FindReference (name);
				if (referenceProjectItem != null) {
					project.References.Remove (referenceProjectItem);
					project.Save ();
					LogRemovedReferenceFromProject (referenceProjectItem);
				}
			});
		}
		
		void LogRemovedReferenceFromProject(ProjectReference referenceProjectItem)
		{
			LogRemovedReferenceFromProject(referenceProjectItem.Reference, ProjectName);
		}
		
		protected virtual void LogRemovedReferenceFromProject(string referenceName, string projectName)
		{
			DebugLogFormat("Removed reference '{0}' from project '{1}'.", referenceName, projectName);
		}
		
		public bool IsSupportedFile(string path)
		{
			return GuiSyncDispatch (() => {
				if (project.IsWebProject ()) {
					return !IsAppConfigFile (path);
				}
				return !IsWebConfigFile (path);
			});
		}
		
		bool IsWebConfigFile(string path)
		{
			return IsFileNameMatchIgnoringPath("web.config", path);
		}
		
		bool IsAppConfigFile(string path)
		{
			return IsFileNameMatchIgnoringPath("app.config", path);
		}
		
		bool IsFileNameMatchIgnoringPath(string fileName1, string path)
		{
			string fileName2 = Path.GetFileName(path);
			return IsMatchIgnoringCase(fileName1, fileName2);
		}
		
		public override void AddFile(string path, Stream stream)
		{
			PhysicalFileSystemAddFile(path, stream);
			GuiSyncDispatch (() => AddFileToProject (path));
		}
		
		protected virtual void PhysicalFileSystemAddFile(string path, Stream stream)
		{
			base.AddFile(path, stream);
		}
		
		public override void AddFile(string path, Action<Stream> writeToStream)
		{
			PhysicalFileSystemAddFile (path, writeToStream);
			GuiSyncDispatch (() => AddFileToProject (path));
		}

		protected virtual void PhysicalFileSystemAddFile (string path, Action<Stream> writeToStream)
		{
			base.AddFile(path, writeToStream);
		}

		void AddFileToProject(string path)
		{
			if (ShouldAddFileToProject(path)) {
				AddFileProjectItemToProject(path);
			}
			OnFileChanged (path);
			LogAddedFileToProject(path);
		}
		
		bool ShouldAddFileToProject(string path)
		{
			return !IsBinDirectory(path) && !FileExistsInProject(path);
		}

		void OnFileChanged (string path)
		{
			GuiSyncDispatch (() => fileService.OnFileChanged (GetFullPath (path)));
		}
		
		bool IsBinDirectory(string path)
		{
			string directoryName = Path.GetDirectoryName(path);
			return IsMatchIgnoringCase(directoryName, "bin");
		}
		
		public bool FileExistsInProject(string path)
		{
			string fullPath = GetFullPath(path);
			return GuiSyncDispatch (() => {
				return project.IsFileInProject (fullPath);
			});
		}
		
		void AddFileProjectItemToProject(string path)
		{
			ProjectFile fileItem = CreateFileProjectItem (path);
			project.AddFile (fileItem);
			project.Save ();
		}
		
		ProjectFile CreateFileProjectItem(string path)
		{
			//TODO custom tool?
			string fullPath = GetFullPath(path);
			string buildAction = project.GetDefaultBuildAction(fullPath);
			return new ProjectFile(fullPath) {
				BuildAction = buildAction
			};
		}
		
		void LogAddedFileToProject(string fileName)
		{
			LogAddedFileToProject(fileName, ProjectName);
		}
		
		protected virtual void LogAddedFileToProject(string fileName, string projectName)
		{
			DebugLogFormat("Added file '{0}' to project '{1}'.", fileName, projectName);
		}
		
		public override void DeleteDirectory(string path, bool recursive)
		{
			GuiSyncDispatch (() => {
				string directory = GetFullPath (path);
				fileService.RemoveDirectory (directory);
				project.Save ();
				LogDeletedDirectory (path);
			});
		}
		
		public override void DeleteFile(string path)
		{
			GuiSyncDispatch (() => {
				string fileName = GetFullPath (path);
				project.Files.Remove (fileName);
				fileService.RemoveFile (fileName);
				project.Save ();
				LogDeletedFileInfo (path);
			});
		}
		
		protected virtual void LogDeletedDirectory(string folder)
		{
			DebugLogFormat("Removed folder '{0}'.", folder);
		}
		
		void LogDeletedFileInfo(string path)
		{
			string fileName = Path.GetFileName(path);
			string directory = Path.GetDirectoryName(path);
			if (String.IsNullOrEmpty(directory)) {
				LogDeletedFile(fileName);
			} else {
				LogDeletedFileFromDirectory(fileName, directory);
			}
		}
		
		protected virtual void LogDeletedFile(string fileName)
		{
			DebugLogFormat("Removed file '{0}'.", fileName);
		}
		
		protected virtual void LogDeletedFileFromDirectory(string fileName, string directory)
		{
			DebugLogFormat("Removed file '{0}' from folder '{1}'.", fileName, directory);
		}
		
		public void AddFrameworkReference(string name)
		{
			GuiSyncDispatch (() => {
				ProjectReference assemblyReference = CreateGacReference (name);
				AddReferenceToProject (assemblyReference);
			});
		}
		
		ProjectReference CreateGacReference(string name)
		{
			return ProjectReference.CreateAssemblyReference (name);
		}
		
		public string ResolvePath(string path)
		{
			return path;
		}
		
		public void AddImport(string targetPath, ProjectImportLocation location)
		{
			GuiSyncDispatch (() => {
				string relativeTargetPath = GetRelativePath (targetPath);
				string condition = GetCondition (relativeTargetPath);
				project.AddImportIfMissing (relativeTargetPath, condition);
				project.Save ();
			});
		}

		static string GetCondition (string targetPath)
		{
			return String.Format ("Exists('{0}')", targetPath);
		}

		string GetRelativePath(string path)
		{
			return MSBuildProjectService.ToMSBuildPath (project.BaseDirectory, path);
		}
		
		public void RemoveImport(string targetPath)
		{
			GuiSyncDispatch (() => {
				string relativeTargetPath = GetRelativePath (targetPath);
				project.RemoveImport (relativeTargetPath);
				RemoveImportWithForwardSlashes (targetPath);

				using (var updater = new EnsureNuGetPackageBuildImportsTargetUpdater ()) {
					updater.RemoveImport (relativeTargetPath);
					project.DisposeProjectBuilder ();
					project.Save ();
				}
			});
		}

		void RemoveImportWithForwardSlashes (string targetPath)
		{
			string relativeTargetPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, targetPath);
			if (Path.DirectorySeparatorChar == '\\') {
				relativeTargetPath = relativeTargetPath.Replace ('\\', '/');
			}
			project.RemoveImport (relativeTargetPath);
		}

		/// <summary>
		/// NuGet sometimes uses CreateFile to replace an existing file.
		/// This happens when the XML transformation (web.config.transform) is reverted on 
		/// uninstalling a NuGet package. It also happens when an XML document transform 
		/// (.install.xdt, .uninstall.xdt) is run.
		/// </summary>
		public override Stream CreateFile (string path)
		{
			OnFileChanged (path);
			return base.CreateFile (path);
		}

		T GuiSyncDispatch<T> (Func<T> action)
		{
			T result = default(T);
			guiSyncDispatcher (() => result = action ());
			return result;
		}

		void GuiSyncDispatch (Action action)
		{
			guiSyncDispatcher (() => action ());
		}
	}
}
