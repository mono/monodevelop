//  SdiWorkspaceWindow.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.IO;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui
{
	internal class SdiWorkspaceWindow : Frame, IWorkbenchWindow, ICommandDelegatorRouter
	{
		IWorkbench workbench;
		IViewContent content;
		
		ArrayList subViewContents = null;
		Notebook subViewNotebook = null;
		Toolbar subViewToolbar = null;
		HBox pathBox = null;
		HBox toolbarBox = null;
		
		VBox box;
		TabLabel tabLabel;
		Widget    tabPage;
		Notebook  tabControl;
		
		string myUntitledTitle     = null;
		string _titleHolder = "";
		
		string documentType;
		MonoDevelop.Ide.Gui.Content.IPathedDocument pathDoc;
		
		bool show_notification = false;
		
		ViewCommandHandlers commandHandler;
		
		public SdiWorkspaceWindow (IWorkbench workbench, IViewContent content, Notebook tabControl, TabLabel tabLabel) : base ()
		{
			this.workbench = workbench;
			this.tabControl = tabControl;
			this.content = content;
			this.tabLabel = tabLabel;
			this.tabPage = content.Control;
			
			content.WorkbenchWindow = this;
			
			content.ContentNameChanged += new EventHandler(SetTitleEvent);
			content.DirtyChanged       += new EventHandler(SetTitleEvent);
			content.BeforeSave         += new EventHandler(BeforeSave);
			content.ContentChanged     += new EventHandler (OnContentChanged);
			
			ShadowType = ShadowType.None;
			box = new VBox ();
			box.Add (content.Control);
			Add (box);
			
			Show ();
			box.Show ();
			content.Control.Show ();
			SetTitleEvent(null, null);
			
			commandHandler = new ViewCommandHandlers (this);
		}
		
		protected SdiWorkspaceWindow (IntPtr p): base (p)
		{
		}
		
		public Widget TabPage {
			get {
				return tabPage;
			}
			set {
				tabPage = value;
			}
		}
		
		internal TabLabel TabLabel {
			get { return tabLabel; }
		}

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
		
		public ArrayList SubViewContents {
			get {
				return subViewContents;
			}
		}
		
		// caution use activeView with care !!
		IBaseViewContent activeView = null;
		public IBaseViewContent ActiveViewContent {
			get {
				if (activeView != null)
					return activeView;
				if (subViewNotebook != null && subViewNotebook.CurrentPage > 0) {
					return (IBaseViewContent)subViewContents[subViewNotebook.CurrentPage - 1];
				}
				return content;
			}
			set {
				this.activeView = value;
				this.OnActiveViewContentChanged (new ActiveViewContentEventArgs (value));
			}
		}
		
		public void SwitchView (int viewNumber)
		{
			if (subViewNotebook != null)
				ShowPage (viewNumber);
		}
		
		public void SelectWindow()	
		{
			int toSelect = tabControl.PageNum (this);
			tabControl.CurrentPage = toSelect;
		}
		

		void BeforeSave(object sender, EventArgs e)
		{
			ISecondaryViewContent secondaryViewContent = ActiveViewContent as ISecondaryViewContent;
			if (secondaryViewContent != null) {
				secondaryViewContent.NotifyBeforeSave();
			}
		}
		
		public IViewContent ViewContent {
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
			if (content == null) {
				return;
			}
		
			string newTitle = "";
			if (content.ContentName == null) {
				if (myUntitledTitle == null) {
					string baseName  = System.IO.Path.GetFileNameWithoutExtension(content.UntitledName);
					int number = 1;
					bool found = true;
					myUntitledTitle = baseName + System.IO.Path.GetExtension (content.UntitledName);
					while (found) {
						found = false;
						foreach (IViewContent windowContent in workbench.ViewContentCollection) {
							string title = windowContent.WorkbenchWindow.Title;
							if (title.EndsWith("*") || title.EndsWith("+")) {
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
				newTitle += "*";
				IdeApp.ProjectOperations.MarkFileDirty (content.ContentName);
			} else if (content.IsReadOnly) {
				newTitle += "+";
			}
			
			if (newTitle != Title) {
				Title = newTitle;
			}
		}
		
		public void OnContentChanged (object o, EventArgs e)
		{
			if (subViewContents != null) {
				foreach (ISecondaryViewContent subContent in subViewContents)
				{
					subContent.BaseContentChanged ();
				}
			}
		}
		
		public void CloseWindow (bool force, bool fromMenu, int pageNum)
		{
			WorkbenchWindowEventArgs args = new WorkbenchWindowEventArgs (force);
			OnClosing (args);
			if (args.Cancel)
				return;

			if (fromMenu == true) {
				workbench.WorkbenchLayout.RemoveTab (tabControl.PageNum(this));
			} else {
				workbench.WorkbenchLayout.RemoveTab (pageNum);
			}
			
			content.ContentNameChanged -= new EventHandler(SetTitleEvent);
			content.DirtyChanged       -= new EventHandler(SetTitleEvent);
			content.BeforeSave         -= new EventHandler(BeforeSave);
			content.ContentChanged     -= new EventHandler (OnContentChanged);
			content.WorkbenchWindow = null;
			
			if (subViewContents != null) {
				foreach (ISecondaryViewContent sv in subViewContents) {
					subViewNotebook.Remove (sv.Control);
					sv.Dispose ();
				}
				this.subViewContents = null;
				subViewNotebook.Remove (content.Control);
			} else {
				box.Remove (content.Control);
			}
			content.Dispose ();
			tabLabel.Dispose ();
			DetachFromPathedDocument ();

			OnClosed (null);
			
			this.content = null;
			this.subViewNotebook = null;
			this.tabControl = null;
			this.tabLabel = null;
			this.tabPage = null;
			Destroy ();
		}
		
		#region lazy UI element creation
		
		void CheckCreateSubViewToolbar ()
		{
			if (subViewToolbar != null)
				return;
			
			subViewToolbar = new Toolbar ();
			subViewToolbar.IconSize = IconSize.SmallToolbar;
			subViewToolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			subViewToolbar.ShowArrow = false;
			subViewToolbar.Show ();
			
			CheckCreateToolbarBox ();
			toolbarBox.PackStart (subViewToolbar, false, false, 0);
		}
		
		void CheckCreateToolbarBox ()
		{
			if (toolbarBox != null)
				return;
			toolbarBox = new HBox (false, 6);
			toolbarBox.Show ();
			box.PackEnd (toolbarBox, false, false, 0);
		}
		
		void CheckCreateSubViewContents ()
		{
			if (subViewContents != null)
				return;
			
			subViewContents = new ArrayList ();
			
			box.Remove (this.ViewContent.Control);
			
			subViewNotebook = new Notebook ();
			subViewNotebook.TabPos = PositionType.Bottom;
			subViewNotebook.ShowTabs = false;
			subViewNotebook.Show ();
			subViewNotebook.SwitchPage += subViewNotebookIndexChanged;
			
			//add existing ViewContent
			AddButton (this.ViewContent.TabPageLabel, this.ViewContent.Control).Active = true;
			
			//pack them in a box
			box.PackStart (subViewNotebook, true, true, 0);
			box.ShowAll ();
		}
		
		#endregion
		
		public void AttachSecondaryViewContent(ISecondaryViewContent subViewContent)
		{
			// need to create child Notebook when first ISecondaryViewContent is added
			CheckCreateSubViewContents ();
			
			subViewContents.Add (subViewContent);
			subViewContent.WorkbenchWindow = this;
			AddButton (subViewContent.TabPageLabel, subViewContent.Control);
			
			OnContentChanged (null, null);
		}
		
		bool updating = false;
		protected ToggleToolButton AddButton (string label, Gtk.Widget page)
		{
			CheckCreateSubViewToolbar ();
			updating = true;
			ToggleToolButton button = new ToggleToolButton ();
			button.Label = label;
			button.IsImportant = true;
			button.Clicked += new EventHandler (OnButtonToggled);
			button.ShowAll ();
			subViewToolbar.Insert (button, -1);
			subViewNotebook.AppendPage (page, new Gtk.Label ());
			page.ShowAll ();
			updating = false;
			return button;
		}
		
		#region Track and display document's "path"
		
		internal void AttachToPathedDocument (MonoDevelop.Ide.Gui.Content.IPathedDocument pathDoc)
		{
			if (this.pathDoc != pathDoc)
				DetachFromPathedDocument ();
			if (pathDoc == null)
				return;
			PathWidgetEnabled = true;
			pathDoc.PathChanged += HandlePathChange;
			this.pathDoc = pathDoc;
		}
		
		internal void DetachFromPathedDocument ()
		{
			if (pathDoc == null)
				return;
			PathWidgetEnabled = false;
			pathDoc.PathChanged -= HandlePathChange;
			pathDoc = null;
		}
		
		void HandlePathChange (object sender, MonoDevelop.Ide.Gui.Content.DocumentPathChangedEventArgs args)
		{
			MonoDevelop.Ide.Gui.Content.IPathedDocument pathDoc = (MonoDevelop.Ide.Gui.Content.IPathedDocument) sender;
			
			while (pathBox.Children.Length > 0)
				pathBox.Remove (pathBox.Children[0]);
			
			if (pathDoc.CurrentPath == null || pathDoc.CurrentPath.Length == 0)
				return;
			
			for (int i = 0; i < pathDoc.CurrentPath.Length; i++) {
				PathMenuButton button = new PathMenuButton (pathDoc, i);
				button.ArrowType = (i + 1 < pathDoc.CurrentPath.Length)? ArrowType.Right : (ArrowType?) null;
				if (i == pathDoc.SelectedIndex)
					button.Markup = string.Concat ("<b>", pathDoc.CurrentPath[i] ,"</b>");
				else
					button.Label = pathDoc.CurrentPath[i];
				pathBox.PackStart (button, false, false, 0);
			}
			pathBox.PackEnd (new Label (string.Empty), true, true, 0);
			pathBox.ShowAll ();
		}
		
		bool PathWidgetEnabled {
			get { return (pathBox != null); }
			set {
				if (PathWidgetEnabled == value)
					return;
				if (value) {
					CheckCreateToolbarBox ();
					
					pathBox = new HBox ();
					pathBox.Spacing = 0;
					
					toolbarBox.PackEnd (pathBox, true, true, 0);
					toolbarBox.ShowAll ();
				} else {
					toolbarBox.Remove (pathBox);
					toolbarBox.Destroy ();
				}
			}
		}
		
		private class PathMenuButton : MenuButton
		{
			MonoDevelop.Ide.Gui.Content.IPathedDocument pathDoc;
			int index;
			
			public PathMenuButton (MonoDevelop.Ide.Gui.Content.IPathedDocument pathDoc, int index)
			{
				this.pathDoc = pathDoc;
				this.index = index;
				this.MenuCreator = PathMenuCreator;
				this.Relief = Gtk.ReliefStyle.None;
			}
			
			Menu PathMenuCreator (MenuButton button)	
			{
				Menu menu = new Menu ();
				MenuItem mi = new MenuItem (GettextCatalog.GetString ("Select"));
				mi.Activated += delegate {
					pathDoc.SelectPath (index);
				};
				menu.Add (mi);
				mi = new MenuItem (GettextCatalog.GetString ("Select contents"));
				mi.Activated += delegate {
					pathDoc.SelectPathContents (index);
				};
				menu.Add (mi);
				menu.ShowAll ();
				return menu;
			}
		}
		
		#endregion
		
		protected void ShowPage (int npage)
		{
			if (subViewNotebook.CurrentPage == npage)
				return;
				
			if (updating) return;
			updating = true;
			
			subViewNotebook.CurrentPage = npage;
			Gtk.Widget[] buttons = subViewToolbar.Children;
			for (int n=0; n<buttons.Length; n++) {
				ToggleToolButton b = (ToggleToolButton) buttons [n];
				b.Active = (n == npage);
			}

			updating = false;
		}
		
		void OnButtonToggled (object s, EventArgs args)
		{
			int i = Array.IndexOf (subViewToolbar.Children, s);
			if (i != -1)
				ShowPage (i);
		}
		
		int oldIndex = -1;
		protected void subViewNotebookIndexChanged(object sender, SwitchPageArgs e)
		{
			if (oldIndex > 0) {
				ISecondaryViewContent secondaryViewContent = subViewContents[oldIndex - 1] as ISecondaryViewContent;
				if (secondaryViewContent != null) {
					secondaryViewContent.Deselected();
				}
			}
			
			if (subViewNotebook.CurrentPage > 0) {
				ISecondaryViewContent secondaryViewContent = subViewContents[subViewNotebook.CurrentPage - 1] as ISecondaryViewContent;
				if (secondaryViewContent != null) {
					secondaryViewContent.Selected();
				}
			}
			oldIndex = subViewNotebook.CurrentPage;
			
			OnActiveViewContentChanged (new ActiveViewContentEventArgs (this.ActiveViewContent));
		}

		object ICommandDelegatorRouter.GetNextCommandTarget ()
		{
			return Parent;
		}
		
		object ICommandDelegatorRouter.GetDelegatedCommandTarget ()
		{
			Gtk.Widget w = content as Gtk.Widget;
			if (w != this.tabPage) {
				// Route commands to the view
				return content;
			} else
				return null;
		}
		
		protected virtual void OnTitleChanged(EventArgs e)
		{
			if (show_notification) {
				tabLabel.Label.Markup = "<span foreground=\"blue\">" + Title + "</span>";
				tabLabel.Label.UseMarkup = true;
			} else {
				tabLabel.Label.Text = Title;
				tabLabel.Label.UseMarkup = false;
			}
			
			if (content.ContentName != null && content.ContentName != "") {
				tabLabel.SetTooltip (content.ContentName, content.ContentName);
			}

			try {
				if (content.StockIconId != null ) {
					tabLabel.Icon = new Gtk.Image ( content.StockIconId, IconSize.Menu );
				}
				else if (content.ContentName != null && content.ContentName.IndexOfAny (new char[] { '*', '+'}) == -1) {
					tabLabel.Icon.Pixbuf = IdeApp.Services.PlatformService.GetPixbufForFile (content.ContentName, Gtk.IconSize.Menu);
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				tabLabel.Icon.Pixbuf = IdeApp.Services.PlatformService.GetPixbufForType ("gnome-fs-regular", Gtk.IconSize.Menu);
			}

			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}

		protected virtual void OnClosing (WorkbenchWindowEventArgs e)
		{
			if (Closing != null) {
				Closing (this, e);
			}
		}

		protected virtual void OnClosed (EventArgs e)
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
		public event EventHandler Closed;
		public event WorkbenchWindowEventHandler Closing;
		public event ActiveViewContentEventHandler ActiveViewContentChanged;
	}
}
