//
// RoslynDocumentExtension.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Documents;
using System.IO;
using System.Linq;
using MonoDevelop.Projects.SharedAssetsProjects;

namespace MonoDevelop.Ide.Gui
{
	[ExportDocumentControllerExtension (MimeType = "*")]
	class RoslynDocumentExtension : DocumentControllerExtension
	{
		RoslynDocumentContext documentContext;

		static RoslynDocumentExtension ()
		{
			if (IdeApp.Workbench != null) {
				IdeApp.Workbench.ActiveDocumentChanged += delegate {
					// reparse on document switch to update the current file with changes done in other files.
					var doc = IdeApp.Workbench.ActiveDocument;
					if (doc == null || doc.Editor == null || doc.DocumentContext == null)
						return;
					doc.DocumentContext.ReparseDocument ();
				};
			}
		}

		public override async Task<bool> SupportsController (DocumentController controller)
		{
			if (controller is FileDocumentController file)
				return (await controller.ServiceProvider.GetService<TypeSystemService> ()).GetParser (file.MimeType) != null;
			return false;
		}

		public RoslynDocumentContext DocumentContext {
			get {
				return documentContext;
			}
		}

		protected override object OnGetContent (Type type)
		{
			if (typeof (DocumentContext).IsAssignableFrom (type))
				return documentContext;
			return base.OnGetContent (type);
		}

		public override Task Initialize (Properties status)
		{
			documentContext = new RoslynDocumentContext ();
			return documentContext.Initialize ((FileDocumentController)Controller);
		}

		protected override void OnOwnerChanged ()
		{
			base.OnOwnerChanged ();
			documentContext.SetProject (Controller.Owner as Project);
		}

		protected override void OnContentChanged ()
		{
			base.OnContentChanged ();
			documentContext.TryEditorInitialization ();
		}

		public override void Dispose ()
		{
			documentContext?.Dispose ();
			base.Dispose ();
		}

		public override async Task OnSave ()
		{
			await base.OnSave ();
			documentContext.NotifySaved ();
		}
	}

	public class RoslynDocumentContext : DocumentContext
	{
		Microsoft.CodeAnalysis.DocumentId analysisDocument;
		ParsedDocument parsedDocument;
		Project project;
		FileDocumentController controller;
		bool wasEdited;
		bool editorInitialized;

		TypeSystemService typeSystemService;
		RootWorkspace rootWorkspace;

		const int ParseDelay = 600;

		public async Task Initialize (FileDocumentController controller)
		{
			this.controller = controller;
			typeSystemService = await controller.ServiceProvider.GetService<TypeSystemService> ();
			rootWorkspace = await controller.ServiceProvider.GetService<RootWorkspace> ();

			MonoDevelopWorkspace.LoadingFinished += ReloadAnalysisDocumentHandler;

			TryEditorInitialization ();
		}

		public void TryEditorInitialization ()
		{
			if (!editorInitialized && Editor != null) {
				editorInitialized = true;
				InitializeEditor ();
				RunWhenRealized (delegate { ListenToProjectLoad (); });
			}
		}

		public TextEditor Editor {
			get {
				return controller.GetContent<TextEditor> ();
			}
		}

		public string OriginalFileName => controller.OriginalContentName;

		public FilePath FileName => controller.FilePath;

		/// <summary>
		/// Returns the roslyn document for this document. This may return <c>null</c> if it's no compileable document.
		/// Even if it's a C# file.
		/// </summary>
		public override Microsoft.CodeAnalysis.Document AnalysisDocument {
			get {
				if (analysisDocument == null)
					return null;

				return RoslynWorkspace.CurrentSolution.GetDocument (analysisDocument);
			}
		}

		public void RunWhenRealized (System.Action action)
		{
			var e = Editor;
			if (e == null) {
				action ();
				return;
			}
			e.RunWhenRealized (action);
		}

		public override T GetContent<T> ()
		{
			return controller.GetContent<T> ();
		}

