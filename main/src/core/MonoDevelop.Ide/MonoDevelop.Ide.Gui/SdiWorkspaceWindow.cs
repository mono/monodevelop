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
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui
{
	internal class SdiWorkspaceWindow : Frame, IWorkbenchWindow, ICommandDelegatorRouter
	{
		DefaultWorkbench workbench;
		IViewContent content;
		
		List<IAttachableViewContent> subViewContents = null;
		Notebook subViewNotebook = null;
		Tabstrip subViewToolbar = null;
		PathBar pathBar = null;
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
		
		public SdiWorkspaceWindow (DefaultWorkbench workbench, IViewContent content, Notebook tabControl, TabLabel tabLabel) : base ()
		{
			this.workbench = workbench;
			this.tabControl = tabControl;
			this.content = content;
			this.tabLabel = tabLabel;
			this.tabPage = content.Control;
			
			ShadowType = ShadowType.None;
			box = new VBox ();
			Add (box);
			box.PackStart (content.Control);
			
			content.WorkbenchWindow = this;
			
			content.ContentNameChanged += new EventHandler(SetTitleEvent);
			content.DirtyChanged       += new EventHandler(SetTitleEvent);
			content.BeforeSave         += new EventHandler(BeforeSave);
			content.ContentChanged     += new EventHandler (OnContentChanged);
			
			box.Show ();
			
			SetTitleEvent(null, null);
			
			commandHandler = new ViewCommandHandlers (this);
			Show ();
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
		
		public IEnumerable<IAttachableViewContent> SubViewContents {
			get {
				return (IEnumerable<IAttachableViewContent>)subViewContents ?? new IAttachableViewContent[0];
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
		
		public void SwitchView (IAttachableViewContent view)
		{
			if (subViewNotebook != null)
				// adding 1 because subviews start at the position 1 of the tab strip. Position 0 is
				// for the main view
				ShowPage (subViewContents.IndexOf (view) + 1);
		}
		
		public int FindView (Type viewType)
		{
			if (viewType.Equals (ViewContent.GetType ()))
				return 0;
				
			int i = 1;
			foreach (IAttachableViewContent item in SubViewContents) {
				if (viewType.Equals (item.GetType ()))
					return i;
				i++;
			}
			
			return -1;
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
						foreach (IViewContent windowContent in workbench.InternalViewContentCollection) {
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
			if (subViewContents == null)
				return;
			foreach (IAttachableViewContent subContent in subViewContents) {
				subContent.BaseContentChanged ();
			}
		}
		
		public bool CloseWindow (bool force, bool fromMenu, int pageNum)
		{
			bool wasActive = workbench.ActiveWorkbenchWindow == this;
			WorkbenchWindowEventArgs args = new WorkbenchWindowEventArgs (force, wasActive);
			args.Cancel = false;
			OnClosing (args);
			if (args.Cancel)
				return false;
			
			if (fromMenu == true) {
				workbench.RemoveTab (tabControl.PageNum(this));
			} else {
				workbench.RemoveTab (pageNum);
			}
			OnClosed (args);
			
			if (subViewContents != null) {
				foreach (IAttachableViewContent sv in subViewContents) {
					sv.Dispose ();
				}
			}
			
			content.ContentNameChanged -= new EventHandler(SetTitleEvent);
			content.DirtyChanged       -= new EventHandler(SetTitleEvent);
			content.BeforeSave         -= new EventHandler(BeforeSave);
			content.ContentChanged     -= new EventHandler (OnContentChanged);
			content.WorkbenchWindow     = null;
			content.Dispose ();
			
			DetachFromPathedDocument ();
			Destroy ();
			return true;
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
		
		void EnsureToolbarBoxSeparator ()
		{
/*			The path bar is now shown at the top

			if (toolbarBox == null || subViewToolbar == null)
				return;

			if (separatorItem != null && pathBar == null) {
				subViewToolbar.Remove (separatorItem);
				separatorItem = null;
			} else if (separatorItem == null && pathBar != null) {
				separatorItem = new SeparatorToolItem ();
				subViewToolbar.PackStart (separatorItem, false, false, 0);
			} else if (separatorItem != null && pathBar != null) {
				Widget[] buttons = subViewToolbar.Children;
				if (separatorItem != buttons [buttons.Length - 1])
					subViewToolbar.ReorderChild (separatorItem, buttons.Length - 1);
			}
			*/
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
			if (subViewContents != null)
				return;
			
			subViewContents = new List<IAttachableViewContent> ();
			
			box.Remove (this.ViewContent.Control);
			
			subViewNotebook = new Notebook ();
			subViewNotebook.TabPos = PositionType.Bottom;
			subViewNotebook.ShowTabs = false;
			subViewNotebook.ShowBorder = false;
			subViewNotebook.Show ();
			
			//add existing ViewContent
			AddButton (this.ViewContent.TabPageLabel, this.ViewContent);
			
			//pack them in a box
			box.PackStart (subViewNotebook, true, true, 1);
			box.ShowAll ();
		}
		#endregion
		
			
		public void AttachViewContent (IAttachableViewContent subViewContent)
		{
			// need to create child Notebook when first IAttachableViewContent is added
			CheckCreateSubViewContents ();
			
			subViewContents.Add (subViewContent);
			subViewContent.WorkbenchWindow = this;
			AddButton (subViewContent.TabPageLabel, subViewContent);
			
			OnContentChanged (null, null);
		}
		
		bool updating = false;
		protected Tab AddButton (string label, IBaseViewContent viewContent)
		{
			CheckCreateSubViewToolbar ();
			updating = true;
			
			Tab tab = new Tab (subViewToolbar, label);
			tab.Tag = subViewToolbar.TabCount;
			tab.Activated += (sender, e) => { SetCurrentView ((int)((Tab)sender).Tag); QueueDraw (); };
			subViewToolbar.AddTab (tab);
			
			Gtk.VBox widgetBox = new Gtk.VBox ();
			widgetBox.Realized += delegate {
				widgetBox.Add (viewContent.Control);
			};
			
			subViewNotebook.AppendPage (widgetBox, new Gtk.Label ());
			widgetBox.ShowAll ();
			
			EnsureToolbarBoxSeparator ();
			updating = false;
			return tab;
		}
		
		#region Track and display document's "path"
		
		internal void AttachToPathedDocument (MonoDevelop.Ide.Gui.Content.IPathedDocument pathDoc)
		{
			if (this.pathDoc != pathDoc)
				DetachFromPathedDocument ();
			if (pathDoc == null)
				return;
			pathDoc.PathChanged += HandlePathChange;
			this.pathDoc = pathDoc;
			PathWidgetEnabled = true;
			pathBar.SetPath (pathDoc.CurrentPath);
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
//			pathBar.SetActive (pathDoc.SelectedIndex);
		}
		
		bool PathWidgetEnabled {
			get { return (pathBar != null); }
			set {
				if (PathWidgetEnabled == value)
					return;
				if (value) {
					pathBar = new PathBar (pathDoc.CreatePathWidget);
					box.PackStart (pathBar, false, true, 0);
					box.ReorderChild (pathBar, 0);
					pathBar.Show ();
				} else {
					box.Remove (pathBar);
					pathBar.Destroy ();
					pathBar = null;
				}
			}
		}
		
		#endregion
		
		protected void ShowPage (int npage)
		{
			if (updating || npage < 0) return;
			updating = true;
			subViewToolbar.ActiveTab = npage;
			updating = false;
		}
		
		int oldIndex = -1;
		
		void SetCurrentView (int newIndex)
		{
			subViewNotebook.CurrentPage = newIndex;
			
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
			DetachFromPathedDocument ();
			
			MonoDevelop.Ide.Gui.Content.IPathedDocument pathedDocument;
			if (oldIndex <= 0) {
				pathedDocument = Document != null ? Document.GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> () : ViewContent.GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> ();
			} else {
				pathedDocument = subViewContents[oldIndex - 1].GetContent<MonoDevelop.Ide.Gui.Content.IPathedDocument> ();
			}

			if (pathedDocument != null)
				AttachToPathedDocument (pathedDocument);
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
