//
// Document.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Tasks;
using Mono.Addins;
using MonoDevelop.Ide.Extensions;
using System.Linq;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Projects.SharedAssetsProjects;
using MonoDevelop.Ide.Editor.Extension;
using System.Collections.Immutable;
using MonoDevelop.Ide.Editor.TextMate;
using MonoDevelop.Core.Assemblies;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Ide.Gui
{

	public class Document : DocumentContext
	{
		internal object MemoryProbe = Counters.DocumentsInMemory.CreateMemoryProbe ();
		
		IWorkbenchWindow window;
		[Obsolete]
		ParsedDocument parsedDocument;
		[Obsolete]
		Microsoft.CodeAnalysis.DocumentId analysisDocument;

		const int ParseDelay = 600;

		public IWorkbenchWindow Window {
			get { return window; }
		}
		
		internal DateTime LastTimeActive {
			get;
			set;
		}

		/// <summary>
		/// Returns the roslyn document for this document. This may return <c>null</c> if it's no compileable document.
		/// Even if it's a C# file.
		/// </summary>
		[Obsolete("Use Roslyn directly")]
		public override Microsoft.CodeAnalysis.Document AnalysisDocument {
			get {
				if (analysisDocument == null)
					return null;
				
				return RoslynWorkspace.CurrentSolution.GetDocument (analysisDocument);
			}
		}
 		
		public override T GetContent<T> ()
		{
			if (window == null)
				return null;
			//check whether the ViewContent can return the type directly
			T ret = Window.ActiveViewContent.GetContent (typeof(T)) as T;
			if (ret != null)
				return ret;
			
			//check the primary viewcontent
			//not sure if this is the right thing to do, but things depend on this behaviour
			if (Window.ViewContent != Window.ActiveViewContent) {
				ret = Window.ViewContent.GetContent (typeof(T)) as T;
				if (ret != null)
					return ret;
			}

			//If we didn't find in ActiveView or ViewContent... Try in SubViews
			foreach (var subView in window.SubViewContents) {
				foreach (var cnt in subView.GetContents<T> ()) {
					return cnt;
				}
			}

			return null;
		}

		internal ProjectReloadCapability ProjectReloadCapability {
			get {
				return Window.ViewContent.ProjectReloadCapability;
			}
		}

		public override IEnumerable<T> GetContents<T> ()
		{
			foreach (var cnt in window.ViewContent.GetContents<T> ()) {
				yield return cnt;
			}

			foreach (var subView in window.SubViewContents) {
				foreach (var cnt in subView.GetContents<T> ()) {
					yield return cnt;
				}
			}
		}


		static Document ()
		{
			if (IdeApp.Workbench != null) {
				IdeApp.Workbench.ActiveDocumentChanged += delegate {
					// reparse on document switch to update the current file with changes done in other files.
					var doc = IdeApp.Workbench.ActiveDocument;
					#pragma warning disable CS0618, 612 // Type or member is obsolete
					if (doc == null || doc.Editor == null)
						return;
					doc.StartReparseThread ();
					#pragma warning restore CS0618, 612 // Type or member is obsolete
				};
			}
		}

		public Document (IWorkbenchWindow window)
		{
			Counters.OpenDocuments++;
			LastTimeActive = DateTime.Now;
			this.window = window;
			window.Closed += OnClosed;
			window.ActiveViewContentChanged += OnActiveViewContentChanged;
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ItemRemovedFromSolution += OnEntryRemoved;
			#pragma warning disable CS0612 // Type or member is obsolete
			if (window.ViewContent.Project != null)
				window.ViewContent.Project.Modified += HandleProjectModified;
			window.ViewsChanged += HandleViewsChanged;
			window.ViewContent.ContentNameChanged += OnContentNameChanged;
			MonoDevelopWorkspace.LoadingFinished += ReloadAnalysisDocumentHandler;
			#pragma warning restore CS0612 // Type or member is obsolete
			DocumentRegistry.Add (this);
		}

		void OnContentNameChanged (object sender, EventArgs e)
		{
			OnFileNameChanged ();
			#pragma warning disable CS0612 // Type or member is obsolete
			ReloadAnalysisDocumentHandler (sender, e);
			#pragma warning restore CS0612 // Type or member is obsolete
		}

		[Obsolete]
		void ReloadAnalysisDocumentHandler (object sender, EventArgs e)
		{
			UnsubscribeAnalysisDocument ();
			UnloadAdhocProject ();
			EnsureAnalysisDocumentIsOpen ().ContinueWith (delegate {
				if (analysisDocument != null)
					#pragma warning disable CS0612 // Type or member is obsolete
					StartReparseThread ();
					#pragma warning restore CS0612 // Type or member is obsolete
			});
		}

/*		void UpdateRegisteredDom (object sender, ProjectDomEventArgs e)
		{
			if (dom == null || dom.Project == null)
				return;
			var project = e.ITypeResolveContext != null ? e.ITypeResolveContext.Project : null;
			if (project != null && project.FileName == dom.Project.FileName)
				dom = e.ITypeResolveContext;
		}*/

		public FilePath FileName {
			get {
				if (Window == null || !Window.ViewContent.IsFile)
					return null;
				return Window.ViewContent.IsUntitled ? Window.ViewContent.UntitledName : Window.ViewContent.ContentName;
			}
		}

		internal FilePath OriginalFileName {
			get {
				if (Window == null || !Window.ViewContent.IsFile)
					return null;
				return Window.ViewContent.OriginalContentName;
			}
		}

		internal event EventHandler FileNameChanged;

		void OnFileNameChanged ()
		{
			FileNameChanged?.Invoke (this, EventArgs.Empty);
		}

		public bool IsFile {
			get { return Window.ViewContent.IsFile; }
		}
		
		public bool IsDirty {
			get { return !Window.ViewContent.IsViewOnly && (Window.ViewContent.ContentName == null || Window.ViewContent.IsDirty); }
			set { Window.ViewContent.IsDirty = value; }
		}

		public object GetDocumentObject ()
		{
			return Window?.ViewContent?.GetDocumentObject ();
		}

		FilePath adHocFile;
		Project adhocProject;
		Solution adhocSolution;

		public override Project Project {
			get { return (Window != null ? Window.ViewContent.Project : null) ?? adhocProject; }
/*			set { 
				Window.ViewContent.Project = value; 
				if (value != null)
					singleFileContext = null;
				// File needs to be in sync with the project, otherwise the parsed document at start may be invalid.
				// better solution: create the document with the project attached.
				StartReparseThread ();
			}*/
		}

		internal override bool IsAdHocProject {
			get { return adhocProject != null; }
		}

		[Obsolete ("Use Visual Studio Editor APIs")]
		public override bool IsCompileableInProject {
			get {
				var project = Project;
				if (project == null)
					return false;
				var solution = project.ParentSolution;

				if (solution != null && IdeApp.Workspace != null) {
					var config = IdeApp.Workspace.ActiveConfiguration;
					if (config != null) {
						var sc = solution.GetConfiguration (config);
						if (sc != null && !sc.BuildEnabledForItem (project))
							return false;
					}
				}

				ProjectFile pf = project.GetProjectFile (OriginalFileName);
				if (pf == null)
					pf = project.GetProjectFile (FileName);

				return pf != null && pf.BuildAction != BuildAction.None;
			}
		}

		public Task<Microsoft.CodeAnalysis.Compilation> GetCompilationAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			var project = TypeSystemService.GetCodeAnalysisProject (adhocProject ?? Project); 
			if (project == null)
				return new Task<Microsoft.CodeAnalysis.Compilation> (() => null);
			return project.GetCompilationAsync (cancellationToken);
		}

		[Obsolete ("Use Visual Studio Editor APIs")]
		public override ParsedDocument ParsedDocument {
			get {
				return parsedDocument;
			}
		}
		
		public string PathRelativeToProject {
			get { return Window.ViewContent.PathRelativeToProject; }
		}
		
		public void Select ()
		{
			window.SelectWindow ();
		}
		
		public DocumentView ActiveView {
			get {
				LoadViews (true);
				return WrapView (window.ActiveViewContent);
			}
		}
		
		public DocumentView PrimaryView {
			get {
				LoadViews (true);
				return WrapView (window.ViewContent);
			}
		}

		public ReadOnlyCollection<DocumentView> Views {
			get {
				LoadViews (true);
				if (viewsRO == null)
					viewsRO = new ReadOnlyCollection<DocumentView> (views);
				return viewsRO;
			}
		}

		ReadOnlyCollection<DocumentView> viewsRO;
		List<DocumentView> views = new List<DocumentView> ();

		void HandleViewsChanged (object sender, EventArgs e)
		{
			LoadViews (false);
		}

		void LoadViews (bool force)
		{
			if (!force && views == null)
				return;
			var newList = new List<DocumentView> ();
			newList.Add (WrapView (window.ViewContent));
			foreach (var v in window.SubViewContents)
				newList.Add (WrapView (v));
			views = newList;
			viewsRO = null;
		}

		DocumentView WrapView (BaseViewContent content)
		{
			if (content == null)
				return null;
			if (views != null)
				return views.FirstOrDefault (v => v.BaseContent == content) ?? new DocumentView (this, content);
			else
				return new DocumentView (this, content);
		}

		public override string Name {
			get {
				ViewContent view = Window.ViewContent;
				return view.IsUntitled ? view.UntitledName : view.ContentName;
			}
		}

		[Obsolete("Use GetContent<ITextBuffer>()")]
		public TextEditor Editor {
			get {
				return GetContent <TextEditor> ();
			}
		}

		[Obsolete("Use GetContent<ITextBuffer> ()")]
		public ITextBuffer TextBuffer => GetContent<ITextBuffer> ();

		public bool IsViewOnly {
			get { return Window.ViewContent.IsViewOnly; }
		}

		public override bool IsUntitled {
			get {
				return Window.ViewContent.IsUntitled;
			}
		}

		Task currentOperationTask = Task.FromResult (true);

		Task RunAsyncOperation (Func<Task> action)
		{
			Runtime.AssertMainThread ();
			return currentOperationTask = currentOperationTask.ContinueWith (t => action(), Runtime.MainTaskScheduler).Unwrap ();
		}

		public Task Reload ()
		{
			return RunAsyncOperation (ReloadTask);
		}

		async Task ReloadTask ()
		{
			ICustomXmlSerializer memento = null;
			IMementoCapable mc = GetContent<IMementoCapable> ();
			if (mc != null) {
				memento = mc.Memento;
			}
			window.ViewContent.DiscardChanges ();
			await window.ViewContent.Load (new FileOpenInformation (window.ViewContent.ContentName) { IsReloadOperation = true });
			if (memento != null) {
				mc.Memento = memento;
			}
			OnReload (EventArgs.Empty);
		}

		public event EventHandler Reloaded;

		protected virtual void OnReload (EventArgs e)
		{
			Reloaded?.Invoke (this, e);
		}


		public Task Save ()
		{
			return RunAsyncOperation (SaveTask);
		}

		async Task SaveTask ()
		{
			// suspend type service "check all file loop" since we have already a parsed document.
			// Or at least one that updates "soon".
			try {
				// Freeze the file change events. There can be several such events, and sending them all together
				// is more efficient
				FileService.FreezeEvents ();
				if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsDirty)
					return;
				if (!Window.ViewContent.IsFile) {
					await Window.ViewContent.Save ();
					return;
				}
				if (IsUntitled) {
					await SaveAs ();
				} else {
					try {
                        FileService.RequestFileEdit ((FilePath)Window.ViewContent.ContentName, true);
					} catch (Exception ex) {
						MessageService.ShowError (GettextCatalog.GetString ("The file could not be saved."), ex.Message, ex);
					}
					
					FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
	
					if (!File.Exists ((string)Window.ViewContent.ContentName) || (File.GetAttributes ((string)window.ViewContent.ContentName) & attr) != 0) {
                        await SaveAs();
					} else {
						string fileName = Window.ViewContent.ContentName;
						// save backup first						
						if (IdeApp.Preferences.CreateFileBackupCopies) {
                            await Window.ViewContent.Save (fileName + "~");
						}
						DocumentRegistry.SkipNextChange (fileName);
						await Window.ViewContent.Save (fileName);
						// Force a change notification. This is needed for FastCheckNeedsBuild to be updated
						// when saving before a build, for example.
						FileService.NotifyFileChanged (fileName);
                        OnSaved(EventArgs.Empty);
					}
				}
			} finally {
				// Send all file change notifications
				FileService.ThawEvents ();

				// Set the file time of the current document after the file time of the written file, to prevent double file updates.
				// Note that the parsed document may be overwritten by a background thread to a more recent one.
				#pragma warning disable CS0612 // Type or member is obsolete
				var doc = parsedDocument;
				#pragma warning restore CS0612 // Type or member is obsolete
				if (doc != null) {
					string fileName = Window.ViewContent.ContentName;
					try {
						// filename could be null if the user cancelled SaveAs and this is a new & unsaved file
						if (fileName != null)
							doc.LastWriteTimeUtc = File.GetLastWriteTimeUtc (fileName);
					} catch (Exception e) {
						doc.LastWriteTimeUtc = DateTime.UtcNow;
						LoggingService.LogWarning ("Exception while getting the write time from " + fileName, e); 
					}
				}
			}
		}

		public Task<bool> SaveAs ()
		{
			return SaveAs (null);
		}

		public Task<bool> SaveAs (string filename)
		{
			return Runtime.RunInMainThread (() => SaveAsTask (filename));
		}

		async Task<bool> SaveAsTask (string filename)
		{
			if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsFile)
				return false;

			Encoding encoding = null;
			
			var tbuffer = GetContent <ITextBuffer> ();
			if (tbuffer != null) {
				encoding = tbuffer.GetEncodingOrUTF8 ();
				if (encoding == null)
					encoding = Encoding.UTF8;
			}
				
			if (filename == null) {
				var dlg = new OpenFileDialog (GettextCatalog.GetString ("Save as..."), MonoDevelop.Components.FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow,
					Encoding = encoding,
					ShowEncodingSelector = (tbuffer != null),
				};
				if (Window.ViewContent.IsUntitled)
					dlg.InitialFileName = Window.ViewContent.UntitledName;
				else {
					dlg.CurrentFolder = Path.GetDirectoryName ((string)Window.ViewContent.ContentName);
					dlg.InitialFileName = Path.GetFileName ((string)Window.ViewContent.ContentName);
				}

				if (!dlg.Run ())
					return false;
				
				filename = dlg.SelectedFile;
				encoding = dlg.Encoding;
			}
		
			if (!FileService.IsValidPath (filename)) {
				MessageService.ShowMessage (GettextCatalog.GetString ("File name {0} is invalid", filename));
				return false;
			}
			// detect preexisting file
			if (File.Exists (filename)) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("File {0} already exists. Overwrite?", filename), AlertButton.OverwriteFile))
					return false;
			}
			
			// save backup first
			if (IdeApp.Preferences.CreateFileBackupCopies) {
				if (tbuffer != null && encoding != null)
					TextFileUtility.WriteText (filename + "~", tbuffer.CurrentSnapshot.GetText (), encoding);
				else
					await Window.ViewContent.Save (new FileSaveInformation (filename + "~", encoding));
			}

			// do actual save
			Window.ViewContent.ContentName = filename;
			Window.ViewContent.Project = Workbench.GetProjectContainingFile (filename);
			await Window.ViewContent.Save (new FileSaveInformation (filename, encoding));
			DesktopService.RecentFiles.AddFile (filename, (Project)null);
			
			OnSaved (EventArgs.Empty);

			#pragma warning disable CS0618 // Type or member is obsolete
			await UpdateParseDocument ();
			#pragma warning restore CS0618 // Type or member is obsolete
			return true;
		}
		
		public async Task<bool> Close ()
		{
			return await ((SdiWorkspaceWindow)Window).CloseWindow (false, true);
		}

		protected override void OnSaved (EventArgs e)
		{
			base.OnSaved (e);
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
		
		bool isClosed;
		void OnClosed (object s, EventArgs a)
		{
			isClosed = true;
//			TypeSystemService.DomRegistered -= UpdateRegisteredDom;
			CancelParseTimeout ();
			ClearTasks ();


			try {
				OnClosed (a);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while calling OnClosed.", ex);
			}

			// Parse the file when the document is closed. In this way if the document
			// is closed without saving the changes, the saved compilation unit
			// information will be restored
/*			if (currentParseFile != null) {
				TypeSystemService.QueueParseJob (dom, delegate (string name, IProgressMonitor monitor) {
					TypeSystemService.Parse (curentParseProject, currentParseFile);
				}, FileName);
			}
			if (isFileDom) {
				TypeSystemService.RemoveFileDom (FileName);
				dom = null;
			}*/

			Counters.OpenDocuments--;
		}

		protected override void OnDispose (bool disposing)
		{
			if (IsDisposed)
				return;
			DocumentRegistry.Remove (this);
			#pragma warning disable CS0612 // Type or member is obsolete
			UnsubscribeAnalysisDocument ();
			UnsubscribeRoslynWorkspace ();
			UnloadAdhocProject ();
			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).DetachFromPathedDocument ();
			window.Closed -= OnClosed;
			window.ActiveViewContentChanged -= OnActiveViewContentChanged;
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ItemRemovedFromSolution -= OnEntryRemoved;

			// Unsubscribe project events
			if (window.ViewContent.Project != null)
				window.ViewContent.Project.Modified -= HandleProjectModified;
			window.ViewsChanged -= HandleViewsChanged;
			window.ViewContent.ContentNameChanged -= OnContentNameChanged;
			MonoDevelopWorkspace.LoadingFinished -= ReloadAnalysisDocumentHandler;

			window = null;

			parsedDocument = null;
			#pragma warning restore CS0612 // Type or member is obsolete
			views = null;
			viewsRO = null;
			base.OnDispose (disposing);
		}

		[Obsolete]
		void UnsubscribeAnalysisDocument ()
		{
			lock (analysisDocumentLock) {
				if (analysisDocument != null) {
					TypeSystemService.InformDocumentClose (analysisDocument, FileName);
					analysisDocument = null;
				}
			}
		}
		#region document tasks
		object lockObj = new object ();
		
		void ClearTasks ()
		{
			lock (lockObj) {
				TaskService.Errors.ClearByOwner (this);
			}
		}
		
