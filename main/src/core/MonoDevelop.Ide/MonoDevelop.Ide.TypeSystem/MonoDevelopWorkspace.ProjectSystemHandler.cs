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
using Microsoft.CodeAnalysis.Host;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Projection;

namespace MonoDevelop.Ide.TypeSystem
{
	public partial class MonoDevelopWorkspace
	{
		internal class ProjectSystemHandler
		{
			readonly MonoDevelopWorkspace workspace;
			readonly ProjectDataMap projectMap;
			readonly ProjectionData projections;
			readonly Lazy<MetadataReferenceHandler> metadataHandler;

			bool added;
			readonly object addLock = new object ();

			internal List<MonoDevelop.Projects.DotNetProject> modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
			SolutionData solutionData;

			public ProjectSystemHandler (MonoDevelopWorkspace workspace, ProjectDataMap projectMap, ProjectionData projections)
			{
				this.workspace = workspace;
				this.projectMap = projectMap;
				this.projections = projections;

				metadataHandler = new Lazy<MetadataReferenceHandler> (() => new MetadataReferenceHandler (workspace.MetadataReferenceManager, projectMap));
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

			static async void OnSolutionModified (object sender, MonoDevelop.Projects.WorkspaceItemEventArgs args)
			{
				var sol = (MonoDevelop.Projects.Solution)args.Item;
				var workspace = await TypeSystemService.GetWorkspaceAsync (sol, CancellationToken.None);
				var solId = workspace.ProjectHandler.GetSolutionId (sol);
				if (solId == null)
					return;

				NotifySolutionModified (sol, solId, workspace);
			}

			static void NotifySolutionModified (MonoDevelop.Projects.Solution sol, SolutionId solId, MonoDevelopWorkspace workspace)
			{
				if (string.IsNullOrWhiteSpace (sol.BaseDirectory))
					return;

				var locService = (MonoDevelopPersistentStorageLocationService)workspace.Services.GetService<IPersistentStorageLocationService> ();
				locService.NotifyStorageLocationChanging (solId, sol.GetPreferencesDirectory ());
			}
			#endregion

			class SolutionData
			{
				public ConcurrentDictionary<string, TextLoader> Files = new ConcurrentDictionary<string, TextLoader> ();
			}

			internal async Task<ProjectInfo> LoadProject (MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.Project oldProject)
			{
				var projectId = projectMap.GetOrCreateId (p, oldProject);

				var config = IdeApp.Workspace != null ? p.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as MonoDevelop.Projects.DotNetProjectConfiguration : null;
				MonoDevelop.Projects.DotNetCompilerParameters cp = config?.CompilationParameters;
				FilePath fileName = IdeApp.Workspace != null ? p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration) : (FilePath)"";
				if (fileName.IsNullOrEmpty)
					fileName = new FilePath (p.Name + ".dll");

				var (references, projectReferences) = await metadataHandler.Value.CreateReferences (p, token);
				if (token.IsCancellationRequested)
					return null;

				var sourceFiles = await p.GetSourceFilesAsync (config?.Selector).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;

				lock (workspace.updatingProjectDataLock) {
					//when reloading e.g. after a save, preserve document IDs
					using (var oldProjectData = projectMap.RemoveData (projectId)) {
						var projectData = projectMap.CreateData (projectId, references);

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
				lock (workspace.projectModifyLock) {
					modifiedWhileLoading = modifiedProjects;
					modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
				}

				foreach (var project in modifiedWhileLoading) {
					// TODO: Maybe optimize this so we don't do O(n^2)
					if (solution.ContainsItem (project)) {
						return true;
					}
				}
				return false;
			}

			internal Task<SolutionInfo> CreateSolutionInfo (MonoDevelop.Projects.Solution sol, CancellationToken ct)
			{
				return Task.Run (delegate {
					return CreateSolutionInfoInternal (sol, ct);
				});

				async Task<SolutionInfo> CreateSolutionInfoInternal (MonoDevelop.Projects.Solution solution, CancellationToken token)
				{
					using (var timer = Counters.AnalysisTimer.BeginTiming ()) {
						projections.ClearOldProjectionList ();

						solutionData = new SolutionData ();

						var projectInfos = await CreateProjectInfos (solution.GetAllProjects (), token).ConfigureAwait (false);
						if (IsModifiedWhileLoading (solution)) {
							return await CreateSolutionInfoInternal (solution, token).ConfigureAwait (false);
						}

						var solutionId = GetSolutionId (solution);
						var solutionInfo = SolutionInfo.Create (solutionId, VersionStamp.Create (), solution.FileName, projectInfos);

						lock (addLock) {
							if (!added) {
								added = true;
								solution.Modified += OnSolutionModified;
								NotifySolutionModified (solution, solutionId, workspace);
								workspace.OnSolutionAdded (solutionInfo);
								lock (workspace.generatedFiles) {
									foreach (var generatedFile in workspace.generatedFiles) {
										if (!workspace.IsDocumentOpen (generatedFile.Key.Id))
											workspace.OnDocumentOpened (generatedFile.Key.Id, generatedFile.Value);
									}
								}
							}
						}
						return solutionInfo;
					}
				}
			}

			static bool CanGenerateAnalysisContextForNonCompileable (MonoDevelop.Projects.Project p, MonoDevelop.Projects.ProjectFile f)
			{
				var mimeType = DesktopService.GetMimeTypeForUri (f.FilePath);
				var node = TypeSystemService.GetTypeSystemParserNode (mimeType, f.BuildAction);
				if (node?.Parser == null)
					return false;
				return node.Parser.CanGenerateAnalysisDocument (mimeType, f.BuildAction, p.SupportedLanguages);
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
						var id = projectData.DocumentData.GetOrCreate (f.Name, oldProjectData);
						if (!duplicates.Add (id))
							continue;
						documents.Add (CreateDocumentInfo (solutionData, p.Name, projectData, f, sck));
					} else {
						foreach (var projectedDocument in GenerateProjections (f, projectData.DocumentData, p, oldProjectData, null)) {
							var projectedId = projectData.DocumentData.GetOrCreate (projectedDocument.FilePath, oldProjectData);
							if (!duplicates.Add (projectedId))
								continue;
							documents.Add (projectedDocument);
						}
					}
				}
				var projectId = projectMap.GetId (p);
				lock (workspace.generatedFiles) {
					foreach (var generatedFile in workspace.generatedFiles) {
						if (generatedFile.Key.Id.ProjectId == projectId)
							documents.Add (generatedFile.Key);
					}
				}
				return Tuple.Create (documents, additionalDocuments);
			}

