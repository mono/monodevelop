//  SdiWorkspaceWindow.cs
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gtk;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.DockNotebook;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Gui
{
	internal class SdiWorkspaceWindow : EventBox, IWorkbenchWindow, ICommandDelegatorRouter
	{
		DefaultWorkbench workbench;
		ViewContent content;
		ExtensionContext extensionContext;
		FileTypeCondition fileTypeCondition = new FileTypeCondition ();

		HPaned subViewNotebook = null;
		Tabstrip subViewToolbar = null;
		HBox toolbarBox = null;
		
		List<BaseViewContent> viewContents = new List<BaseViewContent> ();
		List<BaseViewContent> currentViews = new List<BaseViewContent> ();

		Dictionary<BaseViewContent, ViewContentData> viewContentData = new Dictionary<BaseViewContent, ViewContentData> ();

		class ViewContentData : IDisposable
		{
			private readonly SdiWorkspaceWindow window;
			public ViewContentData(SdiWorkspaceWindow window)
			{
				this.window = window;
			}
			public BaseViewContent ViewContent;
			public VBox VBox = new VBox ();
			public DocumentToolbar Toolbar;
			public PathBar Pathbar;
			public IPathedDocument PathedDocument;
			bool controlAlreadyAdded = false;

			public void EnsureControlAdded ()
			{
				if (controlAlreadyAdded)
					return;
				controlAlreadyAdded = true;
				VBox.Add (ViewContent.Control);
				VBox.Show ();
				VBox.FocusChildSet += ChildGotFocus;
			}

			void ChildGotFocus (object o, FocusChildSetArgs args)
			{
				window.ActiveViewContent = ViewContent;
			}

			public void Dispose ()
			{
				if (VBox != null) {
					VBox.FocusChildSet -= ChildGotFocus;
					VBox.Destroy ();
					VBox = null;
				}
			}
		}

		VBox box;
		DockNotebookTab tab;
		Widget tabPage;
		DockNotebook tabControl;
		
		string myUntitledTitle = null;
		string _titleHolder = "";
		
		string documentType;
		
		bool show_notification = false;

		ViewCommandHandlers commandHandler;

		public event EventHandler ViewsChanged;
		
		public DockNotebook TabControl {
			get {
				return this.tabControl;
			}
		}
		
		internal void SetDockNotebook (DockNotebook tabControl, DockNotebookTab tabLabel)
		{
			this.tabControl = tabControl;
			this.tab = tabLabel;
			SetTitleEvent(null, null);
			SetDockNotebookTabTitle ();
		}

		public SdiWorkspaceWindow (DefaultWorkbench workbench, ViewContent content, DockNotebook tabControl, DockNotebookTab tabLabel) : base ()
		{
			this.workbench = workbench;
			this.tabControl = tabControl;
			this.content = content;
			this.tab = tabLabel;

			fileTypeCondition.SetFileName (content.ContentName ?? content.UntitledName);
			extensionContext = AddinManager.CreateExtensionContext ();
			extensionContext.RegisterCondition ("FileType", fileTypeCondition);
			
			box = new VBox ();
			box.Accessible.SetShouldIgnore (true);

			viewContents.Add (content);

			var viewData = new ViewContentData (this) {
				ViewContent = content
			};
			viewContentData.Add (content, viewData);

			//this fires an event that the content uses to access this object's ExtensionContext
			content.WorkbenchWindow = this;

			// The previous WorkbenchWindow property assignement may end with a call to AttachViewContent,
			// which will add the content control to the subview notebook. In that case, we don't need to add it to box
			content.ContentNameChanged += SetTitleEvent;
			content.DirtyChanged       += HandleDirtyChanged;
			box.Show ();
			Add (box);
			
			SetTitleEvent(null, null);
		}

		internal void CreateCommandHandler ()
		{
			commandHandler = new ViewCommandHandlers (this);
		}

		void HandleDirtyChanged (object sender, EventArgs e)
		{
			OnTitleChanged (null);
		}
		
		public Widget TabPage {
			get {
				if (tabPage == null)
					tabPage = content.Control;
				return tabPage;
			}
			set {
				tabPage = value;
			}
		}
		
		internal DockNotebookTab TabLabel {
			get { return tab; }
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			if (subViewNotebook == null) {
				CheckCreateSubViewContents ();
				SetCurrentViews (new [] { content });
			}
		}

		Document document;
		public Document Document {
			get {
				return document;
			}
			set {
				document = value;
				OnDocumentChanged (EventArgs.Empty);
			}
		}
		
		public ExtensionContext ExtensionContext {
			get { return extensionContext; }
		}

		protected override bool OnWidgetEvent (Gdk.Event evt)
		{
			if (evt.Type == Gdk.EventType.ButtonRelease)
				DockNotebook.ActiveNotebook = (SdiDragNotebook)Parent.Parent;

			return base.OnWidgetEvent (evt);
		}
		
		protected virtual void OnDocumentChanged (EventArgs e)
		{
			EventHandler handler = this.DocumentChanged;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler DocumentChanged;
		
		public bool ShowNotification {
			get {
				return show_notification;
			}
			set {
				if (show_notification != value) {
					show_notification = value;
					OnTitleChanged (null);
				}
			}
		}
		
		public string Title {
			get {
				//FIXME: This breaks, Why? --Todd
				//_titleHolder = tabControl.GetTabLabelText (tabPage);
				return _titleHolder;
			}
			set {
				_titleHolder = value;
				string fileName = content.ContentName;
				if (fileName == null) {
					fileName = content.UntitledName;
				}
				OnTitleChanged(null);
			}
		}
		
		public IEnumerable<BaseViewContent> SubViewContents {
			get {
				return viewContents.OfType<BaseViewContent> ();
			}
		}
		
		// caution use activeView with care !!
		BaseViewContent activeView = null;
		public BaseViewContent ActiveViewContent {
			get {
				if (activeView != null)
					return activeView;
				return content;
			}
			set {
				if (activeView == value)
					return;
				this.activeView = value;
				this.OnActiveViewContentChanged (new ActiveViewContentEventArgs (value));
			}
		}
		
		public void SwitchView (int viewNumber)
		{
			if (subViewNotebook != null)
				ShowPage (viewContents [viewNumber]);
		}

		public void SwitchView (BaseViewContent view)
		{
			if (subViewNotebook != null)
				ShowPage (view);
		}
		
		public int FindView<T> ()
		{
			for (int i = 0; i < viewContents.Count; i++) {
				if (viewContents[i] is T)
					return i;
			}

			return -1;
		}

		public void OnDeactivated ()
		{
			foreach (var data in viewContentData.Values) {
				data.Pathbar?.HideMenu ();
			}
		}
		
		public void OnActivated ()
		{
			if (subViewToolbar != null)
				subViewToolbar.Tabs [subViewToolbar.ActiveTab].Activate ();
		}
		
		public void SelectWindow()
		{
			var window = tabControl.Toplevel as Gtk.Window;
			if (window != null) {
				if (window is DockWindow) {
					DesktopService.GrabDesktopFocus (window);
				}

				// Focusing the main window so hide all the autohide pads
				workbench.DockFrame.MinimizeAllAutohidden ();

				#if MAC
				AppKit.NSWindow nswindow = MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (window);
				if (nswindow != null) {
					nswindow.MakeFirstResponder (nswindow.ContentView);
					nswindow.MakeKeyAndOrderFront (nswindow);
				}
				#endif
			}	

			// The tab change must be done now to ensure that the content is created
			// before exiting this method.
			tabControl.CurrentTabIndex = tab.Index;

			// Focus the tab in the next iteration since presenting the window may take some time
			Application.Invoke ((o, args) => {
				DockNotebook.ActiveNotebook = tabControl;
				DeepGrabFocus (this.ActiveViewContent.Control);
			});
		}

		public bool CanMoveToNextNotebook ()
		{
			return TabControl.GetNextNotebook () != null || (TabControl.Container.AllowRightInsert && TabControl.TabCount > 1);
		}

		public bool CanMoveToPreviousNotebook ()
		{
			return TabControl.GetPreviousNotebook () != null || (TabControl.Container.AllowLeftInsert && TabControl.TabCount > 1);
		}

		public void MoveToNextNotebook ()
		{
			var nextNotebook = TabControl.GetNextNotebook ();
			if (nextNotebook == null) {
				if (TabControl.Container.AllowRightInsert) {
					TabControl.RemoveTab (tab.Index, true);
					TabControl.Container.InsertRight (this);
					SelectWindow ();
				}
				return;
			}

			TabControl.RemoveTab (tab.Index, true);
			var newTab = nextNotebook.AddTab ();
			newTab.Content = this;
			SetDockNotebook (nextNotebook, newTab);
			SelectWindow ();
		}

		public void MoveToPreviousNotebook ()
		{
			var nextNotebook = TabControl.GetPreviousNotebook ();
			if (nextNotebook == null) {
				if (TabControl.Container.AllowLeftInsert) {
					TabControl.RemoveTab (tab.Index, true);
					TabControl.Container.InsertLeft (this);
					SelectWindow ();
				}
				return;
			}

			TabControl.RemoveTab (tab.Index, true);
			var newTab = nextNotebook.AddTab ();
			newTab.Content = this;
			SetDockNotebook (nextNotebook, newTab);
			SelectWindow ();
		}
		
		static void DeepGrabFocus (Gtk.Widget widget)
		{
			Widget first = null;

			foreach (var f in GetFocussableWidgets (widget)) {
				if (f.HasFocus)
					return;
				
				if (first == null)
					first = f;
			}
			if (first != null) {
				first.GrabFocus ();
			}
		}
		
		static IEnumerable<Gtk.Widget> GetFocussableWidgets (Gtk.Widget widget)
		{
			var c = widget as Container;

			if (widget.CanFocus) {
				yield return widget;
			}

			if (c != null) {
				foreach (var f in c.FocusChain.SelectMany (GetFocussableWidgets).Where (y => y != null)) {
					yield return f;
				}
			}

			if (c?.Children?.Length != 0) {
				foreach (var f in c.Children) {
					var container = f as Container;
					if (container != null) {
						foreach (var child in GetFocussableChildren (container)) {
							yield return child;
						}
					}
				}
			}
		}

		static IEnumerable<Gtk.Widget> GetFocussableChildren (Gtk.Container container)
		{
			if (container.CanFocus) {
				yield return container;
			}

			foreach (var f in container.Children) {
				var c = f as Container;
				if (c != null) {
					foreach (var child in GetFocussableChildren (c)) {
						yield return child;
					}
				}
			}
		}

		public DocumentToolbar GetToolbar (BaseViewContent targetView)
		{
			if (!viewContentData.TryGetValue (targetView, out var data))
				throw new ArgumentException ("viewContent is not registered in window.");
			if (data.Toolbar == null) {
				var toolbar = new DocumentToolbar ();
				data.Toolbar = toolbar;
				data.VBox.PackStart (toolbar.Container, false, false, 0);
				data.VBox.ReorderChild (toolbar.Container, 0);
				DetachFromPathedDocument (targetView);
			}
			return data.Toolbar;
		}

		public ViewContent ViewContent {
			get {
				return content;
			}
			set {
				content = value;
			}
		}

		public MonoDevelop.Ide.Gui.ViewCommandHandlers CommandHandler {
			get {
				return commandHandler;
			}
		}

		public string DocumentType {
			get {
				return documentType;
			}
			set {
				documentType = value;
			}
		}
		
		public void SetTitleEvent(object sender, EventArgs e)
		{
			if (content == null)
				return;
				
			string newTitle = "";
			if (content.ContentName == null) {
				if (myUntitledTitle == null) {
					string baseName  = System.IO.Path.GetFileNameWithoutExtension(content.UntitledName);
					int number = 1;
					bool found = true;
					myUntitledTitle = baseName + System.IO.Path.GetExtension (content.UntitledName);
					while (found) {
						found = false;
						foreach (ViewContent windowContent in workbench.InternalViewContentCollection) {
							string title = windowContent.WorkbenchWindow.Title;
							if (title.EndsWith("+")) {
								title = title.Substring(0, title.Length - 1);
							}
							if (title == myUntitledTitle) {
								myUntitledTitle = baseName + number + System.IO.Path.GetExtension (content.UntitledName);
								found = true;
								++number;
								break;
							}
						}
					}
				}
				newTitle = myUntitledTitle;
			} else {
				newTitle = System.IO.Path.GetFileName(content.ContentName);
			}
			
			if (content.IsDirty) {
				if (!String.IsNullOrEmpty (content.ContentName))
					IdeApp.ProjectOperations.MarkFileDirty (content.ContentName);
			} else if (content.IsReadOnly) {
				newTitle += "+";
			}
			if (newTitle != Title) {
				Title = newTitle;
			}
		}
		
		public Task<bool> CloseWindow (bool force)
		{
			return CloseWindow (force, false);
		}

		public async Task<bool> CloseWindow (bool force, bool animate)
		{
			bool wasActive = workbench.ActiveWorkbenchWindow == this;
			WorkbenchWindowEventArgs args = new WorkbenchWindowEventArgs (force, wasActive);
			args.Cancel = false;
			await OnClosing (args);
			if (args.Cancel)
				return false;
			
			workbench.RemoveTab (tabControl, tab.Index, animate);

			OnClosed (args);

			// This may happen if the document contains an attached view that is shown by
			// default. In that case the main view is not added to the notebook and won't
			// be destroyed.
			bool destroyMainPage = tabPage != null && tabPage.Parent == null;

			Destroy ();

			// Destroy after the document is destroyed, since attached views may have references to the main view
			if (destroyMainPage)
				tabPage.Destroy ();
			
			return true;
		}

		protected override void OnDestroyed ()
		{
			if (viewContents != null) {
				foreach (BaseViewContent sv in SubViewContents) {
					DetachFromPathedDocument (sv);
					sv.Dispose ();
				}
				viewContents = null;
			}

			if (content != null) {
				content.ContentNameChanged -= SetTitleEvent;
				content.DirtyChanged       -= HandleDirtyChanged;
				content.WorkbenchWindow     = null;
				content.Dispose ();
				content = null;
			}

			if (subViewToolbar != null) {
				subViewToolbar.Dispose ();
				subViewToolbar = null;
			}

			foreach (var data in viewContentData.Values) {
				data.Dispose ();
			}
			viewContentData.Clear ();

			commandHandler = null;
			document = null;
			extensionContext = null;
			base.OnDestroyed ();
		}
		
		#region lazy UI element creation
		
		void CheckCreateSubViewToolbar ()
		{
			if (subViewToolbar != null)
				return;
			
			subViewToolbar = new Tabstrip ();
			subViewToolbar.Show ();
			
			CheckCreateToolbarBox ();
			toolbarBox.PackStart (subViewToolbar, true, true, 0);
		}

		void CheckCreateToolbarBox ()
		{
			if (toolbarBox != null)
				return;
			toolbarBox = new HBox (false, 0);
			toolbarBox.Show ();
			box.PackEnd (toolbarBox, false, false, 0);
		}
		
		void CheckCreateSubViewContents ()
		{
			if (subViewNotebook != null)
				return;
			
			subViewNotebook = new HPaned ();
			//add existing ViewContent
			InsertButton (0, this.ViewContent);
			
			//pack them in a box
			subViewNotebook.Show ();
			box.PackStart (subViewNotebook, true, true, 1);
			box.Show ();
		}

		void ShowDocumentToolbar (DocumentToolbar toolbar)
		{
		}

		#endregion

		public void AttachViewContent (BaseViewContent subViewContent)
		{
			InsertViewContent (viewContents.Count, subViewContent);
		}

		public void InsertViewContent (int index, BaseViewContent subViewContent)
		{
			// need to create child Notebook when first IAttachableViewContent is added
			CheckCreateSubViewContents ();

			viewContents.Insert (index, subViewContent);
			var viewData = new ViewContentData (this) {
				ViewContent = subViewContent
			};
			viewContentData.Add (subViewContent, viewData);
			subViewContent.WorkbenchWindow = this;
			InsertButton (index, subViewContent);

			if (ViewsChanged != null)
				ViewsChanged (this, EventArgs.Empty);
		}

		bool updating = false;
		protected void InsertButton (int index, BaseViewContent viewContent)
		{
			CheckCreateSubViewToolbar ();
			updating = true;

			var addedContent = (index == 0 || subViewToolbar.TabCount == 0) && IdeApp.Workbench.ActiveDocument == Document;
			// If this is the current displayed document we need to add the control immediately as the tab is already active.
			if (addedContent)
				SetCurrentViews (new [] { viewContent });
			var tabIndex = index;
			IEnumerable<TabPageInfo> tabPageInfos;
			if (viewContent is IMultipleTabs multipleTabs) {
				tabPageInfos = multipleTabs.GetTabPageInfos ();
			} else {
				tabPageInfos = new [] { new TabPageInfo (viewContent.TabPageLabel, viewContent.TabAccessibilityDescription, new [] { viewContent }) };
			}

			foreach (var tabInfo in tabPageInfos) {
				var newTab = new Tab (subViewToolbar, tabInfo.Label) {
					ViewsSelection = tabInfo.ViewsSelection
				};
				if (newTab.Accessible != null) {
					newTab.Accessible.Help = tabInfo.AccessibilityDescription;
				}
				subViewToolbar.InsertTab (tabIndex++, newTab);
				newTab.Activated += (sender, e) => {
					SetCurrentViews (((Tab)sender).ViewsSelection);
				};
			}

			updating = false;

			if (index == 0)
				ShowPage (viewContent);
		}

		#region Track and display document's "path"

		void AttachToPathedDocument (BaseViewContent viewContent)
		{
			if (!viewContentData.TryGetValue (viewContent, out var data))
				throw new ArgumentException ("viewContent is not registered in window.");
			// For backward compatibility don't show PathBar if Toolbar is present
			if (data.Toolbar != null)
				return;
			var pathDoc = viewContent.GetContent<IPathedDocument> ();
			if (data.PathedDocument == pathDoc)
				return;
			DetachFromPathedDocument (viewContent);
			if (pathDoc == null)
				return;
			var pathBar = new PathBar (pathDoc.CreatePathWidget);
			pathDoc.PathChanged += pathBar.HandlePathChange;
			data.VBox.PackStart (pathBar, false, true, 0);
			data.VBox.ReorderChild (pathBar, 0);
			pathBar.Show ();

			pathBar.SetPath (pathDoc.CurrentPath);
			data.Pathbar = pathBar;
			data.PathedDocument = pathDoc;
		}

		void DetachFromPathedDocument (BaseViewContent viewContent)
		{
			if (!viewContentData.TryGetValue (viewContent, out var data))
				throw new ArgumentException ("viewContent is not registered in this window.");
			if (data.PathedDocument == null)
				return;
			data.PathedDocument.PathChanged -= data.Pathbar.HandlePathChange;
			data.PathedDocument = null;
			data.VBox.Remove (data.Pathbar);
			data.Pathbar.Destroy ();
			data.Pathbar = null;
		}

		#endregion

		protected void ShowPage (BaseViewContent viewContent)
		{
			if (updating || viewContent == null) return;
			updating = true;
			for (int i = 0; i < subViewToolbar.TabCount; i++) {
				var tab = subViewToolbar.Tabs [i];
				if (tab.ViewsSelection.Length == 1 && tab.ViewsSelection [0] == viewContent) {
					subViewToolbar.ActiveTab = i;
					break;
				}
			}
			updating = false;
		}

		internal void UpdatePathedDocument ()
		{
			foreach (var view in currentViews) {
				AttachToPathedDocument (view);
			}
		}

		void SetCurrentViews (BaseViewContent[] newSelection)
		{
			var removedViews = currentViews.Except (newSelection).ToArray ();
			var addedViews = newSelection.Except (currentViews).ToArray ();
			currentViews.Clear ();
			currentViews.AddRange (newSelection);
			while (subViewNotebook.Children.Length > 0)
				subViewNotebook.Remove (subViewNotebook.Children [0]);
			if (currentViews.Count > 0) {
				var data = viewContentData [currentViews [0]];
				data.EnsureControlAdded ();
				subViewNotebook.Pack1 (data.VBox, true, true);
				AttachToPathedDocument (currentViews [0]);
			}
			if (currentViews.Count > 1) {
				var data = viewContentData [currentViews [1]];
				data.EnsureControlAdded ();
				subViewNotebook.Pack2 (data.VBox, true, true);
				subViewNotebook.Position = box.Allocation.Width / 2;
				AttachToPathedDocument (currentViews [1]);
			}
			foreach (var removedView in removedViews) {
				removedView.OnDeselected ();
			}
			foreach (var addedView in addedViews) {
				addedView.OnSelected ();
			}

			if(!currentViews.Contains(ActiveViewContent)){
				ActiveViewContent = currentViews [0];
			}
			QueueDraw ();
		}

		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			return Parent;
		}
		
		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
			// If command checks are flowing through this view, it means the view's notebook
			// is the active notebook.
			if (!(Toplevel is Gtk.Window))
				return null;

			if (((Gtk.Window)Toplevel).HasToplevelFocus)
				DockNotebook.ActiveNotebook = (SdiDragNotebook)Parent.Parent;

			// Route commands to the view
			return ActiveViewContent;
		}

		void SetDockNotebookTabTitle ()
		{
			tab.Text = Title;
			tab.Notify = show_notification;
			tab.Dirty = content.IsDirty;
			if (content.ContentName != null && content.ContentName != "") {
				tab.Tooltip = content.ContentName;
			}
			try {
				if (content.StockIconId != null) {
					tab.Icon = ImageService.GetIcon (content.StockIconId, IconSize.Menu);
				}
				else
					if (content.ContentName != null && content.ContentName.IndexOfAny (new char[] {
						'*',
						'+'
					}) == -1) {
						tab.Icon = DesktopService.GetIconForFile (content.ContentName, Gtk.IconSize.Menu);
					}
			}
			catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				tab.Icon = DesktopService.GetIconForType ("gnome-fs-regular", Gtk.IconSize.Menu);
			}
		}
		
		protected virtual void OnTitleChanged(EventArgs e)
		{
			fileTypeCondition.SetFileName (content.ContentName ?? content.UntitledName);
			SetDockNotebookTabTitle ();
			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}

		protected virtual async Task OnClosing (WorkbenchWindowEventArgs e)
		{
			if (Closing != null) {
				foreach (var handler in Closing.GetInvocationList ().Cast<WorkbenchWindowAsyncEventHandler> ()) {
					await handler (this, e);
					if (e.Cancel)
						break;
				}
			}
		}

		protected virtual void OnClosed (WorkbenchWindowEventArgs e)
		{
			if (Closed != null) {
				Closed (this, e);
			}
		}
		
		protected virtual void OnActiveViewContentChanged (ActiveViewContentEventArgs e)
		{
			if (ActiveViewContentChanged != null)
				ActiveViewContentChanged (this, e);
		}

		public event EventHandler TitleChanged;
		public event WorkbenchWindowEventHandler Closed;
		public event WorkbenchWindowAsyncEventHandler Closing;
		public event ActiveViewContentEventHandler ActiveViewContentChanged;
	}
}
