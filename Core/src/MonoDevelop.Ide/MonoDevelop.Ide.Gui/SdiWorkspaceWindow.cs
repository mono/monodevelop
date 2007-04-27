// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using Gtk;

using Gdl;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Utils;
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
		
		TabLabel tabLabel;
		Widget    tabPage;
		Notebook  tabControl;
		
		string myUntitledTitle     = null;
		string _titleHolder = "";

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
			Add (content.Control);
			content.Control.Show ();
			Show ();
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
		
		public IBaseViewContent ActiveViewContent {
			get {
				if (subViewNotebook != null && subViewNotebook.CurrentPage > 0) {
					return (IBaseViewContent)subViewContents[subViewNotebook.CurrentPage - 1];
				}
				return content;
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
			}
			content.Dispose ();
			tabLabel.Dispose ();

			OnClosed (null);
			
			this.content = null;
			this.subViewNotebook = null;
			this.tabControl = null;
			this.tabLabel = null;
			this.tabPage = null;
			Destroy ();
		}
		
		public void AttachSecondaryViewContent(ISecondaryViewContent subViewContent)
		{
			// need to create child Notebook when first ISecondaryViewContent is added
			if (subViewContents == null) {
				subViewContents = new ArrayList ();
				
				this.Remove (this.Child);
				
				subViewNotebook = new Notebook ();
				subViewNotebook.TabPos = PositionType.Bottom;
				subViewNotebook.ShowTabs = false;
				subViewNotebook.Show ();
				subViewNotebook.SwitchPage += subViewNotebookIndexChanged;
			
				// Bottom toolbar
				subViewToolbar = new Toolbar ();
				subViewToolbar.IconSize = IconSize.SmallToolbar;
				subViewToolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
				subViewToolbar.ShowArrow = false;
				subViewToolbar.Show ();
				
				//add existing ViewContent
				AddButton (this.ViewContent.TabPageLabel, this.ViewContent.Control).Active = true;
				
				//pack them in a box
				VBox box = new VBox ();
				box.PackStart (subViewNotebook, true, true, 0);
				box.PackStart (subViewToolbar, false, false, 0);
				box.ShowAll ();
				this.Child = box;
			}
			
			subViewContents.Add (subViewContent);
			subViewContent.WorkbenchWindow = this;
			AddButton (subViewContent.TabPageLabel, subViewContent.Control);
			
			OnContentChanged (null, null);
		}
		
		bool updating = false;
		protected ToggleToolButton AddButton (string label, Gtk.Widget page)
		{
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
			commandHandler.SetNextCommandTarget (Parent); 
			return commandHandler;
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
				else if (content.ContentName.IndexOfAny (new char[] { '*', '+'}) == -1) {
					tabLabel.Icon.Pixbuf = FileIconLoader.GetPixbufForFile (content.ContentName, 16);
				}
			} catch {
				Console.WriteLine ( e);
				tabLabel.Icon.Pixbuf = FileIconLoader.GetPixbufForType ("gnome-fs-regular", 16);
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
