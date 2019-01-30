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

using Gtk;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Ide.Gui
{
	class GtkShellDocumentViewContainer : WorkspaceViewItem, IShellDocumentViewContainer
	{
		DocumentViewContainerMode mode;
		Notebook notebook;
		Gtk.Paned paned;

		public GtkShellDocumentViewContainer (DocumentViewContainer item) : base (item)
		{
			this.container = item;
		}

		void SetupContainer ()
		{
		}

		public void SetSupportedModes (DocumentViewContainerMode supportedModes)
		{
			mode = supportedModes;
			if (notebook == null) {
				notebook = new Notebook ();
				notebook.Show ();
				Add (notebook);
			}
		}

		public IShellDocumentViewItem InsertView (int position, DocumentViewItem view)
		{
			var workbenchView = CreateWorkspaceView (item);
			notebook.InsertPage (workbenchView, new Gtk.Label (), pos);
			return workbenchView;
		}

		public IShellDocumentViewItem ReplaceView (int position, DocumentViewItem view)
		{
			notebook.RemovePage (position);
			return InsertView (position, view);
		}

		public void RemoveView (int tabPos)
		{
			notebook.RemovePage (tabPos);
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			var child = (DocumentViewItem)notebook.Children [oldPosition];
			notebook.ReorderChild (child, newPosition);
		}

		public void RemoveAllViews ()
		{
			while (notebook.NPages > 0)
				notebook.RemovePage (notebook.NPages - 1);
		}
	}
}