		public override IEnumerable<T> GetContents<T> ()
		{
			return controller.GetContents<T> ();
		}

		public override T GetPolicy<T> (IEnumerable<string> types)
		{
			if (adhocProject != null)
				return MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<T> (types);
			return base.GetPolicy<T> (types);
		}

		public override OptionSet GetOptionSet ()
		{
			return typeSystemService.Workspace.Options;
		}

		FilePath adHocFile;
		Project adhocProject;
		Solution adhocSolution;

		public override Project Project {
			get { return controller.Owner as Project ?? adhocProject; }
		}

		internal override bool IsAdHocProject {
			get { return adhocProject != null; }
		}

		public override bool IsCompileableInProject {
			get {
				var project = Project;
				if (project == null)
					return false;
				var solution = project.ParentSolution;

				if (solution != null && IdeApp.IsInitialized) {
					var config = rootWorkspace.ActiveConfiguration;
					if (config != null) {
						var sc = solution.GetConfiguration (config);
						if (sc != null && !sc.BuildEnabledForItem (project))
							return false;
					}
				}

				ProjectFile pf = project.GetProjectFile (controller.OriginalContentName);
				if (pf == null)
					pf = project.GetProjectFile (FileName);

				return pf != null && pf.BuildAction != BuildAction.None;
			}
		}

		public override ParsedDocument ParsedDocument {
			get {
				return parsedDocument;
			}
		}

		// Used by unit tests
		public void SetParsedDocument (ParsedDocument document)
		{
			parsedDocument = document;
		}

		public override string Name {
			get {
				return controller.DocumentTitle;
			}
		}

		public override bool IsUntitled {
			get {
				return controller.IsNewDocument;
			}
		}

		public override void AttachToProject (Project project)
		{
			SetProject (project);
		}

		internal void SetProject (Project newProject)
		{
			UnsubscribeControllerEvents ();

			if (newProject == project || (IsAdHocProject && newProject == adhocProject))
				return;

			project = newProject;

			bool usingAdHocProject = IsAdHocProject
			UnloadAdhocProject ();
			if (adhocProject == null)
				UnsubscribeAnalysisDocument ();

			SubscribeControllerEvents ();

			Editor.InitializeExtensionChain (this);

			// Do not start the parser when the project is set to null and an adHocProject is not being used. This
			// would result in a new adHocProject being created and then RootWorkspace would not update the Document's
			// project since it is non-null.

			if (project != null || (project == null && usingAdHocProject))
				ListenToProjectLoad();
		}

		void SubscribeControllerEvents ()
		{
			UnsubscribeControllerEvents ();
			controller.DocumentTitleChanged += OnContentNameChanged;
			if (project != null)
				project.Modified += HandleProjectModified;
		}

		void UnsubscribeControllerEvents ()
		{
			controller.DocumentTitleChanged -= OnContentNameChanged;
			if (project != null)
				project.Modified -= HandleProjectModified;
		}

		void OnContentNameChanged (object sender, EventArgs e)
		{
			ReloadAnalysisDocumentHandler (sender, e);
		}

		void ListenToProjectLoad ()
		{
			StartReparseThread ();
		}

		void HandleInLoadChanged (object sender, EventArgs e)
		{
			StartReparseThread ();
		}

		void HandleProjectModified (object sender, SolutionItemModifiedEventArgs e)
		{
			if (!e.Any (x => x.Hint == "TargetFramework" || x.Hint == "References"))
				return;
			StartReparseThread ();
		}

