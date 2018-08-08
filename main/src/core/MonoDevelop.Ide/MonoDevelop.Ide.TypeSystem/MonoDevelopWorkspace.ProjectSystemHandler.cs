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
		class ProjectSystemHandler
		{
			public MonoDevelop.Projects.Solution MonoDevelopSolution { get; }
			public MonoDevelopWorkspace Workspace { get; }

			bool added;
			readonly object addLock = new object ();
			readonly object projectionListUpdateLock = new object ();

			ImmutableList<ProjectionEntry> projectionList = ImmutableList<ProjectionEntry>.Empty;
			internal List<MonoDevelop.Projects.DotNetProject> modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
			SolutionData solutionData;

			internal ConcurrentDictionary<MonoDevelop.Projects.Project, ProjectId> projectIdMap = new ConcurrentDictionary<MonoDevelop.Projects.Project, ProjectId> ();
			internal ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project> projectIdToMdProjectMap = ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project>.Empty;
			internal ConcurrentDictionary<ProjectId, ProjectData> projectDataMap = new ConcurrentDictionary<ProjectId, ProjectData> ();

			public ProjectSystemHandler (MonoDevelop.Projects.Solution solution, MonoDevelopWorkspace workspace)
			{
				MonoDevelopSolution = solution;
				Workspace = workspace;
			}


			Dictionary<MonoDevelop.Projects.Solution, SolutionId> solutionIdMap = new Dictionary<MonoDevelop.Projects.Solution, SolutionId> ();

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

			internal MonoDevelop.Projects.Project GetMonoProject (Project project)
			{
				return GetMonoProject (project.Id);
			}

			internal MonoDevelop.Projects.Project GetMonoProject (ProjectId projectId)
			{
				projectIdToMdProjectMap.TryGetValue (projectId, out var result);
				return result;
			}

			internal bool Contains (ProjectId projectId)
			{
				return projectDataMap.ContainsKey (projectId);
			}

			internal ProjectId GetProjectId (MonoDevelop.Projects.Project p)
			{
				lock (projectIdMap) {
					ProjectId result;
					if (projectIdMap.TryGetValue (p, out result))
						return result;
					return null;
				}
			}

			internal ProjectId GetOrCreateProjectId (MonoDevelop.Projects.Project p)
			{
				lock (projectIdMap) {
					if (!projectIdMap.TryGetValue (p, out ProjectId result)) {
						result = ProjectId.CreateNewId (p.Name);
						projectIdMap [p] = result;
						projectIdToMdProjectMap = projectIdToMdProjectMap.Add (result, p);
					}
					return result;
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
					projectDataMap.TryRemove (id, out ProjectData result);
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

			internal class ProjectData : IDisposable
			{
				readonly WeakReference<MonoDevelopWorkspace> workspaceRef;
				readonly ProjectId projectId;
				readonly Dictionary<string, DocumentId> documentIdMap;
				readonly List<MonoDevelopMetadataReference> metadataReferences = new List<MonoDevelopMetadataReference> ();

				public ProjectInfo Info {
					get;
					set;
				}

				public ProjectData (ProjectId projectId, List<MonoDevelopMetadataReference> metadataReferences, MonoDevelopWorkspace ws)
				{
					this.projectId = projectId;
					workspaceRef = new WeakReference<MonoDevelopWorkspace> (ws);

					lock (this.metadataReferences) {
						foreach (var metadataReference in metadataReferences) {
							AddMetadataReference_NoLock (metadataReference);
						}
					}
					documentIdMap = new Dictionary<string, DocumentId> (FilePath.PathComparer);
				}

				void OnMetadataReferenceUpdated (object sender, EventArgs args)
				{
					var reference = (MonoDevelopMetadataReference)sender;
					// If we didn't contain the reference, bail
					if (!RemoveMetadataReference (reference) || !workspaceRef.TryGetTarget (out var workspace))
						return;

					lock (workspace.updatingProjectDataLock) {
						workspace.OnMetadataReferenceRemoved (projectId, reference.CurrentSnapshot);

						reference.UpdateSnapshot ();
						lock (metadataReferences) {
							AddMetadataReference_NoLock (reference);
						}
						workspace.OnMetadataReferenceAdded (projectId, reference.CurrentSnapshot);
					}
				}

				internal void AddMetadataReference_NoLock (MonoDevelopMetadataReference metadataReference)
				{
					System.Diagnostics.Debug.Assert (Monitor.IsEntered (metadataReferences));

					lock (metadataReferences) {
						metadataReferences.Add (metadataReference);
					}
					metadataReference.UpdatedOnDisk += OnMetadataReferenceUpdated;
				}

				internal bool RemoveMetadataReference (MonoDevelopMetadataReference metadataReference)
				{
					lock (metadataReferences) {
						metadataReference.UpdatedOnDisk -= OnMetadataReferenceUpdated;
						return metadataReferences.Remove (metadataReference);
					}
				}

				internal DocumentId GetOrCreateDocumentId (string name, ProjectData previous)
				{
					if (previous != null) {
						var oldId = previous.GetDocumentId (name);
						if (oldId != null) {
							AddDocumentId (oldId, name);
							return oldId;
						}
					}
					return GetOrCreateDocumentId (name);
				}

				internal DocumentId GetOrCreateDocumentId (string name)
				{
					lock (documentIdMap) {
						DocumentId result;
						if (!documentIdMap.TryGetValue (name, out result)) {
							result = DocumentId.CreateNewId (projectId, name);
							documentIdMap [name] = result;
						}
						return result;
					}
				}

				internal void AddDocumentId (DocumentId id, string name)
				{
					lock (documentIdMap) {
						documentIdMap [name] = id;
					}
				}

				public DocumentId GetDocumentId (string name)
				{
					DocumentId result;
					if (!documentIdMap.TryGetValue (name, out result)) {
						return null;
					}
					return result;
				}

				internal void RemoveDocument (string name)
				{
					documentIdMap.Remove (name);
				}

				public void Dispose ()
				{
					if (!workspaceRef.TryGetTarget (out var workspace))
						return;

					lock (workspace.updatingProjectDataLock) {
						lock (metadataReferences) {
							foreach (var reference in metadataReferences)
								reference.UpdatedOnDisk -= OnMetadataReferenceUpdated;
						}
					}
				}
			}

			internal DocumentId GetDocumentId (ProjectId projectId, string name)
			{
				var data = GetProjectData (projectId);
				if (data == null)
					return null;
				return data.GetDocumentId (name);
			}

			internal void UnloadMonoProject (MonoDevelop.Projects.Project project)
			{
				if (project == null)
					throw new ArgumentNullException (nameof (project));
				project.Modified -= Workspace.OnProjectModified;
			}

			internal async Task<ProjectInfo> LoadProject (MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.Project oldProject)
			{
				if (!projectIdMap.ContainsKey (p)) {
					p.Modified += Workspace.OnProjectModified;
				}

				if (oldProject != null) {
					lock (projectIdMap) {
						oldProject.Modified -= Workspace.OnProjectModified;
						projectIdMap.TryRemove (oldProject, out var id);
						projectIdMap [p] = id;
						projectIdToMdProjectMap = projectIdToMdProjectMap.SetItem (id, p);
					}
				}

				var projectId = GetOrCreateProjectId (p);

				var references = await Workspace.CreateMetadataReferences (p, projectId, token).ConfigureAwait (false);
				if (token.IsCancellationRequested)
					return null;
				var config = IdeApp.Workspace != null ? p.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as MonoDevelop.Projects.DotNetProjectConfiguration : null;
				MonoDevelop.Projects.DotNetCompilerParameters cp = null;
				if (config != null)
					cp = config.CompilationParameters;
				FilePath fileName = IdeApp.Workspace != null ? p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration) : (FilePath)"";
				if (fileName.IsNullOrEmpty)
					fileName = new FilePath (p.Name + ".dll");

				var projectReferences = await Workspace.CreateProjectReferences (p, token);
				if (token.IsCancellationRequested)
					return null;

				var sourceFiles = await p.GetSourceFilesAsync (config != null ? config.Selector : null).ConfigureAwait (false);
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
							cp != null ? cp.CreateCompilationOptions () : null,
							cp != null ? cp.CreateParseOptions (config) : null,
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

			internal class SolutionData
			{
				public ConcurrentDictionary<string, TextLoader> Files = new ConcurrentDictionary<string, TextLoader> ();
			}

			internal static Func<SolutionData, string, TextLoader> CreateTextLoader = (data, fileName) => data.Files.GetOrAdd (fileName, a => new MonoDevelopTextLoader (a));



			internal Task<SolutionInfo> CreateSolutionInfo (MonoDevelop.Projects.Solution solution, CancellationToken token)
			{
				return Task.Run (async delegate {
					var timer = Counters.AnalysisTimer.BeginTiming ();
					try {
						var projects = new ConcurrentBag<ProjectInfo> ();
						var mdProjects = solution.GetAllProjects ();
						ImmutableList<ProjectionEntry> toDispose;
						lock (projectionListUpdateLock) {
							toDispose = projectionList;
							projectionList = projectionList.Clear ();
						}
						foreach (var p in toDispose)
							p.Dispose ();

						solutionData = new SolutionData ();
						List<Task> allTasks = new List<Task> ();
						foreach (var proj in mdProjects) {
							if (token.IsCancellationRequested)
								return null;
							var netProj = proj as MonoDevelop.Projects.DotNetProject;
							if (netProj != null && !netProj.SupportsRoslyn)
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
						var modifiedWhileLoading = modifiedProjects;
						modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
						var solutionId = GetSolutionId (solution);
						var solutionInfo = SolutionInfo.Create (solutionId, VersionStamp.Create (), solution.FileName, projects);
						foreach (var project in modifiedWhileLoading) {
							if (solution.ContainsItem (project)) {
								return await CreateSolutionInfo (solution, token).ConfigureAwait (false);
							}
						}

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
					} finally {
						timer.End ();
					}
				});
			}

			internal Tuple<List<DocumentInfo>, List<DocumentInfo>> CreateDocuments (ProjectData projectData, MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.ProjectFile [] sourceFiles, ProjectData oldProjectData)
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

					SourceCodeKind sck;
					if (TypeSystemParserNode.IsCompileableFile (f, out sck) || CanGenerateAnalysisContextForNonCompileable (p, f)) {
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
					yield return DocumentInfo.Create (
						projectData.GetOrCreateDocumentId (projection.Document.FileName, oldProjectData),
						plainName,
						new [] { p.Name }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
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
				return DocumentInfo.Create (
					id.GetOrCreateDocumentId (filePath),
					filePath,
					new [] { projectName }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
					sourceCodeKind,
					CreateTextLoader (data, f.Name),
					f.Name,
					false
				);
			}

			internal IReadOnlyList<ProjectionEntry> ProjectionList {
				get {
					return projectionList;
				}
			}

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
		}
	}
}
