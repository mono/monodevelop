//
// HackyWorkspaceFilesCache.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.FeatureConfiguration;
using MonoDevelop.Projects;
using Newtonsoft.Json;

namespace MonoDevelop.Ide.TypeSystem
{
	class HackyWorkspaceFilesCache
	{
		const int format = 1;
		readonly bool enabled = FeatureSwitchService.IsFeatureEnabled ("HackyCache").GetValueOrDefault ();

		readonly FilePath cacheDir;

		Dictionary<FilePath, ProjectCache> cachedItems = new Dictionary<FilePath, ProjectCache> ();

		public HackyWorkspaceFilesCache (Solution solution)
		{
			if (!IdeApp.IsInitialized || !enabled || solution == null)
				return;

			if (solution.FileName.IsNullOrEmpty)
				return;

			LoggingService.LogDebug ("HackyWorkspaceCache enabled");
			cacheDir = solution.GetPreferencesDirectory ().Combine ("hacky-project-cache");
			Directory.CreateDirectory (cacheDir);

			LoadCache (solution);
		}

		void LoadCache (Solution sol)
		{
			var solConfig = sol.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);

			var serializer = new JsonSerializer ();
			foreach (var project in sol.GetAllProjects ()) {
				var config = solConfig.GetMappedConfiguration (project);

				var projectFilePath = project.FileName;
				var cacheFilePath = GetProjectCacheFile (project, config);

				try {
					if (!File.Exists (cacheFilePath))
						continue;

					using (var sr = File.OpenText (cacheFilePath)) {
						var value = (ProjectCache)serializer.Deserialize (sr, typeof (ProjectCache));

						if (format != value.Format || value.TimeStamp != File.GetLastWriteTimeUtc (projectFilePath))
							continue;

						cachedItems [projectFilePath] = value;

					}
				} catch (Exception ex) {
					LoggingService.LogError ("Could not deserialize project cache", ex);
					TryDeleteFile (cacheFilePath);
					continue;
				}
			}
		}

		string GetProjectCacheFile (Project proj, string configuration)
		{
			return cacheDir.Combine (proj.FileName.FileNameWithoutExtension + "-" + configuration + ".json");
		}

		static void TryDeleteFile (string file)
		{
			try {
				File.Delete (file);
			} catch (Exception ex) {
				LoggingService.LogError ("Could not delete cache file", ex);
			}
		}

		public void Update (ProjectConfiguration projConfig, Project proj, MonoDevelopWorkspace.ProjectDataMap projectMap,
			ImmutableArray<ProjectFile> files,
			ImmutableArray<FilePath> analyzers,
			ImmutableArray<MonoDevelopMetadataReference> metadataReferences,
			ImmutableArray<Microsoft.CodeAnalysis.ProjectReference> projectReferences)
		{
			if (!enabled)
				return;

			var paths = new string [files.Length];
			var actions = new string [files.Length];

			for (int i = 0; i < files.Length; ++i) {
				paths [i] = files [i].FilePath;
				actions [i] = files [i].BuildAction;
			}

			var projectRefs = new ReferenceItem [projectReferences.Length];
			for (int i = 0; i < projectReferences.Length; ++i) {
				var pr = projectReferences [i];
				var mdProject = projectMap.GetMonoProject (pr.ProjectId);
				projectRefs [i] = new ReferenceItem {
					FilePath = mdProject.FileName,
					Aliases = pr.Aliases.ToArray (),
				};
			}

			var item = new ProjectCache {
				Format = format,
				Analyzers = analyzers.Select(x => (string)x).ToArray (),
				Files = paths,
				BuildActions = actions,
				TimeStamp = File.GetLastWriteTimeUtc (proj.FileName),
				MetadataReferences = metadataReferences.Select(x => {
					var ri = new ReferenceItem {
						FilePath = x.FilePath,
						Aliases = x.Properties.Aliases.ToArray (),
					};
					return ri;
				}).ToArray (),
				ProjectReferences = projectRefs,
			};

			var cacheFile = GetProjectCacheFile (proj, projConfig.Id);

			var serializer = new JsonSerializer ();
			using (var fs = File.Open (cacheFile, FileMode.Create))
			using (var sw = new StreamWriter (fs)) {
				serializer.Serialize (sw, item);
			}
		}


