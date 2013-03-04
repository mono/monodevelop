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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Text;
using System.Collections.ObjectModel;

namespace MonoDevelop.Ide.Gui
{
	public class Document : ICSharpCode.NRefactory.AbstractAnnotatable
	{
		internal object MemoryProbe = Counters.DocumentsInMemory.CreateMemoryProbe ();
		
		IWorkbenchWindow window;
		TextEditorExtension editorExtension;
		
		const int ParseDelay = 600;

		public IWorkbenchWindow Window {
			get { return window; }
		}
		
		internal DateTime LastTimeActive {
			get;
			set;
		}
		
		public TextEditorExtension EditorExtension {
			get { return this.editorExtension; }
		}
 		
		public T GetContent<T> () where T : class
		{
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
			
			//no, so look through the TexteditorExtensions as well
			TextEditorExtension nextExtension = editorExtension;
			while (nextExtension != null) {
				if (typeof(T).IsAssignableFrom (nextExtension.GetType ()))
					return nextExtension as T;
				nextExtension = nextExtension.Next as TextEditorExtension;
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
			
			//no, so look through the TexteditorExtensions as well
			TextEditorExtension nextExtension = editorExtension;
			while (nextExtension != null) {
				if (typeof(T).IsAssignableFrom (nextExtension.GetType ()))
					yield return nextExtension as T;
				nextExtension = nextExtension.Next as TextEditorExtension;
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
				if (!Window.ViewContent.IsFile)
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
		
		public bool HasProject {
			get { return Window.ViewContent.Project != null; }
		}
		
		public Project Project {
			get { return Window.ViewContent.Project; }
/*			set { 
				Window.ViewContent.Project = value; 
				if (value != null)
					singleFileContext = null;
				// File needs to be in sync with the project, otherwise the parsed document at start may be invalid.
				// better solution: create the document with the project attached.
				StartReparseThread ();
			}*/
		}

		public bool IsCompileableInProject {
			get {
				var project = Project;
				if (project == null)
					return false;
				var pf = project.GetProjectFile (FileName);
				return pf != null && pf.BuildAction == BuildAction.Compile;
			}
		}

		IProjectContent singleFileContext;
		public  IProjectContent ProjectContent {
			get {
				return Project != null ? TypeSystemService.GetProjectContext (Project) : GetProjectContext ();
			}
		}
		
		public virtual ICompilation Compilation {
			get {
				return Project != null ? TypeSystemService.GetCompilation (Project) : GetProjectContext ().CreateCompilation ();
			}
		}
		
		ParsedDocument parsedDocument;
		public virtual ParsedDocument ParsedDocument {
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

		public string Name {
			get {
				IViewContent view = Window.ViewContent;
				return view.IsUntitled ? view.UntitledName : view.ContentName;
			}
		}

		Mono.TextEditor.ITextEditorDataProvider provider = null;
		public Mono.TextEditor.TextEditorData Editor {
			get {
				if (provider == null) {
					provider = GetContent <Mono.TextEditor.ITextEditorDataProvider> ();
					if (provider == null)
						return null;
				}
				return provider.GetTextEditorData ();
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
					if (!FileService.RequestFileEdit (Window.ViewContent.ContentName))
						MessageService.ShowMessage (GettextCatalog.GetString ("The file could not be saved. Write permission has not been granted."));
					
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
					doc.ParsedFile.LastWriteTime = DateTime.Now;
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
			
			IEncodedTextContent tbuffer = GetContent <IEncodedTextContent> ();
			if (tbuffer != null) {
				encoding = tbuffer.SourceEncoding;
				if (encoding == null)
					encoding = Encoding.Default;
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
					tbuffer.Save (filename + "~", encoding);
				else
					Window.ViewContent.Save (filename + "~");
			}
			TypeSystemService.RemoveSkippedfile (FileName);
			// do actual save
			if (tbuffer != null && encoding != null)
				tbuffer.Save (filename, encoding);
			else
				Window.ViewContent.Save (filename);

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
		
		void OnSaved (EventArgs args)
		{
			if (Saved != null)
				Saved (this, args);
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
			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).DetachFromPathedDocument ();
			window.Closed -= OnClosed;
			window.ActiveViewContentChanged -= OnActiveViewContentChanged;
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ItemRemovedFromSolution -= OnEntryRemoved;

			// Unsubscribe project events
			if (window.ViewContent.Project != null)
				window.ViewContent.Project.Modified -= HandleProjectModified;

			try {
				OnClosed (a);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while calling OnClosed.", ex);
			}
			DetachExtensionChain ();

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
			
			parsedDocument = null;
			provider = null;
			Counters.OpenDocuments--;
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
			DetachExtensionChain ();
			var editor = GetContent<IExtensibleTextEditor> ();

			ExtensionNodeList extensions = window.ExtensionContext.GetExtensionNodes ("/MonoDevelop/Ide/TextEditorExtensions", typeof(TextEditorExtensionNode));
			editorExtension = null;
			TextEditorExtension last = null;
			foreach (TextEditorExtensionNode extNode in extensions) {
				if (!extNode.Supports (FileName))
					continue;
				TextEditorExtension ext = (TextEditorExtension)extNode.CreateInstance ();
				if (ext.ExtendsEditor (this, editor)) {
					if (editorExtension == null)
						editorExtension = ext;
					if (last != null)
						last.Next = ext;
					last = ext;
					ext.Initialize (this);
				}
			}
			if (editorExtension != null)
				last.Next = editor.AttachExtension (editorExtension);
		}

		void DetachExtensionChain ()
		{
			while (editorExtension != null) {
				try {
					editorExtension.Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError ("Exception while disposing extension:" + editorExtension, ex);
				}
				editorExtension = editorExtension.Next as TextEditorExtension;
			}
			editorExtension = null;
		}

		void InitializeEditor (IExtensibleTextEditor editor)
		{
			Editor.Document.TextReplaced += (o, a) => {
				if (parsedDocument != null)
					parsedDocument.IsInvalid = true;

				if (Editor.Document.IsInAtomicUndo) {
					wasEdited = true;
				} else {
					StartReparseThread ();
				}
			};
			
			Editor.Document.BeginUndo += delegate {
				wasEdited = false;
			};
			
			Editor.Document.EndUndo += delegate {
				if (wasEdited)
					StartReparseThread ();
			};
			Editor.Document.Undone += (o, a) => StartReparseThread ();
			Editor.Document.Redone += (o, a) => StartReparseThread ();

			InitializeExtensionChain ();
		}
		
		internal void OnDocumentAttached ()
		{
			IExtensibleTextEditor editor = GetContent<IExtensibleTextEditor> ();
			if (editor != null) {
				InitializeEditor (editor);
				RunWhenLoaded (ReparseDocument);
			}
			
			window.Document = this;
			
			if (window is SdiWorkspaceWindow)
				((SdiWorkspaceWindow)window).AttachToPathedDocument (GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> ());
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
			if (e == null || e.Document == null) {
				action ();
				return;
			}
			e.Document.RunWhenLoaded (action);
		}
		
		internal void SetProject (Project project)
		{
			if (Window.ViewContent.Project == project)
				return;
			DetachExtensionChain ();
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
			StartReparseThread ();
		}

		void HandleProjectModified (object sender, SolutionItemModifiedEventArgs e)
		{
			if (!e.Any (
					x => x is SolutionItemModifiedEventInfo &&
				(((SolutionItemModifiedEventInfo)x).Hint == "TargetFramework" ||
				((SolutionItemModifiedEventInfo)x).Hint == "References")))
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
				if (editor == null)
					return null;
				TypeSystemService.AddSkippedFile (currentParseFile);
				string currentParseText = editor.Text;
				this.parsedDocument = TypeSystemService.ParseFile (Project, currentParseFile, editor.Document.MimeType, currentParseText);
				if (Project == null && this.parsedDocument != null) {
					singleFileContext = GetProjectContext ().AddOrUpdateFiles (parsedDocument.ParsedFile);
				}
			} finally {
				OnDocumentParsed (EventArgs.Empty);
			}
			return this.parsedDocument;
		}

		static readonly Lazy<IUnresolvedAssembly> mscorlib = new Lazy<IUnresolvedAssembly> ( () => new CecilLoader ().LoadAssemblyFile (typeof (object).Assembly.Location));
		static readonly Lazy<IUnresolvedAssembly> systemCore = new Lazy<IUnresolvedAssembly>( () => new CecilLoader ().LoadAssemblyFile (typeof (System.Linq.Enumerable).Assembly.Location));
		static readonly Lazy<IUnresolvedAssembly> system = new Lazy<IUnresolvedAssembly>( () => new CecilLoader ().LoadAssemblyFile (typeof (System.Uri).Assembly.Location));

		static IUnresolvedAssembly Mscorlib { get { return mscorlib.Value; } }
		static IUnresolvedAssembly SystemCore { get { return systemCore.Value; } }
		static IUnresolvedAssembly System { get { return system.Value; } }

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
			CancelParseTimeout ();
			
			parseTimeout = GLib.Timeout.Add (ParseDelay, delegate {
				var editor = Editor;
				if (editor == null)
					return false;
				string currentParseText = editor.Text;
				string mimeType = editor.Document.MimeType;
				ThreadPool.QueueUserWorkItem (delegate {
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
		public void ReparseDocument ()
		{
			StartReparseThread ();
		}
		
		internal object ExtendedCommandTargetChain {
			get {
				// Only go through the text editor chain, if the text editor is selected as subview
				if (Window != null && Window.ActiveViewContent == Window.ViewContent)
					return editorExtension;
				return null;
			}
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem == window.ViewContent.Project)
				window.ViewContent.Project = null;
		}
		
		void OnDocumentParsed (EventArgs e)
		{
			EventHandler handler = this.DocumentParsed;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler Closed;
		public event EventHandler Saved;
		public event EventHandler ViewChanged;
		
		public event EventHandler DocumentParsed;
		
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
			
			Mono.TextEditor.Highlighting.SyntaxMode mode = null;
			foreach (string mt in DesktopService.GetMimeTypeInheritanceChain (loadedMimeType)) {
				mode = Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode (null, mt);
				if (mode != null)
					break;
			}
			
			if (mode == null)
				return null;
			
			List<string> ctags;
			if (mode.Properties.TryGetValue ("LineComment", out ctags) && ctags.Count > 0) {
				return new string [] { ctags [0] };
			}
			List<string> tags = new List<string> ();
			if (mode.Properties.TryGetValue ("BlockCommentStart", out ctags))
				tags.Add (ctags [0]);
			if (mode.Properties.TryGetValue ("BlockCommentEnd", out ctags))
				tags.Add (ctags [0]);
			if (tags.Count == 2)
				return tags.ToArray ();
			else
				return null;
		}
	
//		public MonoDevelop.Projects.CodeGeneration.CodeGenerator CreateCodeGenerator ()
//		{
//			return MonoDevelop.Projects.CodeGeneration.CodeGenerator.CreateGenerator (Editor.Document.MimeType, 
//				Editor.Options.TabsToSpaces, Editor.Options.TabSize, Editor.EolMarker);
//		}

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

