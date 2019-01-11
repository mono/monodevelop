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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class CombinedDesignView : ViewContent
	{
		ViewContent content;
		Gtk.Widget control;
		List<TabView> tabs = new List<TabView> ();
		
		public CombinedDesignView (ViewContent content)
		{
			this.content = content;
	/* This code causes that chagnes in a version control view always select the source code view.
				if (content is IEditableTextBuffer) {
				((IEditableTextBuffer)content).CaretPositionSet += delegate {
					ShowPage (0);
				};
			}*/
			content.DirtyChanged += new EventHandler (OnTextDirtyChanged);
			
			CommandRouterContainer crc = new CommandRouterContainer (content.Control, content, true);
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
			return tabs.Any (p => p.Control.GetNativeWidget<Gtk.Widget> () == page);
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
		
		protected override void OnSetProject (Projects.Project project)
		{
			base.OnSetProject (project);
			content.Project = project; 
		}

		public override ProjectReloadCapability ProjectReloadCapability {
			get {
				return content.ProjectReloadCapability;
			}
		}
		
		protected override void OnWorkbenchWindowChanged ()
		{
			base.OnWorkbenchWindowChanged ();
			if (content != null)
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
			if (content == null)
				return;

			content.DirtyChanged -= new EventHandler (OnTextDirtyChanged);
			IdeApp.Workbench.ActiveDocumentChanged -= OnActiveDocumentChanged;
			content.Dispose ();
			
			content = null;
			control = null;
			
			base.Dispose ();
		}
		
		public override Task Load (FileOpenInformation fileOpenInformation)
		{
			ContentName = fileOpenInformation.FileName;
			return content.Load (ContentName);
		}
		
		public override Control Control {
			get { return control; }
		}
		
		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			return content.Save (fileSaveInformation);
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
		
		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnDirtyChanged ();
		}
		
		void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.GetContent<CombinedDesignView>() == this)
				OnDocumentActivated ();
		}
		
		protected virtual void OnDocumentActivated ()
		{
		}
		
		protected override object OnGetContent (Type type)
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
			return  base.OnGetContent (type) ?? (content !=null  ? content.GetContent (type) : null);
		}

		public void JumpTo (int line, int column)
		{
			var ip = (TextEditor) content.GetContent (typeof(TextEditor));
			if (ip != null) {
				ShowPage (0);
				ip.SetCaretLocation (line, column);
			}
		}
	}
	
	class TabView: BaseViewContent
	{
		string label;
		Gtk.Widget content;
		
		public TabView (string label, Gtk.Widget content)
		{
			this.label = label;
			this.content = content;
		}
		
		protected override object OnGetContent (Type type)
		{
			if (type.IsInstanceOfType (content))
				return content;
			return base.OnGetContent (type);
		}
		
		public override Control Control {
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

