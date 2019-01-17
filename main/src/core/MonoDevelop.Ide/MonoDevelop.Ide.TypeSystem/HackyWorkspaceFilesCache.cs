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
		readonly bool? enabled = FeatureSwitchService.IsFeatureEnabled ("HackyCache");

		readonly FilePath cacheDir;

		Dictionary<FilePath, ProjectCache> cachedItems = new Dictionary<FilePath, ProjectCache> ();

		public HackyWorkspaceFilesCache (Solution solution)
		{
			if (!IdeApp.IsInitialized || enabled.GetValueOrDefault() == false || solution == null)
				return;

			cacheDir = solution.GetPreferencesDirectory ().Combine ("hacky-project-cache");
			Directory.CreateDirectory (cacheDir);

			LoadCache (solution);
		}

		void LoadCache (Solution sol)
		{
			var solConfig = sol.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			var allProjects = sol.GetAllProjects ().ToDictionary (x => x.FileName, x => x);

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

						var cachedStamp = value.TimeStamp;
						if (cachedStamp == File.GetLastWriteTimeUtc (projectFilePath))
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

		public void Update (ProjectConfiguration projConfig, Project proj, ImmutableArray<ProjectFile> files, ImmutableArray<FilePath> analyzers)
		{
			if (enabled.GetValueOrDefault() == false)
				return;

			var paths = new string [files.Length];
			var actions = new string [files.Length];

			for (int i = 0; i < files.Length; ++i) {
				paths [i] = files [i].FilePath;
				actions [i] = files [i].BuildAction;
			}

			var item = new ProjectCache {
				Analyzers = analyzers.Select(x => (string)x).ToArray (),
				Files = paths,
				BuildActions = actions,
				TimeStamp = File.GetLastWriteTimeUtc (proj.FileName),
			};
			cachedItems [proj.FileName] = item;

			var cacheFile = GetProjectCacheFile (proj, projConfig.Id);

			var serializer = new JsonSerializer ();
			using (var fs = File.Open (cacheFile, FileMode.Create))
			using (var sw = new StreamWriter (fs)) {
				serializer.Serialize (sw, item);
			}
		}

		public bool TryGetCachedItems (Project p, out ProjectFile[] files, out string[] analyzers)
		{
			files = null;
			analyzers = null;

			if (!cachedItems.TryGetValue (p.FileName, out var cachedData)) {
				return false;
			}

			cachedItems.Remove (p.FileName);

			if (cachedData.TimeStamp != File.GetLastWriteTimeUtc (p.FileName))
				return false;

			files = new ProjectFile [cachedData.Files.Length];
			for (int i = 0; i < cachedData.Files.Length; ++i) {
				files [i] = new ProjectFile (cachedData.Files [i], cachedData.BuildActions [i]) {
					Project = p,
				};
			}

			analyzers = cachedData.Analyzers;
			return true;
		}

		[Serializable]
		class ProjectCache
		{
			public DateTime TimeStamp;

			public string [] Files;
			public string [] BuildActions;
			public string [] Analyzers;
		}
	}
}