		/// <summary>
		/// This method can take some time to finish. It's not threaded
		/// </summary>
		/// <returns>
		/// A <see cref="ParsedDocument"/> that contains the current dom.
		/// </returns>
		public override async Task<ParsedDocument> UpdateParseDocument ()
		{
			try {
				await EnsureAnalysisDocumentIsOpen ();
				string currentParseFile = GetCurrentParseFileName ();
				var editor = Editor;
				if (editor == null || string.IsNullOrEmpty (currentParseFile))
					return null;
				typeSystemService.AddSkippedFile (currentParseFile);
				var currentParseText = editor.CreateDocumentSnapshot ();
				CancelOldParsing ();
				var project = adhocProject ?? Project;

				var options = new ParseOptions {
					Project = project,
					Content = currentParseText,
					FileName = currentParseFile,
					OldParsedDocument = parsedDocument,
					RoslynDocument = AnalysisDocument,
					IsAdhocProject = IsAdHocProject
				};

				if (project != null && typeSystemService.CanParseProjections (project, Editor.MimeType, FileName)) {
					var projectFile = project.GetProjectFile (currentParseFile);
					if (projectFile != null)
						options.BuildAction = projectFile.BuildAction;

					var p = await typeSystemService.ParseProjection (options, editor.MimeType);
					if (p != null) {
						this.parsedDocument = p.ParsedDocument;
						var projections = p.Projections;
						foreach (var p2 in projections)
							p2.CreateProjectedEditor (this);
						Editor.SetOrUpdateProjections (this, projections, p.DisabledProjectionFeatures);
					}
				} else {
					this.parsedDocument = await typeSystemService.ParseFile (options, editor.MimeType) ?? this.parsedDocument;
				}
			} finally {

				OnDocumentParsed (EventArgs.Empty);
			}
			return this.parsedDocument;
		}

		/// <summary>
		/// This method kicks off an async document parser and should be used instead of 
		/// <see cref="UpdateParseDocument"/> unless you need the parsed document immediately.
		/// </summary>
		public override void ReparseDocument ()
		{
			StartReparseThread ();
		}

		public Task<Microsoft.CodeAnalysis.Compilation> GetCompilationAsync (CancellationToken cancellationToken = default (CancellationToken))
		{
			var project = typeSystemService.GetCodeAnalysisProject (adhocProject ?? Project);
			if (project == null)
				return new Task<Microsoft.CodeAnalysis.Compilation> (() => null);
			return project.GetCompilationAsync (cancellationToken);
		}

		internal override Task<IReadOnlyList<Editor.Projection.Projection>> GetPartialProjectionsAsync (CancellationToken cancellationToken = default (CancellationToken))
		{
			var parser = typeSystemService.GetParser (Editor.MimeType);
			if (parser == null)
				return null;
			var projectFile = Project.GetProjectFile (OriginalFileName);
			if (projectFile == null) {
				projectFile = Project.GetProjectFile (Editor.FileName);
				if (projectFile == null)
					return null;
			}
			if (!parser.CanGenerateProjection (Editor.MimeType, projectFile.BuildAction, Project.SupportedLanguages))
				return null;
			try {
				return parser.GetPartialProjectionsAsync (this, Editor, parsedDocument, cancellationToken);
			} catch (NotSupportedException) {
				return null;
			}
		}

		internal override void UpdateDocumentId (Microsoft.CodeAnalysis.DocumentId newId)
		{
			this.analysisDocument = newId;
			OnAnalysisDocumentChanged (EventArgs.Empty);
		}

		protected override void OnDispose (bool disposing)
		{
			if (IsDisposed)
				return;

			CancelParseTimeout ();
			UnsubscribeAnalysisDocument ();
			UnsubscribeRoslynWorkspace ();
			UnloadAdhocProject ();

			MonoDevelopWorkspace.LoadingFinished -= ReloadAnalysisDocumentHandler;

			parsedDocument = null;
			base.OnDispose (disposing);
		}

		void ReloadAnalysisDocumentHandler (object sender, EventArgs e)
		{
			UnsubscribeAnalysisDocument ();
			UnloadAdhocProject ();
			EnsureAnalysisDocumentIsOpen ().ContinueWith (delegate {
				if (analysisDocument != null)
					StartReparseThread ();
			});
		}

		uint parseTimeout = 0;
		CancellationTokenSource analysisDocumentSrc = new CancellationTokenSource ();

