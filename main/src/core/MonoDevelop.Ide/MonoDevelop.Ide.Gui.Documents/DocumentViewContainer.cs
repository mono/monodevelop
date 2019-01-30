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
		DocumentViewContainerMode supportedModes;
		IShellDocumentViewContainer shellViewContainer;
		DocumentView activeView;
		List<IShellDocumentViewItem> workbenchViews = new List<IShellDocumentViewItem> ();

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
						ActiveViewChanged?.Invoke (this, EventArgs.Empty);
					} else
						shellViewContainer.ActiveView = value.ShellView;
				} else {
					activeView = null;
					if (shellViewContainer != null)
						shellViewContainer.ActiveView = null;
				}
			}
		}

		internal IShellDocumentViewContainer ShellViewContainer {
			get {
				return shellViewContainer;
			}
		}

		internal void SelectView (IShellDocumentViewItem shellView)
		{
			if (shellViewContainer != null)
				shellViewContainer.SelectView (shellView);
		}

		internal override void AttachToView (IShellDocumentViewItem shellView)
		{
			base.AttachToView (shellView);
			shellViewContainer = (IShellDocumentViewContainer)shellView;
			Views.AttachListener (this);
			shellViewContainer.ActiveViewChanged += ShellViewContainer_ActiveViewChanged;
			UpdateViews ();
			if (activeView != null)
				shellViewContainer.ActiveView = activeView.ShellView;
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
			ActiveViewChanged?.Invoke (this, EventArgs.Empty);
			ActiveViewInHierarchy = activeView;
		}

		void UpdateViews ()
		{
			int tabPos = 0;
			foreach (var view in Views) {
				var existingViewIndex = workbenchViews.FindIndex (v => v.Item == view);

				if (tabPos >= workbenchViews.Count || existingViewIndex == -1) {
					// New view
					var newView = shellViewContainer.InsertView (tabPos, view);
					workbenchViews.Insert (tabPos, newView);
				} else if (workbenchViews [tabPos].Item != view) {
					if (existingViewIndex != -1) {
						// The view is in the collection, but not in the same position
						shellViewContainer.ReorderView (existingViewIndex, tabPos);
						var viewToReorder = workbenchViews [existingViewIndex];
						workbenchViews.RemoveAt (existingViewIndex);
						workbenchViews.Insert (tabPos, viewToReorder);
					} else {
						// The view is gone
						shellViewContainer.RemoveView (tabPos);
						workbenchViews.RemoveAt (tabPos);
					}
				}
				tabPos++;
			}
		}

		void IDocumentViewContentCollectionListener.ClearItems ()
		{
			foreach (var it in workbenchViews)
				it.Item.Parent = null;
			workbenchViews.Clear ();
			shellViewContainer.RemoveAllViews ();
		}

		void IDocumentViewContentCollectionListener.InsertItem (int index, DocumentView item)
		{
			item.Parent = this;
			var view = shellViewContainer.InsertView (index, item);
			workbenchViews.Insert (index, view);
		}

		void IDocumentViewContentCollectionListener.RemoveItem (int index)
		{
			workbenchViews [index].Item.Parent = null;
			workbenchViews.RemoveAt (index);
			shellViewContainer.RemoveView (index);
		}

		void IDocumentViewContentCollectionListener.SetItem (int index, DocumentView item)
		{
			workbenchViews [index].Item.Parent = null;
			item.Parent = this;
			var view = shellViewContainer.ReplaceView (index, item);
			workbenchViews [index] = view;
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
	}
}
