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

namespace MonoDevelop.Ide.TypeSystem
{

	class MonoDevelopWorkspace : Workspace
	{
		readonly static HostServices services;
		public readonly WorkspaceId Id;

		CancellationTokenSource src = new CancellationTokenSource ();
		MonoDevelop.Projects.Solution currentMonoDevelopSolution;
		object addLock = new object();
		bool added;
		bool internalChanges;

		public MonoDevelop.Projects.Solution MonoDevelopSolution {
			get {
				return currentMonoDevelopSolution;
			}
		}

		static string[] mefHostServices = new [] {
			"Microsoft.CodeAnalysis.Workspaces",
			"Microsoft.CodeAnalysis.CSharp.Workspaces",
//			"Microsoft.CodeAnalysis.VisualBasic.Workspaces"
		};

		static MonoDevelopWorkspace ()
		{
			List<Assembly> assemblies = new List<Assembly> ();
			foreach (var asmName in mefHostServices) {
				try {
					var asm = Assembly.Load (asmName);
					if (asm == null)
						continue;
					assemblies.Add (asm);
				} catch (Exception) {
					LoggingService.LogError ("Error - can't load host service assembly: " + asmName);
				}
			}
			assemblies.Add (typeof(MonoDevelopWorkspace).Assembly);
			services = Microsoft.CodeAnalysis.Host.Mef.MefHostServices.Create (assemblies);
		}

		public MonoDevelopWorkspace () : base (services, ServiceLayer.Desktop)
		{
			this.Id = WorkspaceId.Next ();
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;
			}
		}

		protected override void Dispose (bool finalize)
		{
			base.Dispose (finalize);
			CancelLoad ();
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged -= HandleActiveConfigurationChanged;
			}
			if (currentMonoDevelopSolution != null) {
				foreach (var prj in currentMonoDevelopSolution.GetAllProjects ()) {
					UnloadMonoProject (prj);
				}
				currentMonoDevelopSolution = null;
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
			Gtk.Application.Invoke (delegate {
				workspacesLoading--;
				if (workspacesLoading == 0 && statusIcon != null) {
					statusIcon.Dispose ();
					statusIcon = null;
					OnLoadingFinished (EventArgs.Empty);
				}
			});
		}

		internal void ShowStatusIcon ()
		{
			Gtk.Application.Invoke (delegate {
				workspacesLoading++;
				if (statusIcon != null)
					return;
				statusIcon = IdeApp.Workbench?.StatusBar.ShowStatusIcon (ImageService.GetIcon ("md-parser"));
			});
		}

