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
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Newtonsoft.Json;

namespace MonoDevelop.Ide.TypeSystem
{
	class HackyWorkspaceFilesCache
	{
		readonly FilePath cacheDir;
		readonly FilePath mappingFile;

		TypesystemCache controlCache = new TypesystemCache ();
		Dictionary<FilePath, ProjectCache> cachedItems = new Dictionary<FilePath, ProjectCache> ();

		public HackyWorkspaceFilesCache (Solution solution, ConfigurationSelector configuration)
		{
			if (!IdeApp.IsInitialized)
				return;

			cacheDir = solution.GetPreferencesDirectory ().Combine ("project-cache");
			Directory.CreateDirectory (cacheDir);

			mappingFile = cacheDir.Combine ("project-map.json");

			LoadCache (solution);
		}

		void LoadCache (Solution sol)
		{
			try {
				var serializer = new JsonSerializer ();
				using (var sr = File.OpenText (mappingFile)) {
					controlCache = (TypesystemCache)serializer.Deserialize (sr, typeof (TypesystemCache));

					var lastWrite = File.GetLastWriteTimeUtc (sol.FileName);
					if (lastWrite != controlCache.SolutionTimestamp)
						return;

					var solConfig = sol.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
					var allProjects = sol.GetAllProjects ().ToDictionary (x => x.FileName, x => x);

					foreach (var project in controlCache.ProjectMap) {
						if (!allProjects.TryGetValue (project.ProjectFilePath, out var mdProject))
							continue;

						var config = solConfig.GetMappedConfiguration (mdProject);
						if (config != project.Configuration)
							continue;

						LoadProjectCacheItem (mdProject.FileName, project.CacheFilePath);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not deserialize typesystem cache", ex);
				TryDeleteFile (mappingFile);
				return;
			}
		}

		void LoadProjectCacheItem (FilePath projectFilePath, FilePath cacheFilePath)
		{
			var serializer = new JsonSerializer ();

			try {
				using (var sr = File.OpenText (cacheFilePath)) {
					cachedItems [projectFilePath] = (ProjectCache)serializer.Deserialize (sr, typeof (ProjectCache));
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not deserialize project cache", ex);
				TryDeleteFile (cacheFilePath);
				return;
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

		public void Update (Project proj, string[] files, string[] analyzers)
		{
			cachedItems [proj.FileName] = new ProjectCache {
				Analyzers = analyzers,
				Files = files,
				TimeStamp = File.GetLastWriteTimeUtc (proj.FileName),
			};
		}

		public void Write (Solution sol)
		{
			var serializer = new JsonSerializer ();

			// Write control file
			using (var fs = File.Open (mappingFile, FileMode.Create))
			using (var sw = new StreamWriter (fs)) {
				serializer.Serialize (sw, controlCache);
			}

			// Write cache files
			var solConfig = sol.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			var allProjects = sol.GetAllProjects ().ToDictionary (x => x.FileName, x => x);

			foreach (var item in cachedItems) {
				var projectFile = item.Key;
				var mdProject = allProjects [projectFile];
				var cacheFile = GetProjectCacheFile (mdProject, solConfig.GetMappedConfiguration (mdProject));

				using (var fs = File.Open (cacheFile, FileMode.Create))
				using (var sw = new StreamWriter (fs)) {
					serializer.Serialize (sw, controlCache);
				}
			}
		}

		public bool TryGetCachedItems (Project p, out string[] files, out string[] analyzers)
		{
			files = analyzers = null;

			if (!cachedItems.TryGetValue (p.FileName, out var cachedData)) {
				return false;
			}

			files = cachedData.Files;
			analyzers = cachedData.Analyzers;
			return true;
		}

		[Serializable]
		class TypesystemCache
		{
			public DateTime SolutionTimestamp;

			public ProjectMapping [] ProjectMap = Array.Empty<ProjectMapping> ();
		}

		[Serializable]
		class ProjectMapping
		{
			public string ProjectFilePath;
			public string Configuration;
			public string CacheFilePath;
		}

		[Serializable]
		class ProjectCache
		{
			public DateTime TimeStamp;

			public string [] Files;
			public string [] Analyzers;
		}
	}
}
