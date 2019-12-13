//
// WorkspaceFilesCache.cs
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
using MonoDevelop.Projects;
using Newtonsoft.Json;

namespace MonoDevelop.Ide.TypeSystem
{
	class WorkspaceFilesCache
	{
		internal const int format = 1;

		FilePath cacheDir;

		Dictionary<FilePath, List<ProjectCache>> cachedItems = new Dictionary<FilePath, List<ProjectCache>> ();
		Dictionary<string, FileLock> writeLockMap = new Dictionary<string, FileLock> ();
		bool loaded;

		public void Load (Solution solution)
		{
			if (loaded)
				return;

			if (solution == null || Runtime.PeekService<RootWorkspace> () == null)
				return;

			if (solution.FileName.IsNullOrEmpty)
				return;

			cacheDir = solution.GetPreferencesDirectory ().Combine ("project-cache");
			Directory.CreateDirectory (cacheDir);

			lock (cachedItems) {
				if (loaded)
					return;

				loaded = true;
				LoadCache (solution);
			}
		}

		void LoadCache (Solution sol)
		{
			var solConfig = sol.GetConfiguration (IdeServices.Workspace.ActiveConfiguration);
			if (solConfig == null)
				return;

			var serializer = new JsonSerializer ();
			foreach (var project in sol.GetAllProjects ()) {
				var config = solConfig.GetMappedConfiguration (project);

				var projectFilePath = project.FileName;
				string cacheFilePath = null;

				List<ProjectCache> cacheList = null;

				foreach (string framework in MonoDevelopWorkspace.GetFrameworks (project)) {
					try {
						cacheFilePath = GetProjectCacheFile (project, config, framework);

						if (!File.Exists (cacheFilePath))
							continue;

						using (var sr = File.OpenText (cacheFilePath)) {
							var value = (ProjectCache)serializer.Deserialize (sr, typeof (ProjectCache));

							if (format != value.Format)
								continue;

							value.Framework = framework;
							if (cacheList == null)
								cacheList = new List<ProjectCache> ();
							cacheList.Add (value);
						}
					} catch (Exception ex) {
						LoggingService.LogError ("Could not deserialize project cache", ex);
						TryDeleteFile (cacheFilePath);
						continue;
					}
				}

				if (cacheList != null)
					cachedItems [projectFilePath] = cacheList;
			}
		}

		internal IEnumerable<string> GetFrameworks (Project p)
		{
			if (p.HasMultipleTargetFrameworks && p is DotNetProject dotNetProject) {
				var frameworks = dotNetProject.TargetFrameworkMonikers;
				foreach (var framework in frameworks)
					yield return framework.ShortName;
			} else {
				yield return null;
			}
		}

		// We only care about invalid characters on Windows, as unix-systems only have `/` as invalid character.
		static readonly char [] invalidChars = Platform.IsWindows ? Path.GetInvalidFileNameChars () : Array.Empty<char> ();
		string GetProjectCacheFile (Project proj, string configuration, string framework)
		{
			string cacheId = string.IsNullOrEmpty (framework) ? configuration : configuration + "-" + framework;
			return cacheDir.Combine (proj.FileName.FileNameWithoutExtension + "-" + Sanitize (cacheId) + ".json");

			string Sanitize(string value)
			{
				if (invalidChars.Length == 0)
					return value;

				int lastIndex = 0;
				while ((lastIndex = value.IndexOfAny (invalidChars, lastIndex)) != -1) {
					value = value.Replace (value [lastIndex], '_');
				}

				return value;
			}
		}

		static void TryDeleteFile (string file)
		{
			try {
				File.Delete (file);
			} catch (Exception ex) {
				LoggingService.LogError ("Could not delete cache file", ex);
			}
		}

		public void Update (ProjectConfiguration projConfig, string framework, Project proj, MonoDevelopWorkspace.ProjectDataMap projectMap, ProjectCacheInfo info)
		{
			if (!loaded)
				return;

			var paths = new string [info.SourceFiles.Length];
			var actions = new string [info.SourceFiles.Length];

			for (int i = 0; i < info.SourceFiles.Length; ++i) {
				paths [i] = info.SourceFiles [i].FilePath;
				actions [i] = info.SourceFiles [i].BuildAction;
			}

			var projectRefs = new ReferenceItem [info.ProjectReferences.Length];
			for (int i = 0; i < info.ProjectReferences.Length; ++i) {
				var pr = info.ProjectReferences [i];
				(Project mdProject, string projectReferenceFramework) = projectMap.GetMonoProjectAndFramework (pr.ProjectId);
				projectRefs [i] = new ReferenceItem {
					FilePath = mdProject.FileName,
					Aliases = pr.Aliases.ToArray (),
					Framework = projectReferenceFramework
				};
			}

			var item = new ProjectCache {
				Format = format,
				AdditionalFiles = info.AdditionalFiles.Select (x => (string)x).ToArray (),
				Analyzers = info.AnalyzerFiles.Select (x => (string)x).ToArray (),
				EditorConfigFiles = info.EditorConfigFiles.Select (x => (string)x).ToArray (),
				Files = paths,
				BuildActions = actions,
				MetadataReferences = info.References.Select (x => {
					var ri = new ReferenceItem {
						FilePath = x.FilePath,
						Aliases = x.Properties.Aliases.ToArray (),
					};
					return ri;
				}).ToArray (),
				ProjectReferences = projectRefs,
			};

			string configId = GetConfigId (projConfig);
			var cacheFile = GetProjectCacheFile (proj, configId, framework);
			WriteCacheFile (item, cacheFile);
		}

