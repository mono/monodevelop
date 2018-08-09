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
using Microsoft.CodeAnalysis;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MonoDevelop.Ide.Editor.Projection;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class ProjectSystemHandler
		{
			public MonoDevelopWorkspace Workspace { get; }

			bool added;
			readonly object addLock = new object ();
			readonly object projectionListUpdateLock = new object ();
			readonly object projectIdMapsLock = new object ();

			ImmutableList<ProjectionEntry> projectionList = ImmutableList<ProjectionEntry>.Empty;
			internal List<MonoDevelop.Projects.DotNetProject> modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
			SolutionData solutionData;

			public ProjectSystemHandler (MonoDevelopWorkspace workspace)
			{
				Workspace = workspace;
			}

#region Project mapping
			ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project> projectIdToMdProjectMap = ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project>.Empty;
			readonly Dictionary<MonoDevelop.Projects.Project, ProjectId> projectIdMap = new Dictionary<MonoDevelop.Projects.Project, ProjectId> ();
			readonly Dictionary<ProjectId, ProjectData> projectDataMap = new Dictionary<ProjectId, ProjectData> ();

			internal ProjectId GetProjectId (MonoDevelop.Projects.Project p)
			{
				lock (projectIdMapsLock) {
					return projectIdMap.TryGetValue (p, out ProjectId result) ? result : null;
				}
			}

			ProjectId GetOrCreateProjectId (MonoDevelop.Projects.Project p)
			{
				lock (projectIdMapsLock) {
					if (!projectIdMap.TryGetValue (p, out ProjectId result)) {
						projectIdMap[p] = result = ProjectId.CreateNewId (p.Name);
						projectIdToMdProjectMap = projectIdToMdProjectMap.Add (result, p);
					}
					return result;
				}
			}

			void MigrateOldProjectInfo (MonoDevelop.Projects.Project p, MonoDevelop.Projects.Project oldProject)
			{
				lock (projectIdMapsLock) {
					if (!projectIdMap.ContainsKey (p)) {
						p.Modified += Workspace.OnProjectModified;
					}

					if (oldProject == null)
						return;

					oldProject.Modified -= Workspace.OnProjectModified;
					if (projectIdMap.TryGetValue (oldProject, out var id)) {
						projectIdMap.Remove (oldProject);
						projectIdMap [p] = id;
						projectIdToMdProjectMap = projectIdToMdProjectMap.SetItem (id, p);
					}
				}
			}

			internal void RemoveProject (MonoDevelop.Projects.Project project, ProjectId id)
			{
				lock (projectIdMapsLock) {
					if (projectIdMap.TryGetValue (project, out ProjectId val))
						projectIdMap.Remove (project);
					projectIdToMdProjectMap = projectIdToMdProjectMap.Remove (val);
				}

				lock (projectDataMap) {
					projectDataMap.Remove (id);
				}
			}

			internal MonoDevelop.Projects.Project GetMonoProject (ProjectId projectId)
			{
				lock (projectIdToMdProjectMap) {
					return projectIdToMdProjectMap.TryGetValue (projectId, out var result) ? result : null;
				}
			}

			internal bool Contains (ProjectId projectId)
			{
				lock (projectDataMap) {
					return projectDataMap.ContainsKey (projectId);
				}
			}

			internal ProjectData GetProjectData (ProjectId id)
			{
				lock (projectDataMap) {
					projectDataMap.TryGetValue (id, out ProjectData result);
					return result;
				}
			}

			ProjectData RemoveProjectData (ProjectId id)
			{
				lock (projectDataMap) {
					if (projectDataMap.TryGetValue (id, out ProjectData result))
						projectDataMap.Remove (id);
					return result;
				}
			}

			ProjectData CreateProjectData (ProjectId id, List<MonoDevelopMetadataReference> metadataReferences)
			{
				lock (projectDataMap) {
					var result = new ProjectData (id, metadataReferences, Workspace);
					projectDataMap [id] = result;
					return result;
				}
			}
#endregion

#region Solution mapping
			// No need to remove mappings, it's handled on workspace dispose.
			readonly Dictionary<MonoDevelop.Projects.Solution, SolutionId> solutionIdMap = new Dictionary<MonoDevelop.Projects.Solution, SolutionId> ();

			internal SolutionId GetSolutionId (MonoDevelop.Projects.Solution solution)
			{
				if (solution == null)
					throw new ArgumentNullException ("solution");

				lock (solutionIdMap) {
					SolutionId result;
					if (!solutionIdMap.TryGetValue (solution, out result)) {
						result = SolutionId.CreateNewId (solution.Name);
						solutionIdMap [solution] = result;
					}
					return result;
				}
			}
#endregion

			internal MonoDevelop.Projects.Project GetMonoProject (Project project)
				=> GetMonoProject (project.Id);

			internal DocumentId GetDocumentId (ProjectId projectId, string name)
				=> GetProjectData (projectId)?.GetDocumentId (name);

			class SolutionData
			{
				public ConcurrentDictionary<string, TextLoader> Files = new ConcurrentDictionary<string, TextLoader> ();
			}

			static Func<SolutionData, string, TextLoader> CreateTextLoader = (data, fileName) => data.Files.GetOrAdd (fileName, a => new MonoDevelopTextLoader (a));

			internal async Task<ProjectInfo> LoadProject (MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.Project oldProject)
			{
				MigrateOldProjectInfo (p, oldProject);

				var projectId = GetOrCreateProjectId (p);

				var references = await CreateMetadataReferences (p, projectId, token).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;
				var config = IdeApp.Workspace != null ? p.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as MonoDevelop.Projects.DotNetProjectConfiguration : null;
				MonoDevelop.Projects.DotNetCompilerParameters cp = config?.CompilationParameters;
				FilePath fileName = IdeApp.Workspace != null ? p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration) : (FilePath)"";
				if (fileName.IsNullOrEmpty)
					fileName = new FilePath (p.Name + ".dll");

				var projectReferences = await CreateProjectReferences (p, token);
				if (token.IsCancellationRequested)
					return null;

				var sourceFiles = await p.GetSourceFilesAsync (config?.Selector).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;

				lock (Workspace.updatingProjectDataLock) {
					//when reloading e.g. after a save, preserve document IDs
					using (var oldProjectData = RemoveProjectData (projectId)) {
						var projectData = CreateProjectData (projectId, references);

						var documents = CreateDocuments (projectData, p, token, sourceFiles, oldProjectData);
						if (documents == null)
							return null;

						// TODO: Pass in the WorkspaceMetadataFileReferenceResolver
						var info = ProjectInfo.Create (
							projectId,
							VersionStamp.Create (),
							p.Name,
							fileName.FileNameWithoutExtension,
							LanguageNames.CSharp,
							p.FileName,
							fileName,
							cp?.CreateCompilationOptions (),
							cp?.CreateParseOptions (config),
							documents.Item1,
							projectReferences,
							references.Select (x => x.CurrentSnapshot),
							additionalDocuments: documents.Item2
						);
						projectData.Info = info;
						return info;
					}
				}
			}

			async Task<ConcurrentBag<ProjectInfo>> CreateProjectInfos (IEnumerable<MonoDevelop.Projects.Project> mdProjects, CancellationToken token)
			{
				var projects = new ConcurrentBag<ProjectInfo> ();
				var allTasks = new List<Task> ();

				foreach (var proj in mdProjects) {
					if (token.IsCancellationRequested)
						return null;
					if (proj is MonoDevelop.Projects.DotNetProject netProj && !netProj.SupportsRoslyn)
						continue;
					var tp = LoadProject (proj, token, null).ContinueWith (t => {
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

			bool IsModifiedWhileLoading (MonoDevelop.Projects.Solution solution)
			{
				List<MonoDevelop.Projects.DotNetProject> modifiedWhileLoading;
				lock (Workspace.projectModifyLock) {
					modifiedWhileLoading = modifiedProjects;
					modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
				}

				foreach (var project in modifiedProjects) {
					// TODO: Maybe optimize this so we don't do O(n^2)
					if (solution.ContainsItem (project)) {
						return true;
					}
				}
				return false;
			}

			void ClearOldProjectionList ()
			{
				ImmutableList<ProjectionEntry> toDispose;
				lock (projectionListUpdateLock) {
					toDispose = projectionList;
					projectionList = projectionList.Clear ();
				}
				foreach (var p in toDispose)
					p.Dispose ();
			}

			internal Task<SolutionInfo> CreateSolutionInfo (MonoDevelop.Projects.Solution sol, CancellationToken ct)
			{
				return Task.Run (delegate {
					return CreateSolutionInfoInternal (sol, ct);
				});

				async Task<SolutionInfo> CreateSolutionInfoInternal (MonoDevelop.Projects.Solution solution, CancellationToken token)
				{
					using (var timer = Counters.AnalysisTimer.BeginTiming ()) {
						ClearOldProjectionList ();

						solutionData = new SolutionData ();

						var projectInfos = await CreateProjectInfos (solution.GetAllProjects (), token);
						if (IsModifiedWhileLoading (solution)) {
							return await CreateSolutionInfoInternal (solution, token).ConfigureAwait (false);
						}

						var solutionId = GetSolutionId (solution);
						var solutionInfo = SolutionInfo.Create (solutionId, VersionStamp.Create (), solution.FileName, projectInfos);

						lock (addLock) {
							if (!added) {
								added = true;
								solution.Modified += OnSolutionModified;
								NotifySolutionModified (solution, solutionId, Workspace);
								Workspace.OnSolutionAdded (solutionInfo);
								lock (Workspace.generatedFiles) {
									foreach (var generatedFile in Workspace.generatedFiles) {
										if (!Workspace.IsDocumentOpen (generatedFile.Key.Id))
											Workspace.OnDocumentOpened (generatedFile.Key.Id, generatedFile.Value);
									}
								}
							}
						}
						return solutionInfo;
					}
				}
			}

			Tuple<List<DocumentInfo>, List<DocumentInfo>> CreateDocuments (ProjectData projectData, MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.ProjectFile [] sourceFiles, ProjectData oldProjectData)
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

					if (TypeSystemParserNode.IsCompileableFile (f, out SourceCodeKind sck) || CanGenerateAnalysisContextForNonCompileable (p, f)) {
						var id = projectData.GetOrCreateDocumentId (f.Name, oldProjectData);
						if (!duplicates.Add (id))
							continue;
						documents.Add (CreateDocumentInfo (solutionData, p.Name, projectData, f, sck));
					} else {
						foreach (var projectedDocument in GenerateProjections (f, projectData, p, oldProjectData)) {
							var projectedId = projectData.GetOrCreateDocumentId (projectedDocument.FilePath, oldProjectData);
							if (!duplicates.Add (projectedId))
								continue;
							documents.Add (projectedDocument);
						}
					}
				}
				var projectId = GetProjectId (p);
				lock (Workspace.generatedFiles) {
					foreach (var generatedFile in Workspace.generatedFiles) {
						if (generatedFile.Key.Id.ProjectId == projectId)
							documents.Add (generatedFile.Key);
					}
				}
				return Tuple.Create (documents, additionalDocuments);
			}

			IEnumerable<DocumentInfo> GenerateProjections (MonoDevelop.Projects.ProjectFile f, ProjectData projectData, MonoDevelop.Projects.Project p, ProjectData oldProjectData, HashSet<DocumentId> duplicates = null)
			{
				var mimeType = DesktopService.GetMimeTypeForUri (f.FilePath);
				var node = TypeSystemService.GetTypeSystemParserNode (mimeType, f.BuildAction);
				if (node == null || !node.Parser.CanGenerateProjection (mimeType, f.BuildAction, p.SupportedLanguages))
					yield break;
				var options = new ParseOptions {
					FileName = f.FilePath,
					Project = p,
					Content = TextFileProvider.Instance.GetReadOnlyTextEditorData (f.FilePath),
				};
				var projections = node.Parser.GenerateProjections (options);
				var entry = new ProjectionEntry ();
				entry.File = f;
				var list = new List<Projection> ();
				entry.Projections = list;
				foreach (var projection in projections.Result) {
					list.Add (projection);
					if (duplicates != null && !duplicates.Add (projectData.GetOrCreateDocumentId (projection.Document.FileName, oldProjectData)))
						continue;
					var plainName = projection.Document.FileName.FileName;
					var folders = new [] { p.Name }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
					yield return DocumentInfo.Create (
						projectData.GetOrCreateDocumentId (projection.Document.FileName, oldProjectData),
						plainName,
						folders,
						SourceCodeKind.Regular,
						TextLoader.From (TextAndVersion.Create (new MonoDevelopSourceText (projection.Document), VersionStamp.Create (), projection.Document.FileName)),
						projection.Document.FileName,
						false
					);
				}
				lock (projectionListUpdateLock)
					projectionList = projectionList.Add (entry);
			}

			static DocumentInfo CreateDocumentInfo (SolutionData data, string projectName, ProjectData id, MonoDevelop.Projects.ProjectFile f, SourceCodeKind sourceCodeKind)
			{
				var filePath = f.FilePath;
				var folders = new [] { projectName }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

				return DocumentInfo.Create (
					id.GetOrCreateDocumentId (filePath),
					filePath,
					folders,
					sourceCodeKind,
					CreateTextLoader (data, f.Name),
					f.Name,
					isGenerated: false
				);
			}

			internal IReadOnlyList<ProjectionEntry> ProjectionList => projectionList;

			internal class ProjectionEntry : IDisposable
			{
				public MonoDevelop.Projects.ProjectFile File;
				public IReadOnlyList<Projection> Projections;

				public void Dispose ()
				{
					Runtime.RunInMainThread (delegate {
						foreach (var p in Projections)
							p.Dispose ();
					});
				}
			}

			internal void UpdateProjectionEntry (MonoDevelop.Projects.ProjectFile projectFile, IReadOnlyList<Projection> projections)
			{
				if (projectFile == null)
					throw new ArgumentNullException (nameof (projectFile));
				if (projections == null)
					throw new ArgumentNullException (nameof (projections));

				lock (projectionListUpdateLock) {
					foreach (var entry in projectionList) {
						if (entry?.File?.FilePath == projectFile.FilePath) {
							projectionList = projectionList.Remove (entry);
							// Since it's disposing projected editor, it needs to dispose in MainThread.
							Runtime.RunInMainThread (() => entry.Dispose ()).Ignore ();
							break;
						}
					}
					projectionList = projectionList.Add (new ProjectionEntry { File = projectFile, Projections = projections });
				}
			}

			async Task<List<MonoDevelopMetadataReference>> CreateMetadataReferences (MonoDevelop.Projects.Project proj, ProjectId projectId, CancellationToken token)
			{
				var result = new List<MonoDevelopMetadataReference> ();

				if (!(proj is MonoDevelop.Projects.DotNetProject netProject)) {
					// create some default references for unsupported project types.
					foreach (var asm in DefaultAssemblies) {
						var metadataReference = Workspace.MetadataReferenceManager.GetOrCreateMetadataReference (asm, MetadataReferenceProperties.Assembly);
						result.Add (metadataReference);
					}
					return result;
				}
				var configurationSelector = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default;
				var hashSet = new HashSet<string> (FilePath.PathComparer);

				try {
					var referencedAssemblies = await netProject.GetReferencedAssemblies (configurationSelector, false).ConfigureAwait (false);
					if (!AddAssemblyReferences (referencedAssemblies))
						return result;
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting referenced assemblies", e);
				}

				try {
					var referencedProjects = netProject.GetReferencedItems (configurationSelector);
					if (!AddProjectReferences (referencedProjects))
						return result;

				} catch (Exception e) {
					LoggingService.LogError ("Error while getting referenced projects", e);
				}
				return result;

				bool AddAssemblyReferences (IEnumerable<MonoDevelop.Projects.AssemblyReference> references)
				{
					foreach (var file in references) {
						if (token.IsCancellationRequested)
							return false;

						if (!hashSet.Add (file.FilePath))
							continue;

						var aliases = file.EnumerateAliases ().ToImmutableArray ();
						var metadataReference = Workspace.MetadataReferenceManager.GetOrCreateMetadataReference (file.FilePath, new MetadataReferenceProperties (aliases: aliases));
						if (metadataReference != null)
							result.Add (metadataReference);
					}

					return true;
				}

				bool AddProjectReferences (IEnumerable<MonoDevelop.Projects.SolutionItem> references)
				{
					foreach (var pr in references) {
						if (token.IsCancellationRequested)
							return false;

						if (!(pr is MonoDevelop.Projects.DotNetProject referencedProject) || !TypeSystemService.IsOutputTrackedProject (referencedProject))
							continue;

						var fileName = referencedProject.GetOutputFileName (configurationSelector);;
						if (!hashSet.Add (fileName))
							continue;

						var metadataReference = Workspace.MetadataReferenceManager.GetOrCreateMetadataReference (fileName, MetadataReferenceProperties.Assembly);
						if (metadataReference != null)
							result.Add (metadataReference);
					}

					return true;
				}
			}

			async Task<IEnumerable<ProjectReference>> CreateProjectReferences (MonoDevelop.Projects.Project p, CancellationToken token)
			{
				if (!(p is MonoDevelop.Projects.DotNetProject netProj))
					return Enumerable.Empty<ProjectReference> ();

				List<MonoDevelop.Projects.AssemblyReference> references;
				try {
					var config = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default;
					references = await netProj.GetReferences (config, token).ConfigureAwait (false);
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting referenced projects.", e);
					return Enumerable.Empty<ProjectReference> ();
				};
				return CreateProjectReferencesFromAssemblyReferences (netProj, references);
			}

			IEnumerable<ProjectReference> CreateProjectReferencesFromAssemblyReferences (MonoDevelop.Projects.DotNetProject p, List<MonoDevelop.Projects.AssemblyReference> references)
			{
				var addedProjects = new HashSet<MonoDevelop.Projects.DotNetProject> ();
				foreach (var pr in references) {
					if (!pr.IsProjectReference || !pr.ReferenceOutputAssembly)
						continue;

					var referencedItem = pr.GetReferencedItem (p.ParentSolution);
					if (!(referencedItem is MonoDevelop.Projects.DotNetProject referencedProject))
						continue;

					if (!addedProjects.Add (referencedProject))
						continue;

					if (TypeSystemService.IsOutputTrackedProject (referencedProject))
						continue;

					var aliases = pr.EnumerateAliases ();
					yield return new ProjectReference (GetOrCreateProjectId (referencedProject), aliases.ToImmutableArray ());
				}
			}

			/// <summary>
			/// Tries the get original file from projection. If the fileName / offset is inside a projection this method tries to convert it 
			/// back to the original physical file.
			/// </summary>
			internal bool TryGetOriginalFileFromProjection (string fileName, int offset, out string originalName, out int originalOffset)
			{
				foreach (var projectionEntry in ProjectionList) {
					var projection = projectionEntry.Projections.FirstOrDefault (p => FilePath.PathComparer.Equals (p.Document.FileName, fileName));
					if (projection == null)
						continue;

					if (projection.TryConvertFromProjectionToOriginal (offset, out originalOffset)) {
						originalName = projectionEntry.File.FilePath;
						return true;
					}
				}

				originalName = fileName;
				originalOffset = offset;
				return false;
			}
		}
	}
}