		void HandleActiveConfigurationChanged (object sender, EventArgs e)
		{
			if (currentMonoDevelopSolution == null)
				return;
			ShowStatusIcon ();
			CancelLoad ();
			var token = src.Token;

			Task.Run (async delegate {
				try {
					var si = await CreateSolutionInfo (currentMonoDevelopSolution, token).ConfigureAwait (false);
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
			});

		}

		SolutionData solutionData;
		async Task<SolutionInfo> CreateSolutionInfo (MonoDevelop.Projects.Solution solution, CancellationToken token)
		{
			var projects = new ConcurrentBag<ProjectInfo> ();
			var mdProjects = solution.GetAllProjects ().OfType<MonoDevelop.Projects.DotNetProject> ();
			projectionList.Clear ();
			solutionData = new SolutionData ();

			List<Task> allTasks = new List<Task> ();
			foreach (var proj in mdProjects) {
				if (token.IsCancellationRequested)
					return null;
				if (!proj.SupportsRoslyn)
					continue;
				var tp = LoadProject (proj, token).ContinueWith (t => {
					if (!t.IsCanceled)
						projects.Add (t.Result);
				});
				allTasks.Add (tp);
			}
			await Task.WhenAll (allTasks.ToArray ());
			if (token.IsCancellationRequested)
				return null;
			var modifiedWhileLoading = modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();
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
		}

		public Task<SolutionInfo> TryLoadSolution (MonoDevelop.Projects.Solution solution, CancellationToken cancellationToken = default(CancellationToken))
		{
			this.currentMonoDevelopSolution = solution;
			return CreateSolutionInfo (solution, cancellationToken);
		}

		public void UnloadSolution ()
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
		ConcurrentDictionary<ProjectId, ProjectData> projectDataMap = new ConcurrentDictionary<ProjectId, ProjectData> ();

		internal MonoDevelop.Projects.Project GetMonoProject (Project project)
		{
			return GetMonoProject (project.Id);
		}

		internal MonoDevelop.Projects.Project GetMonoProject (ProjectId projectId)
		{
			foreach (var kv in projectIdMap) {
				if (kv.Value == projectId)
					return kv.Key;
			}
			return null;
		}

		public bool Contains (ProjectId projectId) 
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

		ProjectData GetOrCreateProjectData (ProjectId id)
		{
			lock (projectIdMap) {
				ProjectData result;
				if (!projectDataMap.TryGetValue (id, out result)) {
					result = new ProjectData (id);
					projectDataMap [id] = result;
				}
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

		public override bool CanApplyChange (ApplyChangesKind feature)
		{
			return true;
		}

		void UnloadMonoProject (MonoDevelop.Projects.Project project)
		{
			if (project == null)
				throw new ArgumentNullException (nameof (project));
			project.FileAddedToProject -= OnFileAdded;
			project.FileRemovedFromProject -= OnFileRemoved;
			project.FileRenamedInProject -= OnFileRenamed;
			project.Modified -= OnProjectModified;
		}

		Task<ProjectInfo> LoadProject (MonoDevelop.Projects.DotNetProject p, CancellationToken token)
		{
			if (!projectIdMap.ContainsKey (p)) {
				p.FileAddedToProject += OnFileAdded;
				p.FileRemovedFromProject += OnFileRemoved;
				p.FileRenamedInProject += OnFileRenamed;
				p.Modified += OnProjectModified;
			}

			var projectId = GetOrCreateProjectId (p);
			var projectData = GetOrCreateProjectData (projectId);
			return Task.Run (async () => {
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

				var sourceFiles = await p.GetSourceFilesAsync (config != null ? config.Selector : null).ConfigureAwait (false);

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
					CreateDocuments (projectData, p, token, sourceFiles),
					CreateProjectReferences (p, token),
					references
				);
				projectData.Info = info;
				return info;
			}, token);
		}

		internal void UpdateProjectionEntry (MonoDevelop.Projects.ProjectFile projectFile, IReadOnlyList<Projection> projections)
		{
			foreach (var entry in projectionList) {
				if (entry.File.FilePath == projectFile.FilePath) {
					projectionList.Remove (entry);
					break;
				}
			}
			projectionList.Add (new ProjectionEntry { File = projectFile, Projections = projections});

		}

		internal class SolutionData
		{
			public ConcurrentDictionary<string, TextLoader> Files = new ConcurrentDictionary<string, TextLoader> (); 
		}

		internal static Func<SolutionData, string, TextLoader> CreateTextLoader = (data, fileName) => data.Files.GetOrAdd (fileName, a => new MonoDevelopTextLoader (a));

		static DocumentInfo CreateDocumentInfo (SolutionData data, string projectName, ProjectData id, MonoDevelop.Projects.ProjectFile f)
		{
			var filePath = f.FilePath;
			var sourceCodeKind = filePath.Extension == ".sketchcs" ? SourceCodeKind.Script : SourceCodeKind.Regular;
			return DocumentInfo.Create (
				id.GetOrCreateDocumentId (filePath),
				f.FilePath,
				new [] { projectName }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
				sourceCodeKind,
				CreateTextLoader (data, f.Name),
				f.Name,
				false
			);
		}
		List<ProjectionEntry> projectionList = new List<ProjectionEntry>();

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

		IEnumerable<DocumentInfo> CreateDocuments (ProjectData projectData, MonoDevelop.Projects.Project p, CancellationToken token, MonoDevelop.Projects.ProjectFile [] sourceFiles)
		{
			var duplicates = new HashSet<DocumentId> ();

			// use given source files instead of project.Files because there may be additional files added by msbuild targets
			foreach (var f in sourceFiles) {
				if (token.IsCancellationRequested)
					yield break;
				if (f.Subtype == MonoDevelop.Projects.Subtype.Directory)
					continue;
				if (TypeSystemParserNode.IsCompileBuildAction (f.BuildAction)) {
					if (!duplicates.Add (projectData.GetOrCreateDocumentId (f.Name)))
						continue;
					yield return CreateDocumentInfo (solutionData, p.Name, projectData, f);
				} else {
					foreach (var projectedDocument in GenerateProjections (f, projectData, p)) {
						yield return projectedDocument;
					}
				}
			}
		}

		IEnumerable<DocumentInfo> GenerateProjections (MonoDevelop.Projects.ProjectFile f, ProjectData projectData, MonoDevelop.Projects.Project p, HashSet<DocumentId> duplicates = null)
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
				if (duplicates != null && !duplicates.Add (projectData.GetOrCreateDocumentId (projection.Document.FileName)))
					continue;
				var plainName = projection.Document.FileName.FileName;
				yield return DocumentInfo.Create (
					projectData.GetOrCreateDocumentId (projection.Document.FileName),
					plainName,
					new [] { p.Name }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
					SourceCodeKind.Regular,
					TextLoader.From (TextAndVersion.Create (new MonoDevelopSourceText (projection.Document), VersionStamp.Create (), projection.Document.FileName)),
					projection.Document.FileName,
					false
				);
			}
			projectionList.Add (entry);
		}