		internal void WriteCacheFile (ProjectCache item, string cacheFile)
		{
			FileLock fileLock = AcquireWriteLock (cacheFile);
			try {
				lock (fileLock) {
					var serializer = new JsonSerializer ();
					using (var fs = File.Open (cacheFile, FileMode.Create))
					using (var sw = new StreamWriter (fs)) {
						serializer.Serialize (sw, item);
					}
				}
			} finally {
				ReleaseWriteLock (cacheFile, fileLock);
			}
		}

		string GetConfigId (ProjectConfiguration config)
		{
			if (string.IsNullOrEmpty (config.Platform))
				return config.Name;

			return config.Name + "|" + config.Platform;
		}

		public bool TryGetCachedItems (Project p, MonoDevelopMetadataReferenceManager provider, MonoDevelopWorkspace.ProjectDataMap projectMap, string framework,
			out ProjectCacheInfo info)
		{
			info = new ProjectCacheInfo ();

			List<ProjectCache> cachedDataList;

			lock (cachedItems) {
				if (!cachedItems.TryGetValue (p.FileName, out cachedDataList)) {
					return false;
				}
			}

			ProjectCache cachedData = cachedDataList.FirstOrDefault (cache => cache.Framework == framework);
			if (cachedData == null)
				return false;

			var filesBuilder = ImmutableArray.CreateBuilder<ProjectFile> (cachedData.Files.Length);
			for (int i = 0; i < cachedData.Files.Length; ++i) {
				filesBuilder.Add(new ProjectFile (cachedData.Files [i], cachedData.BuildActions [i]) {
					Project = p,
				});
			}

			info.SourceFiles = filesBuilder.MoveToImmutable ();

			info.AdditionalFiles = ToImmutableFilePathArray (cachedData.AdditionalFiles);
			info.AnalyzerFiles = ToImmutableFilePathArray (cachedData.Analyzers);
			info.EditorConfigFiles = ToImmutableFilePathArray (cachedData.EditorConfigFiles);

			var mrBuilder = ImmutableArray.CreateBuilder<MonoDevelopMetadataReference> (cachedData.MetadataReferences.Length);
			foreach (var item in cachedData.MetadataReferences) {
				var aliases = item.Aliases != null ? item.Aliases.ToImmutableArray () : default;
				var reference = provider.GetOrCreateMetadataReference (item.FilePath, new Microsoft.CodeAnalysis.MetadataReferenceProperties(aliases: aliases));
				mrBuilder.Add (reference);
			}
			info.References = mrBuilder.MoveToImmutable ();

			var sol = p.ParentSolution;
			var solConfig = sol.GetConfiguration (IdeServices.Workspace.ActiveConfiguration);
			var allProjects = sol.GetAllProjects ().ToDictionary (x => x.FileName, x => x);

			var prBuilder = ImmutableArray.CreateBuilder<Microsoft.CodeAnalysis.ProjectReference> (cachedData.ProjectReferences.Length);
			foreach (var item in cachedData.ProjectReferences) {
				if (!allProjects.TryGetValue (item.FilePath, out var mdProject))
					return false;

				var aliases = item.Aliases != null ? item.Aliases.ToImmutableArray () : default;
				var pr = new Microsoft.CodeAnalysis.ProjectReference (projectMap.GetOrCreateId (mdProject, null, item.Framework), aliases.ToImmutableArray ());
				prBuilder.Add (pr);
			}
			info.ProjectReferences = prBuilder.MoveToImmutable ();
			return true;
		}

		static ImmutableArray<FilePath> ToImmutableFilePathArray (string[] files)
		{
			if (files == null)
				return ImmutableArray<FilePath>.Empty;

			var builder = ImmutableArray.CreateBuilder<FilePath> (files.Length);
			foreach (var file in files)
				builder.Add (file);
			return builder.MoveToImmutable ();
		}

