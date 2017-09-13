//
// MonoDevelopWorkspace.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Host;
using MonoDevelop.Core.Text;
using System.Collections.Concurrent;
using MonoDevelop.Ide.CodeFormatting;
using Gtk;
using MonoDevelop.Ide.Editor.Projection;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Text;
using System.Collections.Immutable;
using System.ComponentModel;
using Mono.Addins;
using MonoDevelop.Core.AddIns;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace MonoDevelop.Ide.TypeSystem
{

	public class MonoDevelopWorkspace : Workspace
	{
		public const string ServiceLayer = nameof(MonoDevelopWorkspace);

		readonly static HostServices services;
		internal readonly WorkspaceId Id;

		CancellationTokenSource src = new CancellationTokenSource ();
		bool disposed;
		readonly MonoDevelop.Projects.Solution monoDevelopSolution;
		object addLock = new object();
		bool added;

		public MonoDevelop.Projects.Solution MonoDevelopSolution {
			get {
				return monoDevelopSolution;
			}
		}

		static string[] mefHostServices = new [] {
			"Microsoft.CodeAnalysis.Workspaces",
			//FIXME: this does not load yet. We should provide alternate implementations of its services.
			//"Microsoft.CodeAnalysis.Workspaces.Desktop",
			"Microsoft.CodeAnalysis.Features",
			"Microsoft.CodeAnalysis.CSharp",
			"Microsoft.CodeAnalysis.CSharp.Workspaces",
			"Microsoft.CodeAnalysis.CSharp.Features",
			"Microsoft.CodeAnalysis.VisualBasic",
			"Microsoft.CodeAnalysis.VisualBasic.Workspaces",
			"Microsoft.CodeAnalysis.VisualBasic.Features",
		};

		internal static HostServices HostServices {
			get {
				return services;
			}
		}

		static MonoDevelopWorkspace ()
		{
			List<Assembly> assemblies = new List<Assembly> ();
			assemblies.Add (typeof (MonoDevelopWorkspace).Assembly);
			foreach (var asmName in mefHostServices) {
				try {
					var asm = Assembly.Load (asmName);
					if (asm == null)
						continue;
					assemblies.Add (asm);
				} catch (Exception ex) {
					LoggingService.LogError ("Error - can't load host service assembly: " + asmName, ex);
				}
			}
			assemblies.Add (typeof(MonoDevelopWorkspace).Assembly);
			foreach (var node in AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/TypeService/MefHostServices")) {
				var assemblyNode = node as AssemblyExtensionNode;
				if (assemblyNode == null)
					continue;
				try {
					var assemblyFilePath = assemblyNode.Addin.GetFilePath(assemblyNode.FileName);
					var assembly = Assembly.LoadFrom(assemblyFilePath);
					assemblies.Add (assembly);
				} catch (Exception e) {
					LoggingService.LogError ("Workspace can't load assembly " + assemblyNode.FileName + " to host mef services.", e);
					continue;
				}
			}
			services = MefHostServices.Create (assemblies);
		}

		/// <summary>
		/// This bypasses the type system service. Use with care.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal void OpenSolutionInfo (SolutionInfo sInfo)
		{
			OnSolutionAdded (sInfo);
		}

		internal MonoDevelopWorkspace (MonoDevelop.Projects.Solution solution) : base (services, "MonoDevelop")
		{
			this.monoDevelopSolution = solution;
			this.Id = WorkspaceId.Next ();
			if (IdeApp.Workspace != null && solution != null) {
				IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;
			}
		}

		protected override void Dispose (bool finalize)
		{
			base.Dispose (finalize);
			if (disposed)
				return;
			disposed = true;
			CancelLoad ();
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged -= HandleActiveConfigurationChanged;
			}
			foreach (var prj in monoDevelopSolution.GetAllProjects ()) {
				UnloadMonoProject (prj);
			}
		}

		internal void InformDocumentTextChange (DocumentId id, SourceText text)
		{
			base.ApplyDocumentTextChanged (id, text);
		}

		void CancelLoad ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		static StatusBarIcon statusIcon = null;
		static int workspacesLoading = 0;

		internal static event EventHandler LoadingFinished;

		static void OnLoadingFinished (EventArgs e)
		{
			var handler = LoadingFinished;
			if (handler != null)
				handler (null, e);
		}

		internal void HideStatusIcon ()
		{
			Gtk.Application.Invoke ((o, args) => {
				workspacesLoading--;
				if (workspacesLoading == 0 && statusIcon != null) {
					statusIcon.Dispose ();
					statusIcon = null;
					OnLoadingFinished (EventArgs.Empty);
					WorkspaceLoaded?.Invoke (this, EventArgs.Empty);
				}
			});
		}

		public event EventHandler WorkspaceLoaded;

		internal void ShowStatusIcon ()
		{
			Gtk.Application.Invoke ((o, args) => {
				workspacesLoading++;
				if (statusIcon != null)
					return;
				statusIcon = IdeApp.Workbench?.StatusBar.ShowStatusIcon (ImageService.GetIcon ("md-parser"));
				if (statusIcon != null)
					statusIcon.ToolTip = GettextCatalog.GetString ("Gathering class information");
			});
		}

		async void HandleActiveConfigurationChanged (object sender, EventArgs e)
		{
			ShowStatusIcon ();
			CancelLoad ();
			var token = src.Token;

			try {
				var si = await CreateSolutionInfo (monoDevelopSolution, token).ConfigureAwait (false);
				if (si != null)
					OnSolutionReloaded (si);
			} catch (OperationCanceledException) {
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (x => x is OperationCanceledException);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while reloading solution.", ex);
			} finally {
				HideStatusIcon ();
			}
		}

		SolutionData solutionData;
		Task<SolutionInfo> CreateSolutionInfo (MonoDevelop.Projects.Solution solution, CancellationToken token)
		{
			return Task.Run (async delegate {
				var projects = new ConcurrentBag<ProjectInfo> ();
				var mdProjects = solution.GetAllProjects ();
				projectionList = projectionList.Clear ();
				projectIdMap.Clear ();
				projectIdToMdProjectMap = projectIdToMdProjectMap.Clear ();
				projectDataMap.Clear ();
				solutionData = new SolutionData ();
				List<Task> allTasks = new List<Task> ();
				foreach (var proj in mdProjects) {
					if (token.IsCancellationRequested)
						return null;
					var netProj = proj as MonoDevelop.Projects.DotNetProject;
					if (netProj != null && !netProj.SupportsRoslyn)
						continue;
					var tp = LoadProject (proj, token).ContinueWith (t => {
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
				var solutionInfo = SolutionInfo.Create (GetSolutionId (solution), VersionStamp.Create (), solution.FileName, projects);
				foreach (var project in modifiedWhileLoading) {
					if (solution.ContainsItem (project)) {
						return await CreateSolutionInfo (solution, token).ConfigureAwait (false);
					}
				}

				lock (addLock) {
					if (!added) {
						added = true;
						OnSolutionAdded (solutionInfo);
					}
				}
				return solutionInfo;
			});
		}

		internal Task<SolutionInfo> TryLoadSolution (CancellationToken cancellationToken = default(CancellationToken))
		{
			return CreateSolutionInfo (monoDevelopSolution, CancellationTokenSource.CreateLinkedTokenSource (cancellationToken, src.Token).Token);
		}

		internal void UnloadSolution ()
		{
			OnSolutionRemoved (); 
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

		ConcurrentDictionary<MonoDevelop.Projects.Project, ProjectId> projectIdMap = new ConcurrentDictionary<MonoDevelop.Projects.Project, ProjectId> ();
		ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project> projectIdToMdProjectMap = ImmutableDictionary<ProjectId, MonoDevelop.Projects.Project>.Empty;
		ConcurrentDictionary<ProjectId, ProjectData> projectDataMap = new ConcurrentDictionary<ProjectId, ProjectData> ();

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
				ProjectId result;
				if (!projectIdMap.TryGetValue (p, out result)) {
					result = ProjectId.CreateNewId (p.Name);
					projectIdMap [p] = result;
					projectIdToMdProjectMap = projectIdToMdProjectMap.Add (result, p);
				}
				return result;
			}
		}

		ProjectData GetProjectData (ProjectId id)
		{
			lock (projectIdMap) {
				ProjectData result;

				if (projectDataMap.TryGetValue (id, out result)) {
					return result;
				}
				return null;
			}
		}

		ProjectData CreateProjectData (ProjectId id)
		{
			lock (projectIdMap) {
				var result = new ProjectData (id);
				projectDataMap [id] = result;
				return result;
			}
		}

		class ProjectData
		{
			readonly ProjectId projectId;
			readonly Dictionary<string, DocumentId> documentIdMap;

			public ProjectInfo Info {
				get;
				set;
			}

			public ProjectData (ProjectId projectId)
			{
				this.projectId = projectId;
				documentIdMap = new Dictionary<string, DocumentId> (FilePath.PathComparer);
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
					documentIdMap[name] = id;
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
		}

		internal DocumentId GetDocumentId (ProjectId projectId, string name)
		{
			var data = GetProjectData (projectId);
			if (data == null)
				return null;
			return data.GetDocumentId (name);
		}

		void UnloadMonoProject (MonoDevelop.Projects.Project project)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));
			project.Modified -= OnProjectModified;
		}

		async Task<ProjectInfo> LoadProject (MonoDevelop.Projects.Project p, CancellationToken token)
		{
			if (!projectIdMap.ContainsKey (p)) {
				p.Modified += OnProjectModified;
			}

			var projectId = GetOrCreateProjectId (p);

			//when reloading e.g. after a save, preserve document IDs
			var oldProjectData = GetProjectData (projectId);
			var projectData = CreateProjectData (projectId);

			var references = await CreateMetadataReferences (p, projectId, token).ConfigureAwait (false);
			if (token.IsCancellationRequested)
				return null;
			var config = IdeApp.Workspace != null ? p.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as MonoDevelop.Projects.DotNetProjectConfiguration : null;
			MonoDevelop.Projects.DotNetCompilerParameters cp = null;
			if (config != null)
				cp = config.CompilationParameters;
			FilePath fileName = IdeApp.Workspace != null ? p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration) : (FilePath)"";
			if (fileName.IsNullOrEmpty)
				fileName = new FilePath (p.Name + ".dll");

			if (token.IsCancellationRequested)
				return null;
			var sourceFiles = await p.GetSourceFilesAsync (config != null ? config.Selector : null).ConfigureAwait (false);
			var documents = CreateDocuments (projectData, p, token, sourceFiles, oldProjectData);
			if (documents == null)
				return null;
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
				await CreateProjectReferences (p, token),
				references,
				additionalDocuments: documents.Item2
			);
			projectData.Info = info;
			return info;
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
						break;
					}
				}
				projectionList = projectionList.Add (new ProjectionEntry { File = projectFile, Projections = projections });
			}

		}

		internal class SolutionData
		{
			public ConcurrentDictionary<string, TextLoader> Files = new ConcurrentDictionary<string, TextLoader> (); 
		}

		internal static Func<SolutionData, string, TextLoader> CreateTextLoader = (data, fileName) => data.Files.GetOrAdd (fileName, a => new MonoDevelopTextLoader (a));

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
		object projectionListUpdateLock = new object ();
		ImmutableList<ProjectionEntry> projectionList = ImmutableList<ProjectionEntry>.Empty;

		internal IReadOnlyList<ProjectionEntry> ProjectionList {
			get {
				return projectionList;
			}
		}

		internal class ProjectionEntry
		{
			public MonoDevelop.Projects.ProjectFile File;
			public IReadOnlyList<Projection> Projections;
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
					var id = projectData.GetOrCreateDocumentId (f.Name, projectData);
					if (!duplicates.Add (id))
						continue;
					additionalDocuments.Add (CreateDocumentInfo (solutionData, p.Name, projectData, f, sck));

					foreach (var projectedDocument in GenerateProjections (f, projectData, p, oldProjectData)) {
						var projectedId = projectData.GetOrCreateDocumentId (projectedDocument.FilePath, oldProjectData);
						if (!duplicates.Add (projectedId))
							continue;
						documents.Add (projectedDocument);
					}
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
			projectionList = projectionList.Add (entry);
		}

		internal static readonly string [] DefaultAssemblies = {
			typeof(string).Assembly.Location,                                // mscorlib
			typeof(System.Text.RegularExpressions.Regex).Assembly.Location,  // System
			typeof(System.Linq.Enumerable).Assembly.Location,                // System.Core
			typeof(System.Data.VersionNotFoundException).Assembly.Location,  // System.Data
			typeof(System.Xml.XmlDocument).Assembly.Location,                // System.Xml
		};


		static async Task<List<MetadataReference>> CreateMetadataReferences (MonoDevelop.Projects.Project proj, ProjectId projectId, CancellationToken token)
		{
			List<MetadataReference> result = new List<MetadataReference> ();

			var netProject = proj as MonoDevelop.Projects.DotNetProject;
			if (netProject == null) {
				// create some default references for unsupported project types.
				foreach (var asm in DefaultAssemblies) {
					var metadataReference = MetadataReferenceCache.LoadReference (projectId, asm);
					result.Add (metadataReference);
				}
				return result;
			}
			var configurationSelector = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default;
			var hashSet = new HashSet<string> (FilePath.PathComparer);

			try {
				foreach (var file in await netProject.GetReferencedAssemblies (configurationSelector, false).ConfigureAwait (false)) {
					if (token.IsCancellationRequested)
						return result;
					string fileName;
					if (!Path.IsPathRooted (file.FilePath)) {
						fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file.FilePath);
					} else {
						fileName = file.FilePath.FullPath;
					}
					if (!hashSet.Add (fileName))
						continue;
					var metadataReference = MetadataReferenceCache.LoadReference (projectId, fileName);
					if (metadataReference == null)
						continue;
					metadataReference = metadataReference.WithAliases (file.EnumerateAliases ());
					result.Add (metadataReference);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting referenced assemblies", e);
			}

			try {
				foreach (var pr in netProject.GetReferencedItems (configurationSelector)) {
					if (token.IsCancellationRequested)
						return result;
					var referencedProject = pr as MonoDevelop.Projects.DotNetProject;
					if (referencedProject == null)
						continue;
					if (TypeSystemService.IsOutputTrackedProject (referencedProject)) {
						var fileName = referencedProject.GetOutputFileName (configurationSelector);
						if (!hashSet.Add (fileName))
							continue;
						var metadataReference = MetadataReferenceCache.LoadReference (projectId, fileName);
						if (metadataReference != null)
							result.Add (metadataReference);
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting referenced projects", e);
			}
			return result;
		}

		async Task<IEnumerable<ProjectReference>> CreateProjectReferences (MonoDevelop.Projects.Project p, CancellationToken token)
		{
			var netProj = p as MonoDevelop.Projects.DotNetProject;
			if (netProj == null)
				return Enumerable.Empty<ProjectReference> ();

			List<MonoDevelop.Projects.AssemblyReference> references;
			try {
				var config = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default;
				references = await netProj.GetReferences (config, token).ConfigureAwait (false);
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting referenced projects.", e);
				return Enumerable.Empty<ProjectReference> ();
			};
			return CreateProjectReferences (netProj, references);
		}

		IEnumerable<ProjectReference> CreateProjectReferences (MonoDevelop.Projects.DotNetProject p, List<MonoDevelop.Projects.AssemblyReference> references)
		{
			var addedProjects = new HashSet<MonoDevelop.Projects.DotNetProject> ();
			foreach (var pr in references.Where (r => r.IsProjectReference && r.ReferenceOutputAssembly)) {
				var referencedProject = pr.GetReferencedItem (p.ParentSolution) as MonoDevelop.Projects.DotNetProject;
				if (referencedProject == null || !addedProjects.Add (referencedProject))
					continue;
				if (TypeSystemService.IsOutputTrackedProject (referencedProject))
					continue;
				yield return new ProjectReference (
					GetOrCreateProjectId (referencedProject),
					ImmutableArray<string>.Empty.AddRange (pr.EnumerateAliases ()));
			}
		}

		#region Open documents
		public override bool CanOpenDocuments {
			get {
				return true;
			}
		}

		public override void OpenDocument (DocumentId documentId, bool activate = true)
		{
			var doc = GetDocument (documentId);
			if (doc != null) {
				var mdProject = GetMonoProject (doc.Project);
				if (doc != null) {
					IdeApp.Workbench.OpenDocument (doc.FilePath, mdProject, activate);
				}
			}
		}

		List<MonoDevelopSourceTextContainer> openDocuments = new List<MonoDevelopSourceTextContainer>();
		internal void InformDocumentOpen (DocumentId documentId, TextEditor editor)
		{
			var document = InternalInformDocumentOpen (documentId, editor);
			if (document as Document != null) {
				foreach (var linkedDoc in ((Document)document).GetLinkedDocumentIds ()) {
					InternalInformDocumentOpen (linkedDoc, editor);
				}
			}
		}

		TextDocument InternalInformDocumentOpen (DocumentId documentId, TextEditor editor)
		{
			var project = this.CurrentSolution.GetProject (documentId.ProjectId);
			if (project == null)
				return null;
			TextDocument document = project.GetDocument (documentId) ?? project.GetAdditionalDocument (documentId);
			if (document == null || openDocuments.Any (d => d.Id == documentId)) {
				return document;
			}
			var monoDevelopSourceTextContainer = new MonoDevelopSourceTextContainer (this, documentId, editor);
			lock (openDocuments) {
				openDocuments.Add (monoDevelopSourceTextContainer);
			}
			if (document is Document) {
				OnDocumentOpened (documentId, monoDevelopSourceTextContainer);
			} else {
				OnAdditionalDocumentOpened (documentId, monoDevelopSourceTextContainer);
			}
			return document;
		}

		ProjectChanges projectChanges;

		protected override void OnDocumentTextChanged (Document document)
		{
			base.OnDocumentTextChanged (document);
		}

		protected override void OnDocumentClosing (DocumentId documentId)
		{
			base.OnDocumentClosing (documentId);
			lock (openDocuments) {
				var openDoc = openDocuments.FirstOrDefault (d => d.Id == documentId);
				if (openDoc != null) {
					openDoc.Dispose ();
					openDocuments.Remove (openDoc);
				}
			}
		}

//		internal override bool CanChangeActiveContextDocument {
//			get {
//				return true;
//			}
//		}

		internal void InformDocumentClose (DocumentId analysisDocument, string filePath)
		{
			try {
				lock (openDocuments) {
					var openDoc = openDocuments.FirstOrDefault (d => d.Id == analysisDocument);
					if (openDoc != null) {
						openDoc.Dispose ();
						openDocuments.Remove (openDoc);
					}
				}
				if (!CurrentSolution.ContainsDocument (analysisDocument))
					return;
				var loader = new MonoDevelopTextLoader (filePath);
				var document = this.GetDocument (analysisDocument);
				var openDocument = this.openDocuments.FirstOrDefault (w => w.Id == analysisDocument);
				if (openDocument != null) {
					openDocument.Dispose ();
					openDocuments.Remove (openDocument);
				}

				if (document == null) {
					var ad = this.GetAdditionalDocument (analysisDocument);
					if (ad != null)
						OnAdditionalDocumentClosed (analysisDocument, loader);
					return;
				}
				OnDocumentClosed (analysisDocument, loader);
				foreach (var linkedDoc in document.GetLinkedDocumentIds ()) {
					OnDocumentClosed (linkedDoc, loader); 
				}
			} catch (Exception e) {
				LoggingService.LogError ("Exception while closing document.", e); 
			}
		}
		
		public override void CloseDocument (DocumentId documentId)
		{
		}

		//FIXME: this should NOT be async. our implementation is doing some very expensive things like formatting that it shouldn't need to do.
		protected override void ApplyDocumentTextChanged (DocumentId id, SourceText text)
		{
			tryApplyState_documentTextChangedTasks.Add (ApplyDocumentTextChangedCore (id, text));
		}

		async Task ApplyDocumentTextChangedCore (DocumentId id, SourceText text)
		{
			var document = GetDocument (id);
			if (document == null)
				return;
			bool isOpen;
			var filePath = document.FilePath;
			Projection projection = null;
			foreach (var entry in ProjectionList) {
				var p = entry.Projections.FirstOrDefault (proj => proj?.Document?.FileName != null && FilePath.PathComparer.Equals (proj.Document.FileName, filePath));
				if (p != null) {
					filePath = entry.File.FilePath;
					projection = p;
					break;
				}
			}
			var data = TextFileProvider.Instance.GetTextEditorData (filePath, out isOpen);
			// Guard against already done changes in linked files.
			// This shouldn't happen but the roslyn merging seems not to be working correctly in all cases :/
			if (document.GetLinkedDocumentIds ().Length > 0 && isOpen && !(text.GetType ().FullName == "Microsoft.CodeAnalysis.Text.ChangedText")) {
				return;
			}

			SourceText formerText;
			lock (tryApplyState_documentTextChangedContents) {
				if (tryApplyState_documentTextChangedContents.TryGetValue (filePath, out formerText)) {
					if (formerText.Length == text.Length && formerText.ToString () == text.ToString ())
						return;
				}
				tryApplyState_documentTextChangedContents[filePath] = text;
			}

			SourceText oldFile;
			if (!isOpen || !document.TryGetText (out oldFile)) {
				oldFile = await document.GetTextAsync ();
			}
			var changes = text.GetTextChanges (oldFile).OrderByDescending (c => c.Span.Start).ToList ();
			int delta = 0;

			if (!isOpen) {
				delta = ApplyChanges (projection, data, changes);
				var formatter = CodeFormatterService.GetFormatter (data.MimeType);
				if (formatter != null && formatter.SupportsPartialDocumentFormatting) {
					var mp = GetMonoProject (CurrentSolution.GetProject (id.ProjectId));
					string currentText = data.Text;

					foreach (var change in changes) {
						delta -= change.Span.Length - change.NewText.Length;
						var startOffset = change.Span.Start - delta;

						if (projection != null) {
							int originalOffset;
							if (projection.TryConvertFromProjectionToOriginal (startOffset, out originalOffset))
								startOffset = originalOffset;
						}

						string str;
						if (change.NewText.Length == 0) {
							str = formatter.FormatText (mp.Policies, currentText, TextSegment.FromBounds (Math.Max (0, startOffset - 1), Math.Min (data.Length, startOffset + 1)));
						} else {
							str = formatter.FormatText (mp.Policies, currentText, new TextSegment (startOffset, change.NewText.Length));
						}
						data.ReplaceText (startOffset, change.NewText.Length, str);
					}
				}
				data.Save ();
				if (projection != null) {
					await UpdateProjectionsDocuments (document, data);
				} else {
					OnDocumentTextChanged (id, new MonoDevelopSourceText (data), PreservationMode.PreserveValue);
				}
				FileService.NotifyFileChanged (filePath);
			} else {
				var formatter = CodeFormatterService.GetFormatter (data.MimeType);
				var documentContext = IdeApp.Workbench.Documents.FirstOrDefault (d => FilePath.PathComparer.Compare (d.FileName, filePath) == 0);
				var root = await projectChanges.NewProject.GetDocument (id).GetSyntaxRootAsync ();
				var annotatedNode = root.DescendantNodesAndSelf ().FirstOrDefault (n => n.HasAnnotation (TypeSystemService.InsertionModeAnnotation));
				SyntaxToken? renameTokenOpt = root.GetAnnotatedNodesAndTokens (Microsoft.CodeAnalysis.CodeActions.RenameAnnotation.Kind)
												  .Where (s => s.IsToken)
												  .Select (s => s.AsToken ())
												  .Cast<SyntaxToken?> ()
												  .FirstOrDefault ();

				if (documentContext != null) {
					var editor = (TextEditor)data;
					await Runtime.RunInMainThread (async () => {
						using (var undo = editor.OpenUndoGroup ()) {
							var oldVersion = editor.Version;
							delta = ApplyChanges (projection, data, changes);
							var versionBeforeFormat = editor.Version;

							if (formatter != null && formatter.SupportsOnTheFlyFormatting) {
								foreach (var change in changes) {
									delta -= change.Span.Length - change.NewText.Length;
									var startOffset = change.Span.Start - delta;
									if (projection != null) {
										int originalOffset;
										if (projection.TryConvertFromProjectionToOriginal (startOffset, out originalOffset))
											startOffset = originalOffset;
									}
									if (change.NewText.Length == 0) {
										formatter.OnTheFlyFormat (editor, documentContext, TextSegment.FromBounds (Math.Max (0, startOffset - 1), Math.Min (data.Length, startOffset + 1)));
									} else {
										formatter.OnTheFlyFormat (editor, documentContext, new TextSegment (startOffset, change.NewText.Length));
									}
								}
							}
							if (annotatedNode != null && GetInsertionPoints != null) {
								IdeApp.Workbench.Documents.First (d => d.FileName == editor.FileName).Select ();
								var formattedVersion = editor.Version;

								int startOffset = versionBeforeFormat.MoveOffsetTo (editor.Version, annotatedNode.Span.Start);
								int endOffset = versionBeforeFormat.MoveOffsetTo (editor.Version, annotatedNode.Span.End);

								// alway whole line start & delimiter
								var startLine = editor.GetLineByOffset (startOffset);
								startOffset = startLine.Offset;

								var endLine = editor.GetLineByOffset (endOffset);
								endOffset = endLine.EndOffsetIncludingDelimiter + 1;

								var insertionCursorSegment = TextSegment.FromBounds (startOffset, endOffset);
								string textToInsert = editor.GetTextAt (insertionCursorSegment).TrimEnd ();
								editor.RemoveText (insertionCursorSegment);
								var insertionPoints = await GetInsertionPoints (editor, editor.CaretOffset);
								if (insertionPoints.Count == 0) {
									// Just to get sure if no insertion points -> go back to the formatted version.
									var textChanges = editor.Version.GetChangesTo (formattedVersion).ToList ();
									using (var undo2 = editor.OpenUndoGroup ()) {
										foreach (var textChange in textChanges) {
											foreach (var v in textChange.TextChanges.Reverse ()) {
												editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
											}
										}
									}
									return;
								}
								string insertionModeOperation;
								const int CSharpMethodKind = 8875;


								bool isMethod = annotatedNode.RawKind == CSharpMethodKind;

								if (!isMethod) {
									// atm only for generate field/property : remove all new lines generated & just insert the plain node.
									// for methods it's not so easy because of "extract code" changes.
									foreach (var textChange in editor.Version.GetChangesTo (oldVersion).ToList ()) {
										foreach (var v in textChange.TextChanges.Reverse ()) {
											editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
										}
									}
								}

								switch (annotatedNode.RawKind) {
								case 8873: // C# field
									insertionModeOperation = GettextCatalog.GetString ("Insert Field");
									break;
								case CSharpMethodKind:
									insertionModeOperation = GettextCatalog.GetString ("Insert Method");
									break;
								case 8892: // C# property 
									insertionModeOperation = GettextCatalog.GetString ("Insert Property");
									break;
								default:
									insertionModeOperation = GettextCatalog.GetString ("Insert Code");
									break;
								}

								var options = new InsertionModeOptions (
									insertionModeOperation,
									insertionPoints,
									point => {
										if (!point.Success)
											return;
										point.InsertionPoint.Insert (editor, textToInsert);
									}
								);
								options.ModeExitedAction += delegate (InsertionCursorEventArgs args) {
									if (!args.Success) {
										var textChanges = editor.Version.GetChangesTo (oldVersion).ToList ();
										using (var undo2 = editor.OpenUndoGroup ()) {
											foreach (var textChange in textChanges) {
												foreach (var v in textChange.TextChanges.Reverse ())
													editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
											}
										}
									}
								};
								for (int i = 0; i < insertionPoints.Count; i++) {
									if (insertionPoints [i].Location.Line < editor.CaretLine) {
										options.FirstSelectedInsertionPoint = Math.Min (isMethod ? i + 1 : i, insertionPoints.Count - 1);
									} else {
										break;
									}
								}
								options.ModeExitedAction += delegate {
									if (renameTokenOpt.HasValue)
										StartRenameSession (editor, documentContext, versionBeforeFormat, renameTokenOpt.Value);
								};
								editor.StartInsertionMode (options);
							}
						}
					});
				}

				if (projection != null) {
					await UpdateProjectionsDocuments (document, data);
				} else {
					OnDocumentTextChanged (id, new MonoDevelopSourceText (data.CreateDocumentSnapshot ()), PreservationMode.PreserveValue);
				}
			}
		}
		internal static Func<TextEditor, int, Task<List<InsertionPoint>>> GetInsertionPoints;
		internal static Action<TextEditor, DocumentContext, ITextSourceVersion, SyntaxToken?> StartRenameSession;

		async Task UpdateProjectionsDocuments (Document document, ITextDocument data)
		{
			var project = TypeSystemService.GetMonoProject (document.Project);
			var file = project.Files.GetFile (data.FileName);
			var node = TypeSystemService.GetTypeSystemParserNode (data.MimeType, file.BuildAction);
			if (node != null && node.Parser.CanGenerateProjection (data.MimeType, file.BuildAction, project.SupportedLanguages)) {
				var options = new ParseOptions {
					FileName = file.FilePath,
					Project = project,
					Content = TextFileProvider.Instance.GetReadOnlyTextEditorData (file.FilePath),
				};
				var projections = await node.Parser.GenerateProjections (options);
				UpdateProjectionEntry (file, projections);
				var projectId = GetProjectId (project);
				var projectdata = GetProjectData (projectId);
				foreach (var projected in projections) {
					OnDocumentTextChanged (projectdata.GetDocumentId (projected.Document.FileName), new MonoDevelopSourceText (projected.Document), PreservationMode.PreserveValue);
				}
			}
		}

		static int ApplyChanges (Projection projection, ITextDocument data, List<Microsoft.CodeAnalysis.Text.TextChange> changes)
		{
			int delta = 0;
			foreach (var change in changes) {
				var offset = change.Span.Start;

				if (projection != null) {
					int originalOffset;
					//If change is outside projection segments don't apply it...
					if (projection.TryConvertFromProjectionToOriginal (offset, out originalOffset)) {
						offset = originalOffset;
						data.ReplaceText (offset, change.Span.Length, change.NewText);
						delta += change.Span.Length - change.NewText.Length;
					}
				} else {
					data.ReplaceText (offset, change.Span.Length, change.NewText);
					delta += change.Span.Length - change.NewText.Length;
				}
			}

			return delta;
		}

		// used to pass additional state from Apply* to TryApplyChanges so it can batch certain operations such as saving projects
		HashSet<MonoDevelop.Projects.Project> tryApplyState_changedProjects = new HashSet<MonoDevelop.Projects.Project> ();
		List<Task> tryApplyState_documentTextChangedTasks = new List<Task> ();
		Dictionary<string, SourceText> tryApplyState_documentTextChangedContents =  new Dictionary<string, SourceText> ();

		internal override bool TryApplyChanges (Solution newSolution, IProgressTracker progressTracker)
		{
			// this is supported on the main thread only
			// see https://github.com/dotnet/roslyn/pull/18043
			// as a result, we can assume that the things it calls are _also_ main thread only
			Runtime.CheckMainThread ();
			lock (projectModifyLock) {
				freezeProjectModify = true;
				try {
					var ret = base.TryApplyChanges (newSolution, progressTracker);

					if (tryApplyState_documentTextChangedTasks.Count > 0) {
						Task.WhenAll (tryApplyState_documentTextChangedTasks).ContinueWith (t => {
							try {
								t.Wait ();
							} catch (Exception ex) {
								LoggingService.LogError ("Error applying changes to documents", ex);
							}
							if (IdeApp.Workbench != null) {
								var changedFiles = new HashSet<string> (tryApplyState_documentTextChangedContents.Keys, FilePath.PathComparer);
								foreach (var w in IdeApp.Workbench.Documents) {
									if (w.IsFile && changedFiles.Contains (w.FileName)) {
										w.StartReparseThread ();
									}
								}
							}
						}, CancellationToken.None, TaskContinuationOptions.None, Runtime.MainTaskScheduler);
					}

					if (tryApplyState_changedProjects.Count > 0) {
						IdeApp.ProjectOperations.SaveAsync (tryApplyState_changedProjects);
					}

					return ret;
				} finally {
					tryApplyState_documentTextChangedContents.Clear ();
					tryApplyState_documentTextChangedTasks.Clear ();
					tryApplyState_changedProjects.Clear ();
					freezeProjectModify = false; 
				}
			}
		}

		public override bool CanApplyChange (ApplyChangesKind feature)
		{
			switch (feature) {
			case ApplyChangesKind.AddDocument:
			case ApplyChangesKind.RemoveDocument:
			case ApplyChangesKind.ChangeDocument:
			//HACK: we don't actually support adding and removing metadata references from project
			//however, our MetadataReferenceCache currently depends on (incorrectly) using TryApplyChanges
			case ApplyChangesKind.AddMetadataReference:
			case ApplyChangesKind.RemoveMetadataReference:
				return true;
			default:
				return false;
			}
		}

		protected override void ApplyProjectChanges (ProjectChanges projectChanges)
		{
			this.projectChanges = projectChanges;
			base.ApplyProjectChanges (projectChanges);
		}

		protected override void ApplyDocumentAdded (DocumentInfo info, SourceText text)
		{
			var id = info.Id;
			MonoDevelop.Projects.Project mdProject = null;

			if (id.ProjectId != null) {
				var project = CurrentSolution.GetProject (id.ProjectId);
				mdProject = GetMonoProject (project);
				if (mdProject == null)
					LoggingService.LogWarning ("Couldn't find project for newly generated file {0} (Project {1}).", info.Name, info.Id.ProjectId);
			}

			var path = DetermineFilePath (info.Id, info.Name, info.FilePath, info.Folders, mdProject?.FileName.ParentDirectory, true);
			info = info.WithFilePath (path).WithTextLoader (new MonoDevelopTextLoader (path));

			string formattedText;
			var formatter = CodeFormatterService.GetFormatter (DesktopService.GetMimeTypeForUri (path)); 
			if (formatter != null && mdProject != null) {
				formattedText = formatter.FormatText (mdProject.Policies, text.ToString ());
			} else {
				formattedText = text.ToString ();
			}

			var textSource = new StringTextSource (formattedText, text.Encoding ?? System.Text.Encoding.UTF8);
			try {
				textSource.WriteTextTo (path);
			} catch (Exception e) {
				LoggingService.LogError ("Exception while saving file to " + path, e);
			}

			if (mdProject != null) {
				var data = GetProjectData (id.ProjectId);
				data.AddDocumentId (info.Id, path);
				var file = new MonoDevelop.Projects.ProjectFile (path);
				mdProject.Files.Add (file);
				tryApplyState_changedProjects.Add (mdProject);
			}

			this.OnDocumentAdded (info);
		}

		protected override void ApplyDocumentRemoved (DocumentId documentId)
		{
			var document = GetDocument (documentId);
			var mdProject = GetMonoProject (documentId.ProjectId);
			if (document == null || mdProject == null) {
				return;
			}

			FilePath filePath = document.FilePath;
			var projectFile = mdProject.Files.GetFile (filePath);
			if (projectFile == null) {
				return;
			}

			//force-close the old doc even if it's dirty
			var openDoc = IdeApp.Workbench.Documents.FirstOrDefault (d => d.IsFile && filePath.Equals (d.FileName));
			if (openDoc != null && openDoc.IsDirty) {
				openDoc.Save ();
				((Gui.SdiWorkspaceWindow)openDoc.Window).CloseWindow (true, true).Wait ();
			}

			//this will fire a OnDocumentRemoved event via OnFileRemoved
			mdProject.Files.Remove (projectFile);
			FileService.DeleteFile (filePath);
			tryApplyState_changedProjects.Add (mdProject);
		}

		string DetermineFilePath (DocumentId id, string name, string filePath, IReadOnlyList<string> docFolders, string defaultFolder, bool createDirectory = false)
		{
			var path = filePath;

			if (string.IsNullOrEmpty (path)) {
				var monoProject = GetMonoProject (id.ProjectId);

				// If the first namespace name matches the name of the project, then we don't want to
				// generate a folder for that.  The project is implicitly a folder with that name.
				IEnumerable<string> folders;
				if (docFolders != null && monoProject != null && docFolders.FirstOrDefault () == monoProject.Name) {
					folders = docFolders.Skip (1);
				} else {
					folders = docFolders;
				}

				if (folders.Any ()) {
					string baseDirectory = Path.Combine (monoProject?.BaseDirectory ?? monoDevelopSolution.BaseDirectory, Path.Combine (folders.ToArray ()));
					try {
						if (createDirectory && !Directory.Exists (baseDirectory))
							Directory.CreateDirectory (baseDirectory);
					} catch (Exception e) {
						LoggingService.LogError ("Error while creating directory for a new file : " + baseDirectory, e);
					}
					path = Path.Combine (baseDirectory, name);
				} else {
					path = Path.Combine (defaultFolder, name);
				}
			}
			return path;
		}
		#endregion

		internal Document GetDocument (DocumentId documentId, CancellationToken cancellationToken = default (CancellationToken))
		{
			var project = CurrentSolution.GetProject (documentId.ProjectId);
			if (project == null)
				return null;
			return project.GetDocument (documentId);
		}

		internal TextDocument GetAdditionalDocument (DocumentId documentId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var project = CurrentSolution.GetProject (documentId.ProjectId);
			if (project == null)
				return null;
			return project.GetAdditionalDocument (documentId);
		}

		internal async void UpdateFileContent (string fileName, string text)
		{
			SourceText newText = SourceText.From (text);
			foreach (var kv in this.projectDataMap) {
				var projectId = kv.Key;
				var docId = this.GetDocumentId (projectId, fileName);
				if (docId != null) {
					try {
						if (this.GetDocument (docId) != null) {
							base.OnDocumentTextChanged (docId, newText, PreservationMode.PreserveIdentity);
						} else if (this.GetAdditionalDocument (docId) != null) {
							base.OnAdditionalDocumentTextChanged (docId, newText, PreservationMode.PreserveIdentity);
						}
					} catch (Exception e) {
						LoggingService.LogWarning ("Roslyn error on text change", e);
					}
				}
				var monoProject = GetMonoProject (projectId);
				if (monoProject != null) {
					var pf = monoProject.GetProjectFile (fileName);
					if (pf != null) {
						var mimeType = DesktopService.GetMimeTypeForUri (fileName);
						if (TypeSystemService.CanParseProjections (monoProject, mimeType, fileName))
							await TypeSystemService.ParseProjection (new ParseOptions { Project = monoProject, FileName = fileName, Content = new StringTextSource(text), BuildAction = pf.BuildAction }, mimeType).ConfigureAwait (false);
					}
				}
			}
		}

		internal void RemoveProject (MonoDevelop.Projects.Project project)
		{
			var id = GetProjectId (project); 
			if (id != null) {
				foreach (var docId in GetOpenDocumentIds (id).ToList ()) {
					ClearOpenDocument (docId);
				}
				ProjectId val;
				projectIdMap.TryRemove (project, out val);
				projectIdToMdProjectMap = projectIdToMdProjectMap.Remove (val);
				ProjectData val2;
				projectDataMap.TryRemove (id, out val2);
				MetadataReferenceCache.RemoveReferences (id);

				UnloadMonoProject (project);
			}
		}

		#region Project modification handlers

		List<MonoDevelop.Projects.DotNetProject> modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
		object projectModifyLock = new object ();
		bool freezeProjectModify;
		void OnProjectModified (object sender, MonoDevelop.Projects.SolutionItemModifiedEventArgs args)
		{
			lock (projectModifyLock) {
				if (freezeProjectModify)
					return;
				try {
					if (!args.Any (x => x.Hint == "TargetFramework" || x.Hint == "References" || x.Hint == "CompilerParameters"))
						return;
					var project = sender as MonoDevelop.Projects.DotNetProject;
					if (project == null)
						return;
					var projectId = GetProjectId (project);
					if (CurrentSolution.ContainsProject (projectId)) {
						HandleActiveConfigurationChanged (this, EventArgs.Empty);
					} else {
						modifiedProjects.Add (project);
					}
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
				}
			}
		}

		#endregion

		/// <summary>
		/// Tries the get original file from projection. If the fileName / offset is inside a projection this method tries to convert it 
		/// back to the original physical file.
		/// </summary>
		internal bool TryGetOriginalFileFromProjection (string fileName, int offset, out string originalName, out int originalOffset)
		{
			foreach (var projectionEntry in ProjectionList) {
				var projection = projectionEntry.Projections.FirstOrDefault (p => FilePath.PathComparer.Equals (p.Document.FileName, fileName));
				if (projection != null) {
					if (projection.TryConvertFromProjectionToOriginal (offset, out originalOffset)) {
						originalName = projectionEntry.File.FilePath;
						return true;
					}
				}
			}

			originalName = fileName;
			originalOffset = offset;
			return false;
		}

	}

	//	static class MonoDevelopWorkspaceFeatures
	//	{
	//		static FeaturePack pack;
	//
	//		public static FeaturePack Features {
	//			get {
	//				if (pack == null)
	//					Interlocked.CompareExchange (ref pack, ComputePack (), null);
	//				return pack;
	//			}
	//		}
	//
	//		static FeaturePack ComputePack ()
	//		{
	//			var assemblies = new List<Assembly> ();
	//			var workspaceCoreAssembly = typeof(Workspace).Assembly;
	//			assemblies.Add (workspaceCoreAssembly);
	//
	//			LoadAssembly (assemblies, "Microsoft.CodeAnalysis.CSharp.Workspaces");
	//			//LoadAssembly (assemblies, "Microsoft.CodeAnalysis.VisualBasic.Workspaces");
	//
	//			var catalogs = assemblies.Select (a => new System.ComponentModel.Composition.Hosting.AssemblyCatalog (a));
	//
	//			return new MefExportPack (catalogs);
	//		}
	//
	//		static void LoadAssembly (List<Assembly> assemblies, string assemblyName)
	//		{
	//			try {
	//				var loadedAssembly = Assembly.Load (assemblyName);
	//				assemblies.Add (loadedAssembly);
	//			} catch (Exception e) {
	//				LoggingService.LogWarning ("Couldn't load assembly:" + assemblyName, e);
	//			}
	//		}
	//	}

	public class RoslynProjectEventArgs : EventArgs
	{
		public ProjectId ProjectId { get; private set; }

		public RoslynProjectEventArgs (ProjectId projectId)
		{
			ProjectId = projectId;
		}
	}

}