		static async Task<List<MetadataReference>> CreateMetadataReferences (MonoDevelop.Projects.DotNetProject netProject, ProjectId projectId, CancellationToken token)
		{
			List<MetadataReference> result = new List<MetadataReference> ();
			
			var configurationSelector = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default;
			var hashSet = new HashSet<string> (FilePath.PathComparer);

			try {
				foreach (string file in await netProject.GetReferencedAssemblies (configurationSelector, false).ConfigureAwait (false)) {
					if (token.IsCancellationRequested)
						return result;
					string fileName;
					if (!Path.IsPathRooted (file)) {
						fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file);
					} else {
						fileName = Path.GetFullPath (file);
					}
					if (hashSet.Contains (fileName))
						continue;
					hashSet.Add (fileName);
					if (!File.Exists (fileName)) {
						LoggingService.LogError ("Error while getting referenced Assembly " + fileName + " for project " + netProject.Name + ": File doesn't exist"); 
						continue;
					}
					var metadataReference = MetadataReferenceCache.LoadReference (projectId, fileName);
					if (metadataReference == null)
						continue;
					result.Add (metadataReference);
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting referenced assemblies", e);
			}

			foreach (var pr in netProject.GetReferencedItems (configurationSelector)) {
				if (token.IsCancellationRequested)
					return result;
				var referencedProject = pr as MonoDevelop.Projects.DotNetProject;
				if (referencedProject == null)
					continue;
				if (TypeSystemService.IsOutputTrackedProject (referencedProject)) {
					var fileName = referencedProject.GetOutputFileName (configurationSelector);
					if (!File.Exists (fileName)) {
						LoggingService.LogError ("Error while getting project Reference (" + referencedProject.Name + ") " + fileName + " for project " + netProject.Name + ": File doesn't exist");
						continue;
					}
					var metadataReference = MetadataReferenceCache.LoadReference (projectId, fileName);
					if (metadataReference != null)
						result.Add (metadataReference);
				}
			}
			return result;
		}

		IEnumerable<ProjectReference> CreateProjectReferences (MonoDevelop.Projects.DotNetProject p, CancellationToken token)
		{
			foreach (var referencedProject in p.GetReferencedAssemblyProjects (IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default)) {
				if (token.IsCancellationRequested)
					yield break;
				if (TypeSystemService.IsOutputTrackedProject (referencedProject))
					continue;
				yield return new ProjectReference (GetOrCreateProjectId (referencedProject));
			}
		}

