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


using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Gui.Shell;
using Xwt.Drawing;
using Gtk;
using System.Threading.Tasks;
using System.Threading;
using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewItem : EventBox, IShellDocumentViewItem, ICommandDelegator
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();
		object delegatedCommandTarget;
		Task loadTask;

		public GtkShellDocumentViewItem ()
		{
			Accessible.SetShouldIgnore (true);
		}

		public bool Loaded { get; private set; }

		public DocumentView Item { get; set; }

		public void SetTitle (string label, Xwt.Drawing.Image icon, string accessibilityDescription)
		{
		}

		public static GtkShellDocumentViewItem CreateShellView (DocumentView item)
		{
			GtkShellDocumentViewItem view;

			if (item is DocumentViewContent content) {
				view = new GtkShellDocumentViewContent ();
			} else if (item is DocumentViewContainer container) {
				view = new GtkShellDocumentViewContainer ();
			} else
				throw new NotSupportedException ();

			item.AttachToView (view);
			return view;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			Load ().Ignore ();
		}

		protected override void OnDestroyed ()
		{
			cancellationTokenSource.Cancel ();
			base.OnDestroyed ();
		}

		public Task Load ()
		{
			return Load (cancellationTokenSource.Token);
		}

		public Task Load (CancellationToken cancellationToken)
		{
			if (loadTask == null)
				loadTask = OnLoad (cancellationToken);
			return loadTask;
		}

		protected virtual Task OnLoad (CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public void SetDelegatedCommandTarget (object target)
		{
			delegatedCommandTarget = target;
		}

		public object GetDelegatedCommandTarget ()
		{
			return delegatedCommandTarget;
		}

		public virtual void DetachFromView ()
		{
			Item.DetachFromView ();
		}
	}
}
