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
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core.Gui.Dialogs;
using Mono.Addins;

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
		RecentOpen recentOpen = null;
		
		public event EventHandler ActiveDocumentChanged;
		public event EventHandler LayoutChanged;
		public event EventHandler GuiLocked;
		public event EventHandler GuiUnlocked;
		
		internal void Initialize (IProgressMonitor monitor)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Initializing Main Window"), 4);
			try {
				workbench = new DefaultWorkbench ();
				monitor.Step (1);
				
				workbench.InitializeWorkspace();
				monitor.Step (1);
				
				workbench.InitializeLayout (new SdiWorkbenchLayout ());
				monitor.Step (1);
				
				((Gtk.Window)workbench).Visible = false;
				workbench.ActiveWorkbenchWindowChanged += new EventHandler (OnDocumentChanged);
				PropertyService.PropertyChanged += new EventHandler<PropertyChangedEventArgs> (TrackPropertyChanges);
				FileService.FileRemoved += (EventHandler<FileEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileEventArgs> (IdeApp.Workbench.RecentOpen.InformFileRemoved));
				FileService.FileRenamed += (EventHandler<FileCopyEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileCopyEventArgs> (IdeApp.Workbench.RecentOpen.InformFileRenamed));
				IdeApp.Workspace.StoringUserPreferences += OnStoringWorkspaceUserPreferences;
				IdeApp.Workspace.LoadingUserPreferences += OnLoadingWorkspaceUserPreferences;
				
				pads = null;	// Make sure we get an up to date pad list.
				monitor.Step (1);
			} finally {
				monitor.EndTask ();
			}
		}
		
		/// <remarks>
		/// This method handles the redraw all event for specific changed IDE properties
		/// </remarks>
		void TrackPropertyChanges(object sender, MonoDevelop.Core.PropertyChangedEventArgs e)
		{
			if (e.OldValue != e.NewValue) {
				switch (e.Key) {
					case "MonoDevelop.Core.Gui.VisualStyle":
					case "CoreProperties.UILanguage":
						workbench.RedrawAllComponents();
						break;
				}
			}
		}
		
		internal void Show (string workbenchMemento)
		{
			RootWindow.Realize ();
			workbench.Memento = PropertyService.Get (workbenchMemento, new Properties ());
			RootWindow.Visible = true;
			workbench.Context = WorkbenchContext.Edit;
			
			// now we have an layout set notify it
			if (LayoutChanged != null)
				LayoutChanged (this, EventArgs.Empty);

			workbench.RedrawAllComponents ();
			monitors.Initialize ();
			
			RootWindow.Present ();
		}
		
		internal bool Close ()
		{
			return ((DefaultWorkbench)workbench).Close();
		}
		
		public RecentOpen RecentOpen {
			get {
				if (recentOpen == null)
					recentOpen = new RecentOpen ();
				return recentOpen;
			}
		}
		
		public ReadOnlyCollection<Document> Documents {
			get { return documents.AsReadOnly (); }
		}

		public Document ActiveDocument {
			get {
				if (workbench == null || workbench.ActiveWorkbenchWindow == null)
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
					foreach (PadCodon pc in workbench.ActivePadContentCollection)
						WrapPad (pc);
				}
				return pads;
			}
		}
		
		
		public Gtk.Window RootWindow {
			get { return (Gtk.Window) workbench; }
		}
		
		public WorkbenchContext Context {
			get { return workbench.Context; }
			set {
				if (workbench.Context != value) {
					workbench.Context = value;
					pads = null;
				}
			}
		}
		
		public bool FullScreen {
			get { return workbench.FullScreen; }
			set { workbench.FullScreen = value; }
		}
		
		public string CurrentLayout {
			get { return workbench != null && workbench.WorkbenchLayout != null ? workbench.WorkbenchLayout.CurrentLayout : ""; }
			set {
				if (value != workbench.WorkbenchLayout.CurrentLayout)
				{
					workbench.WorkbenchLayout.CurrentLayout = value;
					if (LayoutChanged != null)
						LayoutChanged (this, EventArgs.Empty);
				}
			}
		}

		public string[] Layouts {
			get { return workbench.WorkbenchLayout != null ? workbench.WorkbenchLayout.Layouts : new string[0]; }
		}
		
		public ProgressMonitorManager ProgressMonitors {
			get { return monitors; }
		}
		
		public MonoDevelopStatusBar StatusBar {
			get {
				return workbench.StatusBar;
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
			workbench.WorkbenchLayout.DeleteLayout (name);
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

		public Pad AddPad (IPadContent padContent, string id, string label, string defaultPlacement, string icon)
		{
			return AddPad (new PadCodon (padContent, id, label, defaultPlacement, icon));
		}
		
		public Pad ShowPad (IPadContent padContent, string id, string label, string defaultPlacement, string icon)
		{
			return ShowPad (new PadCodon (padContent, id, label, defaultPlacement, icon));
		}
		
		public FileViewer[] GetFileViewers (string fileName)
		{
			List<FileViewer> list = new List<FileViewer> ();
			
			string mimeType = IdeApp.Services.PlatformService.GetMimeTypeForUri (fileName);

			foreach (IDisplayBinding bin in DisplayBindingService.GetBindingsForMimeType (mimeType))
				list.Add (new FileViewer (bin));

			foreach (DesktopApplication app in IdeApp.Services.PlatformService.GetAllApplications (mimeType))
				if (app.Command != "monodevelop")
					list.Add (new FileViewer (app));
				
			return list.ToArray ();
		}
		
		public Document OpenDocument (string fileName)
		{
			return OpenDocument (fileName, true);
		}
		
		public Document OpenDocument (string fileName, string encoding)
		{
			return OpenDocument (fileName, -1, -1, true, encoding, null);
		}
		
		public Document OpenDocument (string fileName, bool bringToFront)
		{
			return OpenDocument (fileName, -1, -1, bringToFront);
		}
		
		public Document OpenDocument (string fileName, int line, int column, bool bringToFront)
		{
			return OpenDocument (fileName, line, column, bringToFront, null, null);
		}
		
		public Document OpenDocument (string fileName, int line, int column, bool bringToFront, string encoding)
		{
			return OpenDocument (fileName, line, column, bringToFront, encoding, null);
		}
		
		internal Document OpenDocument (string fileName, int line, int column, bool bringToFront, string encoding, IDisplayBinding binding)
		{
			NavigationHistoryService.LogActiveDocument ();
			
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
						RootWindow.Present ();
					}
					
					IEditableTextBuffer ipos = (IEditableTextBuffer) vcFound.GetContent (typeof(IEditableTextBuffer));
					if (line >= 1 && ipos != null) {
						ipos.SetCaretTo (line, column >= 1 ? column : 1);
					}
					
					NavigationHistoryService.LogActiveDocument ();
					return doc;
				}
			}
			
			IProgressMonitor pm = ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Opening {0}", fileName), Stock.OpenFileIcon, true);
			FileInformation openFileInfo = new FileInformation();
			openFileInfo.ProgressMonitor = pm;
			openFileInfo.FileName = fileName;
			openFileInfo.BringToFront = bringToFront;
			openFileInfo.Line = line;
			openFileInfo.Column = column;
			openFileInfo.DisplayBinding = binding;
			openFileInfo.Encoding = encoding;
			RealOpenFile (openFileInfo);
			
			if (!pm.AsyncOperation.Success)
				return null;
			
			if (openFileInfo.NewContent != null) {
				Document doc = WrapDocument (openFileInfo.NewContent.WorkbenchWindow);
				NavigationHistoryService.LogActiveDocument ();
				if (bringToFront)
					RootWindow.Present ();
				return doc;
			} else {
				return null;
			}
		}
		
		public Document OpenDocument (IViewContent content, bool bringToFront)
		{
			workbench.ShowView (content, bringToFront);
			if (bringToFront)
				RootWindow.Present ();
			return WrapDocument (content.WorkbenchWindow);
		}
		
		public void ToggleMaximize ()
		{
			SdiWorkbenchLayout sdiLayout = this.workbench.WorkbenchLayout as SdiWorkbenchLayout;
			if (sdiLayout != null)
				sdiLayout.ToggleFullViewMode ();
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
			IDisplayBinding binding = DisplayBindingService.GetBinding (null, mimeType);
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
				if (ops.Run () == (int) Gtk.ResponseType.Ok) {
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

			MonoDevelop.Projects.Gui.Dialogs.DefaultPolicyOptionsDialog ops
				= new MonoDevelop.Projects.Gui.Dialogs.DefaultPolicyOptionsDialog (parentWindow);

			try {
				if (panelId != null)
					ops.SelectPanel (panelId);
				if (ops.Run () == (int) Gtk.ResponseType.Ok) {
					MonoDevelop.Projects.Policies.PolicyService.SaveDefaultPolicies ();
				}
			} finally {
				ops.Destroy ();
			}
		}
		
		internal void ShowNext ()
		{
			// Shows the next item in a pad that implements ILocationListPad.
			
			Pad pad = GetLocationListPad ();
			if (pad != null) {
				pad.BringToFront ();
				ILocationListPad loc = (ILocationListPad) pad.Content;
				string file;
				int lin, col;
				if (loc.GetNextLocation (out file, out lin, out col)) {
					if (!string.IsNullOrEmpty (file))
						OpenDocument (file, lin, col, true);
				}
			}
		}
		
		internal void ShowPrevious ()
		{
			// Shows the previous item in a pad that implements ILocationListPad.
			
			Pad pad = GetLocationListPad ();
			if (pad != null) {
				pad.BringToFront ();
				ILocationListPad loc = (ILocationListPad) pad.Content;
				string file;
				int lin, col;
				if (loc.GetPreviousLocation (out file, out lin, out col)) {
					if (!string.IsNullOrEmpty (file))
						OpenDocument (file, lin, col, true);
				}
			}
		}
		
		internal Pad GetLocationListPad ()
		{
			// Locates a pad which implements ILocationListPad. If there are more than
			// one, it returns the last one being focused.
			
			Pad active = null;
			
			foreach (Pad p in IdeApp.Workbench.Pads) {
				if (!p.Visible)
					continue;
				ILocationListPad loc = p.Content as ILocationListPad;
				if (loc != null && (active == null || p.Window == PadWindow.LastActiveLocationList))
					active = p;
			}
			if (active == null)
				return null;
			
			return active;
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
			window.Closing += new WorkbenchWindowEventHandler (OnWindowClosing);
			window.Closed += new EventHandler (OnWindowClosed);
			documents.Add (doc);
			
			doc.OnDocumentAttached ();
			
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
				}
			}
		}
		
		void OnWindowClosed (object sender, EventArgs args)
		{
			IWorkbenchWindow window = (IWorkbenchWindow) sender;
			documents.Remove (FindDocument (window)); 
		}
		
		void RealOpenFile (object openFileInfo)
		{
			string fileName;
			FileInformation oFileInfo = openFileInfo as FileInformation;
			IProgressMonitor monitor = oFileInfo.ProgressMonitor;

			using (monitor)
			{
				fileName = oFileInfo.FileName;
				
				if (fileName == null) {
					monitor.ReportError (GettextCatalog.GetString ("Invalid file name"), null);
					return;
				}
	
				string origName = fileName;
	
				if (fileName.StartsWith ("file://"))
					fileName = new Uri(fileName).LocalPath;
	
				if (!fileName.StartsWith ("http://"))
					fileName = System.IO.Path.GetFullPath (fileName);
				
				//Debug.Assert(FileService.IsValidPath(fileName));
				if (FileService.IsDirectory (fileName)) {
					monitor.ReportError (GettextCatalog.GetString ("{0} is a directory", fileName), null);
					return;
				}
				// test, if file fileName exists
				if (!fileName.StartsWith("http://")) {
					// test, if an untitled file should be opened
					if (!System.IO.Path.IsPathRooted(origName)) { 
						foreach (Document doc in Documents) {
							if (doc.Window.ViewContent.IsUntitled && doc.Window.ViewContent.UntitledName == origName) {
								doc.Select ();
								oFileInfo.NewContent = doc.Window.ViewContent;
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
						if (oFileInfo.BringToFront) {
							doc.Select ();
							IEditableTextBuffer ipos = doc.GetContent <IEditableTextBuffer> ();
							if (oFileInfo.Line != -1 && ipos != null) {
								ipos.SetCaretTo (oFileInfo.Line, oFileInfo.Column != -1 ? oFileInfo.Column : 0);
							}
						}
						oFileInfo.NewContent = doc.Window.ViewContent;
						return;
					}
				}
				
				IDisplayBinding binding;
				
				if (oFileInfo.DisplayBinding != null) {
					binding = oFileInfo.DisplayBinding;
				} else {
					binding = DisplayBindingService.GetBinding (fileName, IdeApp.Services.PlatformService.GetMimeTypeForUri (fileName));
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
					
					LoadFileWrapper fw = new LoadFileWrapper (workbench, binding, project, oFileInfo);
					fw.Invoke (fileName);
					RecentOpen.AddLastFile (fileName, project != null ? project.Name : null);
				} else {
					try {
						// FIXME: this doesn't seem finished yet in Gtk#
						//MimeType mimetype = new MimeType (new Uri ("file://" + fileName));
						//if (mimetype != null) {
						//	mimetype.DefaultAction.Launch ();
						//} else {
							IdeApp.Services.PlatformService.ShowUrl ("file://" + fileName);
						//}
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
					if (document.TextEditor != null) {
						dp.Line = document.TextEditor.CursorLine;
						dp.Column = document.TextEditor.CursorColumn;
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
				string fileName = Path.GetFullPath (Path.Combine (args.Item.BaseDirectory, doc.FileName));
				if (File.Exists (fileName))
					IdeApp.Workbench.OpenDocument (fileName, doc.Line, doc.Column, fileName == currentFileName);
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
			IViewContent content = workbench.ViewContentCollection[oldPlacement];
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
	}
	
	class FileInformation
	{
		public IProgressMonitor ProgressMonitor;
		public string FileName;
		public bool BringToFront;
		public int Line;
		public int Column;
		public IDisplayBinding DisplayBinding;
		public IViewContent NewContent;
		public string Encoding;
	}
	
	class LoadFileWrapper
	{
		IDisplayBinding binding;
		Project project;
		FileInformation fileInfo;
		IWorkbench workbench;
		IViewContent newContent;
		
		public LoadFileWrapper (IWorkbench workbench, IDisplayBinding binding, FileInformation fileInfo)
		{
			this.workbench = workbench;
			this.fileInfo = fileInfo;
			this.binding = binding;
		}
		
		public LoadFileWrapper (IWorkbench workbench, IDisplayBinding binding, Project project, FileInformation fileInfo)
		{
			this.workbench = workbench;
			this.fileInfo = fileInfo;
			this.binding = binding;
			this.project = project;
		}
		
		public void Invoke (string fileName)
		{
			try {
				if (binding.CanCreateContentForUri (fileName)) {
					newContent = binding.CreateContentForUri (fileName);
				} else {
					string mimeType = IdeApp.Services.PlatformService.GetMimeTypeForUri (fileName);
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

				IEncodedTextContent etc = (IEncodedTextContent) newContent.GetContent (typeof(IEncodedTextContent));
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
			if (project != null)
				newContent.Project = project;
			
			workbench.ShowView (newContent, fileInfo.BringToFront);
			DisplayBindingService.AttachSubWindows (newContent.WorkbenchWindow);
			
			newContent.WorkbenchWindow.DocumentType = binding.Name;
			
			IEditableTextBuffer ipos = (IEditableTextBuffer) newContent.GetContent (typeof(IEditableTextBuffer));
			if (fileInfo.Line != -1 && ipos != null) {
				GLib.Timeout.Add (10, new GLib.TimeoutHandler (JumpToLine));
			}
			fileInfo.NewContent = newContent;
		}
		
		public bool JumpToLine ()
		{
			IEditableTextBuffer ipos = (IEditableTextBuffer) newContent.GetContent (typeof(IEditableTextBuffer));
			ipos.SetCaretTo (Math.Max(1, fileInfo.Line), Math.Max(1, fileInfo.Column));
			return false;
		}
	}
}
