//  DefaultWorkbench.cs
//
// Author:
//   Mike Krüger
//   Lluis Sanchez Gual
//
//  This file was derived from a file from #Develop 2.0
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
//  Copyright (C) 2006 Novell, Inc (http://www.novell.com)
// 
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
// 
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;

using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;

using GLib;
using MonoDevelop.Components.DockToolbars;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the a Workspace with a multiple document interface.
	/// </summary>
	internal class DefaultWorkbench : WorkbenchWindow
	{
		readonly static string mainMenuPath    = "/MonoDevelop/Ide/MainMenu";
		readonly static string viewContentPath = "/MonoDevelop/Ide/Pads";
		readonly static string toolbarsPath    = "/MonoDevelop/Ide/Toolbar";
		readonly static string stockLayoutsPath    = "/MonoDevelop/Ide/WorkbenchLayouts";
		
		static string configFile = System.IO.Path.Combine (PropertyService.ConfigPath, "EditingLayout2.xml");
		const string fullViewModeTag = "[FullViewMode]";
		const int MAX_LASTACTIVEWINDOWS = 10;
		
		// list of layout names for the current context, without the context prefix
		List<string> layouts = new List<string> ();
		
		List<PadCodon> padContentCollection      = new List<PadCodon> ();
		List<IViewContent> viewContentCollection = new List<IViewContent> ();
		Dictionary<PadCodon, IPadWindow> padWindows = new Dictionary<PadCodon, IPadWindow> ();
		Dictionary<IPadWindow, PadCodon> padCodons = new Dictionary<IPadWindow, PadCodon> ();
		
		IWorkbenchWindow lastActive;
		LinkedList<IWorkbenchWindow> lastActiveWindows = new LinkedList<IWorkbenchWindow> ();
		
		bool closeAll;
		bool ignorePageSwitch;
		bool isInFullViewMode;

		bool fullscreen;
		Rectangle normalBounds = new Rectangle(0, 0, 640, 480);
		
		internal static GType gtype;
		
		Gtk.Container rootWidget;
		DockToolbarFrame toolbarFrame;
		DockFrame dock;
		SdiDragNotebook tabControl;
		Gtk.MenuBar topMenu;
		Gtk.Toolbar[] toolbars;
		MonoDevelopStatusBar statusBar;
		Gtk.VBox fullViewVBox;
		DockItem documentDockItem;
		
		enum TargetList {
			UriList = 100
		}

		Gtk.TargetEntry[] targetEntryTypes = new Gtk.TargetEntry[] {
			new Gtk.TargetEntry ("text/uri-list", 0, (uint)TargetList.UriList)
		};
		
#if DUMMY_STRINGS_FOR_TRANSLATION_DO_NOT_COMPILE
		private void DoNotCompile ()
		{
			//The default layout, translated indirectly because it's used as an ID
			GettextCatalog.GetString ("Default");
		}
#endif
		
		public event EventHandler ActiveWorkbenchWindowChanged;
		
		public MonoDevelopStatusBar StatusBar {
			get {
				if (statusBar == null)
					statusBar = new MonoDevelop.Ide.MonoDevelopStatusBar ();
				return statusBar;
			}
		}
		
		public Gtk.MenuBar TopMenu {
			get { return topMenu; }
		}
		
		public IWorkbenchWindow ActiveWorkbenchWindow {
			get {
				if (tabControl == null || tabControl.CurrentPage < 0 || tabControl.CurrentPage >= tabControl.NPages)  {
					return null;
				}
				return (IWorkbenchWindow) tabControl.CurrentPageWidget;
			}
		}
		
		public DockFrame DockFrame {
			get { return dock; }
		}
		
		public bool FullScreen {
			get {
				return fullscreen;
			}
			set {
				fullscreen = value;
				if (fullscreen) {
					this.Fullscreen ();
				} else {
					this.Unfullscreen ();
				}
			}
		}

		public string[] Layouts {
			get {
				return layouts.ToArray ();
			}
		}
		
		public string CurrentLayout {
			get {
				if (dock != null && dock.CurrentLayout != null) {
					string s = dock.CurrentLayout;
					s = s.Substring (s.IndexOf (".") + 1);
					if (s.EndsWith (fullViewModeTag))
						return s.Substring (0, s.Length - fullViewModeTag.Length);
					return s;
				}
				else
					return "";
			}
			set {
				// Leave dragging mode, to avoid problems due to widget relocating
				tabControl.LeaveDragMode (0);
				isInFullViewMode = false;
				
				InitializeLayout (value);
				dock.CurrentLayout = value;
				toolbarFrame.CurrentLayout = value;

				// persist the selected layout
				PropertyService.Set ("MonoDevelop.Core.Gui.CurrentWorkbenchLayout", value);
			}
		}
		
		public void DeleteLayout (string name)
		{
			string layout = name;
			layouts.Remove (name);
			dock.DeleteLayout (layout);
		}
		
		public List<PadCodon> PadContentCollection {
			get {
				return padContentCollection;
			}
		}
		
		internal List<IViewContent> InternalViewContentCollection {
			get {
				Debug.Assert(viewContentCollection != null);
				return viewContentCollection;
			}
		}
		
		public DefaultWorkbench()
		{
			Title = "MonoDevelop";
			LoggingService.LogInfo ("Creating DefaultWorkbench");
			
			WidthRequest = normalBounds.Width;
			HeightRequest = normalBounds.Height;

			DeleteEvent += new Gtk.DeleteEventHandler (OnClosing);
			
			if (Gtk.IconTheme.Default.HasIcon ("monodevelop")) 
				Gtk.Window.DefaultIconName = "monodevelop";
			else
				this.IconList = new Gdk.Pixbuf[] {
					ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Menu),
					ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Button),
					ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Dnd),
					ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.MonoDevelop, Gtk.IconSize.Dialog)
				};

			//this.WindowPosition = Gtk.WindowPosition.None;

			Gtk.Drag.DestSet (this, Gtk.DestDefaults.Motion | Gtk.DestDefaults.Highlight | Gtk.DestDefaults.Drop, targetEntryTypes, Gdk.DragAction.Copy);
			DragDataReceived += new Gtk.DragDataReceivedHandler (onDragDataRec);
			
			IdeApp.CommandService.SetRootWindow (this);
		}

		void onDragDataRec (object o, Gtk.DragDataReceivedArgs args)
		{
			if (args.Info != (uint) TargetList.UriList)
				return;
			string fullData = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);

			foreach (string individualFile in fullData.Split ('\n')) {
				string file = individualFile.Trim ();
				if (file.StartsWith ("file://")) {
					file = new Uri(file).LocalPath;

					try {
						if (Services.ProjectService.IsWorkspaceItemFile (file))
							IdeApp.Workspace.OpenWorkspaceItem(file);
						else
							IdeApp.Workbench.OpenDocument (file);
					} catch (Exception e) {
						LoggingService.LogError ("unable to open file {0} exception was :\n{1}", file, e.ToString());
					}
				}
			}
		}
		
		public void InitializeWorkspace()
		{
			// FIXME: GTKize
			IdeApp.ProjectOperations.CurrentProjectChanged += (ProjectEventHandler) DispatchService.GuiDispatch (new ProjectEventHandler(SetProjectTitle));

			FileService.FileRemoved += (EventHandler<FileEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileEventArgs>(CheckRemovedFile));
			FileService.FileRenamed += (EventHandler<FileCopyEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileCopyEventArgs>(CheckRenamedFile));
			
//			TopMenu.Selected   += new CommandHandler(OnTopMenuSelected);
//			TopMenu.Deselected += new CommandHandler(OnTopMenuDeselected);
			
			if (!DesktopService.SetGlobalMenu (IdeApp.CommandService, mainMenuPath))
				topMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
			
			toolbars = IdeApp.CommandService.CreateToolbarSet (toolbarsPath);
			foreach (Gtk.Toolbar t in toolbars) {
				t.ToolbarStyle = Gtk.ToolbarStyle.Icons;
				t.IconSize = IdeApp.Preferences.ToolbarSize;
			}
			IdeApp.Preferences.ToolbarSizeChanged += delegate {
				foreach (Gtk.Toolbar t in toolbars) {
					t.IconSize = IdeApp.Preferences.ToolbarSize;
				}
			};
				
			AddinManager.ExtensionChanged += OnExtensionChanged;
		}
		
		void OnExtensionChanged (object s, ExtensionEventArgs args)
		{
			if (args.PathChanged (mainMenuPath)) {
				if (DesktopService.SetGlobalMenu (IdeApp.CommandService, mainMenuPath))
					return;
				
				UninstallMenuBar ();
				topMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
				InstallMenuBar ();
			}
			
			if (args.PathChanged (toolbarsPath)) {
				toolbars = IdeApp.CommandService.CreateToolbarSet (toolbarsPath);
				string cl = toolbarFrame.CurrentLayout;
				DockToolbarFrameStatus mem = toolbarFrame.GetStatus ();
				toolbarFrame.ClearToolbars ();
				foreach (DockToolbar tb in toolbars) {
					tb.ToolbarStyle = Gtk.ToolbarStyle.Icons;
					tb.ShowAll ();
					toolbarFrame.AddBar (tb);
				}
				toolbarFrame.SetStatus (mem);
				toolbarFrame.CurrentLayout = cl;
			}
		}
		
		void InstallMenuBar ()
		{
			if (topMenu != null) {
				((VBox)rootWidget).PackStart (topMenu, false, false, 0);
				((Gtk.Box.BoxChild) rootWidget [topMenu]).Position = 0;
				topMenu.ShowAll ();
			}
		}
				
		void UninstallMenuBar ()
		{
			if (topMenu == null)
				return;
			
			rootWidget.Remove (topMenu);
			topMenu = null;
		}
		
		public void CloseContent (IViewContent content)
		{
			if (viewContentCollection.Contains(content)) {
				viewContentCollection.Remove(content);
			}
		}
		
		public void CloseAllViews()
		{
			try {
				closeAll = true;
				List<IViewContent> fullList = new List<IViewContent>(viewContentCollection);
				foreach (IViewContent content in fullList) {
					IWorkbenchWindow window = content.WorkbenchWindow;
					window.CloseWindow(true, true, 0);
				}
			} finally {
				closeAll = false;
				OnActiveWindowChanged (null, null);
			}
		}
		
		public virtual void ShowView (IViewContent content, bool bringToFront)
		{
			viewContentCollection.Add (content);
			
			if (PropertyService.Get ("SharpDevelop.LoadDocumentProperties", true) && content is IMementoCapable) {
				try {
					Properties memento = GetStoredMemento(content);
					if (memento != null) {
						((IMementoCapable)content).Memento = memento;
					}
				} catch (Exception e) {
					LoggingService.LogError ("Can't get/set memento : " + e.ToString());
				}
			}
			
			Gtk.Image mimeimage = null;
			
			if (content.StockIconId != null ) {
				mimeimage = new Gtk.Image ((IconId) content.StockIconId, IconSize.Menu );
			}
			else if (content.IsUntitled && content.UntitledName == null) {
				mimeimage = new Gtk.Image (DesktopService.GetPixbufForType ("gnome-fs-regular", Gtk.IconSize.Menu));
			} else {
				mimeimage = new Gtk.Image (DesktopService.GetPixbufForFile (content.ContentName ?? content.UntitledName, Gtk.IconSize.Menu));
			}			

			TabLabel tabLabel = new TabLabel (new Label (), mimeimage != null ? mimeimage : new Gtk.Image (""));
			tabLabel.CloseClicked += new EventHandler (closeClicked);			
			tabLabel.ClearFlag (WidgetFlags.CanFocus);
			SdiWorkspaceWindow sdiWorkspaceWindow = new SdiWorkspaceWindow (this, content, tabControl, tabLabel);
			sdiWorkspaceWindow.TitleChanged += delegate { SetWorkbenchTitle (); };
			sdiWorkspaceWindow.Closed += CloseWindowEvent;
			tabControl.InsertPage (sdiWorkspaceWindow, tabLabel, -1);
			tabLabel.Show ();
			
			if (bringToFront)
				content.WorkbenchWindow.SelectWindow();
		}
		
		void ShowPadNode (ExtensionNode node)
		{
			if (node is PadCodon) {
				PadCodon pad = (PadCodon) node;
				RegisterPad (pad);
			}
			else if (node is CategoryNode) {
				foreach (ExtensionNode cn in node.ChildNodes)
					ShowPadNode (cn);
			}
		}
		
		void RemovePadNode (ExtensionNode node)
		{
			if (node is PadCodon)
				RemovePad ((PadCodon) node);
			else if (node is CategoryNode) {
				foreach (ExtensionNode cn in node.ChildNodes)
					RemovePadNode (cn);
			}
		}
		
		public void ShowPad (PadCodon content)
		{
			AddPad (content, true);
		}
		
		public void AddPad (PadCodon content)
		{
			AddPad (content, false);
		}
		
		void RegisterPad (PadCodon content)
		{
			if (content.HasId) {
				ActionCommand cmd = new ActionCommand ("Pad|" + content.PadId, GettextCatalog.GetString (content.Label), null);
				cmd.DefaultHandler = new PadActivationHandler (this, content);
				cmd.Category = GettextCatalog.GetString ("View");
				cmd.Description = GettextCatalog.GetString ("Show {0}", cmd.Text);
				IdeApp.CommandService.RegisterCommand (cmd);
			}
			padContentCollection.Add (content);
		}
		
		void AddPad (PadCodon content, bool show)
		{
			DockItem item = GetDockItem (content);
			if (padContentCollection.Contains (content)) {
				if (show && item != null)
					item.Visible = true;
				return;
			}

			RegisterPad (content);
			
			if (item != null) {
				if (show)
					item.Visible = true;
			} else {
				AddPad (content, content.DefaultPlacement, content.DefaultStatus);
			}
		}
		
		public void RemovePad (PadCodon codon)
		{
			if (codon.HasId) {
				Command cmd = IdeApp.CommandService.GetCommand (codon.Id);
				if (cmd != null)
					IdeApp.CommandService.UnregisterCommand (cmd);
			}
			DockItem item = GetDockItem (codon);
			padContentCollection.Remove (codon);
			PadWindow win = (PadWindow) padWindows [codon];
			win.NotifyDestroyed ();
			if (item != null)
				dock.RemoveItem (item);
			padWindows.Remove (codon);
			padCodons.Remove (win);
			
			Counters.PadsLoaded--;
		}
		
		public void BringToFront (PadCodon content)
		{
			BringToFront (content, false);
		}
		
		public virtual void BringToFront (PadCodon content, bool giveFocus)
		{
			if (!IsVisible (content))
				ShowPad (content);

			ActivatePad (content, giveFocus);
		}
		
		void SetWorkbenchTitle ()
		{
			try {
				IWorkbenchWindow window = ActiveWorkbenchWindow;
				if (window != null) {
					if (window.ViewContent.IsUntitled) {
						SetDefaultTitle ();
					} else {
						string post = String.Empty;
						if (window.ViewContent.IsDirty) {
							post = "*";
						}
						if (window.ViewContent.Project != null) {
							Title = window.ViewContent.Project.Name + " - " + window.ViewContent.PathRelativeToProject + post + " - MonoDevelop";
						} else {
							Title = window.ViewContent.ContentName + post + " - MonoDevelop";
						}
					}
				} else {
					SetDefaultTitle ();
					if (isInFullViewMode)
						this.ToggleFullViewMode ();
				}
			} catch (Exception) {
				SetDefaultTitle ();
			}
		}
		
		void SetDefaultTitle ()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
				Title = IdeApp.ProjectOperations.CurrentSelectedProject.Name + " - MonoDevelop";
			} else {
				Title = "MonoDevelop";
			}
		}
		
		public Properties GetStoredMemento(IViewContent content)
		{
			if (content != null && content.ContentName != null) {
				string directory = System.IO.Path.Combine (PropertyService.ConfigPath, "temp");
				if (!Directory.Exists(directory)) {
					Directory.CreateDirectory(directory);
				}
				string fileName = content.ContentName.Substring(3).Replace('/', '.').Replace('\\', '.').Replace(System.IO.Path.DirectorySeparatorChar, '.');
				string fullFileName = directory + System.IO.Path.DirectorySeparatorChar + fileName;
				// check the file name length because it could be more than the maximum length of a file name
				if (FileService.IsValidPath(fullFileName) && File.Exists(fullFileName)) {
					return Properties.Load (fullFileName);
				}
			}
			return null;
		}
		
		public ICustomXmlSerializer Memento {
			get {
				WorkbenchMemento memento   = new WorkbenchMemento (new Properties ());
				int x, y, width, height;
				GetPosition (out x, out y);
				GetSize (out width, out height);
				if (GdkWindow.State == 0) {
					memento.Bounds = new Rectangle (x, y, width, height);
				} else {
					memento.Bounds = normalBounds;
				}
				memento.WindowState = GdkWindow.State;
				memento.FullScreen  = fullscreen;
				memento.ToolbarStatus = toolbarFrame.GetStatus ();
				return memento.ToProperties ();
			}
			set {
				if (value != null) {
					WorkbenchMemento memento = new WorkbenchMemento ((Properties)value);
					
					normalBounds = memento.Bounds;
					Move (normalBounds.X, normalBounds.Y);
					Resize (normalBounds.Width, normalBounds.Height);
					if (memento.WindowState == Gdk.WindowState.Maximized) {
						Maximize ();
					} else if (memento.WindowState == Gdk.WindowState.Iconified) {
						Iconify ();
					}
					//GdkWindow.State = memento.WindowState;
					FullScreen = memento.FullScreen;
					toolbarFrame.SetStatus (memento.ToolbarStatus);
				}
				Decorated = true;
			}
		}
		
		void CheckRemovedFile(object sender, FileEventArgs e)
		{
			if (e.IsDirectory) {
				IViewContent[] views = new IViewContent [viewContentCollection.Count];
				viewContentCollection.CopyTo (views, 0);
				foreach (IViewContent content in views) {
					if (content.ContentName.StartsWith(e.FileName)) {
						content.WorkbenchWindow.CloseWindow(true, true, 0);
					}
				}
			} else {
				foreach (IViewContent content in viewContentCollection) {
					if (content.ContentName != null &&
					    content.ContentName == e.FileName) {
						content.WorkbenchWindow.CloseWindow(true, true, 0);
						return;
					}
				}
			}
		}
		
		void CheckRenamedFile(object sender, FileCopyEventArgs e)
		{
			if (e.IsDirectory) {
				foreach (IViewContent content in viewContentCollection) {
					if (content.ContentName != null && content.ContentName.StartsWith(e.SourceFile)) {
						content.ContentName = e.TargetFile + content.ContentName.Substring(e.SourceFile.Length);
					}
				}
			} else {
				foreach (IViewContent content in viewContentCollection) {
					if (content.ContentName != null &&
					    content.ContentName == e.SourceFile) {
						content.ContentName = e.TargetFile;
						return;
					}
				}
			}
		}
		
		protected /*override*/ void OnClosing(object o, Gtk.DeleteEventArgs e)
		{
			if (Close()) {
				Gtk.Application.Quit ();
			} else {
				e.RetVal = true;
			}
		}
		
		protected /*override*/ void OnClosed(EventArgs e)
		{
			dock.SaveLayouts (configFile);
			UninstallMenuBar ();
			Remove (rootWidget);
			
			foreach (PadCodon content in PadContentCollection) {
				if (content.Initialized)
					content.PadContent.Dispose();
			}
		}
		
		public bool Close() 
		{
			if (!IdeApp.OnExit ())
				return false;

			IdeApp.Workspace.SavePreferences ();
			IdeApp.CommandService.Dispose ();

			bool showDirtyDialog = false;

			foreach (IViewContent content in viewContentCollection)
			{
				if (content.IsDirty) {
					showDirtyDialog = true;
					break;
				}
			}

			if (showDirtyDialog) {
				DirtyFilesDialog dlg = new DirtyFilesDialog ();
				dlg.Modal = true;
				dlg.TransientFor = this;
				int response = dlg.Run ();
				if (response != (int)Gtk.ResponseType.Ok)
					return false;
			}
			
			if (!IdeApp.Workspace.Close (false))
				return false;
			
			CloseAllViews ();
			
			PropertyService.Set ("SharpDevelop.Workbench.WorkbenchMemento", this.Memento);
			IdeApp.OnExited ();
			OnClosed (null);
			return true;
		}
		
		void SetProjectTitle(object sender, ProjectEventArgs e)
		{
			SetWorkbenchTitle ();
		}
		
		void OnActiveWindowChanged (object sender, EventArgs e)
		{
			if (ignorePageSwitch)
				return;
			
			if (lastActive == ActiveWorkbenchWindow)
				return;
			
			if (lastActiveWindows.Count > MAX_LASTACTIVEWINDOWS)
				lastActiveWindows.RemoveFirst ();
			lastActiveWindows.AddLast (lastActive);
			lastActive = ActiveWorkbenchWindow;
			SetWorkbenchTitle ();
			
			if (!closeAll && ActiveWorkbenchWindowChanged != null) {
				ActiveWorkbenchWindowChanged(this, e);
			}
		}
		
		public Gtk.Toolbar[] ToolBars {
			get { return toolbars; }
		}
		
		public PadCodon GetPad(Type type)
		{
			foreach (PadCodon pad in PadContentCollection) {
				if (pad.ClassName == type.FullName) {
					return pad;
				}
			}
			return null;
		}
		
		public PadCodon GetPad(string id)
		{
			foreach (PadCodon pad in PadContentCollection) {
				if (pad.PadId == id) {
					return pad;
				}
			}
			return null;
		}
		
		public void InitializeLayout ()
		{
			AddinManager.AddExtensionNodeHandler (stockLayoutsPath, OnLayoutsExtensionChanged);
			
			ExtensionNodeList padCodons = AddinManager.GetExtensionNodes (viewContentPath);
			foreach (ExtensionNode node in padCodons)
				ShowPadNode (node);
			
			CreateComponents ();
			
			// Subscribe to changes in the extension
			initializing = true;
			AddinManager.AddExtensionNodeHandler (viewContentPath, OnExtensionChanged);
			initializing = false;
		}
		
		void CreateComponents ()
		{
			fullViewVBox = new VBox (false, 0);
			rootWidget = fullViewVBox;
			
			InstallMenuBar ();
			
			toolbarFrame = new CommandFrame (IdeApp.CommandService);
			fullViewVBox.PackStart (toolbarFrame, true, true, 0);
			
			foreach (DockToolbar t in toolbars)
				toolbarFrame.AddBar (t);
			
			// Create the docking widget and add it to the window.
			dock = new DockFrame ();
			
			dock.CompactGuiLevel = ((int)IdeApp.Preferences.WorkbenchCompactness) + 1;
			IdeApp.Preferences.WorkbenchCompactnessChanged += delegate {
				dock.CompactGuiLevel = ((int)IdeApp.Preferences.WorkbenchCompactness) + 1;
			};
			
			/* Side bar is experimental. Disabled for now
			HBox hbox = new HBox ();
			VBox sideBox = new VBox ();
			sideBox.PackStart (new SideBar (workbench, Orientation.Vertical), false, false, 0);
			hbox.PackStart (sideBox, false, false, 0);
			hbox.ShowAll ();
			sideBox.NoShowAll = true;
			hbox.PackStart (dock, true, true, 0);
			DockBar bar = dock.ExtractDockBar (PositionType.Left);
			bar.AlwaysVisible = true;
			sideBox.PackStart (bar, true, true, 0);
			toolbarFrame.AddContent (hbox);
			*/

			toolbarFrame.AddContent (dock);
			
			// Create the notebook for the various documents.
			tabControl = new SdiDragNotebook (dock.ShadedContainer);
			tabControl.Scrollable = true;
			tabControl.SwitchPage += OnActiveWindowChanged;
			tabControl.PageAdded += delegate { OnActiveWindowChanged (null, null); };
			tabControl.PageRemoved += delegate { OnActiveWindowChanged (null, null); };
		
			tabControl.ButtonPressEvent += delegate(object sender, ButtonPressEventArgs e) {
				int tab = tabControl.FindTabAtPosition (e.Event.XRoot, e.Event.YRoot);
				if (tab < 0)
					return;
				tabControl.CurrentPage = tab;
				if (e.Event.Type == Gdk.EventType.TwoButtonPress)
					ToggleFullViewMode ();
			};
			
			this.tabControl.PopupMenu += delegate {
				ShowPopup ();
			};
			this.tabControl.ButtonReleaseEvent += delegate (object sender, Gtk.ButtonReleaseEventArgs e) {
				int tab = tabControl.FindTabAtPosition (e.Event.XRoot, e.Event.YRoot);
				if (tab < 0)
					return;
				if (e.Event.Button == 3)
					ShowPopup ();
			};
			
			tabControl.TabsReordered += new TabsReorderedHandler (OnTabsReordered);

			// The main document area
			documentDockItem = dock.AddItem ("Documents");
			documentDockItem.Behavior = DockItemBehavior.Locked;
			documentDockItem.Expand = true;
			documentDockItem.DrawFrame = false;
			documentDockItem.Label = GettextCatalog.GetString ("Documents");
			documentDockItem.Content = tabControl;
			
			// Add some hiden items to be used as position reference
			DockItem dit = dock.AddItem ("__left");
			dit.DefaultLocation = "Documents/Left";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;
			
			dit = dock.AddItem ("__right");
			dit.DefaultLocation = "Documents/Right";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;
			
			dit = dock.AddItem ("__top");
			dit.DefaultLocation = "Documents/Top";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;
			
			dit = dock.AddItem ("__bottom");
			dit.DefaultLocation = "Documents/Bottom";
			dit.Behavior = DockItemBehavior.Locked;
			dit.DefaultVisible = false;

			Add (fullViewVBox);
			fullViewVBox.ShowAll ();
			
			fullViewVBox.PackEnd (this.StatusBar, false, true, 0);
			
			if (MonoDevelop.Core.PropertyService.IsMac)
				this.StatusBar.HasResizeGrip = true;
			else {
				if (GdkWindow != null && GdkWindow.State == Gdk.WindowState.Maximized)
					IdeApp.Workbench.StatusBar.HasResizeGrip = false;
				SizeAllocated += delegate {
					if (GdkWindow != null)
						IdeApp.Workbench.StatusBar.HasResizeGrip = GdkWindow.State != Gdk.WindowState.Maximized;
				};
			}

			// create DockItems for all the pads
			foreach (PadCodon content in padContentCollection)
				AddPad (content, content.DefaultPlacement, content.DefaultStatus);
			
			try {
				if (System.IO.File.Exists (configFile)) {
					dock.LoadLayouts (configFile);
					foreach (string layout in dock.Layouts) {
						if (!layouts.Contains (layout))
							layouts.Add (layout);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
			CurrentLayout = "Default";
		}
		
		void InitializeLayout (string name)
		{
			if (!layouts.Contains (name))
				layouts.Add (name);
			
			if (dock.Layouts.Contains (name))
				return;
			
			dock.CreateLayout (name, true);
			dock.CurrentLayout = name;
			documentDockItem.Visible = true;
			
			LayoutExtensionNode stockLayout = null;
			foreach (LayoutExtensionNode node in AddinManager.GetExtensionNodes (stockLayoutsPath)) {
				if (node.Name == name) {
					stockLayout = node;
					break;
				}
			}
			
			if (stockLayout == null)
				return;
			
			HashSet<string> visible = new HashSet<string> ();
			
			foreach (LayoutPadExtensionNode pad in stockLayout.ChildNodes) {
				DockItem it = dock.GetItem (pad.Id);
				if (it != null) {
					it.Visible = true;
					string loc = pad.Placement ?? it.DefaultLocation;
					if (!string.IsNullOrEmpty (loc))
					    it.SetDockLocation (ToDockLocation (loc));
					DockItemStatus stat = pad.StatusSet ? pad.Status : it.DefaultStatus;
					it.Status = stat;
					visible.Add (pad.Id);
				}
			}
			
			foreach (PadCodon node in padContentCollection) {
				if (!visible.Contains (node.Id) && node.DefaultLayouts != null && (node.DefaultLayouts.Contains (stockLayout.Id) || node.DefaultLayouts.Contains ("*"))) {
					DockItem it = dock.GetItem (node.Id);
					if (it != null) {
						it.Visible = true;
						if (!string.IsNullOrEmpty (node.DefaultPlacement))
							it.SetDockLocation (ToDockLocation (node.DefaultPlacement));
						it.Status = node.DefaultStatus;
						visible.Add (node.Id);
					}
				}
			}
			
			foreach (DockItem it in dock.GetItems ()) {
				if (!visible.Contains (it.Id) && ((it.Behavior & DockItemBehavior.Sticky) == 0) && it != documentDockItem)
					it.Visible = false;
			}
		}
		
		void ShowPopup ()
		{
			Gtk.Menu contextMenu = IdeApp.CommandService.CreateMenu ("/MonoDevelop/Ide/ContextMenu/DocumentTab");
			if (contextMenu != null)
				contextMenu.Popup ();
		}
		
		void OnTabsReordered (Widget widget, int oldPlacement, int newPlacement)
		{
			IdeApp.Workbench.ReorderDocuments (oldPlacement, newPlacement);
		}
		
		public void ResetToolbars ()
		{
			toolbarFrame.ResetToolbarPositions ();
		}
		
		public void ToggleFullViewMode ()
		{
			isInFullViewMode = !isInFullViewMode;
			this.tabControl.LeaveDragMode (0);
			
			if (isInFullViewMode) {
				string fullViewLayout = "Edit." + CurrentLayout + fullViewModeTag;
				if (!dock.HasLayout (fullViewLayout))
					dock.CreateLayout (fullViewLayout, true);
				dock.CurrentLayout = fullViewLayout;
				foreach (DockItem it in dock.GetItems ()) {
					if (it.Behavior != DockItemBehavior.Locked && it.Visible)
						it.Status = DockItemStatus.AutoHide;
				}
			} else {
				dock.CurrentLayout = "Edit." + CurrentLayout;
			}
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			//FIXME: Mac-ify this. The control key use is hardcoded into DocumentSwitcher too
			Gdk.ModifierType tabSwitchModifier = Gdk.ModifierType.ControlMask;
			
			// Handle Alt+1-0 keys
			Gdk.ModifierType winSwitchModifier = PropertyService.IsMac
				? KeyBindingManager.SelectionModifierControl
				: KeyBindingManager.SelectionModifierAlt;
			
			if ((evnt.State & winSwitchModifier) != 0) {		
				switch (evnt.Key) {
				case Gdk.Key.KP_1:
				case Gdk.Key.Key_1:
					SwitchToDocument (0);
					return true;
				case Gdk.Key.KP_2:
				case Gdk.Key.Key_2:
					SwitchToDocument (1);
					return true;
				case Gdk.Key.KP_3:
				case Gdk.Key.Key_3:
					SwitchToDocument (2);
					return true;
				case Gdk.Key.KP_4:
				case Gdk.Key.Key_4:
					SwitchToDocument (3);
					return true;
				case Gdk.Key.KP_5:
				case Gdk.Key.Key_5:
					SwitchToDocument (4);
					return true;
				case Gdk.Key.KP_6:
				case Gdk.Key.Key_6:
					SwitchToDocument (5);
					return true;
				case Gdk.Key.KP_7:
				case Gdk.Key.Key_7:
					SwitchToDocument (6);
					return true;
				case Gdk.Key.KP_8:
				case Gdk.Key.Key_8:
					SwitchToDocument (7);
					return true;
				case Gdk.Key.KP_9:
				case Gdk.Key.Key_9:
					SwitchToDocument (8);
					return true;
				case Gdk.Key.KP_0:
				case Gdk.Key.Key_0:
					SwitchToDocument (9);
					return true;
				}
			}
			return base.OnKeyPressEvent (evnt); 
		}
		
		void SwitchToDocument (int number)
		{
			if (number >= viewContentCollection.Count || number < 0)
				return;
			viewContentCollection[number].WorkbenchWindow.SelectWindow ();
		}
		
		bool initializing;
		
		void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (initializing)
				return;
			
			if (args.Change == ExtensionChange.Add) {
				ShowPadNode (args.ExtensionNode);
			}
			else {
				RemovePadNode (args.ExtensionNode);
			}
		}
		
		void OnLayoutsExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				layouts.Add (((LayoutExtensionNode)args.ExtensionNode).Name);
			else
				layouts.Remove (((LayoutExtensionNode)args.ExtensionNode).Name);
		}
		
		#region View management
		
		bool SelectLastActiveWindow (IWorkbenchWindow cur)
		{
			if (lastActiveWindows.Count == 0)
				return false;
			IWorkbenchWindow last = null;
			do {
				last = lastActiveWindows.Last.Value;
				lastActiveWindows.RemoveLast ();
			} while (lastActiveWindows.Count > 0 && (last == cur || last == null || (last != null && last.ViewContent == null)));
			if (last != null) {
				last.SelectWindow ();
				return true;
			}
			return false;
		}
		
		void CloseWindowEvent (object sender, WorkbenchWindowEventArgs e)
		{
			SdiWorkspaceWindow f = (SdiWorkspaceWindow) sender;
			
			// Unsubscribe events to avoid memory leaks
			f.TabLabel.CloseClicked -= new EventHandler (closeClicked);
			
			if (f.ViewContent != null) {
				CloseContent (f.ViewContent);
				if (e.WasActive && !SelectLastActiveWindow (f))
					OnActiveWindowChanged(this, null);
			}
			lastActiveWindows.Remove (f);
		}
		
		void closeClicked (object o, EventArgs e)
		{
			Widget tabLabel = ((Widget)o);
			foreach (Widget child in tabControl.Children) {
				if (tabControl.GetTabLabel (child) == tabLabel) {
					int pageNum = tabControl.PageNum (child);
					((SdiWorkspaceWindow)child).CloseWindow (false, false, pageNum);
					break;
				}
			}
		}

		internal void RemoveTab (int pageNum) {
			try {
				// Weird switch page events are fired when a tab is removed.
				// This flag avoids unneeded events.
				ignorePageSwitch = true;
				IWorkbenchWindow w = ActiveWorkbenchWindow;
				tabControl.RemovePage (pageNum);
				ignorePageSwitch = false;
				if (w != ActiveWorkbenchWindow)
					OnActiveWindowChanged (null, null);
			} finally {
				ignorePageSwitch = false;
			}
		}
		
		#endregion

		#region Dock Item management
		
		public IPadWindow GetPadWindow (PadCodon content)
		{
			IPadWindow w;
			padWindows.TryGetValue (content, out w);
			return w;
		}
		
		public bool IsVisible (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				return item.Visible;
			return false;
		}
		
		public bool IsContentVisible (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				return item.ContentVisible;
			return false;
		}
		
		public void HidePad (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null) 
				item.Visible = false;
		}
		
		public void ActivatePad (PadCodon padContent, bool giveFocus)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null)
				item.Present (giveFocus);
		}
		
		public bool IsSticky (PadCodon padContent)
		{
			DockItem item = GetDockItem (padContent);
			return item != null && (item.Behavior & DockItemBehavior.Sticky) != 0;
		}

		public void SetSticky (PadCodon padContent, bool sticky)
		{
			DockItem item = GetDockItem (padContent);
			if (item != null) {
				if (sticky)
					item.Behavior |= DockItemBehavior.Sticky;
				else
					item.Behavior &= ~DockItemBehavior.Sticky;
			}
		}
		
		internal DockItem GetDockItem (PadCodon content)
		{
			if (padContentCollection.Contains (content)) {
				DockItem item = dock.GetItem (content.PadId);
				return item;
			}
			return null;
		}
		
		void CreatePadContent (bool force, PadCodon padCodon, PadWindow window, DockItem item)
		{
			if (force || item.Content == null) {
				IPadContent newContent = padCodon.InitializePadContent (window);

				Gtk.Widget pcontent;
				if (newContent is Widget) {
					pcontent = newContent.Control;
				} else {
					PadCommandRouterContainer crc = new PadCommandRouterContainer (window, newContent.Control, newContent, true);
					crc.Show ();
					pcontent = crc;
				}
				
				PadCommandRouterContainer router = new PadCommandRouterContainer (window, pcontent, toolbarFrame, false);
				router.Show ();
				item.Content = router;
			}
		}
		
		string ToDockLocation (string loc)
		{
			string location = "";
			foreach (string s in loc.Split (' ')) {
				if (string.IsNullOrEmpty (s))
					continue;
				if (location.Length > 0)
					location += ";";
				if (s.IndexOf ('/') == -1)
					location += "__" + s.ToLower () + "/CenterBefore";
				else
					location += s;
			}
			return location;
		}
		
		void AddPad (PadCodon padCodon, string placement, DockItemStatus defaultStatus)
		{
			PadWindow window = new PadWindow (this, padCodon);
			window.Icon = padCodon.Icon;
			padWindows [padCodon] = window;
			padCodons [window] = padCodon;
			
			window.StatusChanged += new EventHandler (UpdatePad);
			
			string location = ToDockLocation (placement);
			
			DockItem item = dock.AddItem (padCodon.PadId);
			item.Label = GettextCatalog.GetString (padCodon.Label);
			item.Icon = ImageService.GetPixbuf (padCodon.Icon, IconSize.Menu);
			item.DefaultLocation = location;
			item.DefaultVisible = false;
			item.DefaultStatus = defaultStatus;
			item.DockLabelProvider = padCodon;
			window.Item = item;
			
			if (padCodon.Initialized) {
				CreatePadContent (true, padCodon, window, item);
			} else {
				item.ContentRequired += delegate {
					CreatePadContent (false, padCodon, window, item);
				};
			}
			
			item.VisibleChanged += delegate {
				if (item.Visible)
					window.NotifyShown ();
				else
					window.NotifyHidden ();
			};
			
			item.ContentVisibleChanged += delegate {
				if (item.ContentVisible)
					window.NotifyContentShown ();
				else
					window.NotifyContentHidden ();
			};
			
			if (!padContentCollection.Contains (padCodon))
				padContentCollection.Add (padCodon);
		}
		
		void UpdatePad (object source, EventArgs args)
		{
			IPadWindow window = (IPadWindow) source;
			if (!padCodons.ContainsKey (window)) 
				return;
			PadCodon codon = padCodons [window];
			DockItem item = GetDockItem (codon);
			if (item != null) {
				string windowTitle = GettextCatalog.GetString (window.Title); 
				if (String.IsNullOrEmpty (windowTitle)) 
					windowTitle = GettextCatalog.GetString (codon.Label);
				if (window.IsWorking)
					windowTitle = "<span foreground='blue'>" + windowTitle + "</span>";
				else if (window.HasErrors && !window.ContentVisible)
					windowTitle = "<span foreground='red'>" + windowTitle + "</span>";
				else if (window.HasNewData && !window.ContentVisible)
					windowTitle = "<b>" + windowTitle + "</b>";
				item.Label = windowTitle;
				item.Icon  = ImageService.GetPixbuf (window.Icon, IconSize.Menu);
			}
		}
		
		#endregion
	}

	class PadActivationHandler: CommandHandler
	{
		PadCodon pad;
		DefaultWorkbench wb;
		
		public PadActivationHandler (DefaultWorkbench wb, PadCodon pad)
		{
			this.pad = pad;
			this.wb = wb;
		}
		
		protected override void Run ()
		{
			wb.BringToFront (pad, true);
		}
	}

	class PadCommandRouterContainer: CommandRouterContainer
	{
		public PadCommandRouterContainer (PadWindow window, Gtk.Widget child, object target, bool continueToParent): base (child, target, continueToParent)
		{
		}
	}
	
	// The SdiDragNotebook class allows redirecting the command route to the ViewCommandHandler
	// object of the selected document, which implement some default commands.
	
	class SdiDragNotebook: DragNotebook, ICommandDelegatorRouter, IShadedWidget
	{
		ShadedContainer shadedContainer;
		
		public SdiDragNotebook (ShadedContainer shadedContainer)
		{
			this.shadedContainer = shadedContainer;
			shadedContainer.Add (this);
		}
		
		public object GetNextCommandTarget ()
		{
			return Parent;
		}

		public object GetDelegatedCommandTarget ()
		{
			SdiWorkspaceWindow win = (SdiWorkspaceWindow) CurrentPageWidget;
			return win != null ? win.CommandHandler : null;
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			shadedContainer.DrawBackground (this);
			return base.OnExposeEvent (evnt);
		}

		public event EventHandler AreasChanged;
		
		public IEnumerable<Gdk.Rectangle> GetShadedAreas ()
		{
			Gdk.Rectangle rect = Allocation;
			if (CurrentPageWidget != null && CurrentPageWidget.Visible)
				rect.Height -= CurrentPageWidget.Allocation.Height;
			yield return rect;
		}
		
		protected override void OnPageAdded (Widget p0, uint p1)
		{
			base.OnPageAdded (p0, p1);
			if (AreasChanged != null)
				AreasChanged (this, EventArgs.Empty);
		}
		
		protected override void OnPageRemoved (Widget p0, uint p1)
		{
			base.OnPageRemoved (p0, p1);
			if (AreasChanged != null)
				AreasChanged (this, EventArgs.Empty);
		}
		
	}
}

