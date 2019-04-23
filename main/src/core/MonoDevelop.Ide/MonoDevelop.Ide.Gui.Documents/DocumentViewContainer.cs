//
// DocumentViewContainer.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Ide.Gui.Shell;
using Roslyn.Utilities;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// A document view that can show other views in tabs or splits.
	/// </summary>
	public sealed class DocumentViewContainer : DocumentView, IDocumentViewContentCollectionListener
	{
		DocumentViewContainerMode supportedModes = DocumentViewContainerMode.Tabs;
		DocumentViewContainerMode currentMode = DocumentViewContainerMode.Tabs;
		IShellDocumentViewContainer shellViewContainer;
		DocumentView activeView;

		public DocumentViewContainer ()
		{
			Views.AttachListener (this);
		}

		/// <summary>
		/// Raised when the active view of this container changes
		/// </summary>
		public EventHandler ActiveViewChanged;

		/// <summary>
		/// Gets or sets the display modes supported by this container
		/// </summary>
		/// <value>The supported modes.</value>
		public DocumentViewContainerMode SupportedModes {
			get => supportedModes;
			set {
				if (supportedModes != value) {
					supportedModes = value;
					if (shellViewContainer != null)
						shellViewContainer.SetSupportedModes (supportedModes);
					if ((CurrentMode & supportedModes) == 0) {
						if ((supportedModes & DocumentViewContainerMode.Tabs) != 0)
							CurrentMode = DocumentViewContainerMode.Tabs;
						else if ((supportedModes & DocumentViewContainerMode.HorizontalSplit) != 0)
							CurrentMode = DocumentViewContainerMode.HorizontalSplit;
						else
							CurrentMode = DocumentViewContainerMode.VerticalSplit;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the display modes supported by this container
		/// </summary>
		/// <value>The supported modes.</value>
		public DocumentViewContainerMode CurrentMode {
			get => shellViewContainer != null ? shellViewContainer.CurrentMode : currentMode;
			set {
				if (currentMode != value) {
					if ((value & supportedModes) == 0)
						return;
					currentMode = value;
					if (shellViewContainer != null)
						shellViewContainer.CurrentMode = currentMode;
					UpdateChildrenVisibility ();
				}
			}
		}

		/// <summary>
		/// Views in the container
		/// </summary>
		/// <value>The views.</value>
		public DocumentViewContentCollection Views { get; } = new DocumentViewContentCollection ();

		/// <summary>
		/// The currently active view
		/// </summary>
		public DocumentView ActiveView {
			get {
				return activeView;
			}
			set {
				if (value != null) {
					if (value.Parent != this)
						throw new InvalidOperationException ("View doesn't belong to this container");
					if (shellViewContainer == null) {
						activeView = value;
						UpdateChildrenVisibility ();
						activeView.OnActivated ();
						ActiveViewChanged?.Invoke (this, EventArgs.Empty);
					} else
						shellViewContainer.ActiveView = value.ShellView;
				} else {
					activeView = null;
					UpdateChildrenVisibility ();
					if (shellViewContainer != null)
						shellViewContainer.ActiveView = null;
				}
			}
		}

		internal override void SetActiveChild (DocumentView child)
		{
			base.SetActiveChild (child);
			if (Views.Contains (child))
				ActiveView = child;
		}

		internal IShellDocumentViewContainer ShellViewContainer {
			get {
				return shellViewContainer;
			}
		}

		internal override IShellDocumentViewItem OnCreateShellView (IWorkbenchWindow window)
		{
			shellViewContainer = window.CreateViewContainer ();
			shellViewContainer.SetSupportedModes (supportedModes);
			shellViewContainer.CurrentMode = currentMode;

			for (int n = 0; n < Views.Count; n++)
				shellViewContainer.InsertView (n, Views [n].CreateShellView (window));

			shellViewContainer.ActiveViewChanged += ShellViewContainer_ActiveViewChanged;
			shellViewContainer.CurrentModeChanged += ShellViewContainer_CurrentModeChanged;

			if (activeView != null)
				shellViewContainer.ActiveView = activeView.ShellView;
			else
				shellViewContainer.ActiveView = Views.FirstOrDefault ()?.ShellView;

			return shellViewContainer;
		}

		private void ShellViewContainer_CurrentModeChanged (object sender, EventArgs e)
		{
			currentMode = shellViewContainer.CurrentMode;
			UpdateChildrenVisibility ();
		}

		internal override void DetachFromView ()
		{
			shellViewContainer.ActiveViewChanged -= ShellViewContainer_ActiveViewChanged;
			Views.DetachListener ();
			base.DetachFromView ();
		}

		void ShellViewContainer_ActiveViewChanged (object sender, EventArgs e)
		{
			activeView = shellViewContainer.ActiveView?.Item;
			UpdateChildrenVisibility ();
			activeView?.OnActivated ();
			ActiveViewChanged?.Invoke (this, EventArgs.Empty);
		}

		internal override void OnActivated ()
		{
			base.OnActivated ();
			ActiveViewInHierarchy = ActiveView?.ActiveViewInHierarchy;
			if (Parent != null)
				Parent.ActiveViewInHierarchy = ActiveViewInHierarchy;
		}

		internal override void OnClearItems (DocumentViewContentCollection list)
		{
			if (list == Views) {
				foreach (var it in Views)
					it.Parent = null;
				if (shellViewContainer != null)
					shellViewContainer.RemoveAllViews ();
				else
					ActiveView = null;
			} else
				base.OnClearItems (list);
		}

		internal override void OnInsertItem (DocumentViewContentCollection list, int index, DocumentView item)
		{
			if (list == Views) {
				item.Parent = this;
				if (shellViewContainer != null)
					shellViewContainer.InsertView (index, item.CreateShellView (window));
				else if (Views.Count == 0)
					ActiveView = item;
			} else
				base.OnInsertItem (list, index, item);
			UpdateChildVisibleStatus (item);
		}

		internal override void OnRemoveItem (DocumentViewContentCollection list, int index)
		{
			if (list == Views) {
				var item = Views [index];
				item.Parent = null;
				if (shellViewContainer != null)
					shellViewContainer.RemoveView (index);
				else if (ActiveView == item) {
					if (index < Views.Count - 1)
						ActiveView = Views [index + 1];
					else if (index > 0)
						ActiveView = Views [index - 1];
					else
						ActiveView = null;
				}
			} else 
				base.OnRemoveItem (list, index);
		}

		internal override void OnItemSet (DocumentViewContentCollection list, int index, DocumentView oldItem, DocumentView item)
		{
			if (list == Views) {
				oldItem.Parent = null;
				item.Parent = this;
				if (shellViewContainer != null)
					shellViewContainer.ReplaceView (index, item.CreateShellView (window));
				else if (ActiveView == oldItem)
					ActiveView = item;
				UpdateChildVisibleStatus (item);
			} else
				base.OnItemSet (list, index, oldItem, item);
		}

		internal override bool ReplaceChildView (DocumentView documentView)
		{
			if (base.ReplaceChildView (documentView))
				return true;
			var index = Views.IndexOf (documentView);
			if (index == -1)
				return false;
			if (shellViewContainer != null)
				shellViewContainer.ReplaceView (index, documentView.CreateShellView (window));
			UpdateChildVisibleStatus (documentView);
			return true;
		}

		internal override bool RemoveChild (DocumentView view)
		{
			return base.RemoveChild (view) || Views.Remove (view);
		}

		internal override IEnumerable<DocumentController> GetActiveControllerHierarchy ()
		{
			var result = base.GetActiveControllerHierarchy ();
			var activeItem = shellViewContainer.ActiveView?.Item;
			if (activeItem != null)
				result = activeItem.GetActiveControllerHierarchy ().Concat (result);
			return result;
		}

		internal override IEnumerable<DocumentController> GetAllControllers ()
		{
			var result = base.GetActiveControllerHierarchy ();
			foreach (var child in Views)
				result = result.Concat (child.GetAllControllers ());
			return result;
		}

		protected override void OnDispose ()
		{
			foreach (var c in Views.ToList ())
				c.Dispose ();
			base.OnDispose ();
		}

		internal override void OnClosed ()
		{
			foreach (var c in Views.ToList ())
				c.Close ();
			base.OnClosed ();
		}

		internal override void UpdateContentVisibility (bool parentIsVisible)
		{
			base.UpdateContentVisibility (parentIsVisible);
			UpdateChildrenVisibility ();
		}

		void UpdateChildrenVisibility ()
		{
			foreach (var view in Views)
				UpdateChildVisibleStatus (view);
		}

		void UpdateChildVisibleStatus (DocumentView view)
		{
			view.UpdateContentVisibility (ContentVisible && (view == activeView || CurrentMode == DocumentViewContainerMode.HorizontalSplit || CurrentMode == DocumentViewContainerMode.VerticalSplit));
		}

		internal override bool DefaultGrabFocus ()
		{
			if (base.DefaultGrabFocus () || Views.Count == 0)
				return true;
			if (CurrentMode == DocumentViewContainerMode.Tabs) {
				ActiveView?.GrabFocus ();
			} else
				Views [0].GrabFocus ();
			return true;
		}
	}
}
