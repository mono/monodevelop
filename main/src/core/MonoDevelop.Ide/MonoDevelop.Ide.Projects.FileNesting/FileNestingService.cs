//
// FileNestingService.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 Microsoft, Inc. (http://microsoft.com)
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Addins;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects.FileNesting
{
	internal static class FileNestingService
	{
		static readonly ConditionalWeakTable<Project, ProjectNestingInfo> loadedProjects = new ConditionalWeakTable<Project, ProjectNestingInfo> ();
		static ImmutableList<NestingRulesProvider> rulesProviders = ImmutableList<NestingRulesProvider>.Empty;

		static FileNestingService ()
		{
			AddinManager.AddExtensionNodeHandler (typeof (NestingRulesProvider), HandleRulesProviderExtension);
		}

		static void HandleRulesProviderExtension (object sender, ExtensionNodeEventArgs args)
		{
			var pr = args.ExtensionObject as NestingRulesProvider;
			if (pr != null) {
				if (args.Change == ExtensionChange.Add && !rulesProviders.Contains (pr)) {
					rulesProviders = rulesProviders.Add (pr);
				} else {
					rulesProviders = rulesProviders.Remove (pr);
				}
			}
		}

		public static bool HasParent (ProjectFile inputFile) => GetParentFile (inputFile) != null;

		static ProjectNestingInfo GetProjectNestingInfo (Project project)
		{
			return loadedProjects.GetValue (project, p => new ProjectNestingInfo (p));
		}

		internal static ProjectFile InternalGetParentFile (ProjectFile inputFile)
		{
			foreach (var rp in rulesProviders) {
				var parentFile = rp.GetParentFile (inputFile);
				if (parentFile != null) {
					return parentFile;
				}
			}

			return null;
		}

		/// <summary>
		/// The project file which had modifications, in most cases, the parent of the added/removed projectfile
		/// </summary>
		public static event Action<ProjectFile, ProjectFile> NestingRulesChanged;

		internal static void NotifyNestingRulesChanged (ProjectFile toUpdate, ProjectFile newParent) => NestingRulesChanged?.Invoke (toUpdate, newParent);

		public static bool IsEnabledForProject (Project project)
		{
			return project?.ParentSolution?.UserProperties?.GetValue<bool> ("MonoDevelop.Ide.FileNesting.Enabled", true) ?? true;
		}

		public static bool AppliesToProject (Project project)
		{
			return rulesProviders.Any (rp => rp.AppliesToProject (project));
		}

		public static ProjectFile GetParentFile (ProjectFile inputFile)
		{
			if (!AppliesToProject (inputFile.Project)) {
				return null;
			}
			return GetProjectNestingInfo (inputFile.Project).GetParentForFile (inputFile);
		}

		public static bool HasChildren (ProjectFile inputFile)
		{
			if (!AppliesToProject (inputFile.Project)) {
				return false;
			}
			var children = GetProjectNestingInfo (inputFile.Project).GetChildrenForFile (inputFile);
			return (children?.Count ?? 0) > 0;
		}

		public static ProjectFileCollection GetChildren (ProjectFile inputFile)
		{
			if (!AppliesToProject (inputFile.Project)) {
				return null;
			}
			return GetProjectNestingInfo (inputFile.Project).GetChildrenForFile (inputFile);
		}

		public static IEnumerable<ProjectFile> GetDependentOrNestedChildren (ProjectFile inputFile)
		{
			if (inputFile.HasChildren)
				return inputFile.DependentChildren;

			return GetChildren (inputFile);
		}

		public static IEnumerable<ProjectFile> GetDependentOrNestedTree (ProjectFile inputFile)
		{
			if (inputFile.HasChildren) {
				foreach (var dep in inputFile.DependentChildren) {
					yield return dep;
				}
			} else {
				var children = GetChildren (inputFile);
				if (children != null && children.Count > 0) {
					var stack = new Stack<ProjectFile> (children);
					while (stack.Count > 0) {
						var current = stack.Pop ();
						yield return current;

						children = GetChildren (current);
						if (children != null) {
							foreach (var child in children) {
								stack.Push (child);
							}
						}
					}
				}
			}
		}
	}

	sealed class ProjectNestingInfo : IDisposable
	{
		sealed class ProjectFileNestingInfo
		{
			public ProjectFile File { get; }
			public ProjectFile Parent { get; set; }
			public ProjectFileCollection Children { get; set; }

			public ProjectFileNestingInfo (ProjectFile projectFile)
			{
				File = projectFile;
			}
		}

		readonly ConcurrentDictionary<ProjectFile, ProjectFileNestingInfo> projectFiles = new ConcurrentDictionary<ProjectFile, ProjectFileNestingInfo> ();
		bool fileNestingEnabled;

		public Project Project { get; }

		public ProjectNestingInfo (Project project)
		{
			Project = project;
			Project.FileAddedToProject += OnFileAddedToProject;
			Project.FileRemovedFromProject += OnFileRemovedFromProject;
			Project.FileRenamedInProject += OnFileRenamedInProject;
			Project.ParentSolution.UserProperties.Changed += OnUserPropertiesChanged;
		}

		bool initialized = false;
		void EnsureInitialized ()
		{
			if (!initialized) {
				initialized = true;

				fileNestingEnabled = FileNestingService.IsEnabledForProject (Project);
				foreach (var file in Project.Files) {
					AddFile (file);
				}
			}
		}

		ProjectFileNestingInfo AddFile (ProjectFile projectFile)
		{
			var tmp = projectFiles.GetOrAdd (projectFile, new ProjectFileNestingInfo (projectFile));
			tmp.Parent = FileNestingService.InternalGetParentFile (projectFile);
			if (tmp.Parent != null) {
				var parent = projectFiles.GetOrAdd (tmp.Parent, new ProjectFileNestingInfo (tmp.Parent));
				if (parent.Children == null) {
					parent.Children = new ProjectFileCollection ();
				}
				if (parent.Children.GetFile (projectFile.FilePath) == null) {
					parent.Children.Add (projectFile);
				}
			}

			projectFiles [projectFile] = tmp;
			return tmp;
		}

		ProjectFileNestingInfo RemoveFile (ProjectFile projectFile, List<ProjectFile> originalFilesToRemove = null)
		{
			bool actuallyRemoveFiles = originalFilesToRemove == null;

			projectFiles.TryRemove (projectFile, out var nestingInfo);

			// Update parent
			if (nestingInfo?.Parent != null) {
				if (projectFiles.TryGetValue (nestingInfo.Parent, out var tmp)) {
					tmp.Children.Remove (projectFile);
				}
			}

			// Update children
			if (nestingInfo?.Children != null) {
				foreach (var child in nestingInfo.Children) {
					if (originalFilesToRemove == null) {
						originalFilesToRemove = new List<ProjectFile> ();
					}
					originalFilesToRemove.Add (child);
					RemoveFile (child, originalFilesToRemove);
				}
			}

			if (actuallyRemoveFiles && originalFilesToRemove != null) {
				Project.Files.RemoveRange (originalFilesToRemove);
			}

			return nestingInfo;
		}

		void OnFileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			foreach (var file in e) {
				var nestingInfo = AddFile (file.ProjectFile);
				NotifyNestingRulesChanged (nestingInfo);
			}
		}

		void OnFileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			foreach (var file in e) {
				var nestingInfo = RemoveFile (file.ProjectFile);
				NotifyNestingRulesChanged (nestingInfo);
			}
		}

		void OnFileRenamedInProject (object sender, ProjectFileRenamedEventArgs e)
		{
			foreach (var file in e) {
				var oldNestingInfo = RemoveFile (file.ProjectFile);
				NotifyNestingRulesChanged (oldNestingInfo);

				var nestingInfo = AddFile (file.ProjectFile);
				NotifyNestingRulesChanged (nestingInfo);
			}
		}

		void NotifyNestingRulesChanged (ProjectFileNestingInfo nestingInfo)
		{
			if (nestingInfo != null && fileNestingEnabled) {
				FileNestingService.NotifyNestingRulesChanged (nestingInfo.File, nestingInfo.Parent);
			}
		}

		void OnUserPropertiesChanged (object sender, Core.PropertyBagChangedEventArgs e)
		{
			bool newValue = FileNestingService.IsEnabledForProject (Project);
			if (fileNestingEnabled != newValue) {
				fileNestingEnabled = newValue;
				foreach (var kvp in projectFiles) {
					var nestingInfo = kvp.Value;
					if (nestingInfo.Children != null) {
						FileNestingService.NotifyNestingRulesChanged (nestingInfo.File, null);
					}
				}
			}
		}

		public ProjectFile GetParentForFile (ProjectFile inputFile)
		{
			EnsureInitialized ();
			if (!fileNestingEnabled)
				return null;

			return projectFiles.TryGetValue (inputFile, out var nestingInfo) ? nestingInfo.Parent : null;
		}

		public ProjectFileCollection GetChildrenForFile (ProjectFile inputFile)
		{
			EnsureInitialized ();
			if (!fileNestingEnabled)
				return null;

			return projectFiles.TryGetValue (inputFile, out var nestingInfo) ? nestingInfo.Children : null;
		}

		public void Dispose ()
		{
			Project.FileAddedToProject -= OnFileAddedToProject;
			Project.FileRemovedFromProject -= OnFileRemovedFromProject;
			Project.FileRenamedInProject -= OnFileRenamedInProject;
			Project.ParentSolution.UserProperties.Changed -= OnUserPropertiesChanged;

			projectFiles?.Clear ();
		}
	}

}