		/// <summary>
		/// Clears the in-memory cache for this project now that the loaded cache information has been used.
		/// Updates for the cache are written to disk.
		/// </summary>
		public bool OnCacheInfoUsed (Project p)
		{
			lock (cachedItems) {
				return cachedItems.Remove (p.FileName);
			}
		}

		void ReleaseWriteLock (string cacheFile, FileLock fileLock)
		{
			lock (writeLockMap) {
				fileLock.ReferenceCount--;
				if (fileLock.ReferenceCount == 0)
					writeLockMap.Remove (cacheFile);
			}
		}

		FileLock AcquireWriteLock (string cacheFile)
		{
			lock (writeLockMap) {
				FileLock fileLock = null;
				if (writeLockMap.TryGetValue (cacheFile, out fileLock)) {
					fileLock.ReferenceCount++;
					return fileLock;
				}

				fileLock = new FileLock {
					ReferenceCount = 1
				};
				writeLockMap [cacheFile] = fileLock;
				return fileLock;
			}
		}

		[Serializable]
		internal class ProjectCache
		{
			public int Format;

			public ReferenceItem [] ProjectReferences;
			public ReferenceItem[] MetadataReferences;
			public string [] Files;
			public string [] BuildActions;
			public string [] Analyzers;
			public string [] AdditionalFiles;
			public string [] EditorConfigFiles;

			internal string Framework;
		}

		[Serializable]
		internal class ReferenceItem
		{
			public string FilePath;
			public string [] Aliases = Array.Empty<string> ();
			public string Framework;
		}

		class FileLock
		{
			public int ReferenceCount;
		}
	}

	internal class ProjectCacheInfo : IEquatable<ProjectCacheInfo>
	{
		public ImmutableArray<ProjectFile> SourceFiles;
		public ImmutableArray<FilePath> AnalyzerFiles;
		public ImmutableArray<FilePath> AdditionalFiles;
		public ImmutableArray<FilePath> EditorConfigFiles;
		public ImmutableArray<MonoDevelopMetadataReference> References;
		public ImmutableArray<Microsoft.CodeAnalysis.ProjectReference> ProjectReferences;

		public override bool Equals (object obj)
		{
			if (obj is ProjectCacheInfo cacheInfo)
				return Equals (cacheInfo);
			return false;
		}

		public bool Equals (ProjectCacheInfo other)
		{
			if (other == null)
				return false;

			if (AdditionalFiles.Length != other.AdditionalFiles.Length ||
				AnalyzerFiles.Length != other.AnalyzerFiles.Length ||
				EditorConfigFiles.Length != other.EditorConfigFiles.Length ||
				SourceFiles.Length != other.SourceFiles.Length ||
				References.Length != other.References.Length ||
				ProjectReferences.Length != other.ProjectReferences.Length) {
				return false;
			}

			for (int i = 0; i < SourceFiles.Length; ++i) {
				var file = SourceFiles [i];
				var otherFile = other.SourceFiles [i];
				if (file.FilePath != otherFile.FilePath ||
					file.BuildAction != otherFile.BuildAction) {
					return false;
				}
			}

			if (!AreFilePathsEqual (AnalyzerFiles, other.AnalyzerFiles) ||
				!AreFilePathsEqual (AdditionalFiles, other.AdditionalFiles) ||
				!AreFilePathsEqual (EditorConfigFiles, other.EditorConfigFiles)) {
				return false;
			}

			for (int i = 0; i < References.Length; ++i) {
				var reference = References [i];
				var otherReference = other.References [i];
				if (reference.FilePath != otherReference.FilePath ||
					reference.Properties.Aliases.Length != otherReference.Properties.Aliases.Length) {
					return false;
				}

				for (int j = 0; j < reference.Properties.Aliases.Length; ++j) {
					if (reference.Properties.Aliases [j] != otherReference.Properties.Aliases [j]) {
						return false;
					}
				}
			}

			for (int i = 0; i < ProjectReferences.Length; ++i) {
				var reference = ProjectReferences [i];
				var otherReference = other.ProjectReferences [i];
				if (reference.ProjectId != otherReference.ProjectId ||
					reference.Aliases.Length != otherReference.Aliases.Length) {
					return false;
				}

				for (int j = 0; j < reference.Aliases.Length; ++j) {
					if (reference.Aliases [j] != otherReference.Aliases [j]) {
						return false;
					}
				}
			}

			return true;
		}

		static bool AreFilePathsEqual (ImmutableArray<FilePath> a, ImmutableArray<FilePath> b)
		{
			for (int i = 0; i < a.Length; ++i) {
				if (a [i] != b [i]) {
					return false;
				}
			}
			return true;
		}
	}
}
