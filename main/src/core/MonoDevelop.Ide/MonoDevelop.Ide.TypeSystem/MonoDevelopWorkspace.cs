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

namespace MonoDevelop.Ide.TypeSystem
{
	class MonoDevelopWorkspace : Workspace
	{
		readonly static HostServices services = Microsoft.CodeAnalysis.Host.Mef.MefHostServices.DefaultHost;
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

		public MonoDevelopWorkspace () : base (services, "MonoDevelopWorkspace")
		{
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;
			}
		}

		protected override void Dispose (bool finalize)
		{
			base.Dispose (finalize);
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.ActiveConfigurationChanged -= HandleActiveConfigurationChanged;
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
			Task.Run (() => {
				try {
					return CreateSolutionInfo (currentMonoDevelopSolution, token);
				} catch (Exception ex) {
					LoggingService.LogError ("Error while reloading solution.", ex); 
					return null;
				}
			}).ContinueWith ((Task<SolutionInfo> t) => {
				try {
					if (t.Status == TaskStatus.RanToCompletion) {
						if (t.Result == null)
							return;
						OnSolutionReloaded (t.Result);
					}
				} finally {
					HideStatusIcon ();
				}
			});
		}

		SolutionInfo CreateSolutionInfo (MonoDevelop.Projects.Solution solution, CancellationToken token)
		{
			var projects = new ConcurrentBag<ProjectInfo> ();
			var mdProjects = solution.GetAllProjects ();
			projectionList.Clear ();
			Parallel.ForEach (mdProjects, proj => {
				if (token.IsCancellationRequested)
					return;
				projects.Add (LoadProject (proj, token));
			});
			if (token.IsCancellationRequested)
				return null;
			var solutionInfo = SolutionInfo.Create (GetSolutionId (solution), VersionStamp.Create (), solution.FileName, projects);
			lock (addLock) {
				if (!added) {
					added = true;
					OnSolutionAdded (solutionInfo);
				}
			}
			return solutionInfo;
		}

		public void TryLoadSolution (MonoDevelop.Projects.Solution solution/*, IProgressMonitor progressMonitor*/)
		{
			this.currentMonoDevelopSolution = solution;
			CancelLoad ();
			CreateSolutionInfo (solution, src.Token);
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
				if (!documentIdMap.TryGetValue (name, out result))
					return null;
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

		ProjectInfo LoadProject (MonoDevelop.Projects.Project p, CancellationToken token)
		{
			if (!projectIdMap.ContainsKey (p)) {
				p.FileAddedToProject += OnFileAdded;
				p.FileRemovedFromProject += OnFileRemoved;
				p.FileRenamedInProject += OnFileRenamed;
				p.Modified += OnProjectModified;
			}

			var projectId = GetOrCreateProjectId (p);
			var projectData = GetOrCreateProjectData (projectId); 
			var config = IdeApp.Workspace != null ? p.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as MonoDevelop.Projects.DotNetProjectConfiguration : null;
			MonoDevelop.Projects.DotNetCompilerParameters cp = null;
			if (config != null)
				cp = config.CompilationParameters;
			FilePath fileName = IdeApp.Workspace != null ? p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration) : (FilePath)"";
			if (fileName.IsNullOrEmpty)
				fileName = new FilePath (p.Name + ".dll");
			var info = ProjectInfo.Create (
				projectId,
				VersionStamp.Create (),
				p.Name, 
				fileName.FileNameWithoutExtension,
				LanguageNames.CSharp,
				p.FileName,
				fileName,
				cp != null ? cp.CreateCompilationOptions () : null,
				cp != null ? cp.CreateParseOptions () : null,
				CreateDocuments (projectData, p, token),
				CreateProjectReferences (p, token),
				CreateMetadataReferences (p, projectId, token)
			);

			projectData.Info = info;
			return info;
		}

		internal void UpdateProjectionEnntry (MonoDevelop.Projects.ProjectFile projectFile, IReadOnlyList<Projection> projections)
		{
			foreach (var entry in projectionList) {
				if (entry.File.FilePath == projectFile.FilePath) {
					projectionList.Remove (entry);
					break;
				}
			}
			projectionList.Add (new ProjectionEntry { File = projectFile, Projections = projections});

		}

