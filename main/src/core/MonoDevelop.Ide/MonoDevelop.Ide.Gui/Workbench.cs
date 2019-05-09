//
// Workbench.cs
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
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Desktop;
using Mono.Addins;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Components.Docking;
using MonoDevelop.Components.DockNotebook;
using System.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using System.Threading.Tasks;
using System.Collections.Immutable;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using MonoDevelop.Ide.Composition;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public sealed class Workbench
	{
		IdeProgressMonitorManager monitors;
		DocumentManager documentManager;
		WorkbenchStatusBar statusBar = new WorkbenchStatusBar ();

		ImmutableList<Document> documents = ImmutableList<Document>.Empty;
		DefaultWorkbench workbench;
		PadCollection pads;
		bool fileEventsFrozen;
		bool hasEverBeenShown = false;

		public event EventHandler<DocumentEventArgs> ActiveDocumentChanged {
			add { documentManager.ActiveDocumentChanged += value; }
			remove { documentManager.ActiveDocumentChanged -= value; }
		}

		public event EventHandler LayoutChanged;
		public event EventHandler GuiLocked;
		public event EventHandler GuiUnlocked;

		internal async Task Initialize (ProgressMonitor monitor)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Initializing Main Window"), 4);
			try {
				monitors = (IdeProgressMonitorManager) await Runtime.GetService<ProgressMonitorManager> ();
				documentManager = await Runtime.GetService<DocumentManager> ();

				await Runtime.GetService<DocumentModelRegistry> ();
				await Runtime.GetService<DocumentControllerService> ();

				Counters.Initialization.Trace ("Creating DefaultWorkbench");
				workbench = (DefaultWorkbench) await Runtime.GetService<IShell> ();
				monitor.Step (1);
				
				((Gtk.Window)workbench).Visible = false;
				workbench.WorkbenchTabsChanged += WorkbenchTabsChanged;
				IdeApp.Workspace.StoringUserPreferences += OnStoringWorkspaceUserPreferences;
				IdeApp.Workspace.LoadingUserPreferences += OnLoadingWorkspaceUserPreferences;
				
				IdeApp.FocusOut += delegate(object o, EventArgs args) {
					if (!fileEventsFrozen) {
						fileEventsFrozen = true;
						FileService.FreezeEvents ();
					}
				};
				IdeApp.FocusIn += delegate(object o, EventArgs args) {
					if (fileEventsFrozen) {
						fileEventsFrozen = false;
						FileService.ThawEvents ();
					}
				};

				pads = null;	// Make sure we get an up to date pad list.
				monitor.Step (1);
			} finally {
				monitor.EndTask ();
			}
		}

		internal void Realize ()
		{
			Counters.Initialization.Trace ("Realizing Root Window");
			RootWindow.Realize ();

			Counters.Initialization.Trace ("Loading memento");
			var memento = IdeApp.Preferences.WorkbenchMemento.Value;
			Counters.Initialization.Trace ("Setting memento");
			workbench.Memento = memento;

			Counters.Initialization.Trace ("Initializing monitors");
		}

		internal void Show ()
		{
			EnsureLayout ();
			Present ();
		}

		void EnsureLayout ()
		{
			if (!hasEverBeenShown) {

				workbench.InitializeWorkspace ();
				workbench.InitializeLayout ();
				statusBar.Attach (workbench.StatusBar);

				workbench.CurrentLayout = "Solution";

				// now we have an layout set notify it
				if (LayoutChanged != null)
					LayoutChanged (this, EventArgs.Empty);

				hasEverBeenShown = true;
			} else if (!RootWindow.Visible) {
				// restore memento if the root window has been hidden before
				var memento = IdeApp.Preferences.WorkbenchMemento.Value;
				workbench.Memento = memento;
			}
		}

		internal void EnsureShown ()
		{
			if (!RootWindow.Visible)
				Show ();
		}

		internal void Hide ()
		{
			if (RootWindow.Visible) {
				IdeApp.Preferences.WorkbenchMemento.Value = (Properties)workbench.Memento;
				// FIXME: On mac the window can not be hidden while in fullscreen, we should unllscreen first and then hide
				if (Platform.IsMac && IdeServices.DesktopService.GetIsFullscreen (RootWindow)) {
					return;
				}
				RootWindow.Hide ();
			}
		}
		
		internal async Task<bool> Close ()
		{
			if (!IdeApp.OnExit ())
				return false;

			IdeApp.Workspace.SavePreferences ();

			bool showDirtyDialog = false;

			foreach (var doc in Documents) {
				if (doc.IsDirty) {
					showDirtyDialog = true;
					break;
				}
			}

			if (showDirtyDialog) {
				using (var dlg = new DirtyFilesDialog ()) {
					dlg.Modal = true;
					if (MessageService.ShowCustomDialog (dlg, workbench) != (int)Gtk.ResponseType.Ok)
						return false;
				}
			}

			if (!await IdeApp.Workspace.Close (false, false))
				return false;

			var tasks = new List<Task> ();
			foreach (var doc in Documents.ToList ())
				tasks.Add (doc.Close (true));
			await Task.WhenAll (tasks);

			workbench.Close ();

			IdeApp.Workspace.SavePreferences ();

			IdeApp.OnExited ();

			IdeApp.CommandService.Dispose ();

			return true;
		}

		internal DocumentManager DocumentManager => documentManager;

		public ImmutableList<Document> Documents {
			get { return documentManager.Documents; }
		}

		/// <summary>
		/// This is a wrapper for use with AutoTest
		/// </summary>
		internal bool DocumentsDirty {
			get { return Documents.Any (d => d.IsDirty); }
		}

		public Document ActiveDocument => documentManager.ActiveDocument;

		public Document GetDocument (FilePath filePath)
		{
			return documentManager.GetDocument (filePath);
		}

		internal TextReader[] GetDocumentReaders (List<string> filenames)
		{
			// TOTEST
			TextReader [] results = new TextReader [filenames.Count];

			int idx = 0;
			foreach (var f in filenames) {
				var doc = documentManager.Documents.Find (d => d.GetContent<ITextBuffer> () != null && FilePath.PathComparer.Equals (f, d.FileName));
				if (doc != null) {
					results [idx] = new Microsoft.VisualStudio.Platform.NewTextSnapshotToTextReader (doc.GetContent<ITextBuffer> ().CurrentSnapshot);
				} else {
					results [idx] = null;
				}

				idx++;
			}

			return results;
		}

		public PadCollection Pads {
			get {
				if (pads == null) {
					pads = new PadCollection ();
					foreach (PadCodon pc in workbench.PadContentCollection)
						WrapPad (pc);
				}
				return pads;
			}
		}

		public Gtk.Window RootWindow {
			get { return workbench; }
		}
		
		/// <summary>
		/// When set to <c>true</c>, opened documents will automatically be reloaded when a change in the underlying
		/// file is detected (unless the document has unsaved changes)
		/// </summary>
		public bool AutoReloadDocuments { get; set; }
		
		/// <summary>
		/// Whether the root window or any undocked part of it has toplevel focus. 
		/// </summary>
		public bool HasToplevelFocus {
			get {
				if (IdeServices.DesktopService.IsModalDialogRunning ())
					return false;
				var windows = Gtk.Window.ListToplevels ();
				var toplevel = windows.FirstOrDefault (x => x.HasToplevelFocus);
				if (toplevel == null)
					return false;
				if (toplevel == RootWindow)
					return true;
				#if WIN32
				var app = System.Windows.Application.Current;
				if (app != null) {
					var wpfWindow = app.Windows.OfType<System.Windows.Window>().SingleOrDefault (x => x.IsActive);
					if (wpfWindow != null)
						return true;
				}
				#endif
				var dock = toplevel as DockFloatingWindow;
				return dock != null && dock.DockParent == RootWindow;
			}
		}
		
		public void Present ()
		{
			EnsureLayout ();
			// Very important: see https://github.com/mono/monodevelop/pull/6064
			// Otherwise the editor may not be focused on IDE startup and can't be
			// focused even by clicking with the mouse.
			RootWindow.Visible = true;

			//HACK: window resets its size on Win32 on Present if it was maximized by snapping to top edge of screen
			//partially work around this by avoiding the present call if it's already toplevel
			if (Platform.IsWindows && RootWindow.HasToplevelFocus)
				return;

			RootWindow.Deiconify ();
			RootWindow.Visible = true;

			//FIXME: this should do a "request for attention" dock bounce on MacOS but only in some cases.
			//Doing it for all Present calls is excessive and annoying. Maybe we have too many Present calls...
			//Mono.TextEditor.GtkWorkarounds.PresentWindowWithNotification (RootWindow);
			RootWindow.Present ();
		}

		public void GrabDesktopFocus ()
		{
			IdeServices.DesktopService.GrabDesktopFocus (RootWindow);
		}
				
		public bool FullScreen {
			get { return workbench.FullScreen; }
			set { workbench.FullScreen = value; }
		}
		
		public string CurrentLayout {
			get { return workbench.CurrentLayout; }
			set {
				if (value != workbench.CurrentLayout) {
					workbench.CurrentLayout = value;
					if (LayoutChanged != null)
						LayoutChanged (this, EventArgs.Empty);
				}
			}
		}

		public IList<string> Layouts {
			get { return workbench.Layouts; }
		}
		
		public IdeProgressMonitorManager ProgressMonitors {
			get { return monitors; }
		}
		
		public StatusBar StatusBar {
			get {
				return statusBar.MainContext;
			}
		}

		public void ShowCommandBar (string barId)
		{
			workbench.Toolbar.ShowCommandBar (barId);
		}
		
		public void HideCommandBar (string barId)
		{
			workbench.Toolbar.HideCommandBar (barId);
		}

		public void ShowInfoBar (bool inActiveView, InfoBarOptions options)
		{
			IInfoBarHost infoBarHost = null;
			if (inActiveView) {
				// Maybe for pads also? Not sure if we should.
				infoBarHost = IdeApp.Workbench.ActiveDocument?.GetContent<IInfoBarHost> (true);
			}

			if (infoBarHost == null)
				infoBarHost = IdeApp.Workbench.RootWindow as IInfoBarHost;

			infoBarHost?.AddInfoBar (options);
		}

		internal MonoDevelop.Components.MainToolbar.MainToolbarController Toolbar {
			get {
				return workbench.Toolbar;
			}
		}

		public Pad GetPad<T> ()
		{
			foreach (Pad pad in Pads) {
				if (typeof(T).FullName == pad.InternalContent.ClassName)
					return pad;
			}
			return null;
		}		
		
		public void DeleteLayout (string name)
		{
			workbench.DeleteLayout (name);
			if (LayoutChanged != null)
				LayoutChanged (this, EventArgs.Empty);
		}
		
		public void LockGui ()
		{
			if (IdeApp.CommandService.LockAll ()) {
				if (GuiLocked != null)
					GuiLocked (this, EventArgs.Empty);
			}
		}
		
		public void UnlockGui ()
		{
			if (IdeApp.CommandService.UnlockAll ()) {
				if (GuiUnlocked != null)
					GuiUnlocked (this, EventArgs.Empty);
			}
		}
		
		public void SaveAll ()
		{
			ITimeTracker tt = Counters.SaveAllTimer.BeginTiming ();
			try {
				// Make a copy of the list, since it may change during save
				Document[] docs = new Document [Documents.Count];
				Documents.CopyTo (docs, 0);

				foreach (Document doc in docs)
					doc.Save ();
			} finally {
				tt.End ();
			}
		}

		internal async Task<bool> SaveAllDirtyFiles ()
		{
			Document[] docs = Documents.Where (doc => doc.IsDirty).ToArray ();
			if (!docs.Any ())
				return true;

			foreach (Document doc in docs) {
				AlertButton result = PromptToSaveChanges (doc);
				if (result == AlertButton.Cancel)
					return false;

				if (result == AlertButton.CloseWithoutSave) {
					await doc.Close (true);
					continue;
				}

				await doc.Save ();
				if (doc.IsDirty) {
					doc.Select ();
					return false;
				}
			}
			return true;
		}

		static AlertButton PromptToSaveChanges (Document doc)
		{
			return MessageService.GenericAlert (MonoDevelop.Ide.Gui.Stock.Warning,
				GettextCatalog.GetString ("Save the changes to document '{0}' before creating a new solution?", doc.Name),
				GettextCatalog.GetString ("If you don't save, all changes will be permanently lost."),
				AlertButton.CloseWithoutSave, AlertButton.Cancel, doc.IsNewDocument ? AlertButton.SaveAs : AlertButton.Save);
		}

		public Task CloseAllDocuments (bool leaveActiveDocumentOpen)
		{
			return documentManager.CloseAllDocuments (leaveActiveDocumentOpen);
		}

		internal Pad ShowPad (PadCodon content)
		{
			workbench.ShowPad (content);
			return WrapPad (content);
		}

		internal Pad AddPad (PadCodon content)
		{
			workbench.AddPad (content);
			return WrapPad (content);
		}

		public Pad AddPad (PadContent padContent, string id, string label, string defaultPlacement, IconId icon)
		{
			return AddPad (new PadCodon (padContent, id, label, defaultPlacement, icon));
		}
		
		public Pad AddPad (PadContent padContent, string id, string label, string defaultPlacement, DockItemStatus defaultStatus, IconId icon)
		{
			return AddPad (new PadCodon (padContent, id, label, defaultPlacement, defaultStatus, icon));
		}
		
		public Pad ShowPad (PadContent padContent, string id, string label, string defaultPlacement, IconId icon)
		{
			return ShowPad (new PadCodon (padContent, id, label, defaultPlacement, icon));
		}
		
		public Pad ShowPad (PadContent padContent, string id, string label, string defaultPlacement, DockItemStatus defaultStatus, IconId icon)
		{
			return ShowPad (new PadCodon (padContent, id, label, defaultPlacement, defaultStatus, icon));
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, bool bringToFront)")]
		public Task<Document> OpenDocument (FilePath fileName, bool bringToFront)
		{
			return OpenDocument (fileName, bringToFront ? OpenDocumentOptions.Default : OpenDocumentOptions.Default & ~OpenDocumentOptions.BringToFront);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Task<Document> OpenDocument (FilePath fileName, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, -1, -1, options, null, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Task<Document> OpenDocument (FilePath fileName, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, -1, -1, options, encoding, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Task<Document> OpenDocument (FilePath fileName, int line, int column, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, line, column, options, null, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, int line, int column, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Task<Document> OpenDocument (FilePath fileName, int line, int column, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, line, column, options, encoding, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options, Encoding encoding, IViewDisplayBinding binding)")]
		internal Task<Document> OpenDocument (FilePath fileName, int line, int column, OpenDocumentOptions options, Encoding encoding, DocumentControllerDescription binding)
		{
			var openFileInfo = new FileOpenInformation (fileName, null) {
				Options = options,
				Line = line,
				Column = column,
				DocumentControllerDescription = binding,
				Encoding = encoding
			};
			return documentManager.OpenDocument (openFileInfo);
		}


		public Task<Document> OpenDocument (FilePath fileName, WorkspaceObject project, bool bringToFront)
		{
			return OpenDocument (fileName, project, bringToFront ? OpenDocumentOptions.Default : OpenDocumentOptions.Default & ~OpenDocumentOptions.BringToFront);
		}

		public Task<Document> OpenDocument (FilePath fileName, WorkspaceObject project, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, -1, -1, options, null, null);
		}

		public Task<Document> OpenDocument (FilePath fileName, WorkspaceObject project, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, -1, -1, options, encoding, null);
		}

		public Task<Document> OpenDocument (FilePath fileName, WorkspaceObject project, int line, int column, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, line, column, options, null, null);
		}

		public Task<Document> OpenDocument (FilePath fileName, WorkspaceObject project, int line, int column, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, line, column, options, encoding, null);
		}

		internal Task<Document> OpenDocument (FilePath fileName, WorkspaceObject project, int line, int column, OpenDocumentOptions options, Encoding encoding, DocumentControllerDescription binding)
		{
			var openFileInfo = new FileOpenInformation (fileName, project) {
				Options = options,
				Line = line,
				Column = column,
				DocumentControllerDescription = binding,
				Encoding = encoding
			};
			return documentManager.OpenDocument (openFileInfo);
		}

		internal Task<Document> OpenDocument (FilePath fileName, WorkspaceObject project, int line, int column, OpenDocumentOptions options, Encoding Encoding, DocumentControllerDescription binding, IShellNotebook dockNotebook)
		{
			var openFileInfo = new FileOpenInformation (fileName, project) {
				Options = options,
				Line = line,
				Column = column,
				DocumentControllerDescription = binding,
				Encoding = Encoding,
				DockNotebook = dockNotebook
			};

			return documentManager.OpenDocument (openFileInfo);
		}

		public Task<Document> OpenDocument (FileOpenInformation info)
		{
			return documentManager.OpenDocument (info);
		}

		public Task<Document> OpenDocument (DocumentController content, bool bringToFront = true)
		{
			return documentManager.OpenDocument (content, bringToFront);
		}

		public Task<Document> OpenDocument (ModelDescriptor modelDescriptor, DocumentControllerRole? role = null, bool bringToFront = true)
		{
			return documentManager.OpenDocument (modelDescriptor, role, bringToFront);
		}

		public void ToggleMaximize ()
		{
			workbench.ToggleFullViewMode ();
		}

		public Task<Document> NewDocument (string defaultName, string mimeType, string content)
		{
			return documentManager.NewDocument (defaultName, mimeType, content);
		}
		
		public Task<Document> NewDocument (string defaultName, string mimeType, Stream content)
		{
			return documentManager.NewDocument (defaultName, mimeType, content);
		}

		public void ShowGlobalPreferencesDialog (Gtk.Window parentWindow)
		{
			ShowGlobalPreferencesDialog (parentWindow, null);
		}
		
		static Properties properties = ((Properties) PropertyService.Get (
			"MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties",
			new Properties()));

		public void ShowGlobalPreferencesDialog (Window parentWindow, string panelId, Action<OptionsDialog> configurationAction = null)
		{
			if (parentWindow == null && IdeApp.Workbench.RootWindow.Visible)
				parentWindow = IdeApp.Workbench.RootWindow;

			OptionsDialog ops = new OptionsDialog (
				parentWindow,
				properties,
				"/MonoDevelop/Ide/GlobalOptionsDialog");

			ops.Title = Platform.IsWindows
				? GettextCatalog.GetString ("Options")
				: GettextCatalog.GetString ("Preferences");

			try {
				if (panelId != null)
					ops.SelectPanel (panelId);
				if (configurationAction != null)
					configurationAction (ops);
				if (MessageService.RunCustomDialog (ops, parentWindow) == (int) Gtk.ResponseType.Ok) {
					PropertyService.SaveProperties ();
					MonoDevelop.Projects.Policies.PolicyService.SavePolicies ();
				}
			} finally {
				ops.Destroy ();
				ops.Dispose ();
			}
		}
		
		public void ShowDefaultPoliciesDialog (Window parentWindow)
		{
			ShowDefaultPoliciesDialog (parentWindow, null);
		}
		
		public void ShowDefaultPoliciesDialog (Window parentWindow, string panelId)
		{
			if (parentWindow == null)
				parentWindow = IdeServices.DesktopService.GetFocusedTopLevelWindow ();

			var ops = new DefaultPolicyOptionsDialog (parentWindow);

			try {
				if (panelId != null)
					ops.SelectPanel (panelId);
				
				MessageService.RunCustomDialog (ops, parentWindow);
			} finally {
				ops.Destroy ();
				ops.Dispose ();
			}
		}
		
		public StringTagModelDescription GetStringTagModelDescription ()
		{
			StringTagModelDescription model = new StringTagModelDescription ();
			model.Add (typeof (Project));
			model.Add (typeof (Solution));
			model.Add (typeof (DotNetProjectConfiguration));
			model.Add (typeof (Workbench));
			return model;
		}
		
		public StringTagModel GetStringTagModel ()
		{
			StringTagModel source = new StringTagModel ();
			source.Add (this);
			if (IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null)
				source.Add (IdeApp.ProjectOperations.CurrentSelectedSolutionItem.GetStringTagModel (IdeApp.Workspace.ActiveConfiguration));
			else if (IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem != null)
				source.Add (IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.GetStringTagModel ());
			return source;
		}
		
		internal void ShowNext ()
		{
			// Shows the next item in a pad that implements ILocationListPad.
			
			if (activeLocationList != null) {
				NavigationPoint next = activeLocationList.GetNextLocation ();
				if (next != null)
					next.ShowDocument ();
			}
		}
		
		internal void ShowPrevious ()
		{
			// Shows the previous item in a pad that implements ILocationListPad.
			
			if (activeLocationList != null) {
				NavigationPoint next = activeLocationList.GetPreviousLocation ();
				if (next != null)
					next.ShowDocument ();
			}
		}
		
		ILocationList activeLocationList;
		
		public ILocationList ActiveLocationList {
			get {
				return activeLocationList;
			}
			set {
				activeLocationList = value;
			}
		}
		
		Pad WrapPad (PadCodon padContent)
		{
			if (pads == null) {
				foreach (Pad p in Pads) {
					if (p.InternalContent == padContent)
						return p;
				}
			}
			Pad pad = new Pad (workbench, padContent);
			Pads.Add (pad);
			pad.Window.PadDestroyed += delegate {
				Pads.Remove (pad);
			};
			return pad;
		}
		
		void OnStoringWorkspaceUserPreferences (object s, UserPreferencesEventArgs args)
		{
			WorkbenchUserPrefs prefs = new WorkbenchUserPrefs ();
			var nbId = 0;
			var fwId = 1;

			foreach (var window in DockWindow.GetAllWindows ()) {
				int x, y;
				window.GetPosition (out x, out y);
				var fwp = new FloatingWindowUserPrefs {
					WindowId = fwId,
					X = x,
					Y = y,
					Width = window.Allocation.Width,
					Height = window.Allocation.Height
				};

				foreach (var nb in window.Container.GetNotebooks ())
					AddNotebookDocuments (args, fwp.Files, nb, nbId++);

				if (fwp.Files.Count > 0) {
					prefs.FloatingWindows.Add (fwp);
					fwId++;
				}
			}

			var mainContainer = workbench.TabControl.Container;

			foreach (var nb in mainContainer.GetNotebooks ())
				AddNotebookDocuments (args, prefs.Files, nb, nbId++);

			foreach (Pad pad in Pads) {
				IMementoCapable mc = pad.GetMementoCapable ();
				if (mc != null) {
					ICustomXmlSerializer mem = mc.Memento;
					if (mem != null) {
						PadUserPrefs data = new PadUserPrefs ();
						data.Id = pad.Id;
						StringWriter w = new StringWriter ();
						XmlTextWriter tw = new XmlTextWriter (w);
						mem.WriteTo (tw);
						XmlDocument doc = new XmlDocument ();
						doc.LoadXml (w.ToString ());
						data.State = doc.DocumentElement;
						prefs.Pads.Add (data);
					}
				}
			}
			
			if (ActiveDocument != null)
				prefs.ActiveDocument = FileService.AbsoluteToRelativePath (args.Item.BaseDirectory, ActiveDocument.FileName);
			
			args.Properties.SetValue ("MonoDevelop.Ide.Workbench", prefs);
		}

		static void AddNotebookDocuments (UserPreferencesEventArgs args, List<DocumentUserPrefs> files, DockNotebook notebook, int notebookId)
		{
			foreach (var tab in notebook.Tabs) {
				var sdiwindow = (SdiWorkspaceWindow)tab.Content;
				var document = sdiwindow.Document;
				if (!String.IsNullOrEmpty (document.FileName)) {
					var dp = CreateDocumentPrefs (args, document);
					dp.NotebookId = notebookId;
					files.Add (dp);
				}
			}
		}

		static DocumentUserPrefs CreateDocumentPrefs (UserPreferencesEventArgs args, Document document)
		{
			string path = (string)document.OriginalFileName ?? document.FileName;

			var dp = new DocumentUserPrefs ();
			dp.FileName = FileService.AbsoluteToRelativePath (args.Item.BaseDirectory, path);
			if (document.GetContent<ITextView> () is ITextView view) {
				var pos = view.Caret.Position.BufferPosition;
				var line = pos.Snapshot.GetLineFromPosition (pos.Position);
				dp.Line = line.LineNumber + 1;
				dp.Column = pos.Position - line.Start + 1;
			}
			return dp;
		}

		async Task OnLoadingWorkspaceUserPreferences (object s, UserPreferencesEventArgs args)
		{
			WorkbenchUserPrefs prefs = args.Properties.GetValue<WorkbenchUserPrefs> ("MonoDevelop.Ide.Workbench");
			if (prefs == null)
				return;

			try {
				IdeApp.Workbench.LockActiveWindowChangeEvent ();
				IdeServices.NavigationHistoryService.LogActiveDocument ();

				var docViews = new List<Tuple<Document, string>> ();
				FilePath baseDir = args.Item.BaseDirectory;
				var floatingWindows = new List<DockWindow> ();

				using (ProgressMonitor pm = ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Loading workspace documents"), Stock.StatusSolutionOperation, true)) {

					var docList = prefs.Files.Distinct (new DocumentUserPrefsFilenameComparer ()).OrderBy (d => d.NotebookId).ToList ();
					await OpenDocumentsInContainer (pm, baseDir, docViews, docList, workbench.TabControl.Container);

					foreach (var fw in prefs.FloatingWindows) {
						var dockWindow = new DockWindow ();
						dockWindow.Move (fw.X, fw.Y);
						dockWindow.Resize (fw.Width, fw.Height);
						docList = fw.Files.Distinct (new DocumentUserPrefsFilenameComparer ()).OrderBy (d => d.NotebookId).ToList ();
						await OpenDocumentsInContainer (pm, baseDir, docViews, docList, dockWindow.Container);
						floatingWindows.Add (dockWindow);
					}

					// Note: At this point, the progress monitor will be disposed which causes the gtk main-loop to be pumped.
					// This is EXTREMELY important, because without this main-loop pumping action, the next foreach() loop will
					// not cause the Solution tree-view to properly expand, nor will the ActiveDocument be set properly.
				}

				string currentFileName = prefs.ActiveDocument != null ? baseDir.Combine (prefs.ActiveDocument).FullPath : null;

				Document activeDoc = null;
				foreach (var t in docViews) {
					if (t.Item2 == currentFileName)
						activeDoc = t.Item1;
				}

				if (activeDoc == null && docViews.Count > 0)
					activeDoc = docViews [0].Item1;

				foreach (PadUserPrefs pi in prefs.Pads) {
					foreach (Pad pad in IdeApp.Workbench.Pads) {

						if (pi.Id == pad.Id) {
							pad.InternalContent.SetPreferences(pi);
							break;
						}
					}
				}

				foreach (var w in floatingWindows)
					w.ShowAll ();

				if (activeDoc != null) {
					activeDoc.RunWhenLoaded (activeDoc.Select);
				}

			} finally {
				IdeApp.Workbench.UnlockActiveWindowChangeEvent ();
			}
		}

		async Task<bool> OpenDocumentsInContainer (ProgressMonitor pm, FilePath baseDir, List<Tuple<Document, string>> docViews, List<DocumentUserPrefs> list, DockNotebookContainer container)
		{
			int currentNotebook = -1;
			DockNotebook nb = container.GetFirstNotebook ();

			foreach (var doc in list) {
				string fileName = baseDir.Combine (doc.FileName).FullPath;
				if (GetDocument(fileName) == null && File.Exists (fileName)) {
					if (doc.NotebookId != currentNotebook) {
						if (currentNotebook != -1 || nb == null)
							nb = container.InsertRight (null);
						currentNotebook = doc.NotebookId;
					}
					// TODO: Get the correct project.
					var document = await documentManager.BatchOpenDocument (pm, fileName, null, doc.Line, doc.Column, nb);

					if (document != null) {
						var t = new Tuple<Document,string> (document, fileName);
						docViews.Add (t);
					}
				}
			}
			return true;
		}

		internal Pad FindPad (PadContent padContent)
		{
			foreach (Pad pad in Pads)
				if (pad.Content == padContent)
					return pad;
			return null;
		}

		internal void ReorderTab (int oldPlacement, int newPlacement)
		{
			workbench.ReorderTab (oldPlacement, newPlacement);
		}

		internal void LockActiveWindowChangeEvent ()
		{
			workbench.LockActiveWindowChangeEvent ();
		}

		internal void UnlockActiveWindowChangeEvent ()
		{
			workbench.UnlockActiveWindowChangeEvent ();
		}

		public event EventHandler<DocumentEventArgs> DocumentOpened {
			add { documentManager.DocumentOpened += value; }
			remove { documentManager.DocumentOpened -= value; }
		}

		public event EventHandler<DocumentEventArgs> DocumentClosed {
			add { documentManager.DocumentClosed += value; }
			remove { documentManager.DocumentClosed -= value; }
		}

		public event DocumentCloseAsyncEventHandler DocumentClosing {
			add { documentManager.DocumentClosing += value; }
			remove { documentManager.DocumentClosing -= value; }
		}

		public void ReparseOpenDocuments ()
		{
			foreach (var doc in Documents) {
				if (doc.DocumentContext.ParsedDocument != null)
					doc.DocumentContext.ReparseDocument ();
			}
		}

		System.Timers.Timer tabsChangedTimer = null;

		void DisposeTimerAndSave (object o, EventArgs e)
		{
			Runtime.RunInMainThread (() => {
				tabsChangedTimer.Stop ();
				tabsChangedTimer.Elapsed -= DisposeTimerAndSave;
				tabsChangedTimer.Dispose ();
				tabsChangedTimer = null;

				IdeApp.Workspace.SavePreferences ();
			});
		}

		void WorkbenchTabsChanged (object sender, EventArgs ev)
		{
			if (tabsChangedTimer != null) {
				// Timer already started, and we want to allow it to complete
				// so it can't be interrupted by triggering WorkbenchTabsChanged
				// every few seconds.
				return;
			}

			tabsChangedTimer = new System.Timers.Timer (10000);
			tabsChangedTimer.AutoReset = false;
			tabsChangedTimer.Elapsed += DisposeTimerAndSave;
			tabsChangedTimer.Start ();
		}
	}
	
	public class FileSaveInformation
	{
		FilePath fileName;
		public FilePath FileName {
			get {
				return fileName;
			}
			set {
				fileName = value.CanonicalPath;
				if (fileName.IsNullOrEmpty)
					LoggingService.LogError ("FileName == null\n" + Environment.StackTrace);
			}
		}
		
		public Encoding Encoding { get; set; }
		
		public FileSaveInformation (FilePath fileName, Encoding encoding = null)
		{
			this.FileName = fileName;
			this.Encoding = encoding;
		}
	}

	[Flags]
	public enum OpenDocumentOptions
	{
		None = 0,
		BringToFront = 1,
		CenterCaretLine = 1 << 1,
		HighlightCaretLine = 1 << 2,
		OnlyInternalViewer = 1 << 3,
		OnlyExternalViewer = 1 << 4,
		TryToReuseViewer = 1 << 5,
		
		Default = BringToFront | CenterCaretLine | HighlightCaretLine | TryToReuseViewer,
		Debugger = BringToFront | CenterCaretLine | TryToReuseViewer,
		DefaultInternal = Default | OnlyInternalViewer,
	}

	class OpenDocumentMetadata : CounterMetadata
	{
		public OpenDocumentMetadata ()
		{
		}

		// CounterMetadata already has a Result property which isn't a string
		// but we can overwrite the property directly in the dictionary
		public string ResultString {
			get => (string)Properties["Result"];
			set => Properties["Result"] = value;
		}

		public string EditorType {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public string OwnerProjectGuid {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public string Extension {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}
	}
}
