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
using Gdk;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewItem : EventBox, IShellDocumentViewItem, ICommandDelegator
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();
		object delegatedCommandTarget;
		Task loadTask;
		bool subscribedWindowsEvents;

		public GtkShellDocumentViewItem ()
		{
			Accessible.SetShouldIgnore (true);
			Events |= EventMask.FocusChangeMask;
		}

		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		public bool Loaded { get; private set; }

		public DocumentView Item { get; set; }

		public void SetTitle (string label, Xwt.Drawing.Image icon, string accessibilityDescription)
		{
			Title = label;
			Icon = icon;
			AccessibilityDescription = accessibilityDescription;
			var parent = Parent;
			while (parent != null && !(parent is GtkShellDocumentViewContainer))
				parent = parent.Parent;
			if (parent is GtkShellDocumentViewContainer container)
				container.SetViewTitle (this, label, icon, accessibilityDescription);
		}

		public string Title { get; set; }
		public Xwt.Drawing.Image Icon { get; set; }
		public string AccessibilityDescription { get; set; }

		protected override void OnRealized ()
		{
			base.OnRealized ();
			Load ().Ignore ();
			SubscribeWindowEvents ();
		}

		protected override void OnDestroyed ()
		{
			UnsubscribeWindowEvents ();
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

		void IShellDocumentViewItem.Dispose ()
		{
			DetachFromView ();
			Destroy ();
		}

		public virtual void DetachFromView ()
		{
			Item = null;
		}

		bool hasFocus;
		bool hasFocusInWindow;
		IDisposable focusLostTimeout;
		static WeakReference<GtkShellDocumentViewItem> lastViewFocused = new WeakReference<GtkShellDocumentViewItem> (null);

		protected override void OnFocusChildSet (Widget widget)
		{
			if (widget != null) {

				hasFocusInWindow = true;

				// Cancel any pending focus lose notification

				if (focusLostTimeout != null) {
					focusLostTimeout.Dispose ();
					focusLostTimeout = null;
				}

				// Check if any other view currently has the focus and notify that the focus has been lost.
				// The focus lose would be detected anyway, but we explicitly notify it here because
				// focus lose notification is a bit delayed, and here we have a chance to be more immediate
				// in notifying it

				GtkShellDocumentViewItem last;
				if (lastViewFocused.TryGetTarget (out last) && last != this && last.Item != null)
					last.NotifyLostFocus ();
				
				NotifyGotFocus ();

			} else {

				hasFocusInWindow = false;

				// Don't notify the lose of focus immediately. When switching between child widgets we
				// we receive a OnFocusChildSet with widget=null for the child that is losing the focus
				// followed by another call with the new child

				if (focusLostTimeout == null) {
					focusLostTimeout = Xwt.Application.TimeoutInvoke (100, () => {
						NotifyLostFocus ();
						return false;
					});
				}

			}
			base.OnFocusChildSet (widget);
		}

		void NotifyLostFocus ()
		{
			if (hasFocus) {
				hasFocus = false;
				LostFocus?.Invoke (this, EventArgs.Empty);
			}
		}

		void NotifyGotFocus ()
		{
			if (!hasFocus) {
				hasFocus = true;
				lastViewFocused.SetTarget (this);
				GotFocus?.Invoke (this, EventArgs.Empty);
			}
		}

		Gtk.Window subscribedWindow;

		void SubscribeWindowEvents ()
		{
			var topLevel = Toplevel as Gtk.Window;
			if (topLevel == subscribedWindow)
				return;

			UnsubscribeWindowEvents ();
			subscribedWindow = topLevel;
			if (subscribedWindow != null) {
				subscribedWindow.FocusInEvent += SubscribedWindow_FocusInEvent;
				subscribedWindow.FocusOutEvent += SubscribedWindow_FocusOutEvent;
			}
		}

		void UnsubscribeWindowEvents ()
		{
			if (subscribedWindow != null) {
				subscribedWindow.FocusInEvent -= SubscribedWindow_FocusInEvent;
				subscribedWindow.FocusOutEvent -= SubscribedWindow_FocusOutEvent;
				subscribedWindow = null;
			}
		}

		private void SubscribedWindow_FocusInEvent (object o, FocusInEventArgs args)
		{
			if (hasFocusInWindow)
				NotifyGotFocus ();
		}

		private void SubscribedWindow_FocusOutEvent (object o, FocusOutEventArgs args)
		{
			NotifyLostFocus ();
		}

		public void GrabViewFocus ()
		{
		}
	}
}
