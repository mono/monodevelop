//
// MonoDevelopMSBuildNuGetProjectSystem.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopMSBuildNuGetProjectSystem : IMSBuildNuGetProjectSystem
	{
		DotNetProject project;
		NuGetFramework targetFramework;
		string projectFullPath;
		IPackageManagementEvents packageManagementEvents;
		IPackageManagementFileService fileService;
		Action<Action> guiSyncDispatcher;
		Func<Func<Task>,Task> guiSyncDispatcherFunc;

		public MonoDevelopMSBuildNuGetProjectSystem (DotNetProject project, INuGetProjectContext context)
		{
			this.project = project;
			NuGetProjectContext = context;
			guiSyncDispatcher = DefaultGuiSyncDispatcher;
			guiSyncDispatcherFunc = GuiSyncDispatchWithException;

			packageManagementEvents = PackageManagementServices.PackageManagementEvents;
			fileService = new PackageManagementFileService ();
		}

		public INuGetProjectContext NuGetProjectContext { get; private set; }

		public string ProjectFullPath {
			get {
				if (projectFullPath == null) {
					projectFullPath = GuiSyncDispatch (() => project.BaseDirectory);
				}
				return projectFullPath;
			}
		}

		public string ProjectName {
			get {
				return GuiSyncDispatch (() => project.Name);
			}
		}

		public string ProjectUniqueName {
			get { return ProjectName; }
		}

		public NuGetFramework TargetFramework {
			get {
				if (targetFramework == null) {
					targetFramework = GuiSyncDispatch (() => GetTargetFramework ());
				}
				return targetFramework;
			}
		}

		NuGetFramework GetTargetFramework()
		{
			var projectTargetFramework = new ProjectTargetFramework (new DotNetProjectProxy (project));
			return NuGetFramework.Parse (projectTargetFramework.TargetFrameworkName.FullName);
		}

		public void AddBindingRedirects ()
		{
		}

		public void AddExistingFile (string path)
		{
			GuiSyncDispatch (async () => await AddFileToProject (path));
		}

		public void AddFile (string path, Stream stream)
		{
			PhysicalFileSystemAddFile (path, stream);
			GuiSyncDispatch (async () => await AddFileToProject (path));
		}

		protected virtual void PhysicalFileSystemAddFile (string path, Stream stream)
		{
			FileSystemUtility.AddFile (ProjectFullPath, path, stream, NuGetProjectContext);
		}

		async Task AddFileToProject (string path)
		{
			if (ShouldAddFileToProject (path)) {
				await AddFileProjectItemToProject (path);
			}
			OnFileChanged (path);
			LogAddedFileToProject (path);
		}

		bool ShouldAddFileToProject (string path)
		{
			return !IsBinDirectory (path) && !FileExistsInProject (path);
		}

		void OnFileChanged (string path)
		{
			GuiSyncDispatch (() => fileService.OnFileChanged (GetFullPath (path)));
		}

		bool IsBinDirectory(string path)
		{
			string directoryName = Path.GetDirectoryName (path);
			return IsMatchIgnoringCase (directoryName, "bin");
		}

		async Task AddFileProjectItemToProject(string path)
		{
			ProjectFile fileItem = CreateFileProjectItem (path);
			project.AddFile (fileItem);
			await SaveAsync (project);
		}

		ProjectFile CreateFileProjectItem(string path)
		{
			//TODO custom tool?
			string fullPath = GetFullPath (path);
			string buildAction = project.GetDefaultBuildAction (fullPath);
			return new ProjectFile (fullPath) {
				BuildAction = buildAction
			};
		}

		void LogAddedFileToProject (string fileName)
		{
			LogAddedFileToProject (fileName, ProjectName);
		}

		protected virtual void LogAddedFileToProject (string fileName, string projectName)
		{
			DebugLogFormat("Added file '{0}' to project '{1}'.", fileName, projectName);
		}

		public void AddFrameworkReference (string name)
		{
			GuiSyncDispatch (async () => {
				ProjectReference assemblyReference = CreateGacReference (name);
				await AddReferenceToProject (assemblyReference);
			});
		}

		ProjectReference CreateGacReference (string name)
		{
			return ProjectReference.CreateAssemblyReference (name);
		}

		public void AddImport (string targetFullPath, ImportLocation location)
		{
			GuiSyncDispatch (async () => {
				string relativeTargetPath = GetRelativePath (targetFullPath);
				string condition = GetCondition (relativeTargetPath);
				using (var handler = CreateNewImportsHandler ()) {
					handler.AddImportIfMissing (relativeTargetPath, condition, (NuGet.ProjectImportLocation)location);
					await SaveAsync (project);
				}
			});
		}

		protected virtual INuGetPackageNewImportsHandler CreateNewImportsHandler ()
		{
			return new NuGetPackageNewImportsHandler ();
		}

		static string GetCondition (string targetPath)
		{
			return String.Format ("Exists('{0}')", targetPath);
		}

		string GetRelativePath (string path)
		{
			return MSBuildProjectService.ToMSBuildPath (project.BaseDirectory, path);
		}

		public void AddReference (string referencePath)
		{
			GuiSyncDispatch (async () => {
				ProjectReference assemblyReference = CreateReference (referencePath);
				packageManagementEvents.OnReferenceAdding (assemblyReference);
				await AddReferenceToProject (assemblyReference);
			});
		}

		ProjectReference CreateReference (string referencePath)
		{
			string fullPath = GetFullPath (referencePath);
			return ProjectReference.CreateAssemblyFileReference (fullPath);
		}

		string GetFullPath (string relativePath)
		{
			return project.BaseDirectory.Combine (relativePath);
		}

		async Task AddReferenceToProject (ProjectReference assemblyReference)
		{
			project.References.Add (assemblyReference);
			await SaveAsync (project);
			LogAddedReferenceToProject (assemblyReference);
		}

		void LogAddedReferenceToProject (ProjectReference referenceProjectItem)
		{
			LogAddedReferenceToProject (referenceProjectItem.Reference, ProjectName);
		}

		protected virtual void LogAddedReferenceToProject (string referenceName, string projectName)
		{
			DebugLogFormat ("Added reference '{0}' to project '{1}'.", referenceName, projectName);
		}

		void DebugLogFormat (string format, params object[] args)
		{
			NuGetProjectContext.Log (MessageLevel.Debug, format, args);
		}

		public void BeginProcessing ()
		{
		}

		// TODO: Support recursive.
		public void DeleteDirectory (string path, bool recursive)
		{
			GuiSyncDispatch (async () => {
				string directory = GetFullPath (path);
				fileService.RemoveDirectory (directory);
				await SaveAsync (project);
				LogDeletedDirectory (path);
			});
		}

		protected virtual void LogDeletedDirectory (string folder)
		{
			DebugLogFormat ("Removed folder '{0}'.", folder);
		}

		public void EndProcessing ()
		{
		}

		public Task ExecuteScriptAsync (PackageIdentity identity, string packageInstallPath, string scriptRelativePath, NuGetProject nuGetProject, bool throwOnFailure)
		{
			string message = GettextCatalog.GetString (
				"WARNING: {0} Package contains PowerShell script '{1}' which will not be run.",
				identity.Id,
				scriptRelativePath);

			NuGetProjectContext.Log (MessageLevel.Warning, message);

			return Task.FromResult (0);
		}

		public bool FileExistsInProject (string path)
		{
			return GuiSyncDispatch (() => {
				string fullPath = GetFullPath (path);
				return project.IsFileInProject (fullPath);
			});
		}

		public IEnumerable<string> GetDirectories (string path)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<string> GetFiles (string path, string filter, bool recursive)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<string> GetFullPaths (string fileName)
		{
			throw new NotImplementedException ();
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

		public bool IsSupportedFile (string path)
		{
			return GuiSyncDispatch (() => {
				if (new DotNetProjectProxy (project).IsWebProject ()) {
					return !IsAppConfigFile (path);
				}
				return !IsWebConfigFile (path);
			});
		}

		bool IsWebConfigFile (string path)
		{
			return IsFileNameMatchIgnoringPath ("web.config", path);
		}

		bool IsAppConfigFile (string path)
		{
			return IsFileNameMatchIgnoringPath ("app.config", path);
		}

		bool IsFileNameMatchIgnoringPath (string fileName1, string path)
		{
			string fileName2 = Path.GetFileName (path);
			return IsMatchIgnoringCase (fileName1, fileName2);
		}

		public bool ReferenceExists (string name)
		{
			return GuiSyncDispatch (() => {
				ProjectReference referenceProjectItem = FindReference (name);
				if (referenceProjectItem != null) {
					return true;
				}
				return false;
			});
		}

		ProjectReference FindReference (string name)
		{
			string referenceName = GetReferenceName (name);
			foreach (ProjectReference referenceProjectItem in project.References) {
				string projectReferenceName = GetProjectReferenceName (referenceProjectItem.Reference);
				if (IsMatchIgnoringCase (projectReferenceName, referenceName)) {
					return referenceProjectItem;
				}
			}
			return null;
		}

		string GetReferenceName (string name)
		{
			if (HasDllOrExeFileExtension (name)) {
				return Path.GetFileNameWithoutExtension (name);
			}
			return name;
		}

		string GetProjectReferenceName (string name)
		{
			string referenceName = GetReferenceName(name);
			return GetAssemblyShortName(referenceName);
		}

		string GetAssemblyShortName(string name)
		{
			string[] parts = name.Split(',');
			return parts[0];
		}

		bool HasDllOrExeFileExtension (string name)
		{
			string extension = Path.GetExtension (name);
			return
				IsMatchIgnoringCase (extension, ".dll") ||
				IsMatchIgnoringCase (extension, ".exe");
		}

		bool IsMatchIgnoringCase (string lhs, string rhs)
		{
			return String.Equals (lhs, rhs, StringComparison.InvariantCultureIgnoreCase);
		}

		public void RegisterProcessedFiles (IEnumerable<string> files)
		{
		}

		public void RemoveFile (string path)
		{
			GuiSyncDispatch (async () => {
				string fileName = GetFullPath (path);
				project.Files.Remove (fileName);
				await SaveAsync (project);
				LogDeletedFileInfo (path);
			});
		}

		void LogDeletedFileInfo (string path)
		{
			string fileName = Path.GetFileName (path);
			string directory = Path.GetDirectoryName (path);
			if (String.IsNullOrEmpty (directory)) {
				LogDeletedFile (fileName);
			} else {
				LogDeletedFileFromDirectory (fileName, directory);
			}
		}

		protected virtual void LogDeletedFile (string fileName)
		{
			DebugLogFormat ("Removed file '{0}'.", fileName);
		}

		protected virtual void LogDeletedFileFromDirectory (string fileName, string directory)
		{
			DebugLogFormat ("Removed file '{0}' from folder '{1}'.", fileName, directory);
		}

		public void RemoveImport (string targetFullPath)
		{
			GuiSyncDispatch (async () => {
				string relativeTargetPath = GetRelativePath (targetFullPath);
				project.RemoveImport (relativeTargetPath);
				RemoveImportWithForwardSlashes (targetFullPath);

				using (var updater = new EnsureNuGetPackageBuildImportsTargetUpdater ()) {
					updater.RemoveImport (relativeTargetPath);
					await SaveAsync (project);
				}

				packageManagementEvents.OnImportRemoved (new DotNetProjectProxy (project), relativeTargetPath);
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

		public void RemoveReference (string name)
		{
			GuiSyncDispatch (async () => {
				ProjectReference referenceProjectItem = FindReference (name);
				if (referenceProjectItem != null) {
					packageManagementEvents.OnReferenceRemoving (referenceProjectItem);
					project.References.Remove (referenceProjectItem);
					await SaveAsync (project);
					LogRemovedReferenceFromProject (referenceProjectItem);
				}
			});
		}

		void LogRemovedReferenceFromProject (ProjectReference referenceProjectItem)
		{
			LogRemovedReferenceFromProject (referenceProjectItem.Reference, ProjectName);
		}

		protected virtual void LogRemovedReferenceFromProject (string referenceName, string projectName)
		{
			DebugLogFormat ("Removed reference '{0}' from project '{1}'.", referenceName, projectName);
		}

		public string ResolvePath (string path)
		{
			return path;
		}

		public void SetNuGetProjectContext (INuGetProjectContext nuGetProjectContext)
		{
			NuGetProjectContext = nuGetProjectContext;
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

		static Task GuiSyncDispatchWithException (Func<Task> func)
		{
			if (Runtime.IsMainThread)
				throw new InvalidOperationException ("GuiSyncDispatch called from GUI thread");
			return Runtime.RunInMainThread (func);
		}

		public static void DefaultGuiSyncDispatcher (Action action)
		{
			Runtime.RunInMainThread (action).Wait ();
		}

		void GuiSyncDispatch (Func<Task> func)
		{
			guiSyncDispatcherFunc (func).Wait ();
		}

		static async Task SaveAsync (DotNetProject project)
		{
			using (var monitor = new ProgressMonitor ()) {
				await project.SaveAsync (monitor);
			}
		}
	}
}