		public bool TryGetCachedItems (Project p, MonoDevelopMetadataReferenceManager provider, MonoDevelopWorkspace.ProjectDataMap projectMap,
			out ImmutableArray<ProjectFile> files,
			out ImmutableArray<FilePath> analyzers,
			out ImmutableArray<MonoDevelopMetadataReference> metadataReferences,
			out ImmutableArray<Microsoft.CodeAnalysis.ProjectReference> projectReferences)
		{
			files = ImmutableArray<ProjectFile>.Empty;
			analyzers = ImmutableArray<FilePath>.Empty;
			metadataReferences = ImmutableArray<MonoDevelopMetadataReference>.Empty;
			projectReferences = ImmutableArray<Microsoft.CodeAnalysis.ProjectReference>.Empty;

			if (!cachedItems.TryGetValue (p.FileName, out var cachedData)) {
				return false;
			}

			cachedItems.Remove (p.FileName);

			if (cachedData.TimeStamp != File.GetLastWriteTimeUtc (p.FileName))
				return false;

			var filesBuilder = ImmutableArray.CreateBuilder<ProjectFile> (cachedData.Files.Length);
			for (int i = 0; i < cachedData.Files.Length; ++i) {
				filesBuilder.Add(new ProjectFile (cachedData.Files [i], cachedData.BuildActions [i]) {
					Project = p,
				});
			}

			files = filesBuilder.MoveToImmutable ();

			var analyzersBuilder = ImmutableArray.CreateBuilder<FilePath> (cachedData.Analyzers.Length);
			foreach (var analyzer in cachedData.Analyzers)
				analyzersBuilder.Add (analyzer);
			analyzers = analyzersBuilder.MoveToImmutable ();

			var mrBuilder = ImmutableArray.CreateBuilder<MonoDevelopMetadataReference> (cachedData.MetadataReferences.Length);
			foreach (var item in cachedData.MetadataReferences) {
				var aliases = item.Aliases != null ? item.Aliases.ToImmutableArray () : default;
				var reference = provider.GetOrCreateMetadataReference (item.FilePath, new Microsoft.CodeAnalysis.MetadataReferenceProperties(aliases: aliases));
				mrBuilder.Add (reference);
			}
			metadataReferences = mrBuilder.MoveToImmutable ();

			var sol = p.ParentSolution;
			var solConfig = sol.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			var allProjects = sol.GetAllProjects ().ToDictionary (x => x.FileName, x => x);

			var prBuilder = ImmutableArray.CreateBuilder<Microsoft.CodeAnalysis.ProjectReference> (cachedData.ProjectReferences.Length);
			foreach (var item in cachedData.ProjectReferences) {
				if (!allProjects.TryGetValue (item.FilePath, out var mdProject))
					return false;

				var aliases = item.Aliases != null ? item.Aliases.ToImmutableArray () : default;
				var pr = new Microsoft.CodeAnalysis.ProjectReference (projectMap.GetOrCreateId (mdProject, null), aliases.ToImmutableArray ());
				prBuilder.Add (pr);
			}
			projectReferences = prBuilder.MoveToImmutable ();
			return true;
		}

		[Serializable]
		class ProjectCache
		{
			public int Format;
			public DateTime TimeStamp;

			public ReferenceItem [] ProjectReferences;
			public ReferenceItem[] MetadataReferences;
			public string [] Files;
			public string [] BuildActions;
			public string [] Analyzers;
		}

		[Serializable]
		internal class ReferenceItem
		{
			public string FilePath;
			public string [] Aliases = Array.Empty<string> ();
		}
	}
}