//		void CompilationUnitUpdated (object sender, ParsedDocumentEventArgs args)
//		{
//			if (this.FileName == args.FileName) {
////				if (!args.Unit.HasErrors)
//				parsedDocument = args.ParsedDocument;
///* TODO: Implement better task update algorithm.
//
//				ClearTasks ();
//				lock (lockObj) {
//					foreach (Error error in args.Unit.Errors) {
//						tasks.Add (new Task (this.FileName, error.Message, error.Column, error.Line, error.ErrorType == ErrorType.Error ? TaskType.Error : TaskType.Warning, this.Project));
//					}
//					IdeApp.Services.TaskService.AddRange (tasks);
//				}*/
//			}
//		}
#endregion
		void OnActiveViewContentChanged (object s, EventArgs args)
		{
			OnViewChanged (args);
		}
		
		void OnClosed (EventArgs args)
		{
			if (Closed != null)
				Closed (this, args);
		}
		
		void OnViewChanged (EventArgs args)
		{
			if (ViewChanged != null)
				ViewChanged (this, args);
		}
		
		bool wasEdited;

		[Obsolete]
		void InitializeExtensionChain ()
		{
			Editor.InitializeExtensionChain (this);
		}

		void AttachPathedDocument ()
		{
			if (window is SdiWorkspaceWindow) {
				((SdiWorkspaceWindow)window).AttachToPathedDocument (GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> ());
			}
		}

		[Obsolete]
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
//			Editor.Undone += (o, a) => StartReparseThread ();
//			Editor.Redone += (o, a) => StartReparseThread ();

			InitializeExtensionChain ();
		}
		
		internal void OnDocumentAttached ()
		{
			#pragma warning disable CS0618, CS0612 // Type or member is obsolete
			if (Editor != null) {
				InitializeEditor ();
				RunWhenRealized (delegate { ListenToProjectLoad (Project); });
			}
			#pragma warning restore CS0618, CS0612

			// this is used by TextViewExtensions.TryGetParentDocument
			var textView = GetContent<Microsoft.VisualStudio.Text.Editor.ITextView> ();
			if (textView != null) {
				textView.Properties.AddProperty (typeof(Document), this);
			}

			AttachPathedDocument ();

			window.Document = this;
		}
		
		/// <summary>
		/// Performs an action when the content is loaded.
		/// </summary>
		/// <param name='action'>
		/// The action to run.
		/// </param>
		[Obsolete("This only works for the old editor")]
		public void RunWhenLoaded (System.Action action)
		{
			var e = Editor;
			if (e == null) {
				action ();
				return;
			}
			e.RunWhenLoaded (action);
		}

		[Obsolete("This only works for the old editor")]
		public void RunWhenRealized (System.Action action)
		{
			var e = Editor;
			if (e == null) {
				action ();
				return;
			}
			e.RunWhenRealized (action);
		}

		public override void AttachToProject (Project project)
		{
			SetProject (project);
		}

		internal void SetProject (Project project)
		{
			if (Window == null || Window.ViewContent == null || Window.ViewContent.Project == project || (IsAdHocProject && project == adhocProject))
				return;
			bool usingAdHocProject = IsAdHocProject;
			UnloadAdhocProject ();
			#pragma warning disable CS0618, CS0612 // Type or member is obsolete
			if (Editor == null) {
				Window.ViewContent.Project = project;
				AttachPathedDocument ();
				return;
			}
			if (adhocProject == null)
				UnsubscribeAnalysisDocument ();
							  // Unsubscribe project events
			if (Window.ViewContent.Project != null)
				Window.ViewContent.Project.Modified -= HandleProjectModified;
			Window.ViewContent.Project = project;
			if (project != null)
				project.Modified += HandleProjectModified;
			InitializeExtensionChain ();
			AttachPathedDocument ();
			// Do not start the parser when the project is set to null and an adHocProject is not being used. This
			// would result in a new adHocProject being created and then RootWorkspace would not update the Document's
			// project since it is non-null.
			if (project != null || (project == null && usingAdHocProject))
				ListenToProjectLoad (project);
			#pragma warning restore CS0618, CS0612 // Type or member is obsolete
		}

		[Obsolete]
		void ListenToProjectLoad (Project project)
		{
			StartReparseThread ();
		}

		[Obsolete]
		void HandleInLoadChanged (object sender, EventArgs e)
		{
			StartReparseThread ();
		}

		[Obsolete]
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
		[Obsolete("Use Visual Studio Editor APIs")]
		public override async Task<ParsedDocument> UpdateParseDocument ()
		{
			try {
				await EnsureAnalysisDocumentIsOpen ();
				string currentParseFile = GetCurrentParseFileName();
				var editor = Editor;
				if (editor == null || string.IsNullOrEmpty (currentParseFile))
					return null;
				var currentParseText = editor.CreateDocumentSnapshot ();
				CancelOldParsing();
				var project = adhocProject ?? Project;

				var options = new ParseOptions {
					Project = project,
					Content = currentParseText,
					FileName = currentParseFile,
					OldParsedDocument = parsedDocument,
					RoslynDocument = AnalysisDocument,
					IsAdhocProject = IsAdHocProject
				};

				if (project != null && TypeSystemService.CanParseProjections (project, Editor.MimeType, FileName)) {
					var projectFile = project.GetProjectFile (currentParseFile);
                    if (projectFile != null)
						options.BuildAction = projectFile.BuildAction;
					
					var p = await TypeSystemService.ParseProjection (options, editor.MimeType);
					if (p != null) {
						this.parsedDocument = p.ParsedDocument;
						var projections = p.Projections;
						foreach (var p2 in projections)
							p2.CreateProjectedEditor (this);
						Editor.SetOrUpdateProjections (this, projections, p.DisabledProjectionFeatures);
					}
				} else { 
					this.parsedDocument = await TypeSystemService.ParseFile (options, editor.MimeType) ?? this.parsedDocument;
				}
			} finally {

				OnDocumentParsed (EventArgs.Empty);
			}
			return this.parsedDocument;
		}
			
		uint parseTimeout = 0;
		CancellationTokenSource analysisDocumentSrc = new CancellationTokenSource ();

		[Obsolete]
		void CancelEnsureAnalysisDocumentIsOpen ()
		{
			analysisDocumentSrc.Cancel ();
			analysisDocumentSrc = new CancellationTokenSource ();
		}

		/// <summary>
		/// During that process ad hoc projects shouldn't be created.
		/// </summary>
		internal static bool IsInProjectSettingLoadingProcess { get; set; }

		[Obsolete]
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
			if (Project != null && !IsUnreferencedSharedProject(Project)) {
				lock (analysisDocumentLock) {
					UnsubscribeRoslynWorkspace ();
					RoslynWorkspace = TypeSystemService.GetWorkspace (this.Project.ParentSolution);
					if (RoslynWorkspace == null) // Solution not loaded yet
						return Task.CompletedTask;
					SubscribeRoslynWorkspace ();
					var newAnalysisDocument = FileName != null ? TypeSystemService.GetDocumentId (this.Project, this.FileName) : null;
					var changedAnalysisDocument = newAnalysisDocument != analysisDocument;
					analysisDocument = newAnalysisDocument;
					if (analysisDocument != null && !RoslynWorkspace.CurrentSolution.ContainsAdditionalDocument (analysisDocument) && !RoslynWorkspace.IsDocumentOpen(analysisDocument)) {
						TypeSystemService.InformDocumentOpen (analysisDocument, TextBuffer.AsTextContainer(), this);
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
					if (!Window.ViewContent.IsUntitled) {
						adHocFile = FileName;
					} else {
						adHocFile = (Platform.IsWindows ? "C:\\" : "/") + Window.ViewContent.UntitledName + ".cs";
					}

					newProject.Files.Add (new ProjectFile (adHocFile, BuildAction.Compile));

					adhocSolution = new Solution ();
					adhocSolution.AddConfiguration ("", true);
					adhocSolution.DefaultSolutionFolder.AddItem (newProject);
					return TypeSystemService.Load (adhocSolution, new ProgressMonitor (), token, false).ContinueWith (task => {
						if (token.IsCancellationRequested)
							return;
						UnsubscribeRoslynWorkspace ();
						RoslynWorkspace = task.Result.FirstOrDefault (); // 1 solution loaded ->1 workspace as result
						SubscribeRoslynWorkspace ();
						analysisDocument = RoslynWorkspace.CurrentSolution.Projects.First ().DocumentIds.First ();
						TypeSystemService.InformDocumentOpen (RoslynWorkspace, analysisDocument, TextBuffer.AsTextContainer (), this);
						OnAnalysisDocumentChanged (EventArgs.Empty);
					});
				}
			}
			return Task.CompletedTask;
		}

		[Obsolete]
		void UnsubscribeRoslynWorkspace ()
		{
			var ws = RoslynWorkspace as MonoDevelopWorkspace;
			if (ws != null) {
				ws.WorkspaceChanged -= HandleRoslynProjectChange;
				ws.DocumentClosed -= HandleRoslynDocumentClosed;
			}
		}

		[Obsolete]
		void SubscribeRoslynWorkspace ()
		{
			var ws = RoslynWorkspace as MonoDevelopWorkspace;
			if (ws != null) {
				ws.WorkspaceChanged += HandleRoslynProjectChange;
				ws.DocumentClosed += HandleRoslynDocumentClosed;
			}
		}

		[Obsolete]
		void HandleRoslynDocumentClosed (object sender, Microsoft.CodeAnalysis.DocumentEventArgs e)
		{
			lock (analysisDocumentLock) {
				if (e.Document.Id == analysisDocument) {
					analysisDocument = null;
				}
			}
		}

		[Obsolete]
		void HandleRoslynProjectChange (object sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
		{
			if (e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectChanged ||
				e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectAdded ||
				e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectRemoved ||
				e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded) {
				StartReparseThread ();
			}
		}

		bool IsUnreferencedSharedProject (Project project)
		{
			return project is SharedAssetsProject;
		}

		static int adhocProjectCount = 0;
		object adhocProjectLock = new object();
		object analysisDocumentLock = new object ();
		void UnloadAdhocProject ()
		{
			#pragma warning disable CS0612 // Type or member is obsolete
			CancelEnsureAnalysisDocumentIsOpen ();
			#pragma warning restore CS0612 // Type or member is obsolete
			lock (adhocProjectLock) {
				if (adhocProject == null)
					return;
				if (adhocSolution != null) {
					TypeSystemService.Unload (adhocSolution);
					adhocSolution.Dispose ();
					adhocSolution = null;
				}
				adhocProject = null;
			}
		}

		CancellationTokenSource parseTokenSource = new CancellationTokenSource();

		[Obsolete]
		void CancelOldParsing()
		{
			parseTokenSource.Cancel ();
			parseTokenSource = new CancellationTokenSource ();
		}

		object reparseTimeoutLock = new object ();

		[Obsolete]
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

		[Obsolete]
		string GetCurrentParseFileName ()
		{
			var editor = Editor;
			string result = adhocProject != null ? adHocFile : editor?.FileName;
			return result ?? FileName;
		}

		[Obsolete]
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
					var options = new ParseOptions {
						Project = project,
						Content = currentParseText,
						FileName = currentParseFile,
						OldParsedDocument = parsedDocument,
						RoslynDocument = AnalysisDocument,
						IsAdhocProject =  IsAdHocProject
					};
					if (projectFile != null)
						options.BuildAction = projectFile.BuildAction;

					if (project != null && TypeSystemService.CanParseProjections (project, mimeType, currentParseFile)) {
						TypeSystemService.ParseProjection (options, mimeType, token).ContinueWith ((task, state) => {
							if (token.IsCancellationRequested)
								return;
							if (currentProject != state)
								return;
							Application.Invoke ((o, args) => {
								// this may be called after the document has closed, in that case the OnDocumentParsed event shouldn't be invoked.
								var taskResult = task.Result;
								if (isClosed || taskResult == null || token.IsCancellationRequested)
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
						TypeSystemService.ParseFile (options, mimeType, token).ContinueWith (task => {
							if (token.IsCancellationRequested)
								return;
							Application.Invoke ((o, args) => {
								// this may be called after the document has closed, in that case the OnDocumentParsed event shouldn't be invoked.
								if (isClosed || task.Result == null || token.IsCancellationRequested)
									return;
								this.parsedDocument = task.Result;
								OnDocumentParsed (EventArgs.Empty);
							});
						}, TaskContinuationOptions.OnlyOnRanToCompletion);
					}
				}
			});
		}

		/// <summary>
		/// This method kicks off an async document parser and should be used instead of 
		/// <see cref="UpdateParseDocument"/> unless you need the parsed document immediately.
		/// </summary>
		[Obsolete("Use Visual Studio Editor APIs")]
		public override void ReparseDocument ()
		{
			StartReparseThread ();
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem == window.ViewContent.Project)
				window.ViewContent.Project = null;
		}
		
		public event EventHandler Closed;
		public event EventHandler ViewChanged;
		

		public string[] CommentTags {
			get {
				if (IsFile)
					return GetCommentTags (FileName);
				else
					return null;
			}
		}

		public static string[] GetCommentTags (string fileName)
		{
			//Document doc = IdeApp.Workbench.ActiveDocument;
			var lang = TextMateLanguage.Create (SyntaxHighlightingService.GetScopeForFileName (fileName));
			if (lang.LineComments.Count > 0)
				return lang.LineComments.ToArray ();

			if (lang.BlockComments.Count> 0)
				return new [] { lang.BlockComments[0].Item1, lang.BlockComments[0].Item2 };
			return null;
		}

		public override T GetPolicy<T> (IEnumerable<string> types)
		{	
			if (adhocProject !=	null)
				return MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<T> (types);
			return base.GetPolicy<T> (types);
		}
	