		#region Open documents
		public override bool CanOpenDocuments {
			get {
				return true;
			}
		}

		List<MonoDevelopSourceTextContainer> openDocuments = new List<MonoDevelopSourceTextContainer>();
		internal void InformDocumentOpen (DocumentId documentId, ITextDocument editor)
		{
			var document = InternalInformDocumentOpen (documentId, editor);
			if (document != null) {
				foreach (var linkedDoc in document.GetLinkedDocumentIds ()) {
					InternalInformDocumentOpen (linkedDoc, editor);
				}
			}
		}

		Document InternalInformDocumentOpen (DocumentId documentId, ITextDocument editor)
		{
			var document = this.GetDocument (documentId);
			if (document == null || IsDocumentOpen (documentId)) {
				return document;
			}
			var monoDevelopSourceTextContainer = new MonoDevelopSourceTextContainer (documentId, editor);
			lock (openDocuments) {
				openDocuments.Add (monoDevelopSourceTextContainer);
			}
			OnDocumentOpened (documentId, monoDevelopSourceTextContainer);
			return document;
		}

		Dictionary<string, SourceText> changedFiles = new Dictionary<string, SourceText> ();
		ProjectChanges projectChanges;

		public override bool TryApplyChanges (Solution newSolution)
		{
			changedFiles.Clear ();
			return base.TryApplyChanges (newSolution);
		}

		protected override void ApplyProjectChanges (ProjectChanges projectChanges)
		{
			try {
				internalChanges = true;
				this.projectChanges = projectChanges;
				base.ApplyProjectChanges (projectChanges);
			} finally {
				internalChanges = false;
			}
		}

		protected override void ApplyAdditionalDocumentAdded (DocumentInfo info, SourceText text)
		{
			base.ApplyAdditionalDocumentAdded (info, text);
		}

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

