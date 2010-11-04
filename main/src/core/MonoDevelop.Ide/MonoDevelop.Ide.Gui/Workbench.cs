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

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public class Workbench
	{
		List<Document> documents = new List<Document> ();
		List<Pad> pads;
		ProgressMonitorManager monitors = new ProgressMonitorManager ();
		DefaultWorkbench workbench;
		
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
				workbench.ActiveWorkbenchWindowChanged += new EventHandler (OnDocumentChanged);
				IdeApp.Workspace.StoringUserPreferences += OnStoringWorkspaceUserPreferences;
				IdeApp.Workspace.LoadingUserPreferences += OnLoadingWorkspaceUserPreferences;
				
				IdeApp.CommandService.ApplicationFocusOut += delegate(object o, EventArgs args) {
					SaveFileStatus ();
				};
				IdeApp.CommandService.ApplicationFocusIn += delegate(object o, EventArgs args) {
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
			workbench.CurrentLayout = "Default";
			
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
			foreach (Document doc in documents) {
				if (FileService.GetFullPath (doc.Name) == name)
					return doc;
			}
			return null;
		}
		
		public List<Pad> Pads {
			get {
				if (pads == null) {
					pads = new List<Pad> ();
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
		/// </value>
		public bool AutoReloadDocuments { get; set; }
		
		/// <summary>
		/// Whether the root window or any undocked part of it has toplevel focus. 
		/// </summary>
		public bool HasToplevelFocus {
			get {
				var toplevel = Gtk.Window.ListToplevels ().Where (x => x.HasToplevelFocus).FirstOrDefault ();
				if (toplevel == null)
					return false;
				if (toplevel == RootWindow)
					return true;
				//FIXME: don't depend on type name string
				var c = toplevel.Child;
				return c != null && c.GetType ().FullName.StartsWith ("MonoDevelop.Components.Docking");
			}
		}
		
		public void Present ()
		{
			//FIXME: Present is broken on Mac GTK+. It maximises the window.
			if (!PropertyService.IsMac)
				RootWindow.Present ();
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
		
		public Pad GetPad<T> ()
		{
			foreach (Pad pad in Pads)
				if (typeof(T).IsInstanceOfType (pad.Content))
					return pad;
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
			IdeApp.CommandService.LockAll ();
			if (GuiLocked != null)
				GuiLocked (this, EventArgs.Empty);
		}
		
		public void UnlockGui ()
		{
			IdeApp.CommandService.UnlockAll ();
			if (GuiUnlocked != null)
				GuiUnlocked (this, EventArgs.Empty);
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
		
		public Pad ShowPad (IPadContent padContent, string id, string label, string defaultPlacement, IconId icon)
		{
			return ShowPad (new PadCodon (padContent, id, label, defaultPlacement, icon));
		}

		public FileViewer[] GetFileViewers (FilePath fileName)
		{
			List<FileViewer> list = new List<FileViewer> ();
			
			string mimeType = DesktopService.GetMimeTypeForUri (fileName);

			foreach (IDisplayBinding bin in DisplayBindingService.GetBindingsForMimeType (mimeType))
				list.Add (new FileViewer (bin));

			foreach (var app in DesktopService.GetApplications (fileName))
				list.Add (new FileViewer (app));
			
			return list.ToArray ();
		}

		public Document OpenDocument (FilePath fileName)
		{
			return OpenDocument (fileName, true);
		}

		public Document OpenDocument (FilePath fileName, string encoding)
		{
			return OpenDocument (fileName, -1, -1, true, encoding, null);
		}

		public Document OpenDocument (FilePath fileName, bool bringToFront)
		{
			return OpenDocument (fileName, -1, -1, bringToFront);
		}

		public Document OpenDocument (FilePath fileName, int line, int column, bool bringToFront)
		{
			return OpenDocument (fileName, line, column, bringToFront, null, null);
		}
		
		public Document OpenDocument (FilePath fileName, int line, int column, bool bringToFront, bool highlightCaretLine)
		{
			return OpenDocument (fileName, line, column, bringToFront, null, null, highlightCaretLine);
		}

		public Document OpenDocument (FilePath fileName, int line, int column, bool bringToFront, string encoding)
		{
			return OpenDocument (fileName, line, column, bringToFront, encoding, null);
		}

		public Document OpenDocument (FilePath fileName, int line, int column, bool bringToFront, string encoding, bool highlightCaretLine)
		{
			return OpenDocument (fileName, line, column, bringToFront, encoding, null, highlightCaretLine);
		}

		internal Document OpenDocument (FilePath fileName, int line, int column, bool bringToFront, string encoding, IDisplayBinding binding)
		{
			return OpenDocument (fileName, line, column, bringToFront, encoding, binding, true);
		}
		
		internal Document OpenDocument (FilePath fileName, int line, int column, bool bringToFront, string encoding, IDisplayBinding binding, bool highlightCaretLine)
		{
			if (string.IsNullOrEmpty (fileName))
				return null;
			using (Counters.OpenDocumentTimer.BeginTiming ("Opening file " + fileName)) {
				NavigationHistoryService.LogActiveDocument ();
				
				Counters.OpenDocumentTimer.Trace ("Look for open document");
				
				foreach (Document doc in Documents) {
					IBaseViewContent vcFound = null;
					int vcIndex = 0;
					
					//search all ViewContents to see if they can "re-use" this filename
					if (doc.Window.ViewContent.CanReuseView (fileName))
						vcFound = doc.Window.ViewContent;
					
					
					//old method as fallback
					if ((vcFound == null) && (doc.FileName == fileName))
						vcFound = doc.Window.ViewContent;
					
					//if found, select window and jump to line
					if (vcFound != null) {
						if (bringToFront) {
							doc.Select ();
							doc.Window.SwitchView (vcIndex);
							Present ();
						}
						
						IEditableTextBuffer ipos = vcFound.GetContent<IEditableTextBuffer> ();
						if (line >= 1 && ipos != null) {
							ipos.SetCaretTo (line, column >= 1 ? column : 1, highlightCaretLine);
						}
						
						NavigationHistoryService.LogActiveDocument ();
						return doc;
					}
				}
				
				Counters.OpenDocumentTimer.Trace ("Initializing monitor");
				IProgressMonitor pm = ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Opening {0}", fileName), Stock.OpenFileIcon, true);
				var openFileInfo = new FileOpenInformation () {
					ProgressMonitor = pm,
					FileName = fileName,
					BringToFront = bringToFront,
					Line = line,
					Column = column,
					DisplayBinding = binding,
					Encoding = encoding,
					HighlightCaretLine = highlightCaretLine,
				};
				RealOpenFile (openFileInfo);
				
				if (!pm.AsyncOperation.Success)
					return null;
				
				if (openFileInfo.NewContent != null) {
					Counters.OpenDocumentTimer.Trace ("Wrapping document");
					Document doc = WrapDocument (openFileInfo.NewContent.WorkbenchWindow);
					if (bringToFront)
						Present ();
					return doc;
				} else {
					return null;
				}
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
			IDisplayBinding binding = DisplayBindingService.GetDefaultBinding (null, mimeType);
			IViewContent newContent;
			
			if (binding != null) {
				try {
					newContent = binding.CreateContentForMimeType (mimeType, content);
				} finally {
					content.Close ();
				}
				
				if (newContent == null) {
					throw new ApplicationException(String.Format("Created view content was null{3}DefaultName:{0}{3}MimeType:{1}{3}Content:{2}", defaultName, mimeType, content, Environment.NewLine));
				}
				newContent.UntitledName = defaultName;
				newContent.IsDirty = true;
				workbench.ShowView(newContent, true);
				DisplayBindingService.AttachSubWindows (newContent.WorkbenchWindow);
			} else {
				throw new ApplicationException("Can't create display binding for mime type: " + mimeType);				
			}
			
			return WrapDocument (newContent.WorkbenchWindow);
		}
		
		public void ShowGlobalPreferencesDialog (Gtk.Window parentWindow)
		{
			ShowGlobalPreferencesDialog (parentWindow, null);
		}
		
		public void ShowGlobalPreferencesDialog (Gtk.Window parentWindow, string panelId)
		{
			if (parentWindow == null)
				parentWindow = IdeApp.Workbench.RootWindow;

			OptionsDialog ops = new OptionsDialog (
				parentWindow,
				TextEditorProperties.Properties,
				"/MonoDevelop/Ide/GlobalOptionsDialog");

			try {
				if (panelId != null)
					ops.SelectPanel (panelId);
				
				if (MessageService.RunCustomDialog (ops, parentWindow) == (int) Gtk.ResponseType.Ok) {
					PropertyService.SaveProperties ();
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
				
				if (MessageService.RunCustomDialog (ops, parentWindow) == (int) Gtk.ResponseType.Ok) {
					MonoDevelop.Projects.Policies.PolicyService.SaveDefaultPolicies ();
				}
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
					next.Show ();
			}
		}
		
		internal void ShowPrevious ()
		{
			// Shows the previous item in a pad that implements ILocationListPad.
			
			if (activeLocationList != null) {
				NavigationPoint next = activeLocationList.GetPreviousLocation ();
				if (next != null)
					next.Show ();
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
				                                                  GettextCatalog.GetString ("Save the changes to document '{0}' before closing?", window.ViewContent.IsUntitled ? window.ViewContent.UntitledName : System.IO.Path.GetFileName (window.ViewContent.ContentName)), 
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
						}
						catch (Exception ex) {
							args.Cancel = true;
							MessageService.ShowException (ex, GettextCatalog.GetString ("The document could not be saved."));
						}
					}
				} else {
					args.Cancel = result != AlertButton.CloseWithoutSave;
					if (!args.Cancel)
						window.ViewContent.DiscardChanges ();
				}
			}
		}
		
		void OnWindowClosed (object sender, WorkbenchWindowEventArgs args)
		{
			IWorkbenchWindow window = (IWorkbenchWindow) sender;
			window.Closing -= OnWindowClosing;
			window.Closed -= OnWindowClosed;
			documents.Remove (FindDocument (window)); 
		}
		
		void RealOpenFile (FileOpenInformation openFileInfo)
		{
			FilePath fileName;
			IProgressMonitor monitor = openFileInfo.ProgressMonitor;

			using (monitor)
			{
				Counters.OpenDocumentTimer.Trace ("Checking file");
				
				string origName = openFileInfo.FileName;

				if (origName == null) {
					monitor.ReportError (GettextCatalog.GetString ("Invalid file name"), null);
					return;
				}

				if (origName.StartsWith ("file://"))
					fileName = new Uri (origName).LocalPath;
				else
					fileName = origName;

				if (!origName.StartsWith ("http://"))
					fileName = fileName.FullPath;
				
				//Debug.Assert(FileService.IsValidPath(fileName));
				if (FileService.IsDirectory (fileName)) {
					monitor.ReportError (GettextCatalog.GetString ("{0} is a directory", fileName), null);
					return;
				}
				// test, if file fileName exists
				if (!origName.StartsWith("http://")) {
					// test, if an untitled file should be opened
					if (!System.IO.Path.IsPathRooted(origName)) { 
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
				
				foreach (Document doc in Documents) {
					if (doc.FileName == fileName) {
						if (openFileInfo.BringToFront) {
							doc.Select ();
							IEditableTextBuffer ipos = doc.GetContent <IEditableTextBuffer> ();
							if (openFileInfo.Line != -1 && ipos != null) {
								ipos.SetCaretTo (openFileInfo.Line, openFileInfo.Column != -1 ? openFileInfo.Column : 0, openFileInfo.HighlightCaretLine);
							}
						}
						openFileInfo.NewContent = doc.Window.ViewContent;
						return;
					}
				}
				
				Counters.OpenDocumentTimer.Trace ("Looking for binding");
				
				IDisplayBinding binding;
				
				if (openFileInfo.DisplayBinding != null) {
					binding = openFileInfo.DisplayBinding;
				} else {
					binding = DisplayBindingService.GetDefaultBinding (fileName, DesktopService.GetMimeTypeForUri (fileName));
				}
				
				if (binding != null) {
					// When looking for the project to which the file belongs, look first
					// in the active project, then the active solution, and so on
					Project project = null;
					if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
						if (IdeApp.ProjectOperations.CurrentSelectedProject.Files.GetFile (fileName) != null)
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
					
					LoadFileWrapper fw = new LoadFileWrapper (workbench, binding, project, openFileInfo);
					fw.Invoke (fileName);
					
					Counters.OpenDocumentTimer.Trace ("Adding to recent files");
					DesktopService.RecentFiles.AddFile (fileName, project);
				} else {
					try {
						Counters.OpenDocumentTimer.Trace ("Showing in browser");
						DesktopService.OpenFile (fileName);
					} catch (Exception ex) {
						LoggingService.LogError ("Error opening file: " + fileName, ex);
						MessageService.ShowError (GettextCatalog.GetString ("File '{0}' could not be opened", fileName));
					}
				}
			}
		}
		
		void OnStoringWorkspaceUserPreferences (object s, UserPreferencesEventArgs args)
		{
			WorkbenchUserPrefs prefs = new WorkbenchUserPrefs ();
			
			foreach (Document document in Documents) {
				if (!String.IsNullOrEmpty (document.FileName)) {
					DocumentUserPrefs dp = new DocumentUserPrefs ();
					dp.FileName = FileService.AbsoluteToRelativePath (args.Item.BaseDirectory, document.FileName);
					if (document.Editor != null) {
						dp.Line = document.Editor.Caret.Line;
						dp.Column = document.Editor.Caret.Column;
					}
					prefs.Files.Add (dp);
				}
			}
			
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
		
		void OnLoadingWorkspaceUserPreferences (object s, UserPreferencesEventArgs args)
		{
			WorkbenchUserPrefs prefs = args.Properties.GetValue<WorkbenchUserPrefs> ("MonoDevelop.Ide.Workbench");
			if (prefs == null)
				return;
			
			string currentFileName = prefs.ActiveDocument != null ? Path.GetFullPath (Path.Combine (args.Item.BaseDirectory, prefs.ActiveDocument)) : null;
			
			foreach (DocumentUserPrefs doc in prefs.Files) {
				FilePath fileName = args.Item.BaseDirectory.Combine (doc.FileName).FullPath;
				if (File.Exists (fileName))
					IdeApp.Workbench.OpenDocument (fileName, doc.Line, doc.Column, fileName == currentFileName, null, null, false);
			}
			
			foreach (PadUserPrefs pi in prefs.Pads) {
				foreach (Pad pad in IdeApp.Workbench.Pads) {
					if (pi.Id == pad.Id && pad.Content is IMementoCapable) {
						try {
							string xml = pi.State.OuterXml;
							IMementoCapable m = (IMementoCapable) pad.Content; 
							XmlReader innerReader = new XmlTextReader (new StringReader (xml));
							innerReader.MoveToContent ();
							ICustomXmlSerializer cs = (ICustomXmlSerializer)m.Memento;
							if (cs != null)
								m.Memento = cs.ReadFrom (innerReader);
						} catch (Exception ex) {
							LoggingService.LogError ("Error loading view memento.", ex);
						}
						break;
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
							FileData fd = new FileData (file, fi.Exists ? fi.LastWriteTime : DateTime.MinValue);
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
					foreach (FileData fd in fileStatus) {
						try {
							FileInfo fi = new FileInfo (fd.File);
							if (fi.Exists) {
								if (fi.LastWriteTime != fd.Time)
									FileService.NotifyFileChanged (fd.File);
							} else if (fd.Time != DateTime.MinValue) {
								FileService.NotifyFileRemoved (fd.File);
							}
						} catch {
							// Ignore
						}
					}
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
		}
		
		struct FileData
		{
			public FileData (FilePath file, DateTime time)
			{
				this.File = file;
				this.Time = time;
			}
			
			public FilePath File;
			public DateTime Time;
		}
		
		protected virtual void OnDocumentOpened (DocumentEventArgs e)
		{
			EventHandler<DocumentEventArgs> handler = this.DocumentOpened;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<DocumentEventArgs> DocumentOpened;

		public void ReparseOpenDocuments ()
		{
			foreach (var doc in Documents) {
				doc.UpdateParseDocument ();
			}
		}
	}

	public class FileOpenInformation
	{
		public IProgressMonitor ProgressMonitor { get; set; }
		public string FileName { get; set; }
		public bool BringToFront { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
		public IDisplayBinding DisplayBinding { get; set; }
		public IViewContent NewContent { get; set; }
		public string Encoding { get; set; }
		public bool HighlightCaretLine { get; set; }
		
		public FileOpenInformation ()
		{
		}
		
		public FileOpenInformation (string fileName, int line, int column, bool bringToFront) 
		{
			this.FileName = fileName;
			this.Line = line;
			this.Column = column;
			this.BringToFront = bringToFront;
		}
	}
	
	class LoadFileWrapper
	{
		IDisplayBinding binding;
		Project project;
		FileOpenInformation fileInfo;
		DefaultWorkbench workbench;
		IViewContent newContent;
		
		public LoadFileWrapper (DefaultWorkbench workbench, IDisplayBinding binding, FileOpenInformation fileInfo)
		{
			this.workbench = workbench;
			this.fileInfo = fileInfo;
			this.binding = binding;
		}
		
		public LoadFileWrapper (DefaultWorkbench workbench, IDisplayBinding binding, Project project, FileOpenInformation fileInfo)
			: this (workbench, binding, fileInfo)
		{
			this.project = project;
		}
		
		public void Invoke (string fileName)
		{
			try {
				Counters.OpenDocumentTimer.Trace ("Creating content");
				if (binding.CanCreateContentForUri (fileName)) {
					newContent = binding.CreateContentForUri (fileName);
				} else {
					string mimeType = DesktopService.GetMimeTypeForUri (fileName);
					if (!binding.CanCreateContentForMimeType (mimeType)) {
						fileInfo.ProgressMonitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), null);
						return;
					}
					newContent = binding.CreateContentForMimeType (mimeType, null);
				}
				if (newContent == null) {
					fileInfo.ProgressMonitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), null);
					return;
				}
				
				if (project != null)
					newContent.Project = project;
				
				Counters.OpenDocumentTimer.Trace ("Loading file");
				
				IEncodedTextContent etc = newContent.GetContent<IEncodedTextContent> ();
				if (fileInfo.Encoding != null && etc != null)
					etc.Load (fileName, fileInfo.Encoding);
				else
					newContent.Load (fileName);
			} catch (Exception ex) {
				fileInfo.ProgressMonitor.ReportError (GettextCatalog.GetString ("The file '{0}' could not be opened.", fileName), ex);
				return;
			}
			// content got re-used
			if (newContent.WorkbenchWindow != null) {
				newContent.WorkbenchWindow.SelectWindow ();
				fileInfo.NewContent = newContent;
				return;
			}
			
			Counters.OpenDocumentTimer.Trace ("Showing view");
			
			workbench.ShowView (newContent, fileInfo.BringToFront);
			DisplayBindingService.AttachSubWindows (newContent.WorkbenchWindow);
			
			newContent.WorkbenchWindow.DocumentType = binding.Name;
			
			IEditableTextBuffer ipos = newContent.GetContent<IEditableTextBuffer> ();
			if (fileInfo.Line != -1 && ipos != null) {
				GLib.Timeout.Add (10, new GLib.TimeoutHandler (JumpToLine));
			}
			fileInfo.NewContent = newContent;
		}
		
		public bool JumpToLine ()
		{
			IEditableTextBuffer ipos = newContent.GetContent<IEditableTextBuffer> ();
			ipos.SetCaretTo (Math.Max(1, fileInfo.Line), Math.Max(1, fileInfo.Column), fileInfo.HighlightCaretLine);
			return false;
		}
		
	}
}
