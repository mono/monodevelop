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

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public sealed class Workbench
	{
		readonly ProgressMonitorManager monitors = new ProgressMonitorManager ();
		readonly List<Document> documents = new List<Document> ();
		readonly List<Split> splits = new List<Split> ();
		DefaultWorkbench workbench;
		PadCollection pads;

		public event EventHandler ActiveDocumentChanged;
		public event EventHandler LayoutChanged;
		public event EventHandler GuiLocked;
		public event EventHandler GuiUnlocked;
		
		internal void Initialize (IProgressMonitor monitor)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Initializing Main Window"), 4);
			try {
				Counters.Initialization.Trace ("Creating DefaultWorkbench");
				workbench = new DefaultWorkbench ();
				monitor.Step (1);
				
				Counters.Initialization.Trace ("Initializing Workspace");
				workbench.InitializeWorkspace();
				monitor.Step (1);
				
				Counters.Initialization.Trace ("Initializing Layout");
				workbench.InitializeLayout ();
				monitor.Step (1);
				
				((Gtk.Window)workbench).Visible = false;
				workbench.ActiveWorkbenchWindowChanged += OnDocumentChanged;
				IdeApp.Workspace.StoringUserPreferences += OnStoringWorkspaceUserPreferences;
				IdeApp.Workspace.LoadingUserPreferences += OnLoadingWorkspaceUserPreferences;
				
				IdeApp.FocusOut += delegate(object o, EventArgs args) {
					SaveFileStatus ();
				};
				IdeApp.FocusIn += delegate(object o, EventArgs args) {
					CheckFileStatus ();
				};

				IdeApp.ProjectOperations.StartBuild += delegate {
					SaveFileStatus ();
				};

				IdeApp.ProjectOperations.EndBuild += delegate {
					// The file status checks outputs as well.
					CheckFileStatus ();
				};

				pads = null;	// Make sure we get an up to date pad list.
				monitor.Step (1);
			} finally {
				monitor.EndTask ();
			}
		}
		
		internal void Show (string workbenchMemento)
		{
			Counters.Initialization.Trace ("Realizing Root Window");
			RootWindow.Realize ();
			Counters.Initialization.Trace ("Loading memento");
			var memento = PropertyService.Get (workbenchMemento, new Properties ());
			Counters.Initialization.Trace ("Setting memento");
			workbench.Memento = memento;
			Counters.Initialization.Trace ("Making Visible");
			RootWindow.Visible = true;
			workbench.CurrentLayout = "Solution";
			
			// now we have an layout set notify it
			Counters.Initialization.Trace ("Setting layout");
			if (LayoutChanged != null)
				LayoutChanged (this, EventArgs.Empty);
			
			Counters.Initialization.Trace ("Initializing monitors");
			monitors.Initialize ();
			
			Present ();
		}
		
		internal bool Close ()
		{
			return workbench.Close();
		}
		
		public ReadOnlyCollection<Document> Documents {
			get { return documents.AsReadOnly (); }
		}

		public Document ActiveDocument {
			get {
				if (workbench.ActiveWorkbenchWindow == null)
					return null;
				return WrapDocument (workbench.ActiveWorkbenchWindow); 
			}
		}
		
		public Document GetDocument (string name)
		{
			var fullPath = (FilePath) FileService.GetFullPath (name);

			foreach (Document doc in documents) {
				var fullDocPath = (FilePath) FileService.GetFullPath (doc.Name);

				if (fullDocPath == fullPath)
					return doc;
			}
			return null;
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

		public WorkbenchWindow RootWindow {
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
				if (DesktopService.IsModalDialogRunning ())
					return false;
				var windows = Gtk.Window.ListToplevels ();
				var toplevel = windows.FirstOrDefault (x => x.HasToplevelFocus);
				if (toplevel == null)
					return false;
				if (toplevel == RootWindow)
					return true;
				var dock = toplevel as DockFloatingWindow;
				return dock != null && dock.DockParent == RootWindow;
			}
		}
		
		public void Present ()
		{
			//HACK: window resets its size on Win32 on Present if it was maximized by snapping to top edge of screen
			//partially work around this by avoiding the present call if it's already toplevel
			if (Platform.IsWindows && RootWindow.HasToplevelFocus)
				return;
			
			//FIXME: this should do a "request for attention" dock bounce on MacOS but only in some cases.
			//Doing it for all Present calls is excessive and annoying. Maybe we have too many Present calls...
			//Mono.TextEditor.GtkWorkarounds.PresentWindowWithNotification (RootWindow);
			RootWindow.Present ();
		}

		public void GrabDesktopFocus ()
		{
			DesktopService.GrabDesktopFocus (RootWindow);
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
		
		public ProgressMonitorManager ProgressMonitors {
			get { return monitors; }
		}
		
		public StatusBar StatusBar {
			get {
				return workbench.StatusBar.MainContext;
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

		internal MonoDevelop.Components.MainToolbar.MainToolbar Toolbar {
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
			// Make a copy of the list, since it may change during save
			Document[] docs = new Document [Documents.Count];
			Documents.CopyTo (docs, 0);
			
			foreach (Document doc in docs)
				doc.Save ();
		}
		
		public void CloseAllDocuments (bool leaveActiveDocumentOpen)
		{
			Document[] docs = new Document [Documents.Count];
			Documents.CopyTo (docs, 0);
			
			// The active document is the last one to close.
			// It avoids firing too many ActiveDocumentChanged events.
			
			foreach (Document doc in docs) {
				if (doc != ActiveDocument)
					doc.Close ();
			}
			if (!leaveActiveDocumentOpen && ActiveDocument != null)
				ActiveDocument.Close ();
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

		public Pad AddPad (IPadContent padContent, string id, string label, string defaultPlacement, IconId icon)
		{
			return AddPad (new PadCodon (padContent, id, label, defaultPlacement, icon));
		}
		
		public Pad AddPad (IPadContent padContent, string id, string label, string defaultPlacement, DockItemStatus defaultStatus, IconId icon)
		{
			return AddPad (new PadCodon (padContent, id, label, defaultPlacement, defaultStatus, icon));
		}
		
		public Pad ShowPad (IPadContent padContent, string id, string label, string defaultPlacement, IconId icon)
		{
			return ShowPad (new PadCodon (padContent, id, label, defaultPlacement, icon));
		}
		
		public Pad ShowPad (IPadContent padContent, string id, string label, string defaultPlacement, DockItemStatus defaultStatus, IconId icon)
		{
			return ShowPad (new PadCodon (padContent, id, label, defaultPlacement, defaultStatus, icon));
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, bool bringToFront)")]
		public Document OpenDocument (FilePath fileName, bool bringToFront)
		{
			return OpenDocument (fileName, bringToFront ? OpenDocumentOptions.Default : OpenDocumentOptions.Default & ~OpenDocumentOptions.BringToFront);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Document OpenDocument (FilePath fileName, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, -1, -1, options, null, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Document OpenDocument (FilePath fileName, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, -1, -1, options, encoding, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Document OpenDocument (FilePath fileName, int line, int column, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, line, column, options, null, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, int line, int column, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)")]
		public Document OpenDocument (FilePath fileName, int line, int column, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, line, column, options, encoding, null);
		}

		[Obsolete("Use OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options, Encoding encoding, IViewDisplayBinding binding)")]
		internal Document OpenDocument (FilePath fileName, int line, int column, OpenDocumentOptions options, Encoding encoding, IViewDisplayBinding binding)
		{
			var openFileInfo = new FileOpenInformation (fileName, null) {
				Options = options,
				Line = line,
				Column = column,
				DisplayBinding = binding,
				Encoding = encoding
			};
			return OpenDocument (openFileInfo);
		}


		public Document OpenDocument (FilePath fileName, Project project, bool bringToFront)
		{
			return OpenDocument (fileName, project, bringToFront ? OpenDocumentOptions.Default : OpenDocumentOptions.Default & ~OpenDocumentOptions.BringToFront);
		}

		public Document OpenDocument (FilePath fileName, Project project, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, -1, -1, options, null, null);
		}

		public Document OpenDocument (FilePath fileName, Project project, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, -1, -1, options, encoding, null);
		}

		public Document OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, line, column, options, null, null);
		}

		public Document OpenDocument (FilePath fileName, Project project, int line, int column, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, line, column, options, encoding, null);
		}

		internal Document OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options, Encoding encoding, IViewDisplayBinding binding)
		{
			var openFileInfo = new FileOpenInformation (fileName, project) {
				Options = options,
				Line = line,
				Column = column,
				DisplayBinding = binding,
				Encoding = encoding
			};
			return OpenDocument (openFileInfo);
		}

		internal Document OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options, Encoding Encoding, IViewDisplayBinding binding, DockNotebook dockNotebook)
		{
			var openFileInfo = new FileOpenInformation (fileName, project) {
				Options = options,
				Line = line,
				Column = column,
				DisplayBinding = binding,
				Encoding = Encoding,
				DockNotebook = dockNotebook
			};

			return OpenDocument (openFileInfo);
		}


		public Document OpenDocument (FileOpenInformation info)
		{
			if (string.IsNullOrEmpty (info.FileName))
				return null;
			// Ensure that paths like /a/./a.cs are equalized 
			using (Counters.OpenDocumentTimer.BeginTiming ("Opening file " + info.FileName)) {
				NavigationHistoryService.LogActiveDocument ();
				if (info.Options.HasFlag (OpenDocumentOptions.TryToReuseViewer)) {
					Counters.OpenDocumentTimer.Trace ("Look for open document");
					foreach (Document doc in Documents) {
						IBaseViewContent vcFound = null;
						int vcIndex = 0;

						//search all ViewContents to see if they can "re-use" this filename
						if (doc.Window.ViewContent.CanReuseView (info.FileName))
							vcFound = doc.Window.ViewContent;
						
						//old method as fallback
						if ((vcFound == null) && (doc.FileName.CanonicalPath == info.FileName)) // info.FileName is already Canonical
							vcFound = doc.Window.ViewContent;
						//if found, select window and jump to line
						if (vcFound != null) {
							if (info.Project != null && doc.Project != info.Project) {
								doc.SetProject (info.Project); 
							}

							IEditableTextBuffer ipos = (IEditableTextBuffer) vcFound.GetContent (typeof(IEditableTextBuffer));
							if (info.Line >= 1 && ipos != null) {
								doc.DisableAutoScroll ();
								doc.RunWhenLoaded (() =>
									ipos.SetCaretTo (
										info.Line,
										info.Column >= 1 ? info.Column : 1,
										info.Options.HasFlag (OpenDocumentOptions.HighlightCaretLine),
										info.Options.HasFlag (OpenDocumentOptions.CenterCaretLine)
									)
								);
							}
							
							if (info.Options.HasFlag (OpenDocumentOptions.BringToFront)) {
								doc.Select ();
								doc.Window.SwitchView (vcIndex);
								doc.Window.SelectWindow ();
								NavigationHistoryService.LogActiveDocument ();
							}
							return doc;
						}
					}
				}
				Counters.OpenDocumentTimer.Trace ("Initializing monitor");
				IProgressMonitor pm = ProgressMonitors.GetStatusProgressMonitor (
					GettextCatalog.GetString ("Opening {0}", info.Project != null ?
						info.FileName.ToRelative (info.Project.ParentSolution.BaseDirectory) :
						info.FileName),
					Stock.StatusWorking,
					true
				);

				RealOpenFile (pm, info);
				pm.Dispose ();
				
				if (info.NewContent != null) {
					Counters.OpenDocumentTimer.Trace ("Wrapping document");
					Document doc = WrapDocument (info.NewContent.WorkbenchWindow);
					if (doc != null && info.Options.HasFlag (OpenDocumentOptions.BringToFront)) {
						doc.RunWhenLoaded (() => {
							if (doc.Window != null)
								doc.Window.SelectWindow ();
						});
					}
					return doc;
				}
				return null;
			}
		}

		IViewContent BatchOpenDocument (IProgressMonitor monitor, FilePath fileName, Project project, int line, int column, DockNotebook dockNotebook)
		{
			if (string.IsNullOrEmpty (fileName))
				return null;
			
			using (Counters.OpenDocumentTimer.BeginTiming ("Batch opening file " + fileName)) {
				var openFileInfo = new FileOpenInformation (fileName, project) {
					Options = OpenDocumentOptions.OnlyInternalViewer,
					Line = line,
					Column = column,
					DockNotebook = dockNotebook
				};
				
				RealOpenFile (monitor, openFileInfo);
				
				return openFileInfo.NewContent;
			}
		}
		
		public Document OpenDocument (IViewContent content, bool bringToFront)
		{
			workbench.ShowView (content, bringToFront);
			if (bringToFront)
				Present ();
			return WrapDocument (content.WorkbenchWindow);
		}
		
		public void ToggleMaximize ()
		{
			workbench.ToggleFullViewMode ();
		}

		public Document NewDocument (string defaultName, string mimeType, string content)
		{
			MemoryStream ms = new MemoryStream ();
			byte[] data = System.Text.Encoding.UTF8.GetBytes (content);
			ms.Write (data, 0, data.Length);
			ms.Position = 0;
			return NewDocument (defaultName, mimeType, ms);
		}
		
		public Document NewDocument (string defaultName, string mimeType, Stream content)
		{
			IViewDisplayBinding binding = DisplayBindingService.GetDefaultViewBinding (null, mimeType, null);
			if (binding == null)
				throw new ApplicationException("Can't create display binding for mime type: " + mimeType);				
			
			IViewContent newContent = binding.CreateContent (null, mimeType, null);
			using (content) {
				newContent.LoadNew (content, mimeType);
			}
				
			if (newContent == null)
				throw new ApplicationException(String.Format("Created view content was null{3}DefaultName:{0}{3}MimeType:{1}{3}Content:{2}",
					defaultName, mimeType, content, Environment.NewLine));
			
			newContent.UntitledName = defaultName;
			newContent.IsDirty = true;
			workbench.ShowView (newContent, true);
			DisplayBindingService.AttachSubWindows (newContent.WorkbenchWindow, binding);
			
			var document = WrapDocument (newContent.WorkbenchWindow);
			document.StartReparseThread ();
			return document;
		}

		public void ShowGlobalPreferencesDialog (Gtk.Window parentWindow)
		{
			ShowGlobalPreferencesDialog (parentWindow, null);
		}
		
		static Properties properties = ((Properties) PropertyService.Get (
			"MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties",
			new Properties()));

		public void ShowGlobalPreferencesDialog (Gtk.Window parentWindow, string panelId, Action<OptionsDialog> configurationAction = null)
		{
			if (parentWindow == null)
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
			}
		}
		
		public void ShowDefaultPoliciesDialog (Gtk.Window parentWindow)
		{
			ShowDefaultPoliciesDialog (parentWindow, null);
		}
		
		public void ShowDefaultPoliciesDialog (Gtk.Window parentWindow, string panelId)
		{
			if (parentWindow == null)
				parentWindow = IdeApp.Workbench.RootWindow;

			var ops = new DefaultPolicyOptionsDialog (parentWindow);

			try {
				if (panelId != null)
					ops.SelectPanel (panelId);
				
				MessageService.RunCustomDialog (ops, parentWindow);
			} finally {
				ops.Destroy ();
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
		
		void OnDocumentChanged (object s, EventArgs a)
		{
			if (ActiveDocumentChanged != null)
				ActiveDocumentChanged (s, a);
			if (ActiveDocument != null)
				ActiveDocument.LastTimeActive = DateTime.Now;
		}
		
		internal Document WrapDocument (IWorkbenchWindow window)
		{
			if (window == null) return null;
			Document doc = FindDocument (window);
			if (doc != null) return doc;
			doc = new Document (window);
			window.Closing += OnWindowClosing;
			window.Closed += OnWindowClosed;
			documents.Add (doc);
			
			doc.OnDocumentAttached ();
			OnDocumentOpened (new DocumentEventArgs (doc));
			
			return doc;
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
		
		void OnWindowClosing (object sender, WorkbenchWindowEventArgs args)
		{
			IWorkbenchWindow window = (IWorkbenchWindow) sender;
			if (!args.Forced && window.ViewContent != null && window.ViewContent.IsDirty) {
				AlertButton result = MessageService.GenericAlert (Stock.Warning,
					GettextCatalog.GetString ("Save the changes to document '{0}' before closing?",
						window.ViewContent.IsUntitled
							? window.ViewContent.UntitledName
							: System.IO.Path.GetFileName (window.ViewContent.ContentName)), 
				    GettextCatalog.GetString ("If you don't save, all changes will be permanently lost."),
				    AlertButton.CloseWithoutSave, AlertButton.Cancel, window.ViewContent.IsUntitled ? AlertButton.SaveAs : AlertButton.Save);
				if (result == AlertButton.Save || result == AlertButton.SaveAs) {
					if (window.ViewContent.ContentName == null) {
						FindDocument (window).Save ();
						args.Cancel = window.ViewContent.IsDirty;
					} else {
						try {
							if (window.ViewContent.IsFile)
								window.ViewContent.Save (window.ViewContent.ContentName);
							else
								window.ViewContent.Save ();
							args.Cancel |= window.ViewContent.IsDirty;

						}
						catch (Exception ex) {
							args.Cancel = true;
							MessageService.ShowException (ex, GettextCatalog.GetString ("The document could not be saved."));
						}
					}
					if (args.Cancel)
						FindDocument (window).Select ();
				} else {
					args.Cancel |= result != AlertButton.CloseWithoutSave;
					if (!args.Cancel)
						window.ViewContent.DiscardChanges ();
				}
			}
			OnDocumentClosing (FindDocument (window));
		}
		
		void OnWindowClosed (object sender, WorkbenchWindowEventArgs args)
		{
			IWorkbenchWindow window = (IWorkbenchWindow) sender;
			var doc = FindDocument (window);
			window.Closing -= OnWindowClosing;
			window.Closed -= OnWindowClosed;
			documents.Remove (doc); 
			OnDocumentClosed (doc);
			doc.DisposeDocument ();
		}
		
		// When looking for the project to which the file belongs, look first
		// in the active project, then the active solution, and so on
		static Project GetProjectContainingFile (FilePath fileName)
		{
			Project project = null;
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
				if (IdeApp.ProjectOperations.CurrentSelectedProject.Files.GetFile (fileName) != null)
					project = IdeApp.ProjectOperations.CurrentSelectedProject;
				else if (IdeApp.ProjectOperations.CurrentSelectedProject.FileName == fileName)
					project = IdeApp.ProjectOperations.CurrentSelectedProject;
			}
			if (project == null && IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem != null) {
				project = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.GetProjectContainingFile (fileName);
				if (project == null) {
					WorkspaceItem it = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.ParentWorkspace;
					while (it != null && project == null) {
						project = it.GetProjectContainingFile (fileName);
						it = it.ParentWorkspace;
					}
				}
			}
			if (project == null) {
				project = IdeApp.Workspace.GetProjectContainingFile (fileName);
			}
			return project;
		}
		
		void RealOpenFile (IProgressMonitor monitor, FileOpenInformation openFileInfo)
		{
			FilePath fileName;
			
			Counters.OpenDocumentTimer.Trace ("Checking file");
			
			string origName = openFileInfo.FileName;
			
			if (origName == null) {
				monitor.ReportError (GettextCatalog.GetString ("Invalid file name"), null);
				return;
			}
			
			if (origName.StartsWith ("file://", StringComparison.Ordinal))
				fileName = new Uri (origName).LocalPath;
			else
				fileName = origName;
			
			if (!origName.StartsWith ("http://", StringComparison.Ordinal))
				fileName = fileName.FullPath;
			
			//Debug.Assert(FileService.IsValidPath(fileName));
			if (FileService.IsDirectory (fileName)) {
				monitor.ReportError (GettextCatalog.GetString ("{0} is a directory", fileName), null);
				return;
			}
			
			// test, if file fileName exists
			if (!origName.StartsWith ("http://", StringComparison.Ordinal)) {
				// test, if an untitled file should be opened
				if (!Path.IsPathRooted(origName)) {
					foreach (Document doc in Documents) {
						if (doc.Window.ViewContent.IsUntitled && doc.Window.ViewContent.UntitledName == origName) {
							doc.Select ();
							openFileInfo.NewContent = doc.Window.ViewContent;
							return;
						}
					}
				}
				
				if (!File.Exists (fileName)) {
					monitor.ReportError (GettextCatalog.GetString ("File not found: {0}", fileName), null);
					return;
				}
			}
			
			Counters.OpenDocumentTimer.Trace ("Looking for binding");
			
			IDisplayBinding binding = null;
			IViewDisplayBinding viewBinding = null;
			Project project = openFileInfo.Project ?? GetProjectContainingFile (fileName);
			
			if (openFileInfo.DisplayBinding != null) {
				binding = viewBinding = openFileInfo.DisplayBinding;
			} else {
				var bindings = DisplayBindingService.GetDisplayBindings (fileName, null, project).ToList ();
				if (openFileInfo.Options.HasFlag (OpenDocumentOptions.OnlyInternalViewer)) {
					binding = bindings.OfType<IViewDisplayBinding>().FirstOrDefault (d => d.CanUseAsDefault)
						?? bindings.OfType<IViewDisplayBinding>().FirstOrDefault ();
					viewBinding = (IViewDisplayBinding) binding;
				}
				else if (openFileInfo.Options.HasFlag (OpenDocumentOptions.OnlyExternalViewer)) {
					binding = bindings.OfType<IExternalDisplayBinding>().FirstOrDefault (d => d.CanUseAsDefault);
					viewBinding = null;
				}
				else {
					binding = bindings.FirstOrDefault (d => d.CanUseAsDefault);
					viewBinding = binding as IViewDisplayBinding;
				}
			}
			
			try {
				if (binding != null) {
					if (viewBinding != null)  {
						var fw = new LoadFileWrapper (monitor, workbench, viewBinding, project, openFileInfo);
						fw.Invoke (fileName);
					} else {
						var extBinding = (IExternalDisplayBinding)binding;
						var app = extBinding.GetApplication (fileName, null, project);
						app.Launch (fileName);
					}
					
					Counters.OpenDocumentTimer.Trace ("Adding to recent files");
					DesktopService.RecentFiles.AddFile (fileName, project);
				} else if (!openFileInfo.Options.HasFlag (OpenDocumentOptions.OnlyInternalViewer)) {
					try {
						Counters.OpenDocumentTimer.Trace ("Showing in browser");
						DesktopService.OpenFile (fileName);
					} catch (Exception ex) {
						LoggingService.LogError ("Error opening file: " + fileName, ex);
						MessageService.ShowError (GettextCatalog.GetString ("File '{0}' could not be opened", fileName));
					}
				}
			} catch (Exception ex) {
				monitor.ReportError ("", ex);
			}
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
			var dp = new DocumentUserPrefs ();
			dp.FileName = FileService.AbsoluteToRelativePath (args.Item.BaseDirectory, document.FileName);
			if (document.Editor != null) {
				dp.Line = document.Editor.Caret.Column;
				dp.Column = document.Editor.Caret.Column;
			}
			return dp;
		}

		void OnLoadingWorkspaceUserPreferences (object s, UserPreferencesEventArgs args)
		{
			WorkbenchUserPrefs prefs = args.Properties.GetValue<WorkbenchUserPrefs> ("MonoDevelop.Ide.Workbench");
			if (prefs == null)
				return;

			try {
				IdeApp.Workbench.LockActiveWindowChangeEvent ();
				NavigationHistoryService.LogActiveDocument ();
				
				List<Tuple<IViewContent,string>> docViews = new List<Tuple<IViewContent,string>> ();
				FilePath baseDir = args.Item.BaseDirectory;
				var floatingWindows = new List<DockWindow> ();

				using (IProgressMonitor pm = ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Loading workspace documents"), Stock.StatusSolutionOperation, true)) {

					var docList = prefs.Files.Distinct (new DocumentUserPrefsFilenameComparer ()).OrderBy (d => d.NotebookId).ToList ();
					OpenDocumentsInContainer (pm, baseDir, docViews, docList, workbench.TabControl.Container);

					foreach (var fw in prefs.FloatingWindows) {
						var dockWindow = new DockWindow ();
						dockWindow.Move (fw.X, fw.Y);
						dockWindow.Resize (fw.Width, fw.Height);
						docList = fw.Files.Distinct (new DocumentUserPrefsFilenameComparer ()).OrderBy (d => d.NotebookId).ToList ();
						OpenDocumentsInContainer (pm, baseDir, docViews, docList, dockWindow.Container);
						floatingWindows.Add (dockWindow);
					}

					// Note: At this point, the progress monitor will be disposed which causes the gtk main-loop to be pumped.
					// This is EXTREMELY important, because without this main-loop pumping action, the next foreach() loop will
					// not cause the Solution tree-view to properly expand, nor will the ActiveDocument be set properly.
				}

				string currentFileName = prefs.ActiveDocument != null ? baseDir.Combine (prefs.ActiveDocument).FullPath : null;

				Document activeDoc = null;
				foreach (var t in docViews) {
					Document doc = WrapDocument (t.Item1.WorkbenchWindow);
					if (t.Item2 == currentFileName)
						activeDoc = doc;
				}

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
					activeDoc.RunWhenLoaded (() => {
						var window = activeDoc.Window;
						if (window != null)
							window.SelectWindow ();
					});
				}

			} finally {
				IdeApp.Workbench.UnlockActiveWindowChangeEvent ();
			}
		}

		void OpenDocumentsInContainer (IProgressMonitor pm, FilePath baseDir, List<Tuple<IViewContent,string>> docViews, List<DocumentUserPrefs> list, DockNotebookContainer container)
		{
			int currentNotebook = -1;
			DockNotebook nb = container.GetFirstNotebook ();

			foreach (var doc in list) {
				string fileName = baseDir.Combine (doc.FileName).FullPath;
				if (File.Exists (fileName)) {
					if (doc.NotebookId != currentNotebook) {
						if (currentNotebook != -1 || nb == null)
							nb = container.InsertRight (null);
						currentNotebook = doc.NotebookId;
					}
					// TODO: Get the correct project.
					var view = IdeApp.Workbench.BatchOpenDocument (pm, fileName, null, doc.Line, doc.Column, nb);

					if (view != null) {
						var t = new Tuple<IViewContent,string> (view, fileName);
						docViews.Add (t);
					}
				}
			}
		}
		
		internal Document FindDocument (IWorkbenchWindow window)
		{
			foreach (Document doc in Documents)
				if (doc.Window == window)
					return doc;
			return null;
		}
		
		internal Pad FindPad (IPadContent padContent)
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

		internal void ReorderDocuments (int oldPlacement, int newPlacement)
		{
			IViewContent content = workbench.InternalViewContentCollection[oldPlacement];
			workbench.InternalViewContentCollection.RemoveAt (oldPlacement);
			workbench.InternalViewContentCollection.Insert (newPlacement, content);

			Document doc = documents [oldPlacement];
			documents.RemoveAt (oldPlacement);
			documents.Insert (newPlacement, doc);
		}
		
		internal void ResetToolbars ()
		{
			workbench.ResetToolbars ();
		}


		internal void LockActiveWindowChangeEvent ()
		{
			workbench.LockActiveWindowChangeEvent ();
		}

		internal void UnlockActiveWindowChangeEvent ()
		{
			workbench.UnlockActiveWindowChangeEvent ();
		}

		List<FileData> fileStatus;
		object fileStatusLock = new object ();
		
		internal void SaveFileStatus ()
		{
			fileStatus = new List<FileData> ();
			
//			DateTime t = DateTime.Now;
			List<FilePath> files = new List<FilePath> (GetKnownFiles ());
//			Console.WriteLine ("SaveFileStatus(0) " + (DateTime.Now - t).TotalMilliseconds + "ms " + files.Count);
			
			ThreadPool.QueueUserWorkItem (delegate {
//				t = DateTime.Now;
				lock (fileStatusLock) {
					foreach (FilePath file in files) {
						try {
							FileInfo fi = new FileInfo (file);
							FileData fd = new FileData (file, fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue);
							fileStatus.Add (fd);
						} catch {
							// Ignore
						}
					}
				}
//				Console.WriteLine ("SaveFileStatus " + (DateTime.Now - t).TotalMilliseconds + "ms " + fileStatus.Count);
			});
		}
		
		internal void CheckFileStatus ()
		{
			if (fileStatus == null)
				return;
			
			ThreadPool.QueueUserWorkItem (delegate {
				lock (fileStatusLock) {
//					DateTime t = DateTime.Now;
					if (fileStatus == null)
						return;
					List<FilePath> modified = new List<FilePath> ();
					foreach (FileData fd in fileStatus) {
						try {
							FileInfo fi = new FileInfo (fd.File);
							if (fi.Exists) {
								if (fi.LastWriteTimeUtc != fd.TimeUtc)
									modified.Add (fd.File);
							} else if (fd.TimeUtc != DateTime.MinValue) {
								FileService.NotifyFileRemoved (fd.File);
							}
						} catch {
							// Ignore
						}
					}
					if (modified.Count > 0)
						FileService.NotifyFilesChanged (modified);
					
//					Console.WriteLine ("CheckFileStatus " + (DateTime.Now - t).TotalMilliseconds + "ms " + fileStatus.Count);
					fileStatus = null;
				}
			});
		}
		
		IEnumerable<FilePath> GetKnownFiles ()
		{
			foreach (WorkspaceItem item in IdeApp.Workspace.Items) {
				foreach (FilePath file in item.GetItemFiles (true))
					yield return file;
			}
			foreach (Document doc in documents) {
				if (!doc.HasProject && doc.IsFile)
					yield return doc.FileName;
			}
		}
		
		struct FileData
		{
			public FileData (FilePath file, DateTime timeUtc)
			{
				this.File = file;
				this.TimeUtc = timeUtc;
			}
			
			public FilePath File;
			public DateTime TimeUtc;
		}
		
		void OnDocumentOpened (DocumentEventArgs e)
		{
			try {
				EventHandler<DocumentEventArgs> handler = this.DocumentOpened;
				if (handler != null)
					handler (this, e);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while opening documents", ex);
			}
		}

		void OnDocumentClosed (Document doc)
		{
			try {
				var e = new DocumentEventArgs (doc);
				EventHandler<DocumentEventArgs> handler = this.DocumentClosed;
				if (handler != null)
					handler (this, e);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while closing documents", ex);
			}
		}

		void OnDocumentClosing (Document doc)
		{
			try {
				var e = new DocumentEventArgs (doc);
				EventHandler<DocumentEventArgs> handler = this.DocumentClosing;
				if (handler != null)
					handler (this, e);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception before closing documents", ex);
			}
		}

		public event EventHandler<DocumentEventArgs> DocumentOpened;
		public event EventHandler<DocumentEventArgs> DocumentClosed;
		public event EventHandler<DocumentEventArgs> DocumentClosing;

		public void ReparseOpenDocuments ()
		{
			foreach (var doc in Documents) {
				if (doc.ParsedDocument != null)
					doc.ReparseDocument ();
			}
		}
	}

	public class FileOpenInformation
	{
		FilePath fileName;
		public FilePath FileName {
			get {
				return fileName;
			}
			set {
				fileName = ResolveSymbolicLink (value.CanonicalPath);
				if (fileName.IsNullOrEmpty)
					LoggingService.LogError ("FileName == null\n" + Environment.StackTrace);
			}
		}

		public OpenDocumentOptions Options { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
		public IViewDisplayBinding DisplayBinding { get; set; }
		public IViewContent NewContent { get; set; }
		public Encoding Encoding { get; set; }
		public Project Project { get; set; }
		internal DockNotebook DockNotebook { get; set; }

		[Obsolete("Use FileOpenInformation (FilePath filePath, Project project, int line, int column, OpenDocumentOptions options)")]
		public FileOpenInformation (string fileName, int line, int column, OpenDocumentOptions options) 
		{
			this.FileName = fileName;
			this.Line = line;
			this.Column = column;
			this.Options = options;

		}

		public FileOpenInformation (FilePath filePath, Project project)
		{
			this.FileName = filePath;
			this.Project = project;
			this.Options = OpenDocumentOptions.Default;
		}
		
		public FileOpenInformation (FilePath filePath, Project project, int line, int column, OpenDocumentOptions options) 
		{
			this.FileName = filePath;
			this.Project = project;
			this.Line = line;
			this.Column = column;
			this.Options = options;
		}

		public FileOpenInformation (FilePath filePath, Project project, bool bringToFront)
		{
			this.FileName = filePath;
			this.Project = project;
			this.Options = OpenDocumentOptions.Default;
			if (bringToFront) {
				this.Options |= OpenDocumentOptions.BringToFront;
			} else {
				this.Options &= ~OpenDocumentOptions.BringToFront;
			}
		}

		static FilePath ResolveSymbolicLink (FilePath fileName)
		{
			if (fileName.IsEmpty)
				return fileName;
			try {
				var alreadyVisted = new HashSet<FilePath> ();
				while (true) {
					if (alreadyVisted.Contains (fileName)) {
						LoggingService.LogError ("Cyclic links detected: " + fileName);
						return FilePath.Empty;
					}
					alreadyVisted.Add (fileName);
					var linkInfo = new Mono.Unix.UnixSymbolicLinkInfo (fileName);
					if (linkInfo.IsSymbolicLink && linkInfo.HasContents) {
						FilePath contentsPath = linkInfo.ContentsPath;
						if (contentsPath.IsAbsolute) {
							fileName = linkInfo.ContentsPath;
						} else {
							fileName = fileName.ParentDirectory.Combine (contentsPath);
						}
						fileName = fileName.CanonicalPath;
						continue;
					}
					return ResolveSymbolicLink (fileName.ParentDirectory).Combine (fileName.FileName).CanonicalPath;
				}
			} catch (Exception) {
				return fileName;
			}
		}
	}
	
	class LoadFileWrapper
	{
		IViewDisplayBinding binding;
		Project project;
		FileOpenInformation fileInfo;
		DefaultWorkbench workbench;
		IProgressMonitor monitor;
		IViewContent newContent;
		
		public LoadFileWrapper (IProgressMonitor monitor, DefaultWorkbench workbench, IViewDisplayBinding binding, FileOpenInformation fileInfo)
		{
			this.monitor = monitor;
			this.workbench = workbench;
			this.fileInfo = fileInfo;
			this.binding = binding;
		}
		
		public LoadFileWrapper (IProgressMonitor monitor, DefaultWorkbench workbench, IViewDisplayBinding binding, Project project, FileOpenInformation fileInfo)
			: this (monitor, workbench, binding, fileInfo)
		{
			this.project = project;
		}
		
		public void Invoke (string fileName)
		{
			try {
				Counters.OpenDocumentTimer.Trace ("Creating content");
				string mimeType = DesktopService.GetMimeTypeForUri (fileName);
				if (binding.CanHandle (fileName, mimeType, project)) {
					newContent = binding.CreateContent (fileName, mimeType, project);
				} else {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), null);
				}
				if (newContent == null) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), null);
					return;
				}
				
				if (project != null)
					newContent.Project = project;
				
				Counters.OpenDocumentTimer.Trace ("Loading file");
				
				IEncodedTextContent etc = (IEncodedTextContent) newContent.GetContent (typeof(IEncodedTextContent));
				try {
					if (fileInfo.Encoding != null && etc != null)
						etc.Load (fileName, fileInfo.Encoding);
					else
						newContent.Load (fileName);
				} catch (InvalidEncodingException iex) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. {1}", fileName, iex.Message), null);
					return;
				} catch (OverflowException) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. File too large.", fileName), null);
					return;
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), ex);
				return;
			}
			
			// content got re-used
			if (newContent.WorkbenchWindow != null) {
				newContent.WorkbenchWindow.SelectWindow ();
				fileInfo.NewContent = newContent;
				return;
			}

			Counters.OpenDocumentTimer.Trace ("Showing view");

			workbench.ShowView (newContent, fileInfo.Options.HasFlag (OpenDocumentOptions.BringToFront), fileInfo.DockNotebook);

			DisplayBindingService.AttachSubWindows (newContent.WorkbenchWindow, binding);
			newContent.WorkbenchWindow.DocumentType = binding.Name;
			
			IEditableTextBuffer ipos = (IEditableTextBuffer) newContent.GetContent (typeof(IEditableTextBuffer));
			if (fileInfo.Line > 0 && ipos != null) {
				Mono.TextEditor.Utils.FileSettingsStore.Remove (fileName);
				ipos.RunWhenLoaded (JumpToLine); 
			}
			
			fileInfo.NewContent = newContent;
		}
		
		void JumpToLine ()
		{
			IEditableTextBuffer ipos = (IEditableTextBuffer) newContent.GetContent (typeof(IEditableTextBuffer));
			ipos.SetCaretTo (Math.Max(1, fileInfo.Line), Math.Max(1, fileInfo.Column), fileInfo.Options.HasFlag (OpenDocumentOptions.HighlightCaretLine));
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
}