			IEnumerable<DocumentInfo> GenerateProjections (MonoDevelop.Projects.ProjectFile f, DocumentMap documentMap, MonoDevelop.Projects.Project p, ProjectData oldProjectData, HashSet<DocumentId> duplicates)
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
				var generatedProjections = node.Parser.GenerateProjections (options);
				var list = new List<Projection> ();
				var entry = new ProjectionEntry {
					File = f,
					Projections = list,
				};

				foreach (var projection in generatedProjections.Result) {
					list.Add (projection);
					if (duplicates != null && !duplicates.Add (documentMap.GetOrCreate (projection.Document.FileName, oldProjectData)))
						continue;
					var plainName = projection.Document.FileName.FileName;
					var folders = GetFolders (p.Name, f);
					yield return DocumentInfo.Create (
						documentMap.GetOrCreate (projection.Document.FileName, oldProjectData),
						plainName,
						folders,
						SourceCodeKind.Regular,
						TextLoader.From (TextAndVersion.Create (new MonoDevelopSourceText (projection.Document), VersionStamp.Create (), projection.Document.FileName)),
						projection.Document.FileName,
						false
					);
				}
				projections.AddProjectionEntry (entry);
			}

			static DocumentInfo CreateDocumentInfo (SolutionData data, string projectName, ProjectData id, MonoDevelop.Projects.ProjectFile f, SourceCodeKind sourceCodeKind)
			{
				var filePath = f.FilePath;
				var folders = GetFolders (projectName, f);

				return DocumentInfo.Create (
					id.DocumentData.GetOrCreate (filePath),
					filePath,
					folders,
					sourceCodeKind,
					CreateTextLoader (f.Name),
					f.Name,
					isGenerated: false
				);

				TextLoader CreateTextLoader (string fileName) => data.Files.GetOrAdd (fileName, a => new MonoDevelopTextLoader (a));
			}

			static IEnumerable<string> GetFolders (string projectName, MonoDevelop.Projects.ProjectFile f)
			{
				return new [] { projectName }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			}
		}
	}
}