		internal static Func<string, TextLoader> CreateTextLoader = fileName => new MonoDevelopTextLoader (fileName);

		static DocumentInfo CreateDocumentInfo (string projectName, ProjectData id, MonoDevelop.Projects.ProjectFile f)
		{
			var sourceCodeKind = f.FilePath.Extension == ".sketchcs" ? SourceCodeKind.Interactive : SourceCodeKind.Regular;
			return DocumentInfo.Create (
				id.GetOrCreateDocumentId (f.FilePath),
				f.FilePath,
				new [] { projectName }.Concat (f.ProjectVirtualPath.ParentDirectory.ToString ().Split (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
				sourceCodeKind,
				CreateTextLoader (f.Name),
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

		IEnumerable<DocumentInfo> CreateDocuments (ProjectData projectData, MonoDevelop.Projects.Project p, CancellationToken token)
		{
			var duplicates = new HashSet<DocumentId> ();
			foreach (var f in p.Files) {
				if (token.IsCancellationRequested)
					yield break;
				if (f.Subtype == MonoDevelop.Projects.Subtype.Directory)
					continue;
				if (TypeSystemParserNode.IsCompileBuildAction (f.BuildAction)) {
					if (!duplicates.Add (projectData.GetOrCreateDocumentId (f.Name)))
						continue;
					yield return CreateDocumentInfo (p.Name, projectData, f);
					continue;
				}
				var mimeType = DesktopService.GetMimeTypeForUri (f.FilePath);
				var node = TypeSystemService.GetTypeSystemParserNode (mimeType, f.BuildAction);
				if (node == null || !node.Parser.CanGenerateProjection (mimeType, f.BuildAction, p.SupportedLanguages))
					continue;
				var options = new ParseOptions {
					FileName = f.FilePath,
					Project = p,
					Content = StringTextSource.ReadFrom (f.FilePath),
				};
				var projections = node.Parser.GenerateProjections (options);
				var entry = new ProjectionEntry ();
				entry.File = f;
				var list = new List<Projection> ();
				entry.Projections = list;
				foreach (var projection in projections.Result) {
					list.Add (projection);
					if (!duplicates.Add (projectData.GetOrCreateDocumentId (projection.Document.FileName)))
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
		}

		static IEnumerable<MetadataReference> CreateMetadataReferences (MonoDevelop.Projects.Project p, ProjectId projectId, CancellationToken token)
		{
			var netProject = p as MonoDevelop.Projects.DotNetProject;
			if (netProject == null)
				yield break;
			var configurationSelector = IdeApp.Workspace?.ActiveConfiguration ?? MonoDevelop.Projects.ConfigurationSelector.Default;
			var hashSet = new HashSet<string> (FilePath.PathComparer);

			bool addFacadeAssemblies = false;

			foreach (string file in netProject.GetReferencedAssemblies (configurationSelector, false)) {
				if (token.IsCancellationRequested)
					yield break;
				string fileName;
				if (!Path.IsPathRooted (file)) {
					fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file);
				} else {
					fileName = Path.GetFullPath (file);
				}
				if (hashSet.Contains (fileName))
					continue;
				hashSet.Add (fileName);
				if (!File.Exists (fileName))
					continue;
				yield return MetadataReferenceCache.LoadReference (projectId, fileName);
				addFacadeAssemblies |= MonoDevelop.Core.Assemblies.SystemAssemblyService.ContainsReferenceToSystemRuntime (fileName);
			}

			// HACK: Facade assemblies should be added by the project system. Remove that when the project system can do that.
			if (addFacadeAssemblies) {
				if (netProject != null) {
					var runtime = netProject.TargetRuntime ?? MonoDevelop.Core.Runtime.SystemAssemblyService.DefaultRuntime;
					var facades = runtime.FindFacadeAssembliesForPCL (netProject.TargetFramework);
					foreach (var facade in facades) {
						if (!File.Exists (facade))
							continue;
						yield return MetadataReferenceCache.LoadReference (projectId, facade);
					}
				}
			}

			foreach (var pr in p.GetReferencedItems (configurationSelector)) {
				if (token.IsCancellationRequested)
					yield break;
				var referencedProject = pr as MonoDevelop.Projects.DotNetProject;
				if (referencedProject == null)
					continue;
				if (TypeSystemService.IsOutputTrackedProject (referencedProject)) {
					var fileName = referencedProject.GetOutputFileName (configurationSelector);
					if (!File.Exists (fileName))
						continue;
					yield return MetadataReferenceCache.LoadReference (projectId, fileName);
				}
			}
		}

		IEnumerable<ProjectReference> CreateProjectReferences (MonoDevelop.Projects.Project p, CancellationToken token)
		{
			foreach (var pr in p.GetReferencedItems (MonoDevelop.Projects.ConfigurationSelector.Default)) {
				if (token.IsCancellationRequested)
					yield break;
				var referencedProject = pr as MonoDevelop.Projects.DotNetProject;
				if (referencedProject == null)
					continue;
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

		public override void OpenDocument (DocumentId documentId, bool activate = true)
		{
			var document = GetDocument (documentId);
			if (document == null)
				return;
			MonoDevelop.Projects.Project prj = null;
			foreach (var curPrj in IdeApp.Workspace.GetAllProjects ()) {
				if (GetProjectId (curPrj) == documentId.ProjectId) {
					prj = curPrj;
					break;
				}
			}
			IdeApp.Workbench.OpenDocument (new MonoDevelop.Ide.Gui.FileOpenInformation (
				DetermineFilePath(document.Id, document.Name, document.FilePath, document.Folders),
				prj,
				activate
			)); 
		}

		List<MonoDevelopSourceTextContainer> openDocuments = new List<MonoDevelopSourceTextContainer>();
		internal void InformDocumentOpen (DocumentId documentId, ITextDocument editor)
		{
			var document = this.GetDocument (documentId);
			if (document == null) {
				return;
			}
			if (IsDocumentOpen (documentId))
				InformDocumentClose (documentId, document.FilePath);
            var monoDevelopSourceTextContainer = new MonoDevelopSourceTextContainer (documentId, editor);
			lock (openDocuments) {
				openDocuments.Add (monoDevelopSourceTextContainer);
			}
			OnDocumentOpened (documentId, monoDevelopSourceTextContainer); 
		}

		Solution newSolution;
		public override bool TryApplyChanges (Solution newSolution)
		{
			this.newSolution = newSolution;
			return base.TryApplyChanges (newSolution);
		}

		protected override void ApplyProjectChanges (ProjectChanges projectChanges)
		{
			try {
				internalChanges = true;
				base.ApplyProjectChanges (projectChanges);
				var data = GetMonoProject (projectChanges.NewProject);
				if (data != null) {
					Application.Invoke (delegate {
						data.SaveAsync (new ProgressMonitor ());	
					});
				}
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
//					openDoc.TextChanged -= HandleTextChanged;
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
				OnDocumentClosed (analysisDocument, new MonoDevelopTextLoader (filePath)); 
			} catch (Exception e) {
				LoggingService.LogError ("Exception while closing document.", e); 
			}
		}
		
		public override void CloseDocument (DocumentId documentId)
		{
		}

		protected override void ApplyDocumentTextChanged (DocumentId id, SourceText text)
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
			var changes = text.GetTextChanges (document.GetTextAsync ().Result).OrderByDescending (c => c.Span.Start).ToList ();

			int delta = 0;
			foreach (var change in changes) {
				var offset = change.Span.Start;

				if (projection != null) {
					int originalOffset;
					if (projection.TryConvertFromProjectionToOriginal (offset, out originalOffset))
						offset = originalOffset;
				}

				data.ReplaceText (offset, change.Span.Length, change.NewText);
				delta += change.Span.Length - change.NewText.Length;
			}

			if (!isOpen) {
				var formatter = CodeFormatterService.GetFormatter (data.MimeType); 
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


					var str = formatter.FormatText (mp.Policies, currentText, startOffset, startOffset + change.NewText.Length);
					data.ReplaceText (startOffset, change.NewText.Length, str);
				}
				data.Save ();
				FileService.NotifyFileChanged (filePath);
			} else {
				var formatter = CodeFormatterService.GetFormatter (data.MimeType); 
				var documentContext = IdeApp.Workbench.Documents.FirstOrDefault (d => FilePath.PathComparer.Compare (d.FileName, filePath) == 0);
				if (documentContext != null) {
					foreach (var change in changes) {
						delta -= change.Span.Length - change.NewText.Length;
						var startOffset = change.Span.Start - delta;

						if (projection != null) {
							int originalOffset;
							if (projection.TryConvertFromProjectionToOriginal (startOffset, out originalOffset))
								startOffset = originalOffset;
						}

						formatter.OnTheFlyFormat ((TextEditor)data, documentContext, startOffset, startOffset + change.NewText.Length);
					}
				}
			}
			OnDocumentTextChanged (id, new MonoDevelopSourceText(data), PreservationMode.PreserveValue);
		}

		protected override void ApplyDocumentAdded (DocumentInfo info, SourceText text)
		{
			var id = info.Id;
			var path = DetermineFilePath (info.Id, info.Name, info.FilePath, info.Folders, true);
			MonoDevelop.Projects.Project mdProject = null;

			if (id.ProjectId != null) {
				var project = CurrentSolution.GetProject (id.ProjectId);
				mdProject = GetMonoProject (project);
				if (mdProject == null)
					LoggingService.LogWarning ("Couldn't find project for newly generated file {0} (Project {1}).", path, info.Id.ProjectId);
			}

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

			OnDocumentAdded (info.WithTextLoader (new MonoDevelopTextLoader (path)));
		}

		string DetermineFilePath (DocumentId id, string name, string filePath, IReadOnlyList<string> docFolders, bool createDirectory = false)
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

		public void AddProject (MonoDevelop.Projects.Project project)
		{
			var info = LoadProject (project, default(CancellationToken));
			OnProjectAdded (info); 
		}

		public void RemoveProject (MonoDevelop.Projects.Project project)
		{
			var id = GetProjectId (project); 
			if (id != null) {
				foreach (var docId in GetOpenDocumentIds (id).ToList ()) {
					ClearOpenDocument (docId);
				}
				OnProjectRemoved (id);
				ProjectId val;
				projectIdMap.TryRemove (project, out val);
				ProjectData val2;
				projectDataMap.TryRemove (id, out val2);
				MetadataReferenceCache.RemoveReferences (id);

				project.FileAddedToProject -= OnFileAdded;
				project.FileRemovedFromProject -= OnFileRemoved;
				project.FileRenamedInProject -= OnFileRenamed;
				project.Modified -= OnProjectModified;
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
				if (!TypeSystemParserNode.IsCompileBuildAction (projectFile.BuildAction))
					continue;
				var projectId = GetProjectId (project);
				var newDocument = CreateDocumentInfo(project.Name, GetProjectData(projectId), projectFile);
				OnDocumentAdded (newDocument);
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
				if (!TypeSystemParserNode.IsCompileBuildAction (projectFile.BuildAction))
					continue;

				var projectId = GetProjectId (project);
				var data = GetProjectData (projectId);

				var id = data.GetDocumentId (fargs.OldName); 
				if (id != null) {
					if (this.IsDocumentOpen (id)) {
						this.InformDocumentClose (id, fargs.OldName);
					}
					OnDocumentRemoved (id);
					data.RemoveDocument (fargs.OldName);
				}

				var newDocument = CreateDocumentInfo (project.Name, GetProjectData (projectId), projectFile);
				OnDocumentAdded (newDocument);
			}
		}

		void OnProjectModified (object sender, MonoDevelop.Projects.SolutionItemModifiedEventArgs args)
		{
			if (internalChanges)
				return;
			if (!args.Any (x => x.Hint == "TargetFramework" || x.Hint == "References"))
				return;
			var project = (MonoDevelop.Projects.Project)sender;
			var projectId = GetProjectId (project);
			if (CurrentSolution.ContainsProject (projectId))
				OnProjectReloaded (LoadProject (project, default(CancellationToken))); 
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