		public void InformDocumentClose (DocumentId analysisDocument, string filePath)
		{
			try {
				var loader = new MonoDevelopTextLoader (filePath);
				OnDocumentClosed (analysisDocument, loader); 
				var document = this.GetDocument (analysisDocument);
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


		protected override async void ApplyDocumentTextChanged (DocumentId id, SourceText text)
		{
			var document = GetDocument (id);
			if (document == null)
				return;
			bool isOpen;
			var filePath = document.FilePath;

			Projection projection = null;
			foreach (var entry in ProjectionList) {
				var p = entry.Projections.FirstOrDefault (proj => FilePath.PathComparer.Equals (proj.Document.FileName, filePath));
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
			if (changedFiles.TryGetValue (filePath, out formerText)) {
				if (formerText.Length == text.Length && formerText.ToString () == text.ToString ())
					return;
			}
			changedFiles [filePath] = text;

			SourceText oldFile;
			if (!isOpen || !document.TryGetText (out oldFile)) {
				oldFile = await document.GetTextAsync ();
			}
			var changes = text.GetTextChanges (oldFile).OrderByDescending (c => c.Span.Start).ToList ();
			int delta = 0;

			if (!isOpen) {
				delta = ApplyChanges (projection, data, changes);
				var formatter = CodeFormatterService.GetFormatter (data.MimeType);
				if (formatter.SupportsPartialDocumentFormatting) {
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

				if (documentContext != null) {
					var editor = (TextEditor)data;
					using (var undo = editor.OpenUndoGroup ()) {
						var oldVersion = editor.Version;
						delta = ApplyChanges (projection, data, changes);
						var versionBeforeFormat = editor.Version;
							
						if (formatter.SupportsOnTheFlyFormatting) {
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
							await Runtime.RunInMainThread (async () => {
								IdeApp.Workbench.Documents.First(d => d.FileName == editor.FileName).Select();
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
										foreach (var v in textChanges) {
											editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
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
									foreach (var v in editor.Version.GetChangesTo (oldVersion).ToList ()) {
										editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
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
											foreach (var v in textChanges) {
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
								editor.StartInsertionMode (options);
							});
						}
					}
				}

				if (projection != null) {
					await UpdateProjectionsDocuments (document, data);
				} else {
					OnDocumentTextChanged (id, new MonoDevelopSourceText (data.CreateDocumentSnapshot ()), PreservationMode.PreserveValue);
				}
				await Runtime.RunInMainThread (() => {
						if (IdeApp.Workbench != null)
						foreach (var w in IdeApp.Workbench.Documents)
							w.StartReparseThread ();
				});
			}
		}
		internal static Func<TextEditor, int, Task<List<InsertionPoint>>> GetInsertionPoints;

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

		static int ApplyChanges (Projection projection, ITextDocument data, List<TextChange> changes)
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
				var file = new MonoDevelop.Projects.ProjectFile (path);
				Application.Invoke (delegate {
					mdProject.Files.Add (file);
					IdeApp.ProjectOperations.SaveAsync (mdProject);
				});
			}
		}

		string DetermineFilePath (DocumentId id, string name, string filePath, IReadOnlyList<string> docFolders, string defaultFolder, bool createDirectory = false)
		{
			var path = filePath;

			if (string.IsNullOrEmpty (path)) {
				var monoProject = GetMonoProject (id.ProjectId);

				// If the first namespace name matches the name of the project, then we don't want to
				// generate a folder for that.  The project is implicitly a folder with that name.
				IEnumerable<string> folders;
				if (docFolders.FirstOrDefault () == monoProject.Name) {
					folders = docFolders.Skip (1);
				} else {
					folders = docFolders;
				}

				if (folders.Any ()) {
					string baseDirectory = Path.Combine (monoProject.BaseDirectory, Path.Combine (folders.ToArray ()));
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

		public Document GetDocument (DocumentId documentId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var project = CurrentSolution.GetProject (documentId.ProjectId);
			if (project == null)
				return null;
			return project.GetDocument (documentId);
		}

		public void UpdateFileContent (string fileName, string text)
		{
			SourceText newText = SourceText.From (text);
			foreach (var kv in this.projectDataMap) {
				var projectId = kv.Key;
				var docId = this.GetDocumentId (projectId, fileName);
				if (docId != null) {
					base.OnDocumentTextChanged (docId, newText, PreservationMode.PreserveIdentity);
				}
				var monoProject = GetMonoProject (projectId);
				if (monoProject != null) {
					var pf = monoProject.GetProjectFile (fileName);
					if (pf != null) {
						var mimeType = DesktopService.GetMimeTypeForUri (fileName);
						if (TypeSystemService.CanParseProjections (monoProject, mimeType, fileName))
							TypeSystemService.ParseProjection (new ParseOptions { Project = monoProject, FileName = fileName, Content = new StringTextSource(text), BuildAction = pf.BuildAction }, mimeType);
					}
				}

			}
		}

		public void RemoveProject (MonoDevelop.Projects.Project project)
		{
			var id = GetProjectId (project); 
			if (id != null) {
				foreach (var docId in GetOpenDocumentIds (id).ToList ()) {
					ClearOpenDocument (docId);
				}
				ProjectId val;
				projectIdMap.TryRemove (project, out val);
				ProjectData val2;
				projectDataMap.TryRemove (id, out val2);
				MetadataReferenceCache.RemoveReferences (id);

				UnloadMonoProject (project);
			}
		}

		#region Project modification handlers

		void OnFileAdded (object sender, MonoDevelop.Projects.ProjectFileEventArgs args)
		{
			if (internalChanges)
				return;
			var project = (MonoDevelop.Projects.Project)sender;
			foreach (MonoDevelop.Projects.ProjectFileEventInfo fargs in args) {
				var projectFile = fargs.ProjectFile;
				if (projectFile.Subtype == MonoDevelop.Projects.Subtype.Directory)
					continue;
				var projectData = GetProjectData (GetProjectId (project));
				if (TypeSystemParserNode.IsCompileBuildAction (projectFile.BuildAction)) {
					var newDocument = CreateDocumentInfo (solutionData, project.Name, projectData, projectFile);
					OnDocumentAdded (newDocument);
				} else {
					foreach (var projectedDocument in GenerateProjections (projectFile, projectData, project)) {
						OnDocumentAdded (projectedDocument);
					}
				}

			}
		}

		void OnFileRemoved (object sender, MonoDevelop.Projects.ProjectFileEventArgs args)
		{
			if (internalChanges)
				return;
			var project = (MonoDevelop.Projects.Project)sender;
			foreach (MonoDevelop.Projects.ProjectFileEventInfo fargs in args) {
				var projectId = GetProjectId (project);
				var data = GetProjectData (projectId);
				var id = data.GetDocumentId (fargs.ProjectFile.FilePath);
				if (id != null) {
					ClearDocumentData (id);
					OnDocumentRemoved (id);
					data.RemoveDocument (fargs.ProjectFile.FilePath);
				} else {
					foreach (var entry in ProjectionList) {
						if (entry.File == fargs.ProjectFile) {
							foreach (var projectedDocument in entry.Projections) {
								id = data.GetDocumentId (projectedDocument.Document.FileName);
								if (id != null) {
									ClearDocumentData (id);
									OnDocumentRemoved (id);
									data.RemoveDocument (projectedDocument.Document.FileName);
								}
							}
							break;
						}
					}
				}
			}
		}

		void OnFileRenamed (object sender, MonoDevelop.Projects.ProjectFileRenamedEventArgs args)
		{
			if (internalChanges)
				return;
			var project = (MonoDevelop.Projects.Project)sender;
			foreach (MonoDevelop.Projects.ProjectFileRenamedEventInfo fargs in args) {
				var projectFile = fargs.ProjectFile;
				if (projectFile.Subtype == MonoDevelop.Projects.Subtype.Directory)
					continue;
				var projectId = GetProjectId (project);
				var data = GetProjectData (projectId);
				if (TypeSystemParserNode.IsCompileBuildAction (projectFile.BuildAction)) {
					var id = data.GetDocumentId (fargs.OldName);
					if (id != null) {
						if (this.IsDocumentOpen (id)) {
							this.InformDocumentClose (id, fargs.OldName);
						}
						OnDocumentRemoved (id);
						data.RemoveDocument (fargs.OldName);
					}
					var newDocument = CreateDocumentInfo (solutionData, project.Name, GetProjectData (projectId), projectFile);
					OnDocumentAdded (newDocument);
				} else {
					foreach (var entry in ProjectionList) {
						if (entry.File == projectFile) {
							foreach (var projectedDocument in entry.Projections) {
								var id = data.GetDocumentId (projectedDocument.Document.FileName);
								if (id != null) {
									if (this.IsDocumentOpen (id)) {
										this.InformDocumentClose (id, projectedDocument.Document.FileName);
									}
									OnDocumentRemoved (id);
									data.RemoveDocument (projectedDocument.Document.FileName);
								}
							}
							break;
						}
					}

					foreach (var projectedDocument in GenerateProjections (fargs.ProjectFile, data, project)) {
						OnDocumentAdded (projectedDocument);
					}
				}
			}
		}

		List<MonoDevelop.Projects.DotNetProject> modifiedProjects = new List<MonoDevelop.Projects.DotNetProject> ();

		async void OnProjectModified (object sender, MonoDevelop.Projects.SolutionItemModifiedEventArgs args)
		{
			if (internalChanges)
				return;
			if (!args.Any (x => x.Hint == "TargetFramework" || x.Hint == "References"))
				return;
			var project = sender as MonoDevelop.Projects.DotNetProject;
			if (project == null)
				return;
			var projectId = GetProjectId (project);
			if (CurrentSolution.ContainsProject (projectId)) {
				OnProjectReloaded (await LoadProject (project, default(CancellationToken)).ConfigureAwait (false));
			} else {
				modifiedProjects.Add (project);
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

}
