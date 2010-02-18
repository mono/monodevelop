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
		PathBar pathBar = null;
		HBox toolbarBox = null;
		
		VBox box;
		TabLabel tabLabel;
		Widget    tabPage;
		Notebook  tabControl;
		SeparatorToolItem separatorItem;
		
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

		public Document Document {
			get;
			set;
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
			if (this.Parent == null)
				return;
			int toSelect = tabControl.PageNum (this);
			tabControl.CurrentPage = toSelect;
			if (tabControl.FocusChild != null) {
				tabControl.FocusChild.GrabFocus ();
			} else {
				DeepGrabFocus (this.ActiveViewContent.Control);
			}
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
			if (first != null)
				first.GrabFocus ();
		}
		
		static IEnumerable<Gtk.Widget> GetFocussableWidgets (Gtk.Widget widget)
		{
			var c = widget as Container;
			if (widget.CanFocus)
				yield return widget;
			if (c != null) {
				foreach (var f in c.FocusChain.SelectMany (x => GetFocussableWidgets (x)).Where (y => y != null))
					yield return f;
			}
		}

		void BeforeSave(object sender, EventArgs e)
		{
			IAttachableViewContent secondaryViewContent = ActiveViewContent as IAttachableViewContent;
			if (secondaryViewContent != null) {
				secondaryViewContent.BeforeSave ();
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
				foreach (IAttachableViewContent subContent in subViewContents)
				{
					subContent.BaseContentChanged ();
				}
			}
		}
		
		public bool CloseWindow (bool force, bool fromMenu, int pageNum)
		{
			bool wasActive = workbench.WorkbenchLayout.ActiveWorkbenchwindow == this;
			WorkbenchWindowEventArgs args = new WorkbenchWindowEventArgs (force, wasActive);
			args.Cancel = false;
			OnClosing (args);
			if (args.Cancel)
				return false;
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
				foreach (IAttachableViewContent sv in subViewContents) {
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
			
			this.subViewToolbar = null;
			this.separatorItem = null;
			DetachFromPathedDocument ();

			OnClosed (args);
			
			this.content = null;
			this.subViewNotebook = null;
			this.tabControl = null;
			this.tabLabel = null;
			this.tabPage = null;
			Destroy ();
			return true;
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
		
		void EnsureToolbarBoxSeparator ()
		{
			if (toolbarBox == null || subViewToolbar == null)
				return;

			if (separatorItem != null && pathBar == null) {
				subViewToolbar.Remove (separatorItem);
				separatorItem = null;
			} else if (separatorItem == null && pathBar != null) {
				separatorItem = new SeparatorToolItem ();
				subViewToolbar.Insert (separatorItem, -1);
			} else if (separatorItem != null && pathBar != null) {
				if (subViewToolbar.GetItemIndex(separatorItem) != subViewToolbar.NumChildren - 1) {
					subViewToolbar.Remove (separatorItem);
					subViewToolbar.Insert (separatorItem, -1);
				}
			}
		}
		
		void CheckCreateToolbarBox ()
		{
			if (toolbarBox != null)
				return;
			toolbarBox = new HBox (false, 6);
			toolbarBox.Show ();
			box.PackEnd (toolbarBox, false, false, 3);
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
			subViewNotebook.ShowBorder = false;
			subViewNotebook.Show ();
			subViewNotebook.SwitchPage += subViewNotebookIndexChanged;
			
			//add existing ViewContent
			AddButton (this.ViewContent.TabPageLabel, this.ViewContent.Control).Active = true;
			
			//pack them in a box
			box.PackStart (subViewNotebook, true, true, 0);
			box.ShowAll ();
		}
		
		#endregion
		
			
		public void AttachViewContent (IAttachableViewContent subViewContent)
		{
			// need to create child Notebook when first IAttachableViewContent is added
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
			EnsureToolbarBoxSeparator ();
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
			var pathDoc = (MonoDevelop.Ide.Gui.Content.IPathedDocument) sender;
			pathBar.SetPath (pathDoc.CurrentPath);
			pathBar.SetActive (pathDoc.SelectedIndex);
		}
		
		bool PathWidgetEnabled {
			get { return (pathBar != null); }
			set {
				if (PathWidgetEnabled == value)
					return;
				if (value) {
					CheckCreateToolbarBox ();
					pathBar = new PathBar (CreatePathMenu);
					toolbarBox.PackEnd (pathBar, true, true, 0);
					toolbarBox.ShowAll ();
				} else {
					toolbarBox.Remove (pathBar);
					toolbarBox.Destroy ();
					pathBar = null;
					toolbarBox = null;
				}
				EnsureToolbarBoxSeparator ();
			}
		}
		
		Menu CreatePathMenu (int index)
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
		
		#endregion
		
		protected void ShowPage (int npage)
		{
			if (updating) return;
			updating = true;
			
			subViewNotebook.CurrentPage = npage;
			Gtk.Widget[] buttons = subViewToolbar.Children;
			for (int n=0; n<buttons.Length; n++) {
				if (buttons [n] is ToggleToolButton) {
					ToggleToolButton b = (ToggleToolButton) buttons [n];
					b.Active = (n == npage);
				}
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
				IAttachableViewContent secondaryViewContent = subViewContents[oldIndex - 1] as IAttachableViewContent;
				if (secondaryViewContent != null) {
					secondaryViewContent.Deselected();
				}
			}
			
			if (subViewNotebook.CurrentPage > 0) {
				IAttachableViewContent secondaryViewContent = subViewContents[subViewNotebook.CurrentPage - 1] as IAttachableViewContent;
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
					tabLabel.Icon = new Gtk.Image ((IconId) content.StockIconId, IconSize.Menu );
				}
				else if (content.ContentName != null && content.ContentName.IndexOfAny (new char[] { '*', '+'}) == -1) {
					tabLabel.Icon.Pixbuf = DesktopService.GetPixbufForFile (content.ContentName, Gtk.IconSize.Menu);
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				tabLabel.Icon.Pixbuf = DesktopService.GetPixbufForType ("gnome-fs-regular", Gtk.IconSize.Menu);
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
		public event WorkbenchWindowEventHandler Closing;
		public event ActiveViewContentEventHandler ActiveViewContentChanged;
	}
}