//		public MonoDevelop.Projects.CodeGeneration.CodeGenerator CreateCodeGenerator ()
//		{
//			return MonoDevelop.Projects.CodeGeneration.CodeGenerator.CreateGenerator (Editor.Document.MimeType, 
//				Editor.Options.TabsToSpaces, Editor.Options.TabSize, Editor.EolMarker);
//		}

		/// <summary>
		/// If the document shouldn't restore the settings after the load it can be disabled with this method.
		/// That is useful when opening a document and programmatically scrolling to a specified location.
		/// </summary>
		public void DisableAutoScroll ()
		{
			if (IsFile)
				FileSettingsStore.Remove (FileName);
		}

		public override OptionSet GetOptionSet ()
		{
			return TypeSystemService.Workspace.Options;
		}

		[Obsolete]
		internal override Task<IReadOnlyList<Editor.Projection.Projection>> GetPartialProjectionsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			var parser = TypeSystemService.GetParser (Editor.MimeType);
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

		[Obsolete]
		internal override void UpdateDocumentId (Microsoft.CodeAnalysis.DocumentId newId)
		{
			this.analysisDocument = newId;
			OnAnalysisDocumentChanged (EventArgs.Empty);
		}
	}
	
	
	[Serializable]
	public sealed class DocumentEventArgs : EventArgs
	{
		public Document Document {
			get;
			set;
		}
		public DocumentEventArgs (Document document)
		{
			this.Document = document;
		}
	}
}

