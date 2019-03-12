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
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewContainer : GtkShellDocumentViewItem, IShellDocumentViewContainer
	{
		DocumentViewContainerMode supportedModes;
		DocumentViewContainerMode mode;
		IGtkShellDocumentViewContainer container;
		double [] splitSizes;

		public event EventHandler ActiveViewChanged;

		public DocumentViewContainerMode CurrentMode => mode;

		public IGtkShellDocumentViewContainer InternalContainer => container;

		public void SetSupportedModes (DocumentViewContainerMode supportedModes)
		{
			this.supportedModes = supportedModes;
		}

		public void SetCurrentMode (DocumentViewContainerMode mode)
		{
			if (this.mode == mode)
				return;

			// Save current split sizes
			if (container is GtkShellDocumentViewContainerSplit split)
				splitSizes = split.GetRelativeSplitSizes ();

			this.mode = mode;

			GtkShellDocumentViewItem activeView = null;

			List<GtkShellDocumentViewItem> allViews = null;
			if (container != null) {
				activeView = container.ActiveView;
				container.ActiveViewChanged -= Container_ActiveViewChanged;
				allViews = container.GetAllViews ().ToList ();
				container.RemoveAllViews ();
				container.Widget.Destroy ();
			}

			if (mode == DocumentViewContainerMode.Tabs)
				container = new GtkShellDocumentViewContainerTabs ();
			else {
				container = new GtkShellDocumentViewContainerSplit (mode);
			}

			if (allViews != null) {
				for (int n = 0; n < allViews.Count; n++)
					container.InsertView (n, allViews [n]);
			}

			// Restore current split sizes
			if (splitSizes != null && container is GtkShellDocumentViewContainerSplit splitContainer)
				splitContainer.SetRelativeSplitSizes (splitSizes);

			container.ActiveView = activeView;
			container.ActiveViewChanged += Container_ActiveViewChanged;
			Add (container.Widget);
			container.Widget.Show ();
		}

		void Container_ActiveViewChanged (object sender, EventArgs e)
		{
			ActiveViewChanged?.Invoke (this, EventArgs.Empty);
		}

		protected override async Task OnLoad (CancellationToken cancellationToken)
		{
			if (mode == DocumentViewContainerMode.Tabs) {
				var item = (GtkShellDocumentViewItem)container.ActiveView;
				if (item != null && !item.Loaded)
					await item.Load (cancellationToken);
			} else {
				var allTasks = new List<Task> ();
				foreach (var c in container.GetAllViews ())
					allTasks.Add (c.Load (cancellationToken));
				await Task.WhenAll (allTasks);
			}
		}

		public override void DetachFromView ()
		{
			foreach (var child in GetAllViews ())
				child.DetachFromView ();
			base.DetachFromView ();
		}

		public void InsertView (int position, IShellDocumentViewItem shellView)
		{
			var widget = (GtkShellDocumentViewItem)shellView;
			widget.Show ();
			container.InsertView (position, widget);
		}

		public void ReplaceView (int position, IShellDocumentViewItem shellView)
		{
			var newView = (GtkShellDocumentViewItem)shellView;
			newView.Show ();
			container.ReplaceView (position, newView);
		}

		public void RemoveView (int tabPos)
		{
			container.RemoveView (tabPos);
		}

		public void ReorderView (int currentIndex, int newIndex)
		{
			container.ReorderView (currentIndex, newIndex);
		}

		public void RemoveAllViews ()
		{
			container.RemoveAllViews ();
		}

		IEnumerable<GtkShellDocumentViewItem> GetAllViews ()
		{
			return container.GetAllViews ();
		}

		public IShellDocumentViewItem ActiveView {
			get => container.ActiveView;
			set => container.ActiveView = (GtkShellDocumentViewItem) value;
		}

		public double [] GetRelativeSplitSizes ()
		{
			if (splitSizes != null)
				return splitSizes;
			if (container is GtkShellDocumentViewContainerSplit split)
				return split.GetRelativeSplitSizes ();
			return null;
		}

		public void SetRelativeSplitSizes (double [] sizes)
		{
			if (container is GtkShellDocumentViewContainerSplit split)
				split.SetRelativeSplitSizes (sizes);
			else
				splitSizes = sizes;
		}
	}

	interface IGtkShellDocumentViewContainer
	{
		Gtk.Widget Widget { get; }
		void InsertView (int position, GtkShellDocumentViewItem view);
		void ReplaceView (int position, GtkShellDocumentViewItem view);
		void RemoveView (int tabPos);
		void ReorderView (int currentIndex, int newIndex);
		void RemoveAllViews ();
		GtkShellDocumentViewItem ActiveView { get; set; }
		event EventHandler ActiveViewChanged;
		IEnumerable<GtkShellDocumentViewItem> GetAllViews ();
		void SetViewTitle (GtkShellDocumentViewItem view, string label, Xwt.Drawing.Image icon, string accessibilityDescription);
	}
}
