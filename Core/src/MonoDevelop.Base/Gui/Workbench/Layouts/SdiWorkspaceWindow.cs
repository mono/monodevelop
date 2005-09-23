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

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using MonoDevelop.Gui.Utils;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui
{
	internal class SdiWorkspaceWindow : Frame, IWorkbenchWindow, ICommandRouter
	{
		Notebook   viewTabControl = null;
		IViewContent content;
		ArrayList    subViewContents = null;

//		ArrayList    subDockItems = null;
		
		TabLabel tabLabel;
		Widget    tabPage;
		Notebook  tabControl;
		
		string myUntitledTitle     = null;
		string _titleHolder = "";

		bool show_notification = false;
		
		ViewCommandHandlers commandHandler;
		
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
				if (viewTabControl != null && viewTabControl.CurrentPage > 0) {
					return (IBaseViewContent)subViewContents[viewTabControl.CurrentPage - 1];
				}
				return content;
			}
		}
		
		public void SwitchView(int viewNumber)
		{
			if (viewTabControl != null) {
				this.viewTabControl.CurrentPage = viewNumber;
			}
		}
		
		public void SelectWindow()	
		{
			int toSelect = tabControl.PageNum (this);
			tabControl.CurrentPage = toSelect;
		}
		
		public SdiWorkspaceWindow(IViewContent content, Notebook tabControl, TabLabel tabLabel) : base ()
		{
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
					int    number    = 1;
					bool   found     = true;
					while (found) {
						found = false;
						foreach (IViewContent windowContent in WorkbenchSingleton.Workbench.ViewContentCollection) {
							string title = windowContent.WorkbenchWindow.Title;
							if (title.EndsWith("*") || title.EndsWith("+")) {
								title = title.Substring(0, title.Length - 1);
							}
							if (title == baseName + number) {
								found = true;
								++number;
								break;
							}
						}
					}
					myUntitledTitle = baseName + number;
				}
				newTitle = myUntitledTitle;
			} else {
				newTitle = System.IO.Path.GetFileName(content.ContentName);
			}
			
			if (content.IsDirty) {
				newTitle += "*";
				Runtime.ProjectService.MarkFileDirty (content.ContentName);
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
		
		public void CloseWindow(bool force, bool fromMenu, int pageNum)
		{
			if (!force && ViewContent != null && ViewContent.IsDirty) {
				
				QuestionResponse response = Runtime.MessageService.AskQuestionWithCancel (GettextCatalog.GetString ("Do you want to save the current changes"));
				
				if (response == QuestionResponse.Cancel) {
					return;
				}

				if (response == QuestionResponse.Yes) {
					if (content.ContentName == null) {
						while (true) {
							Runtime.FileService.SaveFileAs (this);
							if (ViewContent.IsDirty) {
								if (Runtime.MessageService.AskQuestion(GettextCatalog.GetString ("Do you really want to discard your changes ?"))) {
									break;
								}
							} else {
								break;
							}
						}
						
					} else {
						Runtime.FileUtilityService.ObservedSave(new FileOperationDelegate(ViewContent.Save), ViewContent.ContentName , FileErrorPolicy.ProvideAlternative);
					}
				}
			}
			if (fromMenu == true) {
				WorkbenchSingleton.Workbench.WorkbenchLayout.RemoveTab (tabControl.CurrentPage);
			} else {
				WorkbenchSingleton.Workbench.WorkbenchLayout.RemoveTab (pageNum);
			}
			OnWindowDeselected(EventArgs.Empty);
			
			content.ContentNameChanged -= new EventHandler(SetTitleEvent);
			content.DirtyChanged       -= new EventHandler(SetTitleEvent);
			content.BeforeSave         -= new EventHandler(BeforeSave);
			content.ContentChanged     -= new EventHandler (OnContentChanged);
			content.WorkbenchWindow = null;
			
			Remove (content.Control);
			content.Dispose ();
			OnCloseEvent(null);
			
			this.content = null;
			this.tabControl = null;
			this.tabLabel = null;
			this.tabPage = null;
		}
		
		public void AttachSecondaryViewContent(ISecondaryViewContent subViewContent)
		{
			// FIXME: We should use a notebook instead.
			/*if (subViewContents == null) {
				subViewContents = new ArrayList ();
				subDockItems = new ArrayList ();
			}
	
			mainItem.Behavior = DockItemBehavior.CantClose | DockItemBehavior.CantIconify;
			subViewContents.Add (subViewContent);
			DockItem dockitem = new DockItem (subViewContent.TabPageLabel, subViewContent.TabPageLabel, DockItemBehavior.CantClose | DockItemBehavior.CantIconify);
			dockitem.Add (subViewContent.Control);
			subViewContent.Control.ShowAll ();
			dockitem.ShowAll ();
			subDockItems.Add (dockitem);
			AddItem (dockitem, DockPlacement.Bottom);
			OnContentChanged (null, null);*/
		}
		
		
/*		int oldIndex = -1;
		protected void viewTabControlIndexChanged(object sender, EventArgs e)
		{
			if (oldIndex > 0) {
				ISecondaryViewContent secondaryViewContent = subViewContents[oldIndex - 1] as ISecondaryViewContent;
				if (secondaryViewContent != null) {
					secondaryViewContent.Deselected();
				}
			}
			
			if (viewTabControl.CurrentPage > 0) {
				ISecondaryViewContent secondaryViewContent = subViewContents[viewTabControl.CurrentPage - 1] as ISecondaryViewContent;
				if (secondaryViewContent != null) {
					secondaryViewContent.Selected();
				}
			}
			oldIndex = viewTabControl.CurrentPage;
		}
*/
		object ICommandRouter.GetNextCommandTarget ()
		{
			commandHandler.SetNextCommandTarget (Parent); 
			return commandHandler;
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
			
			try {
				if (content.ContentName.IndexOfAny (new char[] { '*', '+'}) == -1) {
					tabLabel.Icon.Pixbuf = FileIconLoader.GetPixbufForFile (content.ContentName, 16);
				}
			} catch {
				tabLabel.Icon.Pixbuf = FileIconLoader.GetPixbufForType ("gnome-fs-regular", 16);
			}
			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}

		protected virtual void OnCloseEvent(EventArgs e)
		{
			OnWindowDeselected(e);
			if (CloseEvent != null) {
				CloseEvent(this, e);
			}
		}

		public virtual void OnWindowSelected(EventArgs e)
		{
			if (WindowSelected != null) {
				WindowSelected(this, e);
			}
		}
		public virtual void OnWindowDeselected(EventArgs e)
		{
			if (WindowDeselected != null) {
				WindowDeselected(this, e);
			}
		}
		
		public event EventHandler WindowSelected;
		public event EventHandler WindowDeselected;
				
		public event EventHandler TitleChanged;
		public event EventHandler CloseEvent;
	}
}
