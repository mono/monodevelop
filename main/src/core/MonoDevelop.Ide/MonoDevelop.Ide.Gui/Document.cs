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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.TextMate;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Ide.Gui
{

	public class Document
	{
		internal object MemoryProbe = Counters.DocumentsInMemory.CreateMemoryProbe ();

		IShell shell;
		IWorkbenchWindow window;
		DocumentController controller;
		DocumentControllerDescription documentControllerDescription;
		DocumentView view;
		DocumentManager documentManager;
		DocumentContext documentContext;
		bool checkedDocumentContext;
		IDisposable callbackRegistration;
		bool closed;
		AsyncCriticalSection asyncCriticalSection = new AsyncCriticalSection ();

		internal IWorkbenchWindow Window {
			get { return window; }
		}

		internal DateTime LastTimeActive {
			get;
			set;
		}

		public DocumentContext DocumentContext {
			get {
				if (!checkedDocumentContext) {
					documentContext = GetContent<RoslynDocumentExtension> ()?.DocumentContext;
					checkedDocumentContext = true;
				}
				return documentContext;
			}
		}

		internal DocumentControllerDescription DocumentControllerDescription => documentControllerDescription;

		public Xwt.Drawing.Image Icon => controller.DocumentIcon;

		IEnumerable<DocumentController> GetControllersForContentCheck ()
		{
			// Exclude root controller from GetAllControllers() since it will be already returned by GetActiveControllerHierarchy()
			return view.GetActiveControllerHierarchy ().Concat (view.GetAllControllers ().Where (c => c != controller));
		}

		/// <summary>
		/// Raised when the content of the document changes, which means that GetContent() may return new content objects
		/// </summary>
		public event EventHandler ContentChanged;

		void OnContentChanged ()
		{
			checkedDocumentContext = false;
			ContentChanged?.Invoke (this, EventArgs.Empty);
			contentCallbackRegistry?.InvokeContentChangeCallbacks ();
		}

		/// <summary>
		/// Asynchronously waits for a specific type of content to be available and returns it
		/// </summary>
		/// <typeparam name="T">Type of the content to return</typeparam>
		/// <typeparam name="cancellationToken">Cancellation token that cancels the wait</typeparam>
		public Task<T> GetContentWhenAvailable<T> (CancellationToken cancellationToken = default (CancellationToken))
		{
			var taskSource = new TaskCompletionSource<T> ();
			var regCancel = cancellationToken.Register (() => taskSource.TrySetCanceled ());
			var regContent = RunWhenContentAdded<T> (c => {
				taskSource.TrySetResult (c);
			});
			taskSource.Task.ContinueWith (t => { regCancel.Dispose (); regContent.Dispose (); });
			return taskSource.Task;
		}

		public T GetContent<T> (bool forActiveView) where T : class
		{
			return (T)GetContent (forActiveView, typeof (T));
		}

		public T GetContent<T> () where T : class
		{
			return GetContent<T> (false);
		}

		public IEnumerable<T> GetContents<T> (bool forActiveView) where T : class
		{
			if (forActiveView)
				return view.GetActiveControllerHierarchy ().Select (controller => controller.GetContents<T> ()).SelectMany (c => c);
			return GetControllersForContentCheck ().Select (controller => controller.GetContents<T> ()).SelectMany (c => c);
		}

		public IEnumerable<T> GetContents<T> () where T : class
		{
			return GetContents<T> (false);
		}

		object GetContent (bool forActiveView, Type type)
		{
			if (forActiveView)
				return GetContentForActiveView (type);
			return GetContentIncludingAllViews (type);
		}

		object GetContentIncludingAllViews (Type type)
		{
			return GetControllersForContentCheck ().Select (controller => controller.GetContent (type)).FirstOrDefault (content => content != null);
		}

		object GetContentForActiveView (Type type)
		{
			return view.GetActiveControllerHierarchy ().Select (controller => controller.GetContent (type)).FirstOrDefault (content => content != null);
		}

		ContentCallbackRegistry contentCallbackRegistry;
		ContentCallbackRegistry contentActiveViewCallbackRegistry;

		ContentCallbackRegistry GetCallbackRegistry ()
		{
			if (contentCallbackRegistry == null)
				contentCallbackRegistry = new ContentCallbackRegistry (GetContentIncludingAllViews);
			return contentCallbackRegistry;
		}

		/// <summary>
		/// Executes an action when a content of the provided type is added to the controller
		/// </summary>
		/// <typeparam name="T">Type of the content to track</typeparam>
		/// <param name="contentCallback">Callback to invoke when the content is added</param>
		/// <returns>A registration object that can be disposed to cancel the callback invocation.</returns>
		public IDisposable RunWhenContentAdded<T> (Action<T> contentCallback)
		{
			return GetCallbackRegistry ().RunWhenContentAdded (contentCallback);
		}

		/// <summary>
		/// Executes an action when a content of the provided type is removed from the controller
		/// </summary>
		/// <typeparam name="T">Type of the content to track</typeparam>
		/// <param name="contentCallback">Callback to invoke when the content is removed</param>
		/// <returns>A registration object that can be disposed to cancel the callback invocation.</returns>
		public IDisposable RunWhenContentRemoved<T> (Action<T> contentCallback)
		{
			return GetCallbackRegistry ().RunWhenContentRemoved (contentCallback);
		}

		/// <summary>
		/// Executes an action when a content of the provided type is added or removed from the controller
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="addedCallback">Callback to invoke when the content is added</param>
		/// <param name="removedCallback">Callback to invoke when the content is removed</param>
		/// <returns>A registration object that can be disposed to cancel the callback invocation.</returns>
		public IDisposable RunWhenContentAddedOrRemoved<T> (Action<T> addedCallback, Action<T> removedCallback)
		{
			return GetCallbackRegistry ().RunWhenContentAddedOrRemoved (addedCallback, removedCallback);
		}

		public bool IsNewDocument {
			get { return controller.IsNewDocument; }
		}

		public FilePath FilePath {
			get { return (controller as FileDocumentController)?.FilePath ?? FilePath.Null; }
		}

		internal ProjectReloadCapability ProjectReloadCapability {
			get {
				return controller.ProjectReloadCapability;
			}
		}

		public string Name {
			get {
				return controller.DocumentTitle;
			}
		}

		internal Document (DocumentManager documentManager, IShell shell, DocumentController controller, DocumentControllerDescription controllerDescription)
		{
			Counters.OpenDocuments++;

			this.shell = shell;
			this.documentManager = documentManager;
			this.documentControllerDescription = controllerDescription;
			this.controller = controller;
			controller.Document = this;

			callbackRegistration = documentManager.ServiceProvider.WhenServiceInitialized<RootWorkspace> (s => {
				s.ItemRemovedFromSolution += OnEntryRemoved;
				callbackRegistration = null;
			});
		}

		internal async Task InitializeWindow (IWorkbenchWindow window)
		{
			this.window = window;
			window.Document = this;

			view = await controller.GetDocumentView ();
			view.IsRoot = true;
			window.SetRootView (view.CreateShellView (window));

			LastTimeActive = DateTime.Now;
			this.window.CloseRequested += Window_CloseRequested;

			SubscribeControllerEvents ();
			DocumentRegistry.Add (this);
		}

		void SubscribeControllerEvents ()
		{
			UnsubscribeControllerEvents ();
			view.ActiveViewInHierarchyChanged += ActiveViewInHierarchyChanged;
			controller.OwnerChanged += HandleOwnerChanged;
			controller.DocumentTitleChanged += OnContentNameChanged;
			controller.ContentChanged += ControllerContentChanged;
			controller.ShowNotificationChanged += ControllerShowNotificationChanged;
		}

		void UnsubscribeControllerEvents ()
		{
			view.ActiveViewInHierarchyChanged -= ActiveViewInHierarchyChanged;
			controller.DocumentTitleChanged -= OnContentNameChanged;
			controller.OwnerChanged -= HandleOwnerChanged;
			controller.ContentChanged -= ControllerContentChanged;
			controller.ShowNotificationChanged -= ControllerShowNotificationChanged;
		}

		internal bool TryReuseDocument (ModelDescriptor modelDescriptor)
		{
			return controller.TryReuseDocument (modelDescriptor);
		}

		internal DocumentController DocumentController => controller;

		void ControllerContentChanged (object sender, EventArgs e)
		{
			OnContentChanged ();
		}

		void ActiveViewInHierarchyChanged (object sender, EventArgs e)
		{
			OnContentChanged ();
		}

		void HandleOwnerChanged (object sender, EventArgs e)
		{
		}

		void ControllerShowNotificationChanged (object sender, EventArgs e)
		{
			window.ShowNotification = controller.ShowNotification;
		}

		void OnContentNameChanged (object sender, EventArgs e)
		{
			OnFileNameChanged ();
		}

		public FilePath FileName {
			get {
				if (controller is FileDocumentController file)
					return file.FilePath;
				else if (controller.Model is FileModel fileModel)
					return fileModel.FilePath;
				else
					return null;
			}
		}

		internal FilePath OriginalFileName {
			get {
				return controller.OriginalContentName;
			}
		}

		internal event EventHandler FileNameChanged;

		void OnFileNameChanged ()
		{
			FileNameChanged?.Invoke (this, EventArgs.Empty);
		}

		public bool IsFile {
			get { return controller is FileDocumentController; }
		}
		
		public bool IsDirty {
			get { return !controller.IsReadOnly && (controller.IsNewDocument || controller.HasUnsavedChanges); }
			set { controller.HasUnsavedChanges = value; }
		}

		public async Task<Microsoft.CodeAnalysis.Compilation> GetCompilationAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return await GetContent<RoslynDocumentExtension> ()?.DocumentContext.GetCompilationAsync (cancellationToken);
		}

		public void Select ()
		{
			window?.SelectWindow ();
			view.GrabFocus ();
		}

		public TextEditor Editor {
			get {
				return GetContent <TextEditor> ();
			}
		}

		public ITextBuffer TextBuffer => GetContent<ITextBuffer> ();

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
			await controller.Reload ();
			OnReload (EventArgs.Empty);
		}

		public event EventHandler Reloaded;

		void OnReload (EventArgs e)
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

			var fileController = controller as FileDocumentController;

			try {
				// Freeze the file change events. There can be several such events, and sending them all together
				// is more efficient
				FileService.FreezeEvents ();
				if (controller.IsViewOnly || !controller.HasUnsavedChanges)
					return;
				if (fileController == null) {
					await controller.Save ();
					return;
				}
				if (IsNewDocument) {
					await SaveAs ();
				} else {
					var fileName = fileController.FilePath;
					try {
                        FileService.RequestFileEdit (fileName, true);
					} catch (Exception ex) {
						MessageService.ShowError (GettextCatalog.GetString ("The file could not be saved."), ex.Message, ex);
					}

					FileAttributes attr = FileAttributes.ReadOnly | FileAttributes.Directory | FileAttributes.Offline | FileAttributes.System;
	
					if (!File.Exists (fileName) || (File.GetAttributes (fileName) & attr) != 0) {
                        await SaveAs();
					} else {
						var allFiles = controller.GetDocumentFiles ().ToList ();

						foreach (var file in allFiles)
							DocumentRegistry.SkipNextChange (file);

						await controller.Save ();

						if (IdeApp.Preferences.CreateFileBackupCopies) {
							foreach (var file in allFiles) {
								try {
									File.Copy (file, file + "~");
								} catch (Exception ex) {
									LoggingService.LogError ("Backup copy could not be created", ex);
								}
							}
						}
					
						// Force a change notification. This is needed for FastCheckNeedsBuild to be updated
						// when saving before a build, for example.
						FileService.NotifyFilesChanged (allFiles);

						Saved?.Invoke (this, EventArgs.Empty);
					}
				}
			} finally {
				// Send all file change notifications
				FileService.ThawEvents ();
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
			var fileController = controller as FileDocumentController;
			if (fileController == null || controller.IsViewOnly || !fileController.SupportsSaveAs)
				return false;

			// TOTEST: that the default encoding is UTF8
			Encoding encoding = fileController.SupportsEncoding ? fileController.Encoding : null;
			
			if (filename == null) {
				var dlg = new OpenFileDialog (GettextCatalog.GetString ("Save as..."), MonoDevelop.Components.FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow,
					Encoding = encoding,
					ShowEncodingSelector = fileController.SupportsEncoding,
				};
				if (controller.IsNewDocument)
					dlg.InitialFileName = FilePath;
				else {
					dlg.CurrentFolder = FilePath.ParentDirectory;
					dlg.InitialFileName = FilePath.FileName;
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

			// do actual save
			await fileController.SaveAs (filename, encoding);

			if (fileController.Owner == null || fileController.Owner is Project project && project.GetProjectFile (filename) == null) {
				// TOTEST
				var workbench = await documentManager.ServiceProvider.GetService<RootWorkspace> ();
				fileController.Owner = workbench.GetProjectContainingFile (filename);
			}

			if (IdeApp.Preferences.CreateFileBackupCopies) {
				foreach (var file in controller.GetDocumentFiles ()) {
					try {
						File.Copy (file, file + "~");
					} catch (Exception ex) {
						LoggingService.LogError ("Backup copy could not be created", ex);
					}
				}
			}

			IdeServices.DesktopService.RecentFiles.AddFile (filename, (Project)null);

			return true;
		}

		public async Task<bool> Close (bool force = false)
		{
			using (await asyncCriticalSection.EnterAsync ()) {
				if (closed)
					return true;

				bool wasActive = documentManager.ActiveDocument == this;

				// Raise the closing event. Handlers have a chance to cancel the save operation

				var args = new DocumentCloseEventArgs (this, force, wasActive);
				args.Cancel = false;
				await OnClosing (args);
				if (!force && args.Cancel)
					return false;

				// Show the File not Saved UI

				if (!await ShowSaveUI (force))
					return false;

				closed = true;

				ClearTasks ();

				try {
					Closed?.Invoke (this, args);
				} catch (Exception ex) {
					LoggingService.LogError ("Exception while calling Closed event.", ex);
				}

				shell.CloseView (window, true);

				Counters.OpenDocuments--;

				Dispose ();

				return true;
			}
		}

		async Task OnClosing (DocumentCloseEventArgs e)
		{
			if (Closing != null) {
				foreach (var handler in Closing.GetInvocationList ().Cast<DocumentCloseAsyncEventHandler> ()) {
					await handler (this, e);
					if (e.Cancel)
						break;
				}
			}
		}

		async Task<bool> ShowSaveUI (bool force)
		{
			if (!force && controller.HasUnsavedChanges) {
				AlertButton result = MessageService.GenericAlert (Stock.Warning,
					GettextCatalog.GetString ("Save the changes to document '{0}' before closing?", controller.DocumentTitle),
					GettextCatalog.GetString ("If you don't save, all changes will be permanently lost."),
					AlertButton.CloseWithoutSave, AlertButton.Cancel, controller.IsNewDocument ? AlertButton.SaveAs : AlertButton.Save);

				if (result == AlertButton.Save) {
					await Save ();
					if (controller.HasUnsavedChanges) {
						// This may happen if the save operation failed
						Select ();
						return false;
					}
				} else if (result == AlertButton.SaveAs) {
					var resultSaveAs = await SaveAs ();
					if (!resultSaveAs || controller.HasUnsavedChanges) {
						// This may happen if the save operation failed or Save As was canceled
						Select ();
						return false;
					}
				} else if (result == AlertButton.CloseWithoutSave) {
					return true;
				} else if (result == AlertButton.Cancel) {
					return false;
				}
			}
			return true;
		}

		void Window_CloseRequested (object sender, EventArgs e)
		{
			Close ().Ignore ();
		}

		internal void Dispose ()
		{
			DocumentRegistry.Remove (this);
			callbackRegistration?.Dispose ();
			var workspace = Runtime.PeekService<RootWorkspace> ();
			if (workspace != null)
				workspace.ItemRemovedFromSolution -= OnEntryRemoved;

			UnsubscribeControllerEvents ();
			window.SetRootView (null);
			view.IsRoot = false;
			view.Dispose (); // This will also dispose the controller

			window = null;

			contentCallbackRegistry = null;
			contentActiveViewCallbackRegistry = null;
		}
		#region document tasks
		object lockObj = new object ();
		
		void ClearTasks ()
		{
			lock (lockObj) {
				var taskService = Runtime.PeekService<TaskService> ();
				taskService?.Errors.ClearByOwner (this);
			}
		}
		
#endregion


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

		public void AttachToProject (WorkspaceObject project)
		{
			controller.Owner = project;
		}

		internal void ConvertToUnsavedFile ()
		{
			throw new NotImplementedException ();
		}

		internal void RenameFile (FilePath newFile)
		{
			if (controller is FileDocumentController file)
				file.FilePath = newFile;
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs args)
		{
			if (args.SolutionItem == controller.Owner)
				controller.Owner = null;
		}

		public event DocumentCloseAsyncEventHandler Closing;
		public event EventHandler Closed;


		public string[] CommentTags {
			get {
				if (IsFile)
					return GetCommentTags (FileName);
				else
					return null;
			}
		}

		public bool IsViewOnly => controller.IsViewOnly;

		public WorkspaceObject Owner => controller.Owner;

		public event EventHandler Saved;

		public static string [] GetCommentTags (string fileName)
		{
			//Document doc = IdeApp.Workbench.ActiveDocument;
			var lang = TextMateLanguage.Create (SyntaxHighlightingService.GetScopeForFileName (fileName));
			if (lang.LineComments.Count > 0)
				return lang.LineComments.ToArray ();

			if (lang.BlockComments.Count> 0)
				return new [] { lang.BlockComments[0].Item1, lang.BlockComments[0].Item2 };
			return null;
		}
	
		/// <summary>
		/// If the document shouldn't restore the settings after the load it can be disabled with this method.
		/// That is useful when opening a document and programmatically scrolling to a specified location.
		/// </summary>
		public void DisableAutoScroll ()
		{
			if (IsFile)
				FileSettingsStore.Remove (FileName);
		}

		internal void UpdateContentVisibility ()
		{
			view.UpdateContentVisibility (window.ContentVisible);
		}

		internal void GrabFocus ()
		{
			view.GrabFocus ();
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

