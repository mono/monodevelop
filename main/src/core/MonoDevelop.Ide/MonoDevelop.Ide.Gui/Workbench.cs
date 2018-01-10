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

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public sealed class Workbench
	{
		readonly ProgressMonitorManager monitors = new ProgressMonitorManager ();
		ImmutableList<Document> documents = ImmutableList<Document>.Empty;
		DefaultWorkbench workbench;
		PadCollection pads;

		public event EventHandler ActiveDocumentChanged;
		public event EventHandler LayoutChanged;
		public event EventHandler GuiLocked;
		public event EventHandler GuiUnlocked;
		
		internal void Initialize (ProgressMonitor monitor)
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
				workbench.WorkbenchTabsChanged += WorkbenchTabsChanged;
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
		
		internal async Task<bool> Close ()
		{
			return await workbench.Close();
		}

		public ImmutableList<Document> Documents {
			get { return documents; }
		}

		/// <summary>
		/// This is a wrapper for use with AutoTest
		/// </summary>
		internal bool DocumentsDirty {
			get { return Documents.Any (d => d.IsDirty); }
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

		internal TextReader[] GetDocumentReaders (List<string> filenames)
		{
			TextReader [] results = new TextReader [filenames.Count];

			int idx = 0;
			foreach (var f in filenames) {
				var fullPath = (FilePath)FileService.GetFullPath (f);

				Document doc = documents.Find (d => d.Editor != null && (fullPath == FileService.GetFullPath (d.Name)));
				if (doc != null) {
					results [idx] = doc.Editor.CreateReader ();
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
				if (DesktopService.IsModalDialogRunning ())
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
			Document[] docs = Documents.Where (doc => doc.IsDirty && doc.Window.ViewContent != null).ToArray ();
			if (!docs.Any ())
				return true;

			foreach (Document doc in docs) {
				AlertButton result = PromptToSaveChanges (doc);
				if (result == AlertButton.Cancel)
					return false;

				if (result == AlertButton.CloseWithoutSave) {
					doc.Window.ViewContent.DiscardChanges ();
					await doc.Window.CloseWindow (true);
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
				GettextCatalog.GetString ("Save the changes to document '{0}' before creating a new solution?",
				(object)(doc.Window.ViewContent.IsUntitled
					? doc.Window.ViewContent.UntitledName
					: System.IO.Path.GetFileName (doc.Window.ViewContent.ContentName))),
				GettextCatalog.GetString ("If you don't save, all changes will be permanently lost."),
				AlertButton.CloseWithoutSave, AlertButton.Cancel, doc.Window.ViewContent.IsUntitled ? AlertButton.SaveAs : AlertButton.Save);
		}
		
		public void CloseAllDocuments (bool leaveActiveDocumentOpen)
		{
			Document[] docs = new Document [Documents.Count];
			Documents.CopyTo (docs, 0);
			
			// The active document is the last one to close.
			// It avoids firing too many ActiveDocumentChanged events.
			
			foreach (Document doc in docs) {
				if (doc != ActiveDocument)
					doc.Close ().Ignore();
			}
			if (!leaveActiveDocumentOpen && ActiveDocument != null)
				ActiveDocument.Close ().Ignore();
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
		internal Task<Document> OpenDocument (FilePath fileName, int line, int column, OpenDocumentOptions options, Encoding encoding, IViewDisplayBinding binding)
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


		public Task<Document> OpenDocument (FilePath fileName, Project project, bool bringToFront)
		{
			return OpenDocument (fileName, project, bringToFront ? OpenDocumentOptions.Default : OpenDocumentOptions.Default & ~OpenDocumentOptions.BringToFront);
		}

		public Task<Document> OpenDocument (FilePath fileName, Project project, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, -1, -1, options, null, null);
		}

		public Task<Document> OpenDocument (FilePath fileName, Project project, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, -1, -1, options, encoding, null);
		}

		public Task<Document> OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, line, column, options, null, null);
		}

		public Task<Document> OpenDocument (FilePath fileName, Project project, int line, int column, Encoding encoding, OpenDocumentOptions options = OpenDocumentOptions.Default)
		{
			return OpenDocument (fileName, project, line, column, options, encoding, null);
		}

		internal Task<Document> OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options, Encoding encoding, IViewDisplayBinding binding)
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

		static void ScrollToRequestedCaretLocation (Document doc, FileOpenInformation info)
		{
			var ipos = doc.Editor;
			if ((info.Line >= 1 || info.Offset >= 0) && ipos != null) {
				doc.DisableAutoScroll ();
				doc.RunWhenLoaded (() => {
					var loc = new DocumentLocation (info.Line, info.Column >= 1 ? info.Column : 1);
					if (info.Offset >= 0) {
						loc = ipos.OffsetToLocation (info.Offset);
					}
					if (loc.IsEmpty)
						return;
					ipos.SetCaretLocation (loc, info.Options.HasFlag (OpenDocumentOptions.HighlightCaretLine), info.Options.HasFlag (OpenDocumentOptions.CenterCaretLine));
				});
			}
		}
		
		internal Task<Document> OpenDocument (FilePath fileName, Project project, int line, int column, OpenDocumentOptions options, Encoding Encoding, IViewDisplayBinding binding, DockNotebook dockNotebook)
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

		public async Task<Document> OpenDocument (FileOpenInformation info)
		{
			if (string.IsNullOrEmpty (info.FileName))
				return null;

			var metadata = CreateOpenDocumentTimerMetadata ();

			using (Counters.OpenDocumentTimer.BeginTiming ("Opening file " + info.FileName, metadata)) {
				NavigationHistoryService.LogActiveDocument ();
				Counters.OpenDocumentTimer.Trace ("Look for open document");
				foreach (Document doc in Documents) {
					BaseViewContent vcFound = null;

					//search all ViewContents to see if they can "re-use" this filename
					if (doc.Window.ViewContent.CanReuseView (info.FileName))
						vcFound = doc.Window.ViewContent;
					
					//old method as fallback
					if ((vcFound == null) && (doc.FileName.CanonicalPath == info.FileName)) // info.FileName is already Canonical
						vcFound = doc.Window.ViewContent;
					//if found, try to reuse or close the old view
					if (vcFound != null) {
						// reuse the view if the binidng didn't change
						if (info.Options.HasFlag (OpenDocumentOptions.TryToReuseViewer) || vcFound.Binding == info.DisplayBinding) {
							if (info.Project != null && doc.Project != info.Project) {
								doc.SetProject (info.Project);
							}

							ScrollToRequestedCaretLocation (doc, info);

							if (info.Options.HasFlag (OpenDocumentOptions.BringToFront)) {
								doc.Select ();
								NavigationHistoryService.LogActiveDocument ();
							}
							return doc;
						} else {
							if (!await doc.Close ())
								return doc;
							break;
						}
					}
				}
				Counters.OpenDocumentTimer.Trace ("Initializing monitor");
				ProgressMonitor pm = ProgressMonitors.GetStatusProgressMonitor (
					GettextCatalog.GetString ("Opening {0}", info.Project != null ?
						info.FileName.ToRelative (info.Project.ParentSolution.BaseDirectory) :
						info.FileName),
					Stock.StatusWorking,
					true
				);

				bool result = await RealOpenFile (pm, info);
				pm.Dispose ();

				AddOpenDocumentTimerMetadata (metadata, info, result);
				
				if (info.NewContent != null) {
					Counters.OpenDocumentTimer.Trace ("Wrapping document");
					Document doc = WrapDocument (info.NewContent.WorkbenchWindow);
					
					ScrollToRequestedCaretLocation (doc, info);
					
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

		Dictionary<string, string> CreateOpenDocumentTimerMetadata ()
		{
			var metadata = new Dictionary<string, string> ();
			metadata ["Result"] = "None";
			return metadata;
		}

		void AddOpenDocumentTimerMetadata (IDictionary<string, string> metadata, FileOpenInformation info, bool result)
		{
			if (info.NewContent != null)
				metadata ["EditorType"] = info.NewContent.GetType ().FullName;
			if (info.Project != null)
				metadata ["OwnerProjectGuid"] = info.Project?.ItemId;
			
			metadata ["Extension"] = info.FileName.Extension;
			metadata ["Result"] = result ? "Success" : "Failure";
		}

		async Task<ViewContent> BatchOpenDocument (ProgressMonitor monitor, FilePath fileName, Project project, int line, int column, DockNotebook dockNotebook)
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
				
				bool result = await RealOpenFile (monitor, openFileInfo);

				AddOpenDocumentTimerMetadata (metadata, openFileInfo, result);
				
				return openFileInfo.NewContent;
			}
		}
		
		public Document OpenDocument (ViewContent content, bool bringToFront)
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
			
			ViewContent newContent = binding.CreateContent (defaultName, mimeType, null);
			using (content) {
				newContent.LoadNew (content, mimeType);
			}
				
			if (newContent == null)
				throw new ApplicationException(String.Format("Created view content was null{3}DefaultName:{0}{3}MimeType:{1}{3}Content:{2}",
					defaultName, mimeType, content, Environment.NewLine));
			
			newContent.UntitledName = defaultName;
			newContent.IsDirty = false;
			newContent.Binding = binding;
			workbench.ShowView (newContent, true, binding);

			var document = WrapDocument (newContent.WorkbenchWindow);
			document.Editor.Encoding = Encoding.UTF8;
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

		public void ShowGlobalPreferencesDialog (Window parentWindow, string panelId, Action<OptionsDialog> configurationAction = null)
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
				parentWindow = IdeApp.Workbench.RootWindow;

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
			documents = documents.Add (doc);

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
		
		async Task OnWindowClosing (object sender, WorkbenchWindowEventArgs args)
		{
			var window = (IWorkbenchWindow) sender;
			var viewContent = window.ViewContent;
			if (!args.Forced && viewContent != null && viewContent.IsDirty) {
				AlertButton result = MessageService.GenericAlert (Stock.Warning,
					GettextCatalog.GetString ("Save the changes to document '{0}' before closing?",
						viewContent.IsUntitled
							? viewContent.UntitledName
							: System.IO.Path.GetFileName (viewContent.ContentName)),
					GettextCatalog.GetString ("If you don't save, all changes will be permanently lost."),
					AlertButton.CloseWithoutSave, AlertButton.Cancel, viewContent.IsUntitled ? AlertButton.SaveAs : AlertButton.Save);
				if (result == AlertButton.Save) {
					var doc = FindDocument (window);
					await doc.Save ();
					if (viewContent.IsDirty) {
						// This may happen if the save operation failed
						args.Cancel = true;
						doc.Select ();
						return;
					}
				} else if (result == AlertButton.SaveAs) {
					var doc = FindDocument (window);
					var resultSaveAs = await doc.SaveAs ();
					if (!resultSaveAs || viewContent.IsDirty) {
						// This may happen if the save operation failed or Save As was canceled
						args.Cancel = true;
						doc.Select ();
						return;
					}
				} else {
					args.Cancel |= result != AlertButton.CloseWithoutSave;
					if (!args.Cancel)
						viewContent.DiscardChanges ();
				}
			}
			OnDocumentClosing (FindDocument (window));
		}
		
		void OnWindowClosed (object sender, WorkbenchWindowEventArgs args)
		{
			IWorkbenchWindow window = (IWorkbenchWindow) sender;
			var doc = FindDocument (window);
			if (doc == null)
				return;

			window.Closing -= OnWindowClosing;
			window.Closed -= OnWindowClosed;
			documents = documents.Remove (doc);

			OnDocumentClosed (doc);
			doc.DisposeDocument ();
		}
		
		// When looking for the project to which the file belongs, look first
		// in the active project, then the active solution, and so on
		internal static Project GetProjectContainingFile (FilePath fileName)
		{
			Project project = null;
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
				if (IdeApp.ProjectOperations.CurrentSelectedProject.Files.GetFile (fileName) != null)
					project = IdeApp.ProjectOperations.CurrentSelectedProject;
				else if (IdeApp.ProjectOperations.CurrentSelectedProject.FileName == fileName)
					project = IdeApp.ProjectOperations.CurrentSelectedProject;
			}
			if (project == null && IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem != null) {
				project = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.GetProjectsContainingFile (fileName).FirstOrDefault ();
				if (project == null) {
					WorkspaceItem it = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.ParentWorkspace;
					while (it != null && project == null) {
						project = it.GetProjectsContainingFile (fileName).FirstOrDefault ();
						it = it.ParentWorkspace;
					}
				}
			}
			if (project == null) {
				project = IdeApp.Workspace.GetProjectsContainingFile (fileName).FirstOrDefault ();
			}
			return project;
		}
		
		async Task<bool> RealOpenFile (ProgressMonitor monitor, FileOpenInformation openFileInfo)
		{
			FilePath fileName;
			
			Counters.OpenDocumentTimer.Trace ("Checking file");
			
			string origName = openFileInfo.FileName;
			
			if (origName == null) {
				monitor.ReportError (GettextCatalog.GetString ("Invalid file name"), null);
				return false;
			}

			fileName = openFileInfo.FileName;
			if (!origName.StartsWith ("http://", StringComparison.Ordinal))
				fileName = fileName.FullPath;
			
			//Debug.Assert(FileService.IsValidPath(fileName));
			if (FileService.IsDirectory (fileName)) {
				monitor.ReportError (GettextCatalog.GetString ("{0} is a directory", fileName), null);
				return false;
			}
			
			// test, if file fileName exists
			if (!origName.StartsWith ("http://", StringComparison.Ordinal)) {
				// test, if an untitled file should be opened
				if (!Path.IsPathRooted(origName)) {
					foreach (Document doc in Documents) {
						if (doc.Window.ViewContent.IsUntitled && doc.Window.ViewContent.UntitledName == origName) {
							doc.Select ();
							openFileInfo.NewContent = doc.Window.ViewContent;
							return true;
						}
					}
				}
				
				if (!File.Exists (fileName)) {
					monitor.ReportError (GettextCatalog.GetString ("File not found: {0}", fileName), null);
					return false;
				}
			}
			
			Counters.OpenDocumentTimer.Trace ("Looking for binding");
			
			IDisplayBinding binding = null;
			IViewDisplayBinding viewBinding = null;
			if (openFileInfo.Project == null) {
				// Set the project if one can be found. The project on the FileOpenInformation
				// is used to add project metadata to the OpenDocumentTimer counter.
				openFileInfo.Project = GetProjectContainingFile (fileName);
			}
			Project project = openFileInfo.Project;
			
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
						await fw.Invoke (fileName);
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
						return false;
					}
				}
			} catch (Exception ex) {
				monitor.ReportError ("", ex);
				return false;
			}
			return true;
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
				dp.Line = document.Editor.CaretLine;
				dp.Column = document.Editor.CaretColumn;
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
				NavigationHistoryService.LogActiveDocument ();
				
				List<Tuple<ViewContent,string>> docViews = new List<Tuple<ViewContent,string>> ();
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
					Document doc = WrapDocument (t.Item1.WorkbenchWindow);
					if (t.Item2 == currentFileName)
						activeDoc = doc;
				}

				if (activeDoc == null) {
					activeDoc = docViews.Select (t => WrapDocument (t.Item1.WorkbenchWindow)).FirstOrDefault ();
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

		async Task<bool> OpenDocumentsInContainer (ProgressMonitor pm, FilePath baseDir, List<Tuple<ViewContent,string>> docViews, List<DocumentUserPrefs> list, DockNotebookContainer container)
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
					var view = await IdeApp.Workbench.BatchOpenDocument (pm, fileName, null, doc.Line, doc.Column, nb);

					if (view != null) {
						var t = new Tuple<ViewContent,string> (view, fileName);
						docViews.Add (t);
					}
				}
			}
			return true;
		}
		
		internal Document FindDocument (IWorkbenchWindow window)
		{
			foreach (Document doc in Documents)
				if (doc.Window == window)
					return doc;
			return null;
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

		internal void ReorderDocuments (int oldPlacement, int newPlacement)
		{
			ViewContent content = workbench.InternalViewContentCollection[oldPlacement];
			workbench.InternalViewContentCollection.RemoveAt (oldPlacement);
			workbench.InternalViewContentCollection.Insert (newPlacement, content);

			Document doc = documents [oldPlacement];
			documents = documents.RemoveAt (oldPlacement).Insert (newPlacement, doc);
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
		SemaphoreSlim fileStatusLock = new SemaphoreSlim (1, 1);
		// http://msdn.microsoft.com/en-us/library/system.io.file.getlastwritetimeutc(v=vs.110).aspx
		static DateTime NonExistentFile = new DateTime(1601, 1, 1);
		internal void SaveFileStatus ()
		{
//			DateTime t = DateTime.Now;
			List<FilePath> files = new List<FilePath> (GetKnownFiles ());
			fileStatus = new List<FileData> (files.Count);
//			Console.WriteLine ("SaveFileStatus(0) " + (DateTime.Now - t).TotalMilliseconds + "ms " + files.Count);
			
			Task.Run (async delegate {
//				t = DateTime.Now;
				try {
					await fileStatusLock.WaitAsync ().ConfigureAwait (false);
					if (fileStatus == null)
						return;
					foreach (FilePath file in files) {
						try {
							DateTime ft = File.GetLastWriteTimeUtc (file);
							FileData fd = new FileData (file, ft != NonExistentFile ? ft : DateTime.MinValue);
							fileStatus.Add (fd);
						} catch {
							// Ignore						}
					}
				} finally {
					fileStatusLock.Release ();
				}
//				Console.WriteLine ("SaveFileStatus " + (DateTime.Now - t).TotalMilliseconds + "ms " + fileStatus.Count);
			});
		}
		
		internal void CheckFileStatus ()
		{
			if (fileStatus == null)
				return;
			
			Task.Run (async delegate {
				try {
//					DateTime t = DateTime.Now;

					await fileStatusLock.WaitAsync ().ConfigureAwait (false);
					if (fileStatus == null)
						return;
					List<FilePath> modified = new List<FilePath> (fileStatus.Count);
					foreach (FileData fd in fileStatus) {
						try {
							DateTime ft = File.GetLastWriteTimeUtc (fd.File);
							if (ft != NonExistentFile) {
								if (ft != fd.TimeUtc)
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
				} finally {
					fileStatusLock.Release ();
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
				DocumentOpened?.Invoke (this, e);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while opening documents", ex);
			}
		}

		void OnDocumentClosed (Document doc)
		{
			try {
				var e = new DocumentEventArgs (doc);
				DocumentClosed?.Invoke (this, e);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while closing documents", ex);
			}
		}

		void OnDocumentClosing (Document doc)
		{
			try {
				var e = new DocumentEventArgs (doc);
				DocumentClosing?.Invoke (this, e);
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
	
	public class FileOpenInformation
	{
		FilePath fileName;
		public FilePath FileName {
			get {
				return fileName;
			}
			set {
				fileName = value.CanonicalPath.ResolveLinks ();
				if (fileName.IsNullOrEmpty)
					LoggingService.LogError ("FileName == null\n" + Environment.StackTrace);
			}
		}

		public OpenDocumentOptions Options { get; set; }

		int offset = -1;
		public int Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		public int Line { get; set; }
		public int Column { get; set; }
		public IViewDisplayBinding DisplayBinding { get; set; }
		public ViewContent NewContent { get; set; }
		public Encoding Encoding { get; set; }
		public Project Project { get; set; }

		/// <summary>
		/// Is true when the file is already open and reload is requested.
		/// </summary>
		public bool IsReloadOperation { get; set; }

		internal DockNotebook DockNotebook { get; set; }

		[Obsolete("Use FileOpenInformation (FilePath filePath, Project project, int line, int column, OpenDocumentOptions options)")]
		public FileOpenInformation (string fileName, int line, int column, OpenDocumentOptions options) 
		{
			this.FileName = fileName;
			this.Line = line;
			this.Column = column;
			this.Options = options;

		}

		public FileOpenInformation (FilePath filePath, Project project = null)
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
		ProgressMonitor monitor;
		ViewContent newContent;
		
		public LoadFileWrapper (ProgressMonitor monitor, DefaultWorkbench workbench, IViewDisplayBinding binding, FileOpenInformation fileInfo)
		{
			this.monitor = monitor;
			this.workbench = workbench;
			this.fileInfo = fileInfo;
			this.binding = binding;
		}
		
		public LoadFileWrapper (ProgressMonitor monitor, DefaultWorkbench workbench, IViewDisplayBinding binding, Project project, FileOpenInformation fileInfo)
			: this (monitor, workbench, binding, fileInfo)
		{
			this.project = project;
		}

		public async Task<bool> Invoke (string fileName)
		{
			try {
				Counters.OpenDocumentTimer.Trace ("Creating content");
				string mimeType = DesktopService.GetMimeTypeForUri (fileName);
				if (binding.CanHandle (fileName, mimeType, project)) {
					try {
						newContent = binding.CreateContent (fileName, mimeType, project);
					} catch (InvalidEncodingException iex) {
						monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. {1}", fileName, iex.Message), null);
						return false;
					} catch (OverflowException) {
						monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. File too large.", fileName), null);
						return false;
					}
				} else {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), null);
				}
				if (newContent == null) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), null);
					return false;
				}

				newContent.Binding = binding;
				if (project != null)
					newContent.Project = project;

				Counters.OpenDocumentTimer.Trace ("Loading file");

				try {
					await newContent.Load (fileInfo);
				} catch (InvalidEncodingException iex) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. {1}", fileName, iex.Message), null);
					return false;
				} catch (OverflowException) {
					monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not opened. File too large.", fileName), null);
					return false;
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), ex);
				return false;
			}

			// content got re-used
			if (newContent.WorkbenchWindow != null) {
				newContent.WorkbenchWindow.SelectWindow ();
				fileInfo.NewContent = newContent;
				return true;
			}

			Counters.OpenDocumentTimer.Trace ("Showing view");

			workbench.ShowView (newContent, fileInfo.Options.HasFlag (OpenDocumentOptions.BringToFront), binding, fileInfo.DockNotebook);

			newContent.WorkbenchWindow.DocumentType = binding.Name;

			var ipos = (TextEditor) newContent.GetContent (typeof(TextEditor));
			if (fileInfo.Line > 0 && ipos != null) {
				FileSettingsStore.Remove (fileName);
				ipos.RunWhenLoaded (JumpToLine);
			}

			fileInfo.NewContent = newContent;
			return true;
		}

		void JumpToLine ()
		{
			var ipos = (TextEditor) newContent.GetContent (typeof(TextEditor));
			var loc = new DocumentLocation (Math.Max(1, fileInfo.Line), Math.Max(1, fileInfo.Column));
			if (fileInfo.Offset >= 0) {
				loc = ipos.OffsetToLocation (fileInfo.Offset);
			}
			ipos.SetCaretLocation (loc, fileInfo.Options.HasFlag (OpenDocumentOptions.HighlightCaretLine));
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
