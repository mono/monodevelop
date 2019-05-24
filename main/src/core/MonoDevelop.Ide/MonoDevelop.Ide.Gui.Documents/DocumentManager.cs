//
// DocumentManager.cs
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using MonoDevelop.Components.DockNotebook;
using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Shell;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Manages a collection of documents
	/// </summary>
	[DefaultServiceImplementation]
	public class DocumentManager: Service
	{
		NavigationHistoryService navigationHistoryManager;
		IShell workbench;
		DesktopService desktopService;

		IEditorOperationsFactoryService editorOperationsFactoryService;

		ImmutableList<Document> documents = ImmutableList<Document>.Empty;
		Document activeDocument;
		Dictionary<IShellNotebook, List<IWorkbenchWindow>> documentHistory = new Dictionary<IShellNotebook, List<IWorkbenchWindow>> ();

		protected override Task OnInitialize (ServiceProvider serviceProvider)
		{
			serviceProvider.WhenServiceInitialized<IShell> (s => {
				workbench = s;
				workbench.ActiveWorkbenchWindowChanged += OnDocumentChanged;
				workbench.WindowReordered += Workbench_WindowReordered;
				workbench.NotebookClosed += Workbench_NotebookClosed;
			});

			FileService.FileRemoved += CheckRemovedFile;
			FileService.FileMoved += CheckRenamedFile;
			FileService.FileRenamed += CheckRenamedFile;
			return Task.CompletedTask;
		}

		async Task InitDesktopService ()
		{
			if (desktopService == null)
				desktopService = await ServiceProvider.GetService<DesktopService> ();
		}

		protected override Task OnDispose ()
		{
			documents = documents.Clear ();
			WatchDirectories ();
			return base.OnDispose ();
		}

		/// <summary>
		/// Raised when a document is opened
		/// </summary>
		public event EventHandler<DocumentEventArgs> DocumentOpened;

		/// <summary>
		/// Raised when a document is closed
		/// </summary>
		public event EventHandler<DocumentEventArgs> DocumentClosed;

		/// <summary>
		/// Raised when a document is going to be closed. There is a chance to cancel the operation.
		/// </summary>
		public event DocumentCloseAsyncEventHandler DocumentClosing;

		/// <summary>
		/// Raised when the active document changes
		/// </summary>
		public event EventHandler<DocumentEventArgs> ActiveDocumentChanged;

		/// <summary>
		/// Preferences for the document manager
		/// </summary>
		/// <value>The preferences.</value>
		public DocumentManagerPreferences Preferences { get; } = new DocumentManagerPreferences ();

		public class DocumentManagerPreferences
		{
			public readonly ConfigurationProperty<bool> LoadDocumentUserProperties = ConfigurationProperty.Create ("SharpDevelop.LoadDocumentProperties", true);
		}

		/// <summary>
		/// A list of all currently open documents
		/// </summary>
		public ImmutableList<Document> Documents {
			get { return documents; }
		}

		/// <summary>
		/// The currently active document
		/// </summary>
		/// <value>The active document.</value>
		public Document ActiveDocument => activeDocument;

		/// <summary>
		/// Gets the documet that for the provided file
		/// </summary>
		/// <returns>The document.</returns>
		/// <param name="filePath">File path.</param>
		public Document GetDocument (FilePath filePath)
		{
			var fullPath = filePath.FullPath;

			foreach (var doc in documents) {
				if (doc.FilePath.FullPath == fullPath || doc.OriginalFileName.FullPath == fullPath)
					return doc;
			}
			return null;
		}

		public async Task CloseAllDocuments (bool leaveActiveDocumentOpen)
		{
			Document [] docs = new Document [Documents.Count];
			Documents.CopyTo (docs, 0);

			// The active document is the last one to close.
			// It avoids firing too many ActiveDocumentChanged events.

			foreach (Document doc in docs) {
				if (doc != ActiveDocument)
					await doc.Close ();
			}
			if (!leaveActiveDocumentOpen && ActiveDocument != null)
				await ActiveDocument.Close ();
		}

		public Task<Document> NewDocument (string defaultName, string mimeType, string content)
		{
			MemoryStream ms = new MemoryStream ();
			byte [] data = System.Text.Encoding.UTF8.GetBytes (content);
			ms.Write (data, 0, data.Length);
			ms.Position = 0;
			return NewDocument (defaultName, mimeType, ms);
		}

		public async Task<Document> NewDocument (string defaultName, string mimeType, Stream content)
		{
			var fileDescriptor = new FileDescriptor (defaultName, mimeType, content, null);

			var documentControllerService = await ServiceProvider.GetService<DocumentControllerService> ();
			var controllerDesc = (await documentControllerService.GetSupportedControllers (fileDescriptor)).FirstOrDefault (c => c.CanUseAsDefault);
			if (controllerDesc == null)
				throw new ApplicationException ("Can't create display binding for mime type: " + mimeType);

			var controller = await controllerDesc.CreateController (fileDescriptor);
			using (content) {
				await controller.Initialize (fileDescriptor, new Properties ());
			}

			controller.HasUnsavedChanges = false;

			var fileOpenInfo = new FileOpenInformation (defaultName);
			fileOpenInfo.DocumentController = controller;
			fileOpenInfo.DocumentControllerDescription = controllerDesc;

			var document = await ShowView (fileOpenInfo);

			if (document.Editor != null)
				document.Editor.Encoding = Encoding.UTF8;
			if (document.DocumentContext != null)
				document.DocumentContext.ReparseDocument ();
			return document;
		}

		public async Task<Document> OpenDocument (ModelDescriptor modelDescriptor, DocumentControllerRole? role = null, bool bringToFront = true)
		{
			var documentControllerService = await ServiceProvider.GetService<DocumentControllerService> ();
			var factories = (await documentControllerService.GetSupportedControllers (modelDescriptor)).Where (c => role == null || c.Role == role).ToList ();
			var controllerDesc = factories.FirstOrDefault (c => c.CanUseAsDefault);
			if (controllerDesc == null)
				controllerDesc = factories.FirstOrDefault ();
			if (controllerDesc == null)
				throw new ApplicationException ("Can't create display binding for model descriptor: " + modelDescriptor);

			var controller = await controllerDesc.CreateController (modelDescriptor);
			await controller.Initialize (modelDescriptor);
			return await OpenDocument (controller, bringToFront);
		}

		public async Task<Document> OpenDocument (DocumentController controller, bool bringToFront = true)
		{
			controller.ServiceProvider = ServiceProvider;

			var docOpenInfo = new DocumentOpenInformation {
				DocumentController = controller
			};

			if (!bringToFront)
				docOpenInfo.Options &= ~OpenDocumentOptions.BringToFront;

			var doc = await ShowView (docOpenInfo);
			if (bringToFront)
				workbench.Present ();
			return doc;
		}

		public async Task<Document> OpenDocument (FileOpenInformation info)
		{
			if (string.IsNullOrEmpty (info.FileName))
				return null;

			if (navigationHistoryManager == null)
				navigationHistoryManager = await ServiceProvider.GetService<NavigationHistoryService> ();

			// Make sure composition manager is ready since ScrollToRequestedCaretLocation will use it
			await Runtime.GetService<CompositionManager> ();

			var metadata = CreateOpenDocumentTimerMetadata ();
			var fileDescriptor = new FileDescriptor (info.FileName, null, info.Owner);

			using (Counters.OpenDocumentTimer.BeginTiming ("Opening file " + info.FileName, metadata)) {
				navigationHistoryManager.LogActiveDocument ();
				Counters.OpenDocumentTimer.Trace ("Look for open document");
				foreach (Document doc in Documents) {

					// Search all ViewContents to see if they can "re-use" this filename.
					if (!doc.TryReuseDocument (fileDescriptor))
						continue;

					//if found, try to reuse or close the old view
					// reuse the view if the binidng didn't change
					if (info.Options.HasFlag (OpenDocumentOptions.TryToReuseViewer) || doc.DocumentControllerDescription == info.DocumentControllerDescription) {
						if (info.Owner != null && doc.Owner != info.Owner) {
							doc.AttachToProject (info.Owner);
						}

						ScrollToRequestedCaretLocation (doc, info);

						if (info.Options.HasFlag (OpenDocumentOptions.BringToFront)) {
							doc.Select ();
							navigationHistoryManager.LogActiveDocument ();
						}
						return doc;
					} else {
						if (!await doc.Close ())
							return doc;
						break;
					}
				}
				Counters.OpenDocumentTimer.Trace ("Initializing monitor");
				var progressMonitorManager = await ServiceProvider.GetService<ProgressMonitorManager> ();
				var pm = progressMonitorManager.GetStatusProgressMonitor (
					GettextCatalog.GetString ("Opening {0}", info.Owner is SolutionFolderItem item ?
						info.FileName.ToRelative (item.ParentSolution.BaseDirectory) :
						info.FileName),
					Stock.StatusWorking,
					true
				);

				var result = await RealOpenFile (pm, info);
				pm.Dispose ();

				AddOpenDocumentTimerMetadata (metadata, info, result.Content, result.Success);

				if (result.Content != null) {
					Counters.OpenDocumentTimer.Trace ("Wrapping document");
					Document doc = result.Content;

					if (doc != null && info.Options.HasFlag (OpenDocumentOptions.BringToFront))
						doc.Select ();
					return doc;
				}
				return null;
			}
		}

		internal async Task<Document> BatchOpenDocument (ProgressMonitor monitor, FilePath fileName, Project project, int line, int column, IShellNotebook dockNotebook)
		{
			if (string.IsNullOrEmpty (fileName))
				return null;

			var metadata = CreateOpenDocumentTimerMetadata ();

			using (Counters.OpenDocumentTimer.BeginTiming ("Batch opening file " + fileName, metadata)) {
				var openFileInfo = new FileOpenInformation (fileName, project) {
					Options = OpenDocumentOptions.OnlyInternalViewer,
					Line = line,
					Column = column,
					DockNotebook = dockNotebook
				};

				var result = await RealOpenFile (monitor, openFileInfo);

				AddOpenDocumentTimerMetadata (metadata, openFileInfo, result.Content, result.Success);

				return result.Content.DocumentController.Document;
			}
		}

		async Task<(bool Success, Document Content)> RealOpenFile (ProgressMonitor monitor, FileOpenInformation openFileInfo)
		{
			FilePath fileName;

			await InitDesktopService ();

			Counters.OpenDocumentTimer.Trace ("Checking file");

			string origName = openFileInfo.FileName;

			if (origName == null) {
				monitor.ReportError (GettextCatalog.GetString ("Invalid file name"), null);
				return (false, null);
			}

			fileName = openFileInfo.FileName;
			if (!origName.StartsWith ("http://", StringComparison.Ordinal))
				fileName = fileName.FullPath;

			//Debug.Assert(FileService.IsValidPath(fileName));
			if (FileService.IsDirectory (fileName)) {
				monitor.ReportError (GettextCatalog.GetString ("{0} is a directory", fileName), null);
				return (false, null);
			}

			// test, if file fileName exists
			if (!origName.StartsWith ("http://", StringComparison.Ordinal)) {
				// test, if an untitled file should be opened
				if (!Path.IsPathRooted (origName)) {
					foreach (Document doc in Documents) {
						if (doc.IsNewDocument && doc.FilePath == origName) {
							doc.Select ();
							ScrollToRequestedCaretLocation (doc, openFileInfo);
							return (true, doc);
						}
					}
				}

				if (!File.Exists (fileName)) {
					monitor.ReportError (GettextCatalog.GetString ("File not found: {0}", fileName), null);
					return (false, null);
				}
			}

			Counters.OpenDocumentTimer.Trace ("Looking for binding");

			var documentControllerService = await ServiceProvider.GetService<DocumentControllerService> ();

			IExternalDisplayBinding externalBinding = null;
			DocumentControllerDescription internalBinding = null;
			WorkspaceObject project = null;

			if (openFileInfo.Owner == null) {
				var workspace = await ServiceProvider.GetService<RootWorkspace> ();
				// Set the project if one can be found. The project on the FileOpenInformation
				// is used to add project metadata to the OpenDocumentTimer counter.
				project = workspace.GetProjectContainingFile (fileName);

				// In some cases, the file may be a symlinked file. We cannot find the resolved symlink path
				// in the project, so we should try looking up the original file.
				if (project == null)
					project = workspace.GetProjectContainingFile (openFileInfo.OriginalFileName);
				openFileInfo.Owner = project;
			} else
				project = openFileInfo.Owner;

			var displayBindingService = await ServiceProvider.GetService<DisplayBindingService> ();

			var fileDescriptor = new FileDescriptor (fileName, null, project);
			var internalViewers = await documentControllerService.GetSupportedControllers (fileDescriptor);
			var externalViewers = displayBindingService.GetDisplayBindings (fileName, null, project as Project).OfType<IExternalDisplayBinding> ().ToList ();

			if (openFileInfo.DocumentControllerDescription != null) {
				internalBinding = openFileInfo.DocumentControllerDescription;
			} else {
				var bindings = displayBindingService.GetDisplayBindings (fileName, null, project as Project).ToList ();
				if (openFileInfo.Options.HasFlag (OpenDocumentOptions.OnlyInternalViewer)) {
					internalBinding = internalViewers.FirstOrDefault (d => d.CanUseAsDefault) ?? internalViewers.FirstOrDefault ();
				} else if (openFileInfo.Options.HasFlag (OpenDocumentOptions.OnlyExternalViewer)) {
					externalBinding = externalViewers.FirstOrDefault (d => d.CanUseAsDefault) ?? externalViewers.FirstOrDefault ();
				} else {
					internalBinding = internalViewers.FirstOrDefault (d => d.CanUseAsDefault);
					if (internalBinding == null) {
						externalBinding = externalViewers.FirstOrDefault (d => d.CanUseAsDefault);
						if (externalBinding == null) {
							internalBinding = internalViewers.FirstOrDefault ();
							if (internalBinding == null)
								externalBinding = externalViewers.FirstOrDefault ();
						}
					}
				}
			}

			Document newContent = null;
			try {
				if (internalBinding != null) {
					newContent = await LoadFile (fileName, monitor, internalBinding, project, openFileInfo);
				} else if (externalBinding != null) {
					var extBinding = (IExternalDisplayBinding)externalBinding;
					var app = extBinding.GetApplication (fileName, null, project as Project);
					app.Launch (fileName);
				} else if (!openFileInfo.Options.HasFlag (OpenDocumentOptions.OnlyInternalViewer)) {
					try {
						Counters.OpenDocumentTimer.Trace ("Showing in browser");
						desktopService.OpenFile (fileName);
					} catch (Exception ex) {
						LoggingService.LogError ("Error opening file: " + fileName, ex);
						MessageService.ShowError (GettextCatalog.GetString ("File '{0}' could not be opened", fileName));
						return (false, null);
					}
				}
				Counters.OpenDocumentTimer.Trace ("Adding to recent files");
				desktopService.RecentFiles.AddFile (fileName, project);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), ex);
				return (false, null);
			}
			return (true, newContent);
		}

		async Task<Document> LoadFile (FilePath fileName, ProgressMonitor monitor, DocumentControllerDescription binding, WorkspaceObject project, FileOpenInformation fileInfo)
		{
			// Make sure composition manager is ready since ScrollToRequestedCaretLocation will use it
			await Runtime.GetService<CompositionManager> ();

			string mimeType = desktopService.GetMimeTypeForUri (fileName);
			var fileDescriptor = new FileDescriptor (fileName, mimeType, project);

			try {
				Counters.OpenDocumentTimer.Trace ("Creating content");
				DocumentController controller;

				try {
					fileInfo.DocumentControllerDescription = binding;
					controller = fileInfo.DocumentController = await binding.CreateController (fileDescriptor);
				} catch (InvalidEncodingException iex) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. {1}", fileName, iex.Message), null);
					return null;
				} catch (OverflowException) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. File too large.", fileName), null);
					return null;
				}

				if (controller == null) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), null);
					return null;
				}

				Counters.OpenDocumentTimer.Trace ("Loading file");

				try {
					await controller.Initialize (fileDescriptor, GetStoredMemento (fileName));
					controller.OriginalContentName = fileInfo.OriginalFileName;
					if (fileInfo.Owner != null)
						controller.Owner = fileInfo.Owner;
				} catch (InvalidEncodingException iex) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. {1}", fileName, iex.Message), iex);
					return null;
				} catch (OverflowException) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. File too large.", fileName), null);
					return null;
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), ex);
				return null;
			}

			Counters.OpenDocumentTimer.Trace ("Showing view");

			var doc = await ShowView (fileInfo);

			ScrollToRequestedCaretLocation (doc, fileInfo);

			return doc;
		}

		async Task<Document> ShowView (DocumentOpenInformation documentOpenInfo)
		{
			if (IdeApp.Workbench != null)
				IdeApp.Workbench.EnsureShown ();

			await InitDesktopService ();

			var commandHandler = new ViewCommandHandlers ();

			// If the controller has not been initialized, do it now
			if (!documentOpenInfo.DocumentController.Initialized)
				await documentOpenInfo.DocumentController.Initialize (null, null);

			// Make sure the shell is now initialized
			await ServiceProvider.GetService<IShell> ();

			var window = await workbench.ShowView (documentOpenInfo.DocumentController, documentOpenInfo.DockNotebook, commandHandler);

			var doc = new Document (this, workbench, documentOpenInfo.DocumentController, documentOpenInfo.DocumentControllerDescription);
			await doc.InitializeWindow (window);

			doc.Closing += OnWindowClosing;
			doc.Closed += OnWindowClosed;
			doc.Window.NotebookChanged += Window_NotebookChanged;
			documents = documents.Add (doc);
			WatchDocument (doc);

			OnDocumentOpened (new DocumentEventArgs (doc));

			if (documentOpenInfo.Options.HasFlag (OpenDocumentOptions.BringToFront) || documents.Count == 1)
				doc.Select ();

			// Ensure the active document is up to date
			OnDocumentChanged (null, null);

			commandHandler.Initialize (doc);

			CountFileOpened (documentOpenInfo.DocumentController);

			return doc;
		}

		void CountFileOpened (DocumentController controller)
		{
			string type;
			if (controller is FileDocumentController fileDocumentController) {
				type = System.IO.Path.GetExtension (fileDocumentController.FilePath);
				var mt = !string.IsNullOrEmpty (fileDocumentController.MimeType) ? fileDocumentController.MimeType : desktopService.GetMimeTypeForUri (fileDocumentController.FilePath);
				if (!string.IsNullOrEmpty (mt))
					type += " (" + mt + ")";
			} else
				type = "(not a file)";

			var metadata = new Dictionary<string, object> () {
				{ "FileType", type },
				{ "DisplayBinding", controller.GetType ().FullName },
			};

			metadata ["DisplayBindingAndType"] = type + " | " + controller.GetType ().FullName;

			Counters.DocumentOpened.Inc (metadata);
		}

		Properties GetStoredMemento (FilePath file)
		{
			if (Preferences.LoadDocumentUserProperties) {
				try {
					string directory = UserProfile.Current.CacheDir.Combine ("temp");
					if (!Directory.Exists (directory)) {
						Directory.CreateDirectory (directory);
					}
					string fileName = file.ToString ().Substring (3).Replace ('/', '.').Replace ('\\', '.').Replace (System.IO.Path.DirectorySeparatorChar, '.');
					string fullFileName = directory + System.IO.Path.DirectorySeparatorChar + fileName;
					// check the file name length because it could be more than the maximum length of a file name
					if (FileService.IsValidPath (fullFileName) && File.Exists (fullFileName))
						return Properties.Load (fullFileName) ?? new Properties ();
				} catch (Exception ex) {
					LoggingService.LogError ("Loading of document properties failed", ex);
				}
			}
			return new Properties ();
		}

		OpenDocumentMetadata CreateOpenDocumentTimerMetadata ()
		{
			var metadata = new OpenDocumentMetadata {
				ResultString = "None"
			};

			return metadata;
		}

		void AddOpenDocumentTimerMetadata (OpenDocumentMetadata metadata, FileOpenInformation info, Document document, bool result)
		{
			if (document != null)
				metadata.EditorType = document.DocumentController.GetType ().FullName;
			if (info.Owner != null)
				metadata.OwnerProjectGuid = (info.Owner as SolutionItem)?.ItemId;

			metadata.Extension = info.FileName.Extension;
			metadata.ResultString = result ? "Success" : "Failure";
		}

		void ScrollToRequestedCaretLocation (Document doc, FileOpenInformation info)
		{
			if (info.Line < 1 && info.Offset < 0)
				return;

			if (editorOperationsFactoryService == null)
				editorOperationsFactoryService = CompositionManager.Instance.GetExportedValue<IEditorOperationsFactoryService> ();

			FileSettingsStore.Remove (doc.FileName);
			doc.DisableAutoScroll ();

			doc.RunWhenContentAdded<ITextView> (textView => {
				var ipos = doc.Editor;
				if (ipos != null) {
					var loc = new DocumentLocation (info.Line, info.Column >= 1 ? info.Column : 1);
					if (info.Offset >= 0) {
						loc = ipos.OffsetToLocation (info.Offset);
					}
					if (loc.IsEmpty)
						return;
					ipos.SetCaretLocation (loc, info.Options.HasFlag (OpenDocumentOptions.HighlightCaretLine), info.Options.HasFlag (OpenDocumentOptions.CenterCaretLine));
				} else {
					var offset = info.Offset;
					if (offset < 0) {
						var line = textView.TextSnapshot.GetLineFromLineNumber (info.Line - 1);
						if (info.Column >= 1)
							offset = line.Start + info.Column - 1;
						else
							offset = line.Start;
					}
					if (editorOperationsFactoryService != null) {
						var editorOperations = editorOperationsFactoryService.GetEditorOperations (textView);
						var point = new VirtualSnapshotPoint (textView.TextSnapshot, offset);
						editorOperations.SelectAndMoveCaret (point, point, TextSelectionMode.Stream, EnsureSpanVisibleOptions.AlwaysCenter);
					} else {
						LoggingService.LogError ("Missing editor operations");
					}
				}
			});

/*			var navigator = (ISourceFileNavigator)newContent.GetContent (typeof (ISourceFileNavigator));
			if (fileInfo.Offset >= 0)
				navigator.JumpToOffset (fileInfo.Offset);
			else
				navigator.JumpToLine (fileInfo.Line, fileInfo.Column);*/
		}

		Document FindDocument (IWorkbenchWindow window)
		{
			foreach (Document doc in Documents)
				if (doc.Window == window)
					return doc;
			return null;
		}

		void WatchDocument (Document doc)
		{
			if (doc.IsFile) {
				doc.FileNameChanged += OnContentNameChanged;
				WatchDirectories ();
			}
		}

		void UnwatchDocument (Document doc)
		{
			if (doc.IsFile) {
				doc.FileNameChanged -= OnContentNameChanged;
				WatchDirectories ();
			}
		}

		void OnContentNameChanged (object sender, EventArgs e)
		{
			// Refresh file watcher.
			WatchDirectories ();
		}

		object directoryWatchId = new object ();
		void WatchDirectories ()
		{
			// TOTEST
			HashSet<FilePath> directories = null;
			foreach (Document doc in documents) {
				if (doc.IsFile && !doc.IsNewDocument && doc.FilePath.IsAbsolute && File.Exists (doc.FilePath)) {
					if (directories == null)
						directories = new HashSet<FilePath> ();
					directories.Add (doc.FileName.ParentDirectory);
				}
			}

			FileWatcherService.WatchDirectories (directoryWatchId, directories).Ignore ();
		}

		Task OnWindowClosing (object sender, DocumentCloseEventArgs args)
		{
			return OnDocumentClosing (args);
		}

		void OnWindowClosed (object sender, EventArgs args)
		{
			var doc = (Document)sender;
			if (doc == null)
				return;

			doc.Closing -= OnWindowClosing;
			doc.Closed -= OnWindowClosed;
			doc.Window.NotebookChanged -= Window_NotebookChanged;
			documents = documents.Remove (doc);
			UnwatchDocument (doc);

			OnDocumentClosed (doc);
		}

		void OnDocumentOpened (DocumentEventArgs e)
		{
			DocumentOpened?.SafeInvoke (this, e);
		}

		void OnDocumentClosed (Document doc)
		{
			var e = new DocumentEventArgs (doc);
			DocumentClosed?.SafeInvoke (this, e);
		}

		async Task OnDocumentClosing (DocumentCloseEventArgs args)
		{
			try {
				if (DocumentClosing != null) {
					foreach (var handler in DocumentClosing.GetInvocationList ().Cast<DocumentCloseAsyncEventHandler> ()) {
						await handler (this, args);
						if (args.Cancel)
							break;
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Exception before closing documents", ex);
			}
		}

		void OnDocumentChanged (object s, EventArgs a)
		{
			if (activeDocument == workbench.ActiveWorkbenchWindow?.Document)
				return;

			activeDocument = workbench.ActiveWorkbenchWindow?.Document;

			foreach (var doc in documents)
				doc.UpdateContentVisibility ();

			ActiveDocumentChanged?.SafeInvoke (s, new DocumentEventArgs (activeDocument));

			if (activeDocument != null) {
				activeDocument.LastTimeActive = DateTime.Now;
				activeDocument.GrabFocus ();
			}
		}

		void Workbench_WindowReordered (object sender, WindowReorderedEventArgs e)
		{
			Document doc = documents [e.OldPosition];
			documents = documents.RemoveAt (e.OldPosition).Insert (e.NewPosition, doc);
		}

		void Workbench_NotebookClosed (object sender, NotebookEventArgs e)
		{
		}

		void Window_NotebookChanged (object sender, NotebookChangeEventArgs e)
		{
		}

		bool MatchesEvent (FilePath filePath, FileEventInfo e) => e.IsDirectory && filePath.IsChildPathOf (e.FileName) || !e.IsDirectory && filePath == e.FileName;

		void CheckRemovedFile (object sender, FileEventArgs args)
		{
			foreach (var e in args) {
				if (e.IsDirectory) {
					CheckRemovedDirectory (e.FileName);
				} else {
					foreach (var doc in documents) {
						if (doc.IsFile && !doc.IsNewDocument && doc.FilePath == e.FileName) {
							CloseViewForRemovedFile (doc);
							return;
						}
					}
					CheckRemovedDirectory (e.FileName);
				}
			}
		}

		void CheckRemovedDirectory (FilePath fileName)
		{
			foreach (var doc in documents) {
				if (doc.IsFile && !doc.IsNewDocument && doc.FilePath.IsChildPathOf (fileName)) {
					CloseViewForRemovedFile (doc);
				}
			}
		}

		static void CloseViewForRemovedFile (Document doc)
		{
			if (doc.IsDirty) {
				// TOTEST
				doc.ConvertToUnsavedFile ();
			} else {
				doc.Close ().Ignore ();
			}
		}

		void CheckRenamedFile (object sender, FileCopyEventArgs args)
		{
			foreach (FileEventInfo e in args) {
				if (e.IsDirectory) {
					foreach (var doc in documents) {
						if (doc.IsFile && !doc.IsNewDocument && doc.FilePath.IsChildPathOf (e.SourceFile)) {
							var newFile = e.TargetFile.Combine (doc.FilePath.ToRelative (e.SourceFile)).FullPath;
							doc.RenameFile (newFile);
						}
					}
				} else {
					foreach (var doc in documents) {
						if (doc.IsFile && !doc.IsNewDocument && doc.FilePath == e.SourceFile && File.Exists (e.TargetFile)) {
							doc.RenameFile (e.TargetFile);
							return;
						}
					}
				}
			}
		}
	}
}
