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
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Timers;

using MonoDevelop.Projects;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;

using GLib;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Components.DockNotebook;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the a Workspace with a multiple document interface.
	/// </summary>
	internal partial class DefaultWorkbench : WorkbenchWindow, ICommandRouter, IInfoBarHost, IShell, IService
	{
		readonly static string mainMenuPath    = "/MonoDevelop/Ide/MainMenu";
		readonly static string appMenuPath    = "/MonoDevelop/Ide/AppMenu";
		readonly static string viewContentPath = "/MonoDevelop/Ide/Pads";
//		readonly static string toolbarsPath    = "/MonoDevelop/Ide/Toolbar";
		readonly static string stockLayoutsPath    = "/MonoDevelop/Ide/WorkbenchLayouts";
		
		static string configFile = UserProfile.Current.ConfigDir.Combine ("EditingLayout.xml");
		const string fullViewModeTag = "[FullViewMode]";
		const int MAX_LASTACTIVEWINDOWS = 10;
		const int MinimumWidth = 800;
		const int MinimumHeight = 540;
		
		// list of layout names for the current context, without the context prefix
		List<string> layouts = new List<string> ();
		
		List<PadCodon> padContentCollection      = new List<PadCodon> ();
		List<SdiWorkspaceWindow> viewContentCollection = new List<SdiWorkspaceWindow> ();
		Dictionary<PadCodon, IPadWindow> padWindows = new Dictionary<PadCodon, IPadWindow> ();
		Dictionary<IPadWindow, PadCodon> padCodons = new Dictionary<IPadWindow, PadCodon> ();
		
		IWorkbenchWindow lastActive;

		bool closeAll;
		bool? fullScreenState = null;

		Rectangle requestedBounds = Rectangle.Empty;
		
		Gtk.Container rootWidget;
		CommandFrame toolbarFrame;
		Gtk.VBox infoBarFrame;
		DockFrame dock;
		SdiDragNotebook tabControl;
		Gtk.MenuBar topMenu;
		Gtk.VBox fullViewVBox;
		DockItem documentDockItem;
		MainToolbarController toolbar;
		MonoDevelopStatusBar bottomBar;

#if DUMMY_STRINGS_FOR_TRANSLATION_DO_NOT_COMPILE
		private void DoNotCompile ()
		{
			//The default layout, translated indirectly because it's used as an ID
			GettextCatalog.GetString ("Default");
		}
#endif
		
		public event EventHandler ActiveWorkbenchWindowChanged;
		public event EventHandler WorkbenchTabsChanged;
		public event EventHandler<NotebookEventArgs> NotebookClosed;

		public MonoDevelop.Ide.StatusBar StatusBar {
			get {
				return toolbar.StatusBar;
			}
		}

		public MainToolbarController Toolbar {
			get {
				return toolbar;
			}
		}

		public Gtk.MenuBar TopMenu {
			get { return topMenu; }
		}

		public MonoDevelopStatusBar BottomBar {
			get { return bottomBar; }
		}

		internal SdiDragNotebook TabControl {
			get {
				return tabControl;
			}
			set {
				tabControl = value;
				tabControl.NavigationButtonsVisible = true;
			}
		}
		
		public IWorkbenchWindow ActiveWorkbenchWindow {
			get {
				if (DockNotebook.ActiveNotebook == null || DockNotebook.ActiveNotebook.CurrentTabIndex < 0 || DockNotebook.ActiveNotebook.CurrentTabIndex >= DockNotebook.ActiveNotebook.TabCount)  {
					return null;
				}
				return (IWorkbenchWindow) DockNotebook.ActiveNotebook.CurrentTab.Content;
			}
		}
		
		public DockFrame DockFrame {
			get { return dock; }
		}

		public bool FullScreen {
			get {
				if (Visible)
					return IdeServices.DesktopService.GetIsFullscreen (this);
				return fullScreenState ?? false;
			}
			set {
				// If this window is not visible, don't set full screen mode
				// until it is, as that would conflict with other windows we
				// might be opening before (Start Window, for instance)
				if (Visible) {
					IdeServices.DesktopService.SetIsFullscreen (this, value);
				} else {
					fullScreenState = value;
				}
			}
		}

		public IList<string> Layouts {
			get {
				return layouts;
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
				var oldLayout = dock.CurrentLayout;
				
				InitializeLayout (value);
				dock.CurrentLayout = value;
				
				DestroyFullViewLayouts (oldLayout);
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

		async Task IService.Initialize (ServiceProvider serviceProvider)
		{
			Title = BrandingService.ApplicationLongName;
			LoggingService.LogInfo ("Creating DefaultWorkbench");

			WidthRequest = MinimumWidth;
			HeightRequest = MinimumHeight;

			DeleteEvent += new Gtk.DeleteEventHandler (OnClosing);
			BrandingService.ApplicationNameChanged += ApplicationNameChanged;

			SetAppIcons ();

			var commandService = await Runtime.GetService<CommandManager> ();
			commandService.SetRootWindow (this);

			DockNotebook.NotebookChanged += NotebookPagesChanged;

			Drag.DestSet (this, DestDefaults.All, targetEntryTypes, Gdk.DragAction.Copy);

			Accessible.SetIsMainWindow (true);
		}

		Task IService.Dispose ()
		{
			return Task.CompletedTask;
		}

		void NotebookPagesChanged (object sender, EventArgs e)
		{
			WorkbenchTabsChanged?.Invoke (this, EventArgs.Empty);
		}

		void SetAppIcons ()
		{
			//first try to get the icon from the GTK icon theme
			var appIconName = BrandingService.GetString ("ApplicationIconId")
				?? BrandingService.ApplicationName.ToLower ();
			if (Gtk.IconTheme.Default.HasIcon (appIconName)) {
				Gtk.Window.DefaultIconName = appIconName;
				return;
			}

			if (!Platform.IsMac) {
				//branded icons
				var iconsEl = BrandingService.GetElement ("ApplicationIcons");
				if (iconsEl != null) {
					try {
						var icons = iconsEl.Elements ("Icon")
							.Select (el => new Gdk.Pixbuf (BrandingService.GetFile ((string)el))).ToArray ();
						Gtk.Window.DefaultIconList = icons;
						foreach (var icon in icons)
							icon.Dispose ();
						return;
					} catch (Exception ex) {
						LoggingService.LogError ("Could not load app icons", ex);
					}
				}
			}
			
			//built-ins
			var appIcon = ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.MonoDevelop);
			Gtk.Window.DefaultIconList = new Gdk.Pixbuf[] {
				appIcon.ToPixbuf (Gtk.IconSize.Menu),
				appIcon.ToPixbuf (Gtk.IconSize.Button),
				appIcon.ToPixbuf (Gtk.IconSize.Dnd),
				appIcon.ToPixbuf (Gtk.IconSize.Dialog)
			};
		}

		public void InitializeWorkspace()
		{
			IdeApp.ProjectOperations.CurrentProjectChanged += (s,a) => SetWorkbenchTitle ();

			if (!IdeServices.DesktopService.SetGlobalMenu (IdeApp.CommandService, mainMenuPath, appMenuPath)) {
				CreateMenuBar ();
			}
			
			AddinManager.ExtensionChanged += OnExtensionChanged;

			// this is no longer used, clear any old values
			PropertyService.Set ("MonoDevelop.Core.Gui.CurrentWorkbenchLayout", null);
		}
		
		void OnExtensionChanged (object s, ExtensionEventArgs args)
		{
			if (args.PathChanged (mainMenuPath) || args.PathChanged (appMenuPath)) {
				if (IdeServices.DesktopService.SetGlobalMenu (IdeApp.CommandService, mainMenuPath, appMenuPath))
					return;
				
				UninstallMenuBar ();
				CreateMenuBar ();
				InstallMenuBar ();
			}
		}

		void CreateMenuBar ()
		{
			topMenu = IdeApp.CommandService.CreateMenuBar (mainMenuPath);
			var appMenu = IdeApp.CommandService.CreateMenu (appMenuPath);
			if (appMenu != null && appMenu.Children.Length > 0) {
				var item = new MenuItem (BrandingService.ApplicationName);
				item.Submenu = appMenu;
				topMenu.Insert (item, 0);
			}
		}

		void InstallMenuBar ()
		{
			if (topMenu != null) {
				((VBox)rootWidget).PackStart (topMenu, false, false, 0);
				((Gtk.Box.BoxChild)rootWidget [topMenu]).Position = 0;
				topMenu.ShowAll ();
			}
		}

		void UninstallMenuBar ()
		{
			if (topMenu == null)
				return;

			rootWidget.Remove (topMenu);
			topMenu.Destroy ();
			topMenu = null;
		}

		readonly HashSet<object> deduplicationMap = new HashSet<object> ();
		public void AddInfoBar (InfoBarOptions options)
		{
			var dedupId = options.Id ?? options.Description;
			if (!deduplicationMap.Add (dedupId))
				return;

#if NATIVE_INFO_BAR
			// disabled for now, needs a patch in gtk.
			Xwt.Widget infoBar = null;
			Xwt.Toolkit.NativeEngine.Invoke (() => {
				infoBar = new XwtInfoBar (options.Description, OnDispose, options.Items);
			});
			var widget = Xwt.Toolkit.CurrentEngine.WrapWidget (infoBar);
			var gtkWidget = widget.ToGtkWidget ();
			infoBarFrame.Add (gtkWidget);
#else
			var infoBar = new XwtInfoBar (options.Description, OnDispose, options.Items);
			infoBarFrame.Add (infoBar.ToGtkWidget ());
#endif

			void OnDispose () => deduplicationMap.Remove (dedupId);
		}
		
		public Task<IWorkbenchWindow> ShowView (DocumentController controller, IShellNotebook notebook, object viewCommandHandler)
		{
			var mimeimage = controller.DocumentIcon?.WithSize (Gtk.IconSize.Menu);

			var addToControl = (DockNotebook) notebook ?? DockNotebook.ActiveNotebook ?? tabControl;
			var tab = addToControl.AddTab ();

			var sdiWorkspaceWindow = new SdiWorkspaceWindow (this, controller, addToControl, tab);
			sdiWorkspaceWindow.ViewCommandHandler = viewCommandHandler;
			sdiWorkspaceWindow.TitleChanged += delegate { SetWorkbenchTitle (); };
			sdiWorkspaceWindow.Show ();
			viewContentCollection.Add (sdiWorkspaceWindow);

			tab.Content = sdiWorkspaceWindow;
			if (mimeimage != null)
				tab.Icon = mimeimage;

			// The insertion of the tab may have changed the active view (or maybe not, this is checked in OnActiveWindowChanged)
			OnActiveWindowChanged (null, null);

			return Task.FromResult<IWorkbenchWindow> (sdiWorkspaceWindow);
		}

		public void CloseView (IWorkbenchWindow view, bool animate)
		{
			var window = (SdiWorkspaceWindow)view;
			viewContentCollection.Remove (window);
			RemoveTab (window.TabControl, window.DockNotebookTab.Index, animate);
			if (window.DocumentController == subscribedController)
				UnsubscribeControllerEvents ();
			window.Close ();
		}

		void ShowPadNode (ExtensionNode node)
		{
			if (node is PadCodon) {
				PadCodon pad = (PadCodon) node;
				AddPad (pad, pad.DefaultPlacement, pad.DefaultStatus);
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
				string lab = content.Label.Length > 0 ? content.Label : "";
				ActionCommand cmd = new ActionCommand ("Pad|" + content.PadId, lab, null);
				cmd.DefaultHandler = new PadActivationHandler (this, content);
				cmd.Category = GettextCatalog.GetString ("View (Pads)");
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
			PadWindow win = (PadWindow) GetPadWindow (codon);
			if (win != null) {
				win.NotifyDestroyed ();
				Counters.PadsLoaded--;
				padCodons.Remove (win);
			}
			if (item != null)
				dock.RemoveItem (item);
			padWindows.Remove (codon);
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
		
		internal static string GetTitle (SdiWorkspaceWindow window)
		{
			var controller = window.DocumentController;
			if (string.IsNullOrEmpty (controller.DocumentTitle))
				return GetDefaultTitle ();
			string post = String.Empty;
			if (controller.HasUnsavedChanges) {
				post = "*";
			}
			if (controller.Owner is Project project) {
				var fileController = controller as FileDocumentController;
				if (fileController != null)
					return project.Name + " – " + fileController.FilePath.ToRelative (project.BaseDirectory) + post + " – " + BrandingService.ApplicationLongName;
				else
					return project.Name + " – " + controller.DocumentTitle + post + " – " + BrandingService.ApplicationLongName;
			}
			return controller.DocumentTitle + post + " – " + BrandingService.ApplicationLongName;
		}

		void SetAccessibilityDetails (SdiWorkspaceWindow window)
		{
			var controller = window.DocumentController;

			string documentUrl = string.Empty, filename = string.Empty;

			if (controller.Owner is Project project)
				documentUrl = "file://" + project.FileName;

			if (controller is FileDocumentController file)
				filename = file.FilePath.FileName;

			Accessible.SetDocument (documentUrl);
			Accessible.SetFilename (filename);
		}

		void SetWorkbenchTitle ()
		{
			try {
				var window = (SdiWorkspaceWindow) ActiveWorkbenchWindow;
				if (window != null) {
					if (window.Toplevel == this)
						Title = GetTitle (window);

					SetAccessibilityDetails (window);
				} else {
					Title = GetDefaultTitle ();
					if (IsInFullViewMode)
						this.ToggleFullViewMode ();
					Accessible.SetDocument ("");
					Accessible.SetUrl ("");
				}

			} catch (Exception) {
				Title = GetDefaultTitle ();
				Accessible.SetDocument ("");
				Accessible.SetFilename ("");
			}
		}
		
		static string GetDefaultTitle ()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null)
				return IdeApp.ProjectOperations.CurrentSelectedProject.Name + " – " + BrandingService.ApplicationLongName;
			return BrandingService.ApplicationLongName;
		}

		void ApplicationNameChanged (object sender, EventArgs e)
		{
			SetWorkbenchTitle ();
		}
		
		public ICustomXmlSerializer Memento {
			get {
				var memento  = new WorkbenchMemento (new Properties ());
				GetPosition (out int x, out int y);
				GetSize (out int width, out int height);
				// HACK: always capture bounds on OS X because we don't restore Gdk.WindowState.Maximized due to
				// the bug mentioned below. So we simular Maximized by capturing the Maximized size.
				if (GdkWindow.State == 0 || Platform.IsMac) {
					memento.Bounds = new Rectangle (x, y, width, height);
				} else {
					memento.Bounds = requestedBounds;
				}
				memento.WindowState = GdkWindow.State;
				memento.FullScreen  = FullScreen;
				return memento.ToProperties ();
			}
			set {
				if (value != null) {
					WorkbenchMemento memento = new WorkbenchMemento ((Properties)value);
					
					requestedBounds = memento.Bounds;
					if (requestedBounds.Width < MinimumWidth)
						requestedBounds.Width = MinimumWidth;
					if (requestedBounds.Height < MinimumHeight)
						requestedBounds.Height = MinimumHeight;
					IdeServices.DesktopService.PlaceWindow (this, requestedBounds.X, requestedBounds.Y, requestedBounds.Width, requestedBounds.Height);
					
					// HACK: don't restore Gdk.WindowState.Maximized on OS X, because there's a bug in 
					// GdkWindow.State that means it doesn't reflect the real state, it only reflects values set
					// internally by GTK e.g. by Maximize calls. Because MD restores state, if it ever becomes 
					// Maximized it becomes "stuck" and it's difficult for the user to make it "normal".
					if (memento.WindowState == Gdk.WindowState.Maximized && !Platform.IsMac) {
						Maximize ();
					} else if (memento.WindowState == Gdk.WindowState.Iconified) {
						Iconify ();
					}
					//GdkWindow.State = memento.WindowState;
					FullScreen = memento.FullScreen;
				}
				Decorated = true;
			}
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			bool isFullscreen = IdeServices.DesktopService.GetIsFullscreen (this);
			if (fullScreenState != null && fullScreenState != isFullscreen) {
				isFullscreen = (bool)fullScreenState;
				IdeServices.DesktopService.SetIsFullscreen (this, isFullscreen);
				fullScreenState = null;
			}
			if (Platform.IsMac && !isFullscreen) {
				// HACK: GTK bug, macOS will limit the NSWindow size to the target screen size
				//       even if a bigger size has been requested. However although Gtk and Cocoa
				//       will report the limited size correctly, Gtk will still continue using
				//       the requested (invalid) size internally, resulting a pointer event offset
				//       by the difference of current and requested sizes. Resizing the Gtk window
				//       to the current new size, solves this problem.
				// NOTE: this bug does not occur in fillscreen mode
				GetSize (out int width, out int height);
				if (requestedBounds.Width > width || requestedBounds.Height > height)
					Resize (width, height);
			}
		}

		bool closing;
		void OnClosing (object o, Gtk.DeleteEventArgs e)
		{
			// don't allow Gtk to close the workspace, in case Close() leaves the synchronization context
			// Gtk.Application.Quit () will handle it for us.
			e.RetVal = true;

			IdeApp.Exit ().Ignore ();
		}
		
		public void Close()
		{
			BrandingService.ApplicationNameChanged -= ApplicationNameChanged;

			if (rootWidget != null) {
				PropertyService.Set ("SharpDevelop.Workbench.WorkbenchMemento", this.Memento);
				//don't allow the "full view" layouts to persist - they are always derived from the "normal" layout
				foreach (var fv in dock.Layouts)
					if (fv.EndsWith (fullViewModeTag))
						dock.DeleteLayout (fv);
				try {
					dock.SaveLayouts (configFile);
				} catch (Exception ex) {
					LoggingService.LogError ("Error while saving layout.", ex);
				}
				UninstallMenuBar ();
				Remove (rootWidget);

				foreach (PadCodon content in PadContentCollection) {
					if (content.Initialized)
						content.PadContent.Dispose ();
				}

				rootWidget.Destroy ();
			}
			Destroy ();
		}

		int activeWindowChangeLock = 0;

		public void LockActiveWindowChangeEvent ()
		{
			activeWindowChangeLock++;
		}
		
		public void UnlockActiveWindowChangeEvent ()
		{
			activeWindowChangeLock--;
			OnActiveWindowChanged (null, null);
		}

		internal void OnActiveWindowChanged (object sender, EventArgs e)
		{
			if (activeWindowChangeLock > 0)
				return;
			if (lastActive == ActiveWorkbenchWindow)
				return;

			UnsubscribeControllerEvents ();

			WelcomePage.WelcomePageService.HideWelcomePageOrWindow ();

			if (lastActive != null)
				((SdiWorkspaceWindow)lastActive).OnDeactivated ();

			lastActive = ActiveWorkbenchWindow;
			SetWorkbenchTitle ();

			if (!closeAll && ActiveWorkbenchWindow != null)
				((SdiWorkspaceWindow)ActiveWorkbenchWindow).OnActivated ();

			if (!closeAll && ActiveWorkbenchWindowChanged != null) {
				ActiveWorkbenchWindowChanged(this, e);
			}

			var controller = ((SdiWorkspaceWindow)ActiveWorkbenchWindow)?.DocumentController;
			if (controller != null)
				SubscribeControllerEvents (controller);
		}

		DocumentController subscribedController;
		Project subscribedProject;

		void SubscribeControllerEvents (DocumentController controller)
		{
			UnsubscribeControllerEvents ();
			subscribedController = controller;
			controller.OwnerChanged += OwnerChanged;
			subscribedProject = controller.Owner as Project;
			if (subscribedProject != null)
				subscribedProject.NameChanged += HandleProjectNameChanged;
		}

		void UnsubscribeControllerEvents ()
		{
			if (subscribedController != null) {
				subscribedController.OwnerChanged -= OwnerChanged;
				subscribedController = null;
				if (subscribedProject != null) {
					subscribedProject.NameChanged -= HandleProjectNameChanged;
					subscribedProject = null;
				}
			}
		}

		void HandleProjectNameChanged (object sender, SolutionItemRenamedEventArgs e)
		{
			SetWorkbenchTitle ();
		}

		void OwnerChanged (object sender, EventArgs e)
		{
			var controller = (DocumentController)sender;
			UnsubscribeControllerEvents ();
			SubscribeControllerEvents (controller);
			SetWorkbenchTitle ();
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
			
			CreateComponents ();
			
			// Subscribe to changes in the extension
			initializing = true;
			AddinManager.AddExtensionNodeHandler (viewContentPath, OnExtensionChanged);
			initializing = false;
		}

		Task layoutChangedTask;
		async void LayoutChanged (object o, EventArgs e)
		{
			if (layoutChangedTask != null) {
				return;
			}

			layoutChangedTask = Task.Delay (10000);
			await layoutChangedTask;
			layoutChangedTask = null;

			dock.SaveLayouts (configFile);
		}

		void CreateComponents ()
		{
			Accessible.Name = "MainWindow";

			fullViewVBox = new VBox (false, 0);
			fullViewVBox.Accessible.Name = "MainWindow.Root";
			fullViewVBox.Accessible.SetLabel ("Label");
			fullViewVBox.Accessible.SetShouldIgnore (true);

			rootWidget = fullViewVBox;
			
			InstallMenuBar ();
			Realize ();
			toolbar = IdeServices.DesktopService.CreateMainToolbar (this);
			IdeServices.DesktopService.SetMainWindowDecorations (this);
			IdeServices.DesktopService.AttachMainToolbar (fullViewVBox, toolbar);


			infoBarFrame = new VBox (false, 0);
			infoBarFrame.Accessible.Name = "MainWindow.InfoBars";
			infoBarFrame.Accessible.SetLabel ("Label");
			infoBarFrame.Accessible.SetShouldIgnore (true);

			fullViewVBox.PackStart (infoBarFrame, false, false, 0);

			toolbarFrame = new CommandFrame (IdeApp.CommandService);
			toolbarFrame.Accessible.Name = "MainWindow.Root.ToolbarFrame";
			toolbarFrame.Accessible.SetShouldIgnore (true);

			fullViewVBox.PackStart (toolbarFrame, true, true, 0);

			// Create the docking widget and add it to the window.
			dock = new DockFrame ();
			dock.LayoutChanged += LayoutChanged;

			dock.CompactGuiLevel = ((int)IdeApp.Preferences.WorkbenchCompactness.Value) + 1;
			IdeApp.Preferences.WorkbenchCompactness.Changed += delegate {
				dock.CompactGuiLevel = ((int)IdeApp.Preferences.WorkbenchCompactness.Value) + 1;
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

			toolbarFrame.Add (dock);
			
			// Create the notebook for the various documents.
			tabControl = new SdiDragNotebook (this);

			DockNotebook.ActiveNotebookChanged += delegate {
				OnActiveWindowChanged (null, null);
			};

			Add (fullViewVBox);
			fullViewVBox.ShowAll ();
			bottomBar = new MonoDevelopStatusBar ();
			fullViewVBox.PackEnd (bottomBar, false, true, 0);
			bottomBar.ShowAll ();

			// In order to get the correct bar height we need to calculate the tab size using the
			// correct style (the style of the window). At this point the widget is not yet a child
			// of the window, so its style is not yet the correct one.
			tabControl.InitSize ();

			// The main document area
			documentDockItem = dock.AddItem ("Documents");
			documentDockItem.Behavior = DockItemBehavior.Locked;
			documentDockItem.Expand = true;
			documentDockItem.DrawFrame = false;
			documentDockItem.Label = GettextCatalog.GetString ("Documents");
			documentDockItem.Content = new DockNotebookContainer (tabControl, true);

			LoadDockStyles ();
			Styles.Changed += (sender, e) => LoadDockStyles ();

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

			if (MonoDevelop.Core.Platform.IsMac)
				bottomBar.HasResizeGrip = true;
			else {
				if (GdkWindow != null && GdkWindow.State == Gdk.WindowState.Maximized)
					bottomBar.HasResizeGrip = false;
				SizeAllocated += delegate {
					if (GdkWindow != null)
						bottomBar.HasResizeGrip = GdkWindow.State != Gdk.WindowState.Maximized;
				};
			}

			// create DockItems for all the pads
			ExtensionNodeList padCodons = AddinManager.GetExtensionNodes (viewContentPath);
			foreach (ExtensionNode node in padCodons)
				ShowPadNode (node);

			try {
				if (System.IO.File.Exists (configFile)) {
					dock.LoadLayouts (configFile);
					foreach (string layout in dock.Layouts) {
						if (!layouts.Contains (layout) && !layout.EndsWith (fullViewModeTag))
							layouts.Add (layout);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}

		void LoadDockStyles ()
		{
			var barHeight = tabControl.BarHeight;

			DockVisualStyle style = new DockVisualStyle ();
			style.PadTitleLabelColor = Styles.PadLabelColor;
			style.InactivePadTitleLabelColor = Styles.InactivePadLabelColor;
			style.PadBackgroundColor = Styles.PadBackground;
			style.TreeBackgroundColor = Styles.BaseBackgroundColor;
			style.InactivePadBackgroundColor = Styles.InactivePadBackground;
			style.PadTitleHeight = barHeight;
			dock.DefaultVisualStyle = style;

			style = new DockVisualStyle ();
			style.PadTitleLabelColor = Styles.PadLabelColor;
			style.InactivePadTitleLabelColor = Styles.InactivePadLabelColor;
			style.PadTitleHeight = barHeight;
			// style.ShowPadTitleIcon = false; // VV: Now we want to have icons on all pads
			style.UppercaseTitles = false;
			style.ExpandedTabs = true;
			style.PadBackgroundColor = Styles.BrowserPadBackground;
			style.InactivePadBackgroundColor = Styles.InactiveBrowserPadBackground;
			style.TreeBackgroundColor = Styles.BrowserPadBackground;
			dock.SetDockItemStyle ("ProjectPad", style);
			dock.SetDockItemStyle ("ClassPad", style);



			//			dock.SetRegionStyle ("Documents/Left", style);
			//dock.SetRegionStyle ("Documents/Right", style);

			//			style = new DockVisualStyle ();
			//			style.SingleColumnMode = true;
			//			dock.SetRegionStyle ("Documents/Left;Documents/Right", style);
			//			dock.SetDockItemStyle ("Documents", style);

			dock.UpdateStyles ();
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
		
		internal void ShowPopup (DockNotebook notebook, int tabIndex, Gdk.EventButton evt)
		{
			notebook.CurrentTabIndex = tabIndex;
			var entrySet = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/Ide/ContextMenu/DocumentTab");
			IdeApp.CommandService.ShowContextMenu (notebook, evt, entrySet, notebook.CurrentTab.Content);
		}
		
		internal void OnTabsReordered (DockNotebookTab widget, int oldPlacement, int newPlacement)
		{
			WindowReordered?.Invoke (this, new WindowReorderedEventArgs { OldPosition = oldPlacement, NewPosition = newPlacement });
		}

		internal string GetUniqueTabName (FilePath fileName)
		{
			string baseName = fileName.FileNameWithoutExtension;
			int number = 1;
			bool found = true;
			var myUntitledTitle = baseName + fileName.Extension;
			while (found) {
				found = false;
				foreach (var window in viewContentCollection) {
					string title = window.Title;
					if (title == myUntitledTitle) {
						myUntitledTitle = baseName + number + fileName.Extension;
						found = true;
						++number;
						break;
					}
				}
			}
			return myUntitledTitle;
		}

		public event EventHandler<WindowReorderedEventArgs> WindowReordered;


		bool IsInFullViewMode {
			get {
				return dock.CurrentLayout != null && dock.CurrentLayout.EndsWith(fullViewModeTag, StringComparison.Ordinal);
			}
		}

		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			IdeTheme.UpdateStyles ();
		}
		
		protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
		{
			SetActiveWidget (Focus);
			base.OnConfigureEvent (evnt);
			return false;
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			SetActiveWidget (Focus);
			return base.OnFocusInEvent (evnt);
		}

		bool haveFocusedToolbar = false;
		protected override bool OnFocused (DirectionType direction)
		{
			// If the toolbar is not focused, focus it, and focus the next child once it loses focus
			switch (direction) {
			case DirectionType.TabForward:
				if (!haveFocusedToolbar) {
					haveFocusedToolbar = true;
					toolbar.ToolbarView.Focus(direction, (d) => {
						haveFocusedToolbar = false;
						if (!dock.ChildFocus(d)) {
							OnFocused(d);
						}
					});
				} else {
					if (!dock.ChildFocus(direction)) {
						haveFocusedToolbar = false;
						OnFocused(direction);
					}
				}
				break;

			case DirectionType.TabBackward:
				if (!dock.ChildFocus(direction)) {
					haveFocusedToolbar = true;
					toolbar.ToolbarView.Focus (direction, (d) => {
						haveFocusedToolbar = false;
						if (!dock.ChildFocus (d)) {
							OnFocused (d);
						}
					});
				}
				break;

			default:
				return base.OnFocused(direction);
			}
			return true;
		}

		/// <summary>
		/// Sets the current active document widget.
		/// </summary>
		internal void SetActiveWidget (Widget child)
		{
			while (child != null) {
				var dragNotebook = child as SdiDragNotebook;
				if (dragNotebook != null) {
					OnActiveWindowChanged (dragNotebook, EventArgs.Empty);
					break;
				}
				child = child.Parent;
			}
		}
		
		//don't allow the "full view" layouts to persist - they are always derived from the "normal" layout
		//else they will diverge
		void DestroyFullViewLayouts (string oldLayout)
		{
			if (oldLayout != null && oldLayout.EndsWith (fullViewModeTag)) {
				dock.DeleteLayout (oldLayout);
			}
		}
		
		public void ToggleFullViewMode ()
		{
			if (IsInFullViewMode) {
				var oldLayout = dock.CurrentLayout;
				dock.CurrentLayout = CurrentLayout;
				DestroyFullViewLayouts (oldLayout);
			} else {
				string fullViewLayout = CurrentLayout + fullViewModeTag;
				if (!dock.HasLayout (fullViewLayout))
					dock.CreateLayout (fullViewLayout, true);
				dock.CurrentLayout = fullViewLayout;
				foreach (DockItem it in dock.GetItems ()) {
					if (it.Behavior != DockItemBehavior.Locked && it.Visible)
						it.Status = DockItemStatus.AutoHide;
				}
			}
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			return FilterWindowKeypress (evnt) || base.OnKeyPressEvent (evnt); 
		}

		internal bool FilterWindowKeypress (Gdk.EventKey evnt)
		{
			// Handle Alt+1-0 keys
			Gdk.ModifierType winSwitchModifier = Platform.IsMac ? KeyBindingManager.SelectionModifierControl : KeyBindingManager.SelectionModifierAlt;
			if ((evnt.State & winSwitchModifier) != 0 && (evnt.State & (Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask)) != (Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask)) {
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
			return false;
		}
		
		void SwitchToDocument (int number)
		{
			if (DockNotebook.ActiveNotebook == null)
				return;
			
			if (number >= DockNotebook.ActiveNotebook.TabCount || number < 0)
				return;
			var window = DockNotebook.ActiveNotebook.Tabs [number].Content as IWorkbenchWindow;
			if (window != null)
				window.SelectWindow ();
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
		
		internal void CloseClicked (object o, TabEventArgs e)
		{
			if (e.Tab.Content != null) {
				if (e.Tab.Content is SdiWorkspaceWindow sdiWorkspace) {
					sdiWorkspace.RequestClose ();
				} else {
					LoggingService.LogError ($"Tab content is not an SdiWorkspaceWindow, is {e.Tab.Content.GetType ()}");
				}
			} else {
				LoggingService.LogError ("Tab content is null");
			}
		}

		internal void RemoveTab (DockNotebook tabControl, int pageNum, bool animate)
		{
			try {
				// Weird switch page events are fired when a tab is removed.
				// This flag avoids unneeded events.
				LockActiveWindowChangeEvent ();
				IWorkbenchWindow w = ActiveWorkbenchWindow;
				tabControl.RemoveTab (pageNum, animate);
			} finally {
				UnlockActiveWindowChangeEvent ();
			}
		}

		internal void ReorderTab (int oldPlacement, int newPlacement)
		{
			DockNotebookTab tab = (DockNotebookTab)tabControl.GetTab (oldPlacement);
			DockNotebookTab targetTab = (DockNotebookTab)tabControl.GetTab (newPlacement);
			tabControl.ReorderTab (tab, targetTab);
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
			WelcomePage.WelcomePageService.HideWelcomePageOrWindow ();

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
				PadContent newContent = padCodon.InitializePadContent (window);

				Gtk.Widget crc = new PadCommandRouterContainer (window, newContent.Control, newContent, true);
				crc.Show ();

				Gtk.Widget router = new PadCommandRouterContainer (window, crc, toolbarFrame, false);
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
			RegisterPad (padCodon);

			PadWindow window = new PadWindow (this, padCodon);
			window.Icon = padCodon.Icon;
			padWindows [padCodon] = window;
			padCodons [window] = padCodon;
			
			window.StatusChanged += new EventHandler (UpdatePad);
			
			string location = ToDockLocation (placement);
			
			DockItem item = dock.AddItem (padCodon.PadId);
			item.Label = padCodon.Label;
			item.Icon = ImageService.GetIcon (padCodon.Icon).WithSize (IconSize.Menu);
			item.DefaultLocation = location;
			item.DefaultVisible = false;
			item.DefaultStatus = defaultStatus;
			window.Item = item;
			
			if (padCodon.Initialized) {
				CreatePadContent (true, padCodon, window, item);
			} else {
				item.ContentRequired += delegate {
					CreatePadContent (false, padCodon, window, item);
				};
			}
			
			item.VisibleChanged += (s,a) => {
				if (item.Visible)
					window.NotifyShown (a);
				else
					window.NotifyHidden (a);
			};
			
			item.ContentVisibleChanged += delegate {
				if (item.ContentVisible)
					window.NotifyContentShown ();
				else
					window.NotifyContentHidden ();
			};
		}
		
		void UpdatePad (object source, EventArgs args)
		{
			IPadWindow window = (IPadWindow) source;
			if (!padCodons.ContainsKey (window)) 
				return;
			PadCodon codon = padCodons [window];
			DockItem item = GetDockItem (codon);
			if (item != null) {
				string windowTitle = window.Title;
				var windowIcon = ImageService.GetIcon (window.Icon).WithSize (IconSize.Menu);
				if (String.IsNullOrEmpty (windowTitle)) 
					windowTitle = codon.Label;
				if (window.HasErrors && !window.ContentVisible) {
					windowTitle = "<span foreground='" + Styles.ErrorForegroundColor.ToHexString (false) + "'>" + windowTitle + "</span>";
					windowIcon = windowIcon.WithStyles ("error");
				} else if (window.HasNewData && !window.ContentVisible)
					windowTitle = "<b>" + windowTitle + "</b>";
				item.Label = windowTitle;
				item.Icon = windowIcon;
			}
		}
		
		#endregion

		#region ICommandRouter implementation

		object ICommandRouter.GetNextCommandTarget ()
		{
			return toolbar;
		}

		#endregion

		#region DnD

		enum TargetList
		{
			UriList = 100
		}

		static TargetEntry [] targetEntryTypes = { new TargetEntry ("text/uri-list", 0, (uint)TargetList.UriList) };


		protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time_)
		{
			// Bring this window to the front. Otherwise, the drop may end being done in another window that overlaps this one
			if (!Platform.IsWindows)
				Present ();
			return base.OnDragMotion (context, x, y, time_);
		}

		protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y, SelectionData selection_data, uint info, uint time_)
		{
			if (info != (uint)TargetList.UriList)
				return;
			string fullData = System.Text.Encoding.UTF8.GetString (selection_data.Data);

			var files = new List<FileOpenInformation> ();

			foreach (string individualFile in fullData.Split ('\n')) {
				string file = individualFile.Trim ();
				if (!file.StartsWith ("file://", StringComparison.Ordinal))
					continue;

				var filePath = new FilePath (file);
				if (filePath.IsDirectory)
					filePath = Directory.EnumerateFiles (filePath).FirstOrDefault (p => Services.ProjectService.IsWorkspaceItemFile (p));
				if (!filePath.IsNullOrEmpty)
					files.Add (new FileOpenInformation (filePath, null, 0, 0, OpenDocumentOptions.DefaultInternal));
			}

			if (files.Count > 0) {
				try {
					IdeApp.OpenFiles (files);
				} catch (Exception e) {
					LoggingService.LogError ($"Failed to open dropped files", e);
				}
			}

			base.OnDragDataReceived (context, x, y, selection_data, info, time_);
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
	
	class SdiDragNotebook: DockNotebook, ICommandDelegatorRouter, ICommandBar
	{
		public SdiDragNotebook (DefaultWorkbench window)
		{
			NextButtonClicked += delegate {
				IdeApp.CommandService.DispatchCommand (Ide.Commands.NavigationCommands.NavigateForward);
			};
			PreviousButtonClicked += delegate {
				IdeApp.CommandService.DispatchCommand (Ide.Commands.NavigationCommands.NavigateBack);
			};
			SwitchPage += window.OnActiveWindowChanged;
			PageRemoved += window.OnActiveWindowChanged;
			TabClosed += window.CloseClicked;
			TabActivated += delegate {
				window.ToggleFullViewMode ();
			};
			TabsReordered += window.OnTabsReordered;
			
			DoPopupMenu = window.ShowPopup;
			Events |= Gdk.EventMask.FocusChangeMask | Gdk.EventMask.KeyPressMask;
			IdeApp.CommandService.RegisterCommandBar (this);
		}
		
		protected override void OnDestroyed ()
		{
			IdeApp.CommandService.UnregisterCommandBar (this); 
			base.OnDestroyed ();
		}
		
		public object GetNextCommandTarget ()
		{
			return Parent;
		}

		public object GetDelegatedCommandTarget ()
		{
			SdiWorkspaceWindow win = CurrentTab != null ? (SdiWorkspaceWindow) CurrentTab.Content : null;
			return win != null ? win.ViewCommandHandler : null;
		}
		
		#region ICommandBar implementation
		bool isEnabled = true;

		void ICommandBar.Update (object activeTarget)
		{
			var ci = IdeApp.CommandService.GetCommandInfo (Ide.Commands.NavigationCommands.NavigateForward);
			NextButtonEnabled = isEnabled && ci.Enabled && ci.Visible;

			ci = IdeApp.CommandService.GetCommandInfo (Ide.Commands.NavigationCommands.NavigateBack);
			PreviousButtonEnabled = isEnabled && ci.Enabled && ci.Visible;
		}

		void ICommandBar.SetEnabled (bool enabled)
		{
			isEnabled = enabled;
		}
		#endregion
	}
}