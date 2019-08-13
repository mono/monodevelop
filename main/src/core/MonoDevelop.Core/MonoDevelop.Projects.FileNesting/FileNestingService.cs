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
using System.Runtime.CompilerServices;
using Mono.Addins;

namespace MonoDevelop.Projects.FileNesting
{
	public static class FileNestingService
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
			if (!loadedProjects.TryGetValue (project, out var nestingInfo)) {
				nestingInfo = new ProjectNestingInfo (project);
				loadedProjects.Add (project, nestingInfo);
			}

			return nestingInfo;
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

		public static event Action<Project> NestingRulesChanged;

		internal static void NotifyNestingRulesChanged (Project project) => NestingRulesChanged?.Invoke (project);

		public static bool IsEnabledForProject (Project project)
		{
			return project?.ParentSolution?.UserProperties?.GetValue<bool> ("FileNesting", true) ?? true;
		}

		public static ProjectFile GetParentFile (ProjectFile inputFile)
		{
			if (!IsEnabledForProject (inputFile.Project)) {
				return null;
			}

			return GetProjectNestingInfo (inputFile.Project).GetParentForFile (inputFile);
		}

		public static bool HasChildren (ProjectFile inputFile)
		{
			if (!IsEnabledForProject (inputFile.Project)) {
				return false;
			}

			var children = GetProjectNestingInfo (inputFile.Project).GetChildrenForFile (inputFile);
			return (children?.Count ?? 0) > 0;
		}

		public static IEnumerable<ProjectFile> GetChildren (ProjectFile inputFile)
		{
			if (!IsEnabledForProject (inputFile.Project)) {
				return null;
			}

			return GetProjectNestingInfo (inputFile.Project).GetChildrenForFile (inputFile);
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

		void AddFile (ProjectFile projectFile)
		{
			var tmp = projectFiles.GetOrAdd (projectFile, new ProjectFileNestingInfo (projectFile));
			tmp.Parent = FileNestingService.InternalGetParentFile (projectFile);
			if (tmp.Parent != null) {
				var parent = projectFiles.GetOrAdd (tmp.Parent, new ProjectFileNestingInfo (tmp.Parent));
				if (parent.Children == null) {
					parent.Children = new ProjectFileCollection ();
				}
				if (!parent.Children.Contains (projectFile)) {
					parent.Children.Add (projectFile);
				}
			}

			projectFiles [projectFile] = tmp;
		}

		void OnFileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			foreach (var file in e) {
				AddFile (file.ProjectFile);
			}

			if (fileNestingEnabled)
				FileNestingService.NotifyNestingRulesChanged (Project);
		}

		void OnFileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			foreach (var file in e) {
				projectFiles.TryRemove (file.ProjectFile, out var nestingInfo);

				// Update parent
				if (nestingInfo?.Parent != null) {
					if (projectFiles.TryGetValue (nestingInfo.Parent, out var tmp)) {
						tmp.Children.Remove (file.ProjectFile);
					}
				}

				// Update children
				if (nestingInfo?.Children != null) {
					foreach (var child in nestingInfo.Children) {
						AddFile (child);
					}
				}
			}

			if (fileNestingEnabled)
				FileNestingService.NotifyNestingRulesChanged (Project);
		}

		void OnUserPropertiesChanged (object sender, Core.PropertyBagChangedEventArgs e)
		{
			bool newValue = FileNestingService.IsEnabledForProject (Project);
			if (fileNestingEnabled != newValue) {
				fileNestingEnabled = newValue;
				FileNestingService.NotifyNestingRulesChanged (Project);
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
			Project.ParentSolution.UserProperties.Changed -= OnUserPropertiesChanged;
		}
	}

}
