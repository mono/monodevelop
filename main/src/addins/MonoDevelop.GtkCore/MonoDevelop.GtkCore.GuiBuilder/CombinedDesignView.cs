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
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class CombinedDesignView : FileDocumentController
	{
		DocumentController content;
		DocumentViewContainer container;
		Dictionary<Gtk.Widget, DocumentViewContent> pages = new Dictionary<Widget, DocumentViewContent> ();

		public CombinedDesignView (DocumentController content)
		{
			this.content = content;
			/* This code causes that chagnes in a version control view always select the source code view.
				if (content is IEditableTextBuffer) {
				((IEditableTextBuffer)content).CaretPositionSet += delegate {
					ShowPage (0);
				};
			}*/
			content.HasUnsavedChangesChanged += new EventHandler (OnTextDirtyChanged);
			
			IdeApp.Workbench.ActiveDocumentChanged += OnActiveDocumentChanged;
		}

		protected override Task OnInitialize (ModelDescriptor modelDescriptor, Properties status)
		{
			return content.Initialize (modelDescriptor, status);
		}

		protected override async Task<DocumentView> OnInitializeView ()
		{
			container = new DocumentViewContainer ();
			container.ActiveViewChanged += Container_ActiveViewChanged;
			var sourceView = await content.GetDocumentView ();
			sourceView.Title = GettextCatalog.GetString ("Source");
			container.Views.Add (sourceView);
			return container;
		}

		public virtual Stetic.Designer Designer {
			get { return null; }
		}
		
		protected void AddButton (string label, Gtk.Widget page)
		{
			var sourceView = new DocumentViewContent (() => page) {
				Title = label
			};
			container.Views.Add (sourceView);
			pages [page] = sourceView;
		}
		
		public bool HasPage (Gtk.Widget page)
		{
			return pages.ContainsKey (page);
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

		protected override void OnOwnerChanged ()
		{
			base.OnOwnerChanged ();
			content.Owner = Owner;
		}

		internal protected override ProjectReloadCapability OnGetProjectReloadCapability ()
		{
			return content.ProjectReloadCapability;
		}

		void Container_ActiveViewChanged (object sender, EventArgs e)
		{
			if (container.ActiveView != null)
				OnPageShown (container.Views.IndexOf (container.ActiveView));
		}

		public void ShowPage (int npage)
		{
			if (container != null)
				container.ActiveView = container.Views [npage];
		}
		
		protected virtual void OnPageShown (int npage)
		{
		}
		
		protected override void OnDispose ()
		{
			if (content == null)
				return;

			content.HasUnsavedChangesChanged -= OnTextDirtyChanged;
			IdeApp.Workbench.ActiveDocumentChanged -= OnActiveDocumentChanged;

			content = null;

			base.OnDispose ();
		}
		
		protected override Task OnSave ()
		{
			return content.Save ();
		}

		public virtual void AddCurrentWidgetToClass ()
		{
		}
		
		public virtual void JumpToSignalHandler (Stetic.Signal signal)
		{
		}
		
		void OnTextDirtyChanged (object s, EventArgs args)
		{
			OnCombinedDirtyChanged ();
		}

		protected virtual bool IsDirtyCombined { get => content.HasUnsavedChanges; set => content.HasUnsavedChanges = value; }

		protected void OnCombinedDirtyChanged ()
		{
			HasUnsavedChanges = IsDirtyCombined;
		}

		void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.GetContent<CombinedDesignView>() == this)
				OnDocumentActivated ();
		}
		
		protected virtual void OnDocumentActivated ()
		{
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
}

