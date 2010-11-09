//
// CombinedDesignView.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Collections.Generic;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class CombinedDesignView : AbstractViewContent
	{
		IViewContent content;
		Gtk.Widget control;
		List<TabView> tabs = new List<TabView> ();
		
		public CombinedDesignView (IViewContent content)
		{
			this.content = content;
			if (content is IEditableTextBuffer) {
				((IEditableTextBuffer)content).CaretPositionSet += delegate {
					ShowPage (0);
				};
			}
			content.ContentChanged += new EventHandler (OnTextContentChanged);
			content.DirtyChanged += new EventHandler (OnTextDirtyChanged);
			
			CommandRouterContainer crc = new CommandRouterContainer (content.Control, content, true);
			crc.Show ();
			control = crc;
			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
		}
		
		public virtual Stetic.Designer Designer {
			get { return null; }
		}
		
		public override string TabPageLabel {
			get {
				return GettextCatalog.GetString ("Source");
			}
		}
		
		protected void AddButton (string label, Gtk.Widget page)
		{
			TabView view = new TabView (label, page);
			tabs.Add (view);
			if (WorkbenchWindow != null) {
				view.WorkbenchWindow = WorkbenchWindow;
				WorkbenchWindow.AttachViewContent (view);
			}
		}
		
		public bool HasPage (Gtk.Widget page)
		{
			return tabs.Any (p => p.Control == page);
		}
		
		public void RemoveButton (Gtk.Widget page)
		{
/*			int i = notebook.PageNum (page);
			if (i != -1)
				RemoveButton (i);*/
		}
		
		public void RemoveButton (int npage)
		{
/*			if (npage >= toolbar.Children.Length)
				return;
			notebook.RemovePage (npage);
			Gtk.Widget cw = toolbar.Children [npage];
			toolbar.Remove (cw);
			cw.Destroy ();
			ShowPage (0);*/
		}
		
		public override MonoDevelop.Projects.Project Project {
			get { return base.Project; }
			set { 
				base.Project = value; 
				content.Project = value; 
			}
		}
		
		protected override void OnWorkbenchWindowChanged (EventArgs e)
		{
			base.OnWorkbenchWindowChanged (e);
			content.WorkbenchWindow = WorkbenchWindow;
			if (WorkbenchWindow != null) {
				foreach (TabView view in tabs) {
					view.WorkbenchWindow = WorkbenchWindow;
					WorkbenchWindow.AttachViewContent (view);
				}
				WorkbenchWindow.ActiveViewContentChanged += OnActiveViewContentChanged;
			}
		}

		void OnActiveViewContentChanged (object o, ActiveViewContentEventArgs e)
		{
			if (WorkbenchWindow.ActiveViewContent == this)
				OnPageShown (0);
			else {
				TabView tab = WorkbenchWindow.ActiveViewContent as TabView;
				if (tab != null) {
					int n = tabs.IndexOf (tab);
					if (n != -1)
						OnPageShown (n + 1);
				}
			}
		}
		
		public void ShowPage (int npage)
		{
			if (WorkbenchWindow != null) {
				if (npage == 0)
					WorkbenchWindow.SwitchView (0);
				else {
					var view = tabs [npage - 1];
					WorkbenchWindow.SwitchView (view);
				}
			}
		}
		
		protected virtual void OnPageShown (int npage)
		{
		}
		
		public override void Dispose ()
		{
			content.ContentChanged -= new EventHandler (OnTextContentChanged);
			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
			IdeApp.Workbench.ActiveDocumentChanged -= new EventHandler (OnActiveDocumentChanged);
			content.Dispose ();
			
			content = null;
			control = null;
			
			base.Dispose ();
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			content.Load (fileName);
		}
		
		public override Gtk.Widget Control {
			get { return control; }
		}
		
		public override void Save (string fileName)
		{
			content.Save (fileName);
		}
		
		public override bool IsDirty {
			get {
				return content.IsDirty;
			}
			set {
				content.IsDirty = value;
			}
		}
		
		public override bool IsReadOnly
		{
			get {
				return content.IsReadOnly;
			}
		}
		
		public virtual void AddCurrentWidgetToClass ()
		{
		}
		
		public virtual void JumpToSignalHandler (Stetic.Signal signal)
		{
		}
		
		void OnTextContentChanged (object s, EventArgs args)
		{
			OnContentChanged (args);
		}
		
		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnDirtyChanged (args);
		}
		
		void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.GetContent<CombinedDesignView>() == this)
				OnDocumentActivated ();
		}
		
		protected virtual void OnDocumentActivated ()
		{
		}
		
		public override T GetContent<T> ()
		{
//			if (type == typeof(IEditableTextBuffer)) {
//				// Intercept the IPositionable interface, since we need to
//				// switch to the text editor when jumping to a line
//				if (content.GetContent (type) != null)
//					return this;
//				else
//					return null;
//			}
//			
			return  base.GetContent<T> () ?? content.GetContent<T> ();
		}

		public void JumpTo (int line, int column)
		{
			IEditableTextBuffer ip = content.GetContent<IEditableTextBuffer> ();
			if (ip != null) {
				ShowPage (0);
				ip.SetCaretTo (line, column);
			}
		}
	}
	
	class TabView: AbstractBaseViewContent, IAttachableViewContent
	{
		string label;
		Gtk.Widget content;
		
		public TabView (string label, Gtk.Widget content)
		{
			this.label = label;
			this.content = content;
		}
		
		public override T GetContent<T> ()
		{
			if (Control is T)
				return (T) (object) Control;
			return base.GetContent<T> ();
		}
		
		#region IAttachableViewContent implementation
		public virtual void Selected ()
		{
		}

		public virtual void Deselected ()
		{
		}

		public virtual void BeforeSave ()
		{
		}

		public virtual void BaseContentChanged ()
		{
		}
		#endregion

		public override Widget Control {
			get {
				return content;
			}
		}

		public override string TabPageLabel {
			get {
				return label;
			}
		}
	}
}

