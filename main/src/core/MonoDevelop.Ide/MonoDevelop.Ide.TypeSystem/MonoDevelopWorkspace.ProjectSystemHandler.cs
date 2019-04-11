//
// MonoDevelopWorkspace.ProjectSystemHandler.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Projection;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class ProjectSystemHandler : IDisposable
		{
			readonly MonoDevelopWorkspace workspace;
			readonly ProjectDataMap projectMap;
			readonly ProjectionData projections;
			readonly Lazy<MetadataReferenceHandler> metadataHandler;
			readonly Lazy<HostDiagnosticUpdateSource> hostDiagnosticUpdateSource;
			WorkspaceFilesCache workspaceCache;
			readonly List<MonoDevelopAnalyzer> analyzersToDispose = new List<MonoDevelopAnalyzer> ();
			IDisposable persistentStorageLocationServiceRegistration;

			bool added;
			readonly object addLock = new object ();

			SolutionData solutionData;

			public ProjectSystemHandler (MonoDevelopWorkspace workspace, ProjectDataMap projectMap, ProjectionData projections)
			{
				this.workspace = workspace;
				this.projectMap = projectMap;
				this.projections = projections;
				this.workspaceCache = new WorkspaceFilesCache ();

				metadataHandler = new Lazy<MetadataReferenceHandler> (() => new MetadataReferenceHandler (workspace.MetadataReferenceManager, projectMap));
				hostDiagnosticUpdateSource = new Lazy<HostDiagnosticUpdateSource> (() => new HostDiagnosticUpdateSource (workspace, workspace.compositionManager.GetExportedValue<IDiagnosticUpdateSourceRegistrationService> ()));

				var persistentStorageLocationService = (MonoDevelopPersistentStorageLocationService)workspace.Services.GetService<IPersistentStorageLocationService> ();
				if (workspace.MonoDevelopSolution != null)
					persistentStorageLocationServiceRegistration = persistentStorageLocationService.RegisterPrimaryWorkspace (workspace.Id);
			}

			#region Solution mapping
			// No need to remove mappings, it's handled on workspace dispose.
			readonly Dictionary<MonoDevelop.Projects.Solution, SolutionId> solutionIdMap = new Dictionary<MonoDevelop.Projects.Solution, SolutionId> ();

			internal SolutionId GetSolutionId (MonoDevelop.Projects.Solution solution)
			{
				if (solution == null)
					throw new ArgumentNullException (nameof (solution));

				lock (solutionIdMap) {
					if (!solutionIdMap.TryGetValue (solution, out SolutionId result)) {
						solutionIdMap [solution] = result = SolutionId.CreateNewId (solution.Name);
					}
					return result;
				}
			}
			#endregion

			class SolutionData
			{
				public ConcurrentDictionary<string, TextLoader> Files = new ConcurrentDictionary<string, TextLoader> ();
			}

			internal async Task<ProjectInfo> LoadProject (MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.Project oldProject, ProjectCacheInfo cacheInfo)
			{
				var projectId = projectMap.GetOrCreateId (p, oldProject);

				var config = GetDotNetProjectConfiguration (p);
				MonoDevelop.Projects.DotNetCompilerParameters cp = config?.CompilationParameters;
				FilePath fileName = IdeApp.IsInitialized ? p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration) : (FilePath)"";

				if (fileName.IsNullOrEmpty) {
					fileName = new FilePath (p.Name + ".dll");
				}

				if (cacheInfo == null) {
					cacheInfo = await LoadProjectCacheInfo (p, config, token).ConfigureAwait (false);
					if (token.IsCancellationRequested)
						return null;

					if (config != null)
						workspaceCache.Update (config, p, projectMap, cacheInfo);
				}

				if (token.IsCancellationRequested)
					return null;

				var loader = workspace.Services.GetService<IAnalyzerService> ().GetLoader ();

				ProjectData projectData, oldProjectData;
				List<DocumentInfo> mainDocuments, additionalDocuments;
				try {
					await workspace.LoadLock.WaitAsync ().ConfigureAwait (false);
					//when reloading e.g. after a save, preserve document IDs
					oldProjectData = projectMap.RemoveData (projectId);
					projectData = projectMap.CreateData (projectId, cacheInfo.References);

					var documents = await CreateDocuments (projectData, p, token, cacheInfo.SourceFiles, oldProjectData).ConfigureAwait (false);
					if (documents == null)
						return null;

					mainDocuments = documents.Item1;
					additionalDocuments = documents.Item2;
				} finally {
					workspace.LoadLock.Release ();
				}

				// TODO: Pass in the WorkspaceMetadataFileReferenceResolver
				var info = ProjectInfo.Create (
					projectId,
					GetVersionStamp (p),
					p.Name,
					fileName.FileNameWithoutExtension,
					(p as MonoDevelop.Projects.DotNetProject)?.RoslynLanguageName ?? LanguageNames.CSharp,
					p.FileName,
					fileName,
					cp?.CreateCompilationOptions (),
					cp?.CreateParseOptions (config),
					mainDocuments,
					cacheInfo.ProjectReferences,
					cacheInfo.References.Select (x => x.CurrentSnapshot),
					analyzerReferences: cacheInfo.AnalyzerFiles.SelectAsArray (x => {
						var analyzer = new MonoDevelopAnalyzer (x, hostDiagnosticUpdateSource.Value, projectId, workspace, loader, LanguageNames.CSharp);
						analyzersToDispose.Add (analyzer);
						return analyzer.GetReference ();
					}),
					additionalDocuments: additionalDocuments
				);
				return info;
			}

			VersionStamp GetVersionStamp (MonoDevelop.Projects.Project project)
			{
				try {
					return VersionStamp.Create (File.GetLastWriteTimeUtc (project.FileName));
				} catch (Exception e) {
					LoggingService.LogInternalError ("Failed to create version stamp", e);
					return VersionStamp.Create ();
				}
			}

			static DotNetProjectConfiguration GetDotNetProjectConfiguration (MonoDevelop.Projects.Project p)
			{
				var workspace = Runtime.PeekService<RootWorkspace> ();
				return workspace != null ? p.GetConfiguration (workspace.ActiveConfiguration) as MonoDevelop.Projects.DotNetProjectConfiguration : p.DefaultConfiguration as MonoDevelop.Projects.DotNetProjectConfiguration;
			}

			async Task<ProjectCacheInfo> LoadProjectCacheInfo (MonoDevelop.Projects.Project p, DotNetProjectConfiguration config, CancellationToken token)
			{
				await TypeSystemService.FreezeLoad ().ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;

				var (references, projectReferences) = await metadataHandler.Value.CreateReferences (p, token).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;

				await TypeSystemService.FreezeLoad ().ConfigureAwait (false);
				var sourceFiles = await p.GetSourceFilesAsync (config?.Selector).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;

				await TypeSystemService.FreezeLoad ().ConfigureAwait (false);
				var analyzerFiles = await p.GetAnalyzerFilesAsync (config?.Selector).ConfigureAwait (false);

				return new ProjectCacheInfo {
					AnalyzerFiles = analyzerFiles,
					SourceFiles = sourceFiles,
					ProjectReferences = projectReferences,
					References = references
				};
			}

			async Task<ConcurrentBag<ProjectInfo>> CreateProjectInfos (IEnumerable<MonoDevelop.Projects.Project> mdProjects, CancellationToken token)
			{
				var projects = new ConcurrentBag<ProjectInfo> ();
				var allTasks = new List<Task> ();

				foreach (var proj in mdProjects) {
					if (token.IsCancellationRequested)
						return null;
					if (!CanLoadProject (proj))
						continue;
					var tp = LoadProject (proj, token, null, null).ContinueWith (t => {
						if (!t.IsCanceled)
							projects.Add (t.Result);
					});
					allTasks.Add (tp);

				}
				await Task.WhenAll (allTasks.ToArray ()).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;

				return projects;
			}

			/// <summary>
			/// TODO: Logic is very similar to CreateProjectInfos - can we reduce the duplication?
			/// </summary>
			async Task<ConcurrentBag<ProjectInfo>> CreateProjectInfosFromCache (IEnumerable<MonoDevelop.Projects.Project> mdProjects, CancellationToken token)
			{
				var projects = new ConcurrentBag<ProjectInfo> ();
				var allTasks = new List<Task> ();

				foreach (var proj in mdProjects) {
					if (token.IsCancellationRequested)
						return null;
					if (!CanLoadProject (proj))
						continue;
					if (!workspaceCache.TryGetCachedItems (proj, workspace.MetadataReferenceManager, projectMap, out var cacheInfo))
						continue;

					var tp = LoadProject (proj, token, null, cacheInfo).ContinueWith (t => {
						if (!t.IsCanceled)
							projects.Add (t.Result);
					});
					allTasks.Add (tp);
				}

				if (allTasks.Count == 0) {
					return null;
				}

				await Task.WhenAll (allTasks.ToArray ()).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;

				return projects;
			}

			internal bool CanLoadProject (MonoDevelop.Projects.Project project)
			{
				return (project is DotNetProject dotNetProject) && dotNetProject.SupportsRoslyn;
			}

			bool IsModifiedWhileLoading (MonoDevelop.Projects.Solution solution)
			{
				List<MonoDevelop.Projects.DotNetProject> modifiedWhileLoading;
				lock (workspace.projectModifyLock) {
					modifiedWhileLoading = workspace.modifiedProjects;
					workspace.modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
				}

				foreach (var project in modifiedWhileLoading) {
					// TODO: Maybe optimize this so we don't do O(n^2)
					if (solution.ContainsItem (project)) {
						return true;
					}
				}
				return false;
			}

			internal async Task<ProjectInfo> LoadProjectIfCacheOutOfDate (MonoDevelop.Projects.Project p, CancellationToken token)
			{
				if (!workspaceCache.TryGetCachedItems (p, workspace.MetadataReferenceManager, projectMap, out var cacheInfo)) {
					// No cached info need to load the project
					return await LoadProject (p, token, null, null).ConfigureAwait (false);
				}

				var config = GetDotNetProjectConfiguration (p);
				var updatedCacheInfo = await LoadProjectCacheInfo (p, config, token).ConfigureAwait (false);
				if (updatedCacheInfo == null)
					return null;

				workspaceCache.OnCacheInfoUsed (p);

				if (updatedCacheInfo.Equals (cacheInfo))
					return null;

				// Update cache.
				if (config != null)
					workspaceCache.Update (config, p, projectMap, updatedCacheInfo);

				return await LoadProject (p, token, null, updatedCacheInfo).ConfigureAwait (false);
			}

			/// <summary>
			/// This takes the modified projects that occurred during the solution load and re-plays
			/// the modifications to the workspace as though they occurred after the load so any updated
			/// references due to a NuGet restore are made available to the type system.
			/// </summary>
			void ReloadModifiedProjects ()
			{
				lock (workspace.projectModifyLock) {
					if (!workspace.modifiedProjects.Any ())
						return;
					var modifiedWhileLoading = workspace.modifiedProjects;
					workspace.modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
					foreach (var project in modifiedWhileLoading) {
						var args = new MonoDevelop.Projects.SolutionItemModifiedEventArgs (project, "References");
						workspace.OnProjectModified (project, args);
					}
				}
			}

			/// <summary>
			/// This checks that the new project was modified whilst it was being added to the
			/// workspace and re-plays project modifications to the workspace as though they
			/// happened after the project was added. This ensures any updated references due to
			/// a NuGet restore are made available to the type system.
			/// </summary>
			internal void ReloadModifiedProject (MonoDevelop.Projects.Project project)
			{
				lock (workspace.projectModifyLock) {
					if (!workspace.modifiedProjects.Any ())
						return;

					int removed = workspace.modifiedProjects.RemoveAll (p => p == project);
					if (removed > 0) {
						var args = new MonoDevelop.Projects.SolutionItemModifiedEventArgs (project, "References");
						workspace.OnProjectModified (project, args);
					}
				}
			}

			internal Task<(MonoDevelop.Projects.Solution, SolutionInfo)> CreateSolutionInfo (MonoDevelop.Projects.Solution sol, CancellationToken ct)
			{
				return Task.Run (delegate {
					return CreateSolutionInfoInternal (sol, ct);
				});

				async Task<(MonoDevelop.Projects.Solution, SolutionInfo)> CreateSolutionInfoInternal (MonoDevelop.Projects.Solution solution, CancellationToken token)
				{
					using (var timer = Counters.AnalysisTimer.BeginTiming ()) {
						projections.ClearOldProjectionList ();

						solutionData = new SolutionData ();

						var projectInfos = await CreateProjectInfos (solution.GetAllProjects (), token).ConfigureAwait (false);
						if (IsModifiedWhileLoading (solution)) {
							return await CreateSolutionInfoInternal (solution, token).ConfigureAwait (false);
						}

						if (token.IsCancellationRequested)
							return (solution, null);

						var solutionId = GetSolutionId (solution);
						var solutionInfo = SolutionInfo.Create (solutionId, VersionStamp.Create (), solution.FileName, projectInfos);

						lock (addLock) {
							if (!added) {
								added = true;
								OnSolutionOpened (workspace, solutionInfo);
							}
						}
						// Check for modified projects here after the solution has been added to the workspace
						// in case a NuGet package restore finished after the IsModifiedWhileLoading check. This
						// ensures the type system does not have missing references that may have been added by
						// the restore.
						ReloadModifiedProjects ();
						return (solution, solutionInfo);
					}
				}
			}

			internal Task<(MonoDevelop.Projects.Solution, SolutionInfo)> CreateSolutionInfoFromCache (MonoDevelop.Projects.Solution sol, CancellationToken ct)
			{
				return Task.Run (delegate {
					return CreateSolutionInfoFromCacheInternal (sol, ct);
				});

				async Task<(MonoDevelop.Projects.Solution, SolutionInfo)> CreateSolutionInfoFromCacheInternal (MonoDevelop.Projects.Solution solution, CancellationToken token)
				{
					projections.ClearOldProjectionList ();
					solutionData = new SolutionData ();

					workspaceCache.Load (solution);

					var projectInfos = await CreateProjectInfosFromCache (solution.GetAllProjects (), token).ConfigureAwait (false);
					if (projectInfos == null)
						return (solution, null);

					if (token.IsCancellationRequested)
						return (solution, null);

					var solutionId = GetSolutionId (solution);
					var solutionInfo = SolutionInfo.Create (solutionId, VersionStamp.Create (), solution.FileName, projectInfos);

					lock (addLock) {
						if (!added) {
							added = true;
							OnSolutionOpened (workspace, solutionInfo);
						}
					}

					// Clear modified projects during load. The projects will be loaded later.
					lock (workspace.projectModifyLock) {
						workspace.modifiedProjects.RemoveAll (p => p.ParentSolution == solution);
					}

					return (solution, solutionInfo);
				}
			}

			void OnSolutionOpened (MonoDevelopWorkspace workspace, SolutionInfo solutionInfo)
			{
				workspace.OnSolutionAdded (solutionInfo);
				
				var service = (MonoDevelopPersistentStorageLocationService)workspace.Services.GetService<IPersistentStorageLocationService> ();
				service.SetupSolution (workspace);

				Runtime.RunInMainThread (IdeServices.TypeSystemService.UpdateRegisteredOpenDocuments);
			}

			static bool CanGenerateAnalysisContextForNonCompileable (MonoDevelop.Projects.Project p, MonoDevelop.Projects.ProjectFile f)
			{
				var mimeType = IdeServices.DesktopService.GetMimeTypeForUri (f.FilePath);
				var node = IdeApp.TypeSystemService.GetTypeSystemParserNode (mimeType, f.BuildAction);
				if (node?.Parser == null)
					return false;
				return node.Parser.CanGenerateAnalysisDocument (mimeType, f.BuildAction, p.SupportedLanguages);
			}

			async Task<Tuple<List<DocumentInfo>, List<DocumentInfo>>> CreateDocuments (ProjectData projectData, MonoDevelop.Projects.Project p, CancellationToken token, ImmutableArray<MonoDevelop.Projects.ProjectFile> sourceFiles, ProjectData oldProjectData)
			{
				var documents = new List<DocumentInfo> ();
				// We don' add additionalDocuments anymore because they were causing slowdown of compilation generation
				// and no upside to setting additionalDocuments, keeping this around in case this changes in future.
				var additionalDocuments = new List<DocumentInfo> ();
				var duplicates = new HashSet<DocumentId> ();
				// use given source files instead of project.Files because there may be additional files added by msbuild targets
				foreach (var f in sourceFiles) {
					if (token.IsCancellationRequested)
						return null;
					if (f.Subtype == MonoDevelop.Projects.Subtype.Directory)
						continue;

					if (p.IsCompileable (f.FilePath) || CanGenerateAnalysisContextForNonCompileable (p, f)) {
						var filePath = (FilePath)f.Name;
						var id = projectData.DocumentData.GetOrCreate (filePath.ResolveLinks (), oldProjectData?.DocumentData);
						if (!duplicates.Add (id))
							continue;
						documents.Add (CreateDocumentInfo (solutionData, p.Name, projectData, f));
					} else {
						foreach (var projectedDocument in await GenerateProjections (f, projectData.DocumentData, p, token, oldProjectData, null)) {
							var projectedId = projectData.DocumentData.GetOrCreate (projectedDocument.FilePath, oldProjectData?.DocumentData);
							if (!duplicates.Add (projectedId))
								continue;
							documents.Add (projectedDocument);
						}
					}
				}
				var projectId = projectMap.GetId (p);
				return Tuple.Create (documents, additionalDocuments);
			}

			async Task<List<DocumentInfo>> GenerateProjections (MonoDevelop.Projects.ProjectFile f, DocumentMap documentMap, MonoDevelop.Projects.Project p, CancellationToken token, ProjectData oldProjectData, HashSet<DocumentId> duplicates)
			{
				var mimeType = IdeServices.DesktopService.GetMimeTypeForUri (f.FilePath);
				var node = IdeApp.TypeSystemService.GetTypeSystemParserNode (mimeType, f.BuildAction);
				if (node == null || !node.Parser.CanGenerateProjection (mimeType, f.BuildAction, p.SupportedLanguages))
					return new List<DocumentInfo> ();

				var options = new ParseOptions {
					FileName = f.FilePath,
					Project = p,
					Content = TextFileProvider.Instance.GetReadOnlyTextEditorData (f.FilePath),
				};
				var generatedProjections = await node.Parser.GenerateProjections (options, token);
				var list = new List<Projection> ();
				var entry = new ProjectionEntry {
					File = f,
					Projections = list,
				};

				var result = new List<DocumentInfo> (generatedProjections.Count);
				foreach (var projection in generatedProjections) {
					list.Add (projection);
					if (duplicates != null && !duplicates.Add (documentMap.GetOrCreate (projection.Document.FileName, oldProjectData?.DocumentData)))
						continue;
					var plainName = projection.Document.FileName.FileName;
					var folders = GetFolders (p.Name, f);
					result.Add(DocumentInfo.Create (
						documentMap.GetOrCreate (projection.Document.FileName, oldProjectData?.DocumentData),
						plainName,
						folders,
						SourceCodeKind.Regular,
						TextLoader.From (TextAndVersion.Create (new MonoDevelopSourceText (projection.Document), VersionStamp.Create (), projection.Document.FileName)),
						projection.Document.FileName,
						false)
					);
				}
				projections.AddProjectionEntry (entry);
				return result;
			}

			static DocumentInfo CreateDocumentInfo (SolutionData data, string projectName, ProjectData id, MonoDevelop.Projects.ProjectFile f)
			{
				var filePath = f.FilePath.ResolveLinks ();
				var folders = GetFolders (projectName, f);

				return DocumentInfo.Create (
					id.DocumentData.GetOrCreate (filePath),
					filePath,
					folders,
					f.SourceCodeKind,
					CreateTextLoader (filePath),
					filePath,
					isGenerated: false
				);

				TextLoader CreateTextLoader (string fileName) => data.Files.GetOrAdd (fileName, a => new MonoDevelopTextLoader (a));
			}

			static IEnumerable<string> GetFolders (string projectName, MonoDevelop.Projects.ProjectFile f)
			{
				return new [] { projectName }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			}

			internal void ReloadProjectCache ()
			{
				workspaceCache = new WorkspaceFilesCache ();
				workspaceCache.Load (workspace.MonoDevelopSolution);
			}

			public void Dispose ()
			{
				persistentStorageLocationServiceRegistration?.Dispose ();

				solutionIdMap.Clear ();

				foreach (var analyzer in analyzersToDispose)
					analyzer.Dispose ();
				analyzersToDispose.Clear ();
			}
		}
	}
}
