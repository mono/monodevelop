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
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Text;
using System.Collections.ObjectModel;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Gui
{

	public class Document : DocumentContext
	{
		internal object MemoryProbe = Counters.DocumentsInMemory.CreateMemoryProbe ();
		
		IWorkbenchWindow window;
		ParsedDocument parsedDocument;
		IProjectContent singleFileContext;

		const int ParseDelay = 600;

		public IWorkbenchWindow Window {
			get { return window; }
		}
		
		internal DateTime LastTimeActive {
			get;
			set;
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

			return null;
		}
		
		public IEnumerable<T> GetContents<T> () where T : class
		{
			//check whether the ViewContent can return the type directly
			T ret = (T) Window.ActiveViewContent.GetContent (typeof(T));
			if (ret != null)
				yield return ret;
			
			//check the primary viewcontent
			//not sure if this is the right thing to do, but things depend on this behaviour
			if (Window.ViewContent != Window.ActiveViewContent) {
				ret = (T) Window.ViewContent.GetContent (typeof(T));
				if (ret != null)
					yield return ret;
			}
		}


		static Document ()
		{
			if (IdeApp.Workbench != null) {
				IdeApp.Workbench.ActiveDocumentChanged += delegate {
					// reparse on document switch to update the current file with changes done in other files.
					var doc = IdeApp.Workbench.ActiveDocument;
					if (doc == null || doc.Editor == null)
						return;
					doc.StartReparseThread ();
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
			if (window.ViewContent.Project != null)
				window.ViewContent.Project.Modified += HandleProjectModified;
			window.ViewsChanged += HandleViewsChanged;
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

		public bool IsFile {
			get { return Window.ViewContent.IsFile; }
		}
		
		public bool IsDirty {
			get { return !Window.ViewContent.IsViewOnly && (Window.ViewContent.ContentName == null || Window.ViewContent.IsDirty); }
			set { Window.ViewContent.IsDirty = value; }
		}

		
		public override Project Project {
			get { return Window != null ? Window.ViewContent.Project : null; }
/*			set { 
				Window.ViewContent.Project = value; 
				if (value != null)
					singleFileContext = null;
				// File needs to be in sync with the project, otherwise the parsed document at start may be invalid.
				// better solution: create the document with the project attached.
				StartReparseThread ();
			}*/
		}

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

				var pf = project.GetProjectFile (FileName);
				return pf != null && pf.BuildAction != BuildAction.None;
			}
		}

		public override IProjectContent ProjectContent {
			get {
				return Project != null ? TypeSystemService.GetProjectContext (Project) : GetProjectContext ();
			}
		}
		
		public override ICompilation Compilation {
			get {
				return Project != null ? TypeSystemService.GetCompilation (Project) : GetProjectContext ().CreateCompilation ();
			}
		}
		
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

		DocumentView WrapView (IBaseViewContent content)
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
				IViewContent view = Window.ViewContent;
				return view.IsUntitled ? view.UntitledName : view.ContentName;
			}
		}

		public TextEditor Editor {
			get {
				return GetContent <TextEditor> ();
			}
		}

		public bool IsViewOnly {
			get { return Window.ViewContent.IsViewOnly; }
		}
		
		public void Reload ()
		{
			ICustomXmlSerializer memento = null;
			IMementoCapable mc = GetContent<IMementoCapable> ();
			if (mc != null) {
				memento = mc.Memento;
			}
			window.ViewContent.Load (window.ViewContent.ContentName);
			if (memento != null) {
				mc.Memento = memento;
			}
		}
		
		public void Save ()
		{
			// suspend type service "check all file loop" since we have already a parsed document.
			// Or at least one that updates "soon".
			TypeSystemService.TrackFileChanges = false;
			try {
				if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsDirty)
					return;
	
				if (!Window.ViewContent.IsFile) {
					Window.ViewContent.Save ();
					return;
				}
				
				if (Window.ViewContent.ContentName == null) {
					SaveAs ();
				} else {
					try {
						FileService.RequestFileEdit (Window.ViewContent.ContentName, true);
					} catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("The file could not be saved."));
					}
					
					FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
	
					if (!File.Exists (Window.ViewContent.ContentName) || (File.GetAttributes (window.ViewContent.ContentName) & attr) != 0) {
						SaveAs ();
					} else {
						string fileName = Window.ViewContent.ContentName;
						// save backup first						
						if ((bool)PropertyService.Get ("SharpDevelop.CreateBackupCopy", false)) {
							Window.ViewContent.Save (fileName + "~");
							FileService.NotifyFileChanged (fileName);
						}
						Window.ViewContent.Save (fileName);
						FileService.NotifyFileChanged (fileName);
						OnSaved (EventArgs.Empty);
					}
				}
			} finally {
				// Set the file time of the current document after the file time of the written file, to prevent double file updates.
				// Note that the parsed document may be overwritten by a background thread to a more recent one.
				var doc = parsedDocument;
				if (doc != null && doc.ParsedFile != null) {
					string fileName = Window.ViewContent.ContentName;
					try {
						doc.ParsedFile.LastWriteTime = File.GetLastWriteTimeUtc (fileName);
					} catch (Exception e) {
						doc.ParsedFile.LastWriteTime = DateTime.UtcNow;
						LoggingService.LogWarning ("Exception while getting the write time from " + fileName, e); 
					}
				}
				TypeSystemService.TrackFileChanges = true;
			}
		}
		
		public void SaveAs ()
		{
			SaveAs (null);
		}
		
		public void SaveAs (string filename)
		{
			if (Window.ViewContent.IsViewOnly || !Window.ViewContent.IsFile)
				return;


			Encoding encoding = null;
			
			var tbuffer = GetContent <ITextSource> ();
			if (tbuffer != null) {
				encoding = tbuffer.Encoding;
				if (encoding == null)
					encoding = Encoding.UTF8;
			}
				
			if (filename == null) {
				var dlg = new OpenFileDialog (GettextCatalog.GetString ("Save as..."), FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow,
					Encoding = encoding,
					ShowEncodingSelector = (tbuffer != null),
				};
				if (Window.ViewContent.IsUntitled)
					dlg.InitialFileName = Window.ViewContent.UntitledName;
				else {
					dlg.CurrentFolder = Path.GetDirectoryName (Window.ViewContent.ContentName);
					dlg.InitialFileName = Path.GetFileName (Window.ViewContent.ContentName);
				}
				
				if (!dlg.Run ())
					return;
				
				filename = dlg.SelectedFile;
				encoding = dlg.Encoding;
			}
		
			if (!FileService.IsValidPath (filename)) {
				MessageService.ShowMessage (GettextCatalog.GetString ("File name {0} is invalid", filename));
				return;
			}
			// detect preexisting file
			if (File.Exists (filename)) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("File {0} already exists. Overwrite?", filename), AlertButton.OverwriteFile))
					return;
			}
			
			// save backup first
			if ((bool)PropertyService.Get ("SharpDevelop.CreateBackupCopy", false)) {
				if (tbuffer != null && encoding != null)
					TextFileUtility.WriteText (filename + "~", tbuffer.Text, encoding, tbuffer.UseBOM);
				else
					Window.ViewContent.Save (new FileSaveInformation (filename + "~", encoding));
			}
			TypeSystemService.RemoveSkippedfile (FileName);
			// do actual save
			Window.ViewContent.Save (new FileSaveInformation (filename + "~", encoding));

			FileService.NotifyFileChanged (filename);
			DesktopService.RecentFiles.AddFile (filename, (Project)null);
			
			OnSaved (EventArgs.Empty);
			UpdateParseDocument ();
		}
		
		public bool IsBuildTarget
		{
			get
			{
				if (this.IsViewOnly)
					return false;
				if (Window.ViewContent.ContentName != null)
					return Services.ProjectService.CanCreateSingleFileProject(Window.ViewContent.ContentName);
				
				return false;
			}
		}
		
		public IAsyncOperation Build ()
		{
			return IdeApp.ProjectOperations.BuildFile (Window.ViewContent.ContentName);
		}
		
		public IAsyncOperation Rebuild ()
		{
			return Build ();
		}
		
		public void Clean ()
		{
		}
		
		public IAsyncOperation Run ()
		{
			return Run (Runtime.ProcessService.DefaultExecutionHandler);
		}

		public IAsyncOperation Run (IExecutionHandler handler)
		{
			return IdeApp.ProjectOperations.ExecuteFile (Window.ViewContent.ContentName, handler);
		}

		public bool CanRun ()
		{
			return CanRun (Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		public bool CanRun (IExecutionHandler handler)
		{
			return IsBuildTarget && Window.ViewContent.ContentName != null && IdeApp.ProjectOperations.CanExecuteFile (Window.ViewContent.ContentName, handler);
		}
		
		public bool Close ()
		{
			return ((SdiWorkspaceWindow)Window).CloseWindow (false, true);
		}

		protected override void OnSaved (EventArgs e)
		{
			IdeApp.Workbench.SaveFileStatus ();
			base.OnSaved (e);
		}

		public void CancelParseTimeout ()
		{
			if (parseTimeout != 0) {
				GLib.Source.Remove (parseTimeout);
				parseTimeout = 0;
			}
		}
		
		bool isClosed;
		void OnClosed (object s, EventArgs a)
		{
			isClosed = true;
//			TypeSystemService.DomRegistered -= UpdateRegisteredDom;
			CancelParseTimeout ();
			ClearTasks ();
			TypeSystemService.RemoveSkippedfile (FileName);


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

		internal void DisposeDocument ()
		{
			if (Editor != null) {
				Editor.Dispose ();
			}
			RemoveAnnotations (typeof(System.Object));
			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).DetachFromPathedDocument ();
			window.Closed -= OnClosed;
			window.ActiveViewContentChanged -= OnActiveViewContentChanged;
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ItemRemovedFromSolution -= OnEntryRemoved;

			// Unsubscribe project events
			if (window.ViewContent.Project != null)
				window.ViewContent.Project.Modified -= HandleProjectModified;
			window.ViewsChanged += HandleViewsChanged;
			window = null;

			parsedDocument = null;
			singleFileContext = null;
			views = null;
			viewsRO = null;
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

		void InitializeExtensionChain ()
		{
			Editor.InitializeExtensionChain (this, Editor);

			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).AttachToPathedDocument (GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> ());

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
			
			Editor.BeginUndo += delegate {
				wasEdited = false;
			};
			
			Editor.EndUndo += delegate {
				if (wasEdited)
					StartReparseThread ();
			};
//			Editor.Undone += (o, a) => StartReparseThread ();
//			Editor.Redone += (o, a) => StartReparseThread ();

			InitializeExtensionChain ();
		}
		
		internal void OnDocumentAttached ()
		{
			if (Editor != null) {
				InitializeEditor ();
				RunWhenLoaded (delegate { ListenToProjectLoad (Project); });
			}
			
			window.Document = this;
		}
		
		/// <summary>
		/// Performs an action when the content is loaded.
		/// </summary>
		/// <param name='action'>
		/// The action to run.
		/// </param>
		public void RunWhenLoaded (System.Action action)
		{
			var e = Editor;
			if (e == null) {
				action ();
				return;
			}
			e.RunWhenLoaded (action);
		}

		public override void AttachToProject (Project project)
		{
			SetProject (project);
		}

		TypeSystemService.ProjectContentWrapper currentWrapper;
		internal void SetProject (Project project)
		{
			if (Window.ViewContent.Project == project)
				return;
			ISupportsProjectReload pr = GetContent<ISupportsProjectReload> ();
			if (pr != null) {
				// Unsubscribe project events
				if (Window.ViewContent.Project != null)
					Window.ViewContent.Project.Modified -= HandleProjectModified;
				Window.ViewContent.Project = project;
				pr.Update (project);
			}
			if (project != null)
				project.Modified += HandleProjectModified;
			InitializeExtensionChain ();

			ListenToProjectLoad (project);
		}

		void ListenToProjectLoad (Project project)
		{
			if (currentWrapper != null) {
				currentWrapper.Loaded -= HandleInLoadChanged;
				currentWrapper = null;
			}
			if (project != null) {
				var wrapper = TypeSystemService.GetProjectContentWrapper (project);
				wrapper.Loaded += HandleInLoadChanged;
				currentWrapper = wrapper;
				RunWhenLoaded (delegate {
					currentWrapper.RequestLoad ();
				});
			}
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
		public ParsedDocument UpdateParseDocument ()
		{
			try {
				string currentParseFile = FileName;
				var editor = Editor;
				if (editor == null || string.IsNullOrEmpty (currentParseFile))
					return null;
				TypeSystemService.AddSkippedFile (currentParseFile);
				string currentParseText = editor.Text;
				this.parsedDocument = TypeSystemService.ParseFile (Project, currentParseFile, editor.MimeType, currentParseText);
				if (Project == null && this.parsedDocument != null) {
					singleFileContext = GetProjectContext ().AddOrUpdateFiles (parsedDocument.ParsedFile);
				}
			} finally {

				OnDocumentParsed (EventArgs.Empty);
			}
			return this.parsedDocument;
		}

		static readonly Lazy<IUnresolvedAssembly> mscorlib = new Lazy<IUnresolvedAssembly> ( () => new IkvmLoader ().LoadAssemblyFile (typeof (object).Assembly.Location));
		static readonly Lazy<IUnresolvedAssembly> systemCore = new Lazy<IUnresolvedAssembly>( () => new IkvmLoader ().LoadAssemblyFile (typeof (System.Linq.Enumerable).Assembly.Location));
		static readonly Lazy<IUnresolvedAssembly> system = new Lazy<IUnresolvedAssembly>( () => new IkvmLoader ().LoadAssemblyFile (typeof (System.Uri).Assembly.Location));

		static IUnresolvedAssembly Mscorlib { get { return mscorlib.Value; } }
		static IUnresolvedAssembly SystemCore { get { return systemCore.Value; } }
		static IUnresolvedAssembly System { get { return system.Value; } }

		public override bool IsProjectContextInUpdate {
			get {
				if (currentWrapper == null)
					return false;
				return !currentWrapper.IsLoaded || !currentWrapper.ReferencesConnected;
			}
		}

		public virtual IProjectContent GetProjectContext ()
		{
			if (Project == null) {
				if (singleFileContext == null) {
					singleFileContext = new ICSharpCode.NRefactory.CSharp.CSharpProjectContent ();
					singleFileContext = singleFileContext.AddAssemblyReferences (new [] { Mscorlib, System, SystemCore });
				}
				if (parsedDocument != null)
					return singleFileContext.AddOrUpdateFiles (parsedDocument.ParsedFile);
				return singleFileContext;
			}
			
			return TypeSystemService.GetProjectContext (Project);
		}
		
		uint parseTimeout = 0;
		internal void StartReparseThread ()
		{
			// Don't directly parse the document because doing it at every key press is
			// very inefficient. Do it after a small delay instead, so several changes can
			// be parsed at the same time.
			string currentParseFile = FileName;
			if (string.IsNullOrEmpty (currentParseFile))
				return;
			CancelParseTimeout ();
			if (IsProjectContextInUpdate)
				return;
			parseTimeout = GLib.Timeout.Add (ParseDelay, delegate {
				var editor = Editor;
				if (editor == null || IsProjectContextInUpdate) {
					parseTimeout = 0;
					return false;
				}
				string currentParseText = editor.Text;
				string mimeType = editor.MimeType;
				ThreadPool.QueueUserWorkItem (delegate {
					if (IsProjectContextInUpdate)
						return;
					TypeSystemService.AddSkippedFile (currentParseFile);
					var currentParsedDocument = TypeSystemService.ParseFile (Project, currentParseFile, mimeType, currentParseText);
					Application.Invoke (delegate {
						// this may be called after the document has closed, in that case the OnDocumentParsed event shouldn't be invoked.
						if (isClosed)
							return;

						this.parsedDocument = currentParsedDocument;
						OnDocumentParsed (EventArgs.Empty);
					});
				});
				parseTimeout = 0;
				return false;
			});
		}
		
		/// <summary>
		/// This method kicks off an async document parser and should be used instead of 
		/// <see cref="UpdateParseDocument"/> unless you need the parsed document immediately.
		/// </summary>
		public override void ReparseDocument ()
		{
			StartReparseThread ();
		}
		
		internal object ExtendedCommandTargetChain {
			get {
				// Only go through the text editor chain, if the text editor is selected as subview
				if (Window != null && Window.ActiveViewContent == Window.ViewContent && Editor != null)
					return Editor.CommandRouter;
				return null;
			}
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
			string loadedMimeType = DesktopService.GetMimeTypeForUri (fileName);

			var result = TextEditorFactory.GetSyntaxProperties (loadedMimeType, "LineComment");
			if (result != null)
				return result;

			var start = TextEditorFactory.GetSyntaxProperties (loadedMimeType, "BlockCommentStart");
			var end = TextEditorFactory.GetSyntaxProperties (loadedMimeType, "BlockCommentEnd");
			if (start != null && end != null)
				return new [] { start[0], end[0] };
			return null;
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
			FileSettingsStore.Remove (FileName);
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