		void CancelEnsureAnalysisDocumentIsOpen ()
		{
			analysisDocumentSrc.Cancel ();
			analysisDocumentSrc = new CancellationTokenSource ();
		}

		/// <summary>
		/// During that process ad hoc projects shouldn't be created.
		/// </summary>
		internal static bool IsInProjectSettingLoadingProcess { get; set; }

		bool IsUnreferencedSharedProject (Project project)
		{
			return project is SharedAssetsProject;
		}


		Task EnsureAnalysisDocumentIsOpen ()
		{
			if (analysisDocument != null) {
				Microsoft.CodeAnalysis.Document doc;
				try {
					doc = RoslynWorkspace.CurrentSolution.GetDocument (analysisDocument);
					if (doc == null && RoslynWorkspace.CurrentSolution.ContainsAdditionalDocument (analysisDocument)) {
						return Task.CompletedTask;
					}
				} catch (Exception) {
					doc = null;
				}
				if (doc != null)
					return Task.CompletedTask;
			}
			if (Editor == null) {
				UnsubscribeAnalysisDocument ();
				return Task.CompletedTask;
			}
			if (Project != null && !IsUnreferencedSharedProject (Project)) {
				lock (analysisDocumentLock) {
					UnsubscribeRoslynWorkspace ();
					RoslynWorkspace = typeSystemService.GetWorkspace (this.Project.ParentSolution);
					if (RoslynWorkspace == null) // Solution not loaded yet
						return Task.CompletedTask;
					SubscribeRoslynWorkspace ();
					var newAnalysisDocument = FileName != null ? typeSystemService.GetDocumentId (this.Project, this.FileName) : null;
					var changedAnalysisDocument = newAnalysisDocument != analysisDocument;
					analysisDocument = newAnalysisDocument
					if (analysisDocument != null && !RoslynWorkspace.CurrentSolution.ContainsAdditionalDocument (analysisDocument) && !RoslynWorkspace.IsDocumentOpen (analysisDocument)) {
						typeSystemService.InformDocumentOpen (analysisDocument, TextBuffer.AsTextContainer(), this);
					}
					if (changedAnalysisDocument)
						OnAnalysisDocumentChanged (EventArgs.Empty);
					return Task.CompletedTask;
				}
			}
			lock (adhocProjectLock) {
				var token = analysisDocumentSrc.Token;
				if (adhocProject != null || IsInProjectSettingLoadingProcess) {
					return Task.CompletedTask;
				}

				if (TextBuffer.ContentType.TypeName == "CSharp") {
					var newProject = Services.ProjectService.CreateDotNetProject ("C#");

					this.adhocProject = newProject;

					newProject.Name = "InvisibleProject";
					newProject.References.Add (ProjectReference.CreateAssemblyReference ("mscorlib"));
					newProject.References.Add (ProjectReference.CreateAssemblyReference ("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
					newProject.References.Add (ProjectReference.CreateAssemblyReference ("System.Core"));

					// Use a different name for each project, otherwise the msbuild builder will complain about duplicate projects.
					newProject.FileName = "adhoc_" + (++adhocProjectCount) + ".csproj";
					if (!controller.IsNewDocument) {
						adHocFile = FileName;
					} else {
						adHocFile = (Platform.IsWindows ? "C:\\" : "/") + FileName + ".cs";
					}

					newProject.Files.Add (new ProjectFile (adHocFile, BuildAction.Compile));

					adhocSolution = new Solution ();
					adhocSolution.AddConfiguration ("", true);
					adhocSolution.DefaultSolutionFolder.AddItem (newProject);
					return typeSystemService.Load (adhocSolution, new ProgressMonitor (), token, false).ContinueWith (task => {
						if (token.IsCancellationRequested)
							return;
						UnsubscribeRoslynWorkspace ();
						RoslynWorkspace = task.Result.FirstOrDefault (); // 1 solution loaded ->1 workspace as result
						SubscribeRoslynWorkspace ();
						analysisDocument = RoslynWorkspace.CurrentSolution.Projects.First ().DocumentIds.First ();
						typeSystemService.InformDocumentOpen (RoslynWorkspace, analysisDocument, TextBuffer.AsTextContainer (), this);
						OnAnalysisDocumentChanged (EventArgs.Empty);
					});
				}
			}
			return Task.CompletedTask;
		}

		void UnsubscribeRoslynWorkspace ()
		{
			var ws = RoslynWorkspace as MonoDevelopWorkspace;
			if (ws != null) {
				ws.WorkspaceChanged -= HandleRoslynProjectChange;
				ws.DocumentClosed -= HandleRoslynDocumentClosed;
			}
		}

		void SubscribeRoslynWorkspace ()
		{
			var ws = RoslynWorkspace as MonoDevelopWorkspace;
			if (ws != null) {
				ws.WorkspaceChanged += HandleRoslynProjectChange;
				ws.DocumentClosed += HandleRoslynDocumentClosed;
			}
		}

		void HandleRoslynDocumentClosed (object sender, Microsoft.CodeAnalysis.DocumentEventArgs e)
		{
			lock (analysisDocumentLock) {
				if (e.Document.Id == analysisDocument) {
					analysisDocument = null;
				}
			}
		}

		void HandleRoslynProjectChange (object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
		{
			if (e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectChanged ||
				e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectAdded ||
				e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectRemoved ||
				e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded) {
				StartReparseThread ();
			}
		}

		void UnsubscribeAnalysisDocument ()
		{
			lock (analysisDocumentLock) {
				if (analysisDocument != null) {
					typeSystemService.InformDocumentClose (analysisDocument, FileName);
					analysisDocument = null;
				}
			}
		}

		static int adhocProjectCount = 0;
		object adhocProjectLock = new object ();
		object analysisDocumentLock = new object ();
		void UnloadAdhocProject ()
		{
			CancelEnsureAnalysisDocumentIsOpen ();
			lock (adhocProjectLock) {
				if (adhocProject == null)
					return;
				if (adhocSolution != null) {
					typeSystemService.Unload (adhocSolution);
					adhocSolution.Dispose ();
					adhocSolution = null;
				}
				adhocProject = null;
			}
		}

		CancellationTokenSource parseTokenSource = new CancellationTokenSource ();

		void CancelOldParsing ()
		{
			parseTokenSource.Cancel ();
			parseTokenSource = new CancellationTokenSource ();
		}

		object reparseTimeoutLock = new object ();

		internal void StartReparseThread ()
		{
			RunWhenRealized (() => {
				string currentParseFile = GetCurrentParseFileName ();
				var editor = Editor;
				if (string.IsNullOrEmpty (currentParseFile) || editor == null || editor.IsDisposed == true)
					return;
				lock (reparseTimeoutLock) {
					CancelParseTimeout ();

					parseTimeout = GLib.Timeout.Add (ParseDelay, delegate {
						StartReparseThreadDelayed (currentParseFile);
						parseTimeout = 0;
						return false;
					});
				}
			});
		}

		public void CancelParseTimeout ()
		{
			lock (reparseTimeoutLock) {
				var timeout = parseTimeout;
				if (timeout != 0) {
					GLib.Source.Remove (timeout);
					parseTimeout = 0;
				}
			}
		}

		string GetCurrentParseFileName ()
		{
			var editor = Editor;
			string result = adhocProject != null ? adHocFile : editor?.FileName;
			return result ?? FileName;
		}

		async void StartReparseThreadDelayed (FilePath currentParseFile)
		{
			var editor = Editor;
			if (editor == null || editor.IsDisposed)
				return;

			// Don't directly parse the document because doing it at every key press is
			// very inefficient. Do it after a small delay instead, so several changes can
			// be parsed at the same time.
			await EnsureAnalysisDocumentIsOpen ();
			var currentParseText = editor.CreateSnapshot ();
			string mimeType = editor.MimeType;
			CancelOldParsing ();
			var token = parseTokenSource.Token;
			var currentProject = adhocProject ?? Project;
			var projectsContainingFile = currentProject?.ParentSolution?.GetProjectsContainingFile (currentParseFile);
			if (projectsContainingFile == null || !projectsContainingFile.Any ())
				projectsContainingFile = new Project [] { currentProject };

			ThreadPool.QueueUserWorkItem (delegate {
				foreach (var project in projectsContainingFile) {
					var projectFile = project?.GetProjectFile (currentParseFile);
					typeSystemService.AddSkippedFile (currentParseFile);
					var options = new ParseOptions {
						Project = project,
						Content = currentParseText,
						FileName = currentParseFile,
						OldParsedDocument = parsedDocument,
						RoslynDocument = AnalysisDocument,
						IsAdhocProject = IsAdHocProject
					};
					if (projectFile != null)
						options.BuildAction = projectFile.BuildAction;

					if (project != null && typeSystemService.CanParseProjections (project, mimeType, currentParseFile)) {
						typeSystemService.ParseProjection (options, mimeType, token).ContinueWith ((task, state) => {
							if (token.IsCancellationRequested)
								return;
							if (currentProject != state)
								return;
							Runtime.RunInMainThread (() => {
								// this may be called after the document has closed, in that case the OnDocumentParsed event shouldn't be invoked.
								var taskResult = task.Result;
								if (IsDisposed || taskResult == null || token.IsCancellationRequested)
									return;
								this.parsedDocument = taskResult.ParsedDocument;
								var projections = taskResult.Projections;
								foreach (var p2 in projections)
									p2.CreateProjectedEditor (this);
								Editor.SetOrUpdateProjections (this, projections, taskResult.DisabledProjectionFeatures);
								OnDocumentParsed (EventArgs.Empty);
							});
						}, project, TaskContinuationOptions.OnlyOnRanToCompletion);
					} else if (project == null || currentProject == project) {
						typeSystemService.ParseFile (options, mimeType, token).ContinueWith (task => {
							if (token.IsCancellationRequested)
								return;
							Runtime.RunInMainThread (() => {
								// this may be called after the document has closed, in that case the OnDocumentParsed event shouldn't be invoked.
								if (IsDisposed || task.Result == null || token.IsCancellationRequested)
									return;
								this.parsedDocument = task.Result;
								OnDocumentParsed (EventArgs.Empty);
							});
						}, TaskContinuationOptions.OnlyOnRanToCompletion);
					}
				}
			});
		}

		void InitializeEditor ()
		{
			Editor.TextChanged += (o, a) => {
				if (parsedDocument != null)
					parsedDocument.IsInvalid = true;

				if (Editor.IsInAtomicUndo) {
					wasEdited = true;
				} else {
					StartReparseThread ();
				}
			};

			Editor.BeginAtomicUndoOperation += delegate {
				wasEdited = false;
			};

			Editor.EndAtomicUndoOperation += delegate {
				if (wasEdited)
					StartReparseThread ();
			};

			Editor.InitializeExtensionChain (this);
		}

		internal void NotifySaved ()
		{
			// Set the file time of the current document after the file time of the written file, to prevent double file updates.
			// Note that the parsed document may be overwritten by a background thread to a more recent one.
			var doc = parsedDocument;
			if (doc != null) {
				try {
					// filename could be null if the user cancelled SaveAs and this is a new & unsaved file
					if (!FileName.IsNullOrEmpty)
						doc.LastWriteTimeUtc = File.GetLastWriteTimeUtc (FileName);
				} catch (Exception e) {
					doc.LastWriteTimeUtc = DateTime.UtcNow;
					LoggingService.LogWarning ("Exception while getting the write time from " + FileName, e);
				}
			}

			OnSaved (EventArgs.Empty);

			UpdateParseDocument ().Ignore ();
		}
	}
}
