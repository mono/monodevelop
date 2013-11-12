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
		Widget control;
		readonly List<TabView> tabs = new List<TabView> ();
		
		public CombinedDesignView (IViewContent content)
		{
			this.content = content;
	/* This code causes that chagnes in a version control view always select the source code view.
				if (content is IEditableTextBuffer) {
				((IEditableTextBuffer)content).CaretPositionSet += delegate {
					ShowPage (0);
				};
			}*/
			content.ContentChanged += OnTextContentChanged;
			content.DirtyChanged += OnTextDirtyChanged;
			
			var crc = new CommandRouterContainer (content.Control, content, true);
			crc.Show ();
			control = crc;
			
			IdeApp.Workbench.ActiveDocumentChanged += OnActiveDocumentChanged;
		}
		
		public virtual Stetic.Designer Designer {
			get { return null; }
		}
		
		public override string TabPageLabel {
			get {
				return GettextCatalog.GetString ("Source");
			}
		}
		
		protected void AddButton (string label, Widget page)
		{
			var view = new TabView (label, page);
			tabs.Add (view);
			if (WorkbenchWindow != null) {
				view.WorkbenchWindow = WorkbenchWindow;
				WorkbenchWindow.AttachViewContent (view);
			}
		}
		
		public bool HasPage (Widget page)
		{
			return tabs.Any (p => p.Control == page);
		}
		
		public void RemoveButton (Widget page)
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
				var tab = WorkbenchWindow.ActiveViewContent as TabView;
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
			content.ContentChanged -= OnTextContentChanged;
			content.DirtyChanged -= OnTextDirtyChanged;
			IdeApp.Workbench.ActiveDocumentChanged -= OnActiveDocumentChanged;
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
		
		public override Widget Control {
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
		
		public override object GetContent (Type type)
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
			return  base.GetContent (type) ?? (content !=null  ? content.GetContent (type) : null);
		}

		public void JumpTo (int line, int column)
		{
			var ip = (IEditableTextBuffer) content.GetContent (typeof(IEditableTextBuffer));
			if (ip != null) {
				ShowPage (0);
				ip.SetCaretTo (line, column);
			}
		}
	}
	
	class TabView: AbstractBaseViewContent, IAttachableViewContent
	{
		readonly string label;
		readonly Widget content;
		
		public TabView (string label, Widget content)
		{
			this.label = label;
			this.content = content;
		}
		
		public override object GetContent (Type type)
		{
			return type.IsInstanceOfType (Control) ? Control : base.GetContent (type);
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

