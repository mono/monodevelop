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
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Ide.Gui.Documents
{
	/// <summary>
	/// Base type for views that can show the content of documents
	/// </summary>
	public class DocumentView : ICommandDelegator, IDisposable
	{
		string title;
		string accessibilityDescription;
		Xwt.Drawing.Image icon;
		DocumentView activeChildView;
		IShellDocumentViewItem shellView;

		/// <summary>
		/// Raised when the active view in this hierarchy of views changes
		/// </summary>
		public EventHandler ActiveViewInHierarchyChanged;
		private DocumentController sourceController;

		/// <summary>
		/// The controller that was used to create this view
		/// </summary>
		/// <value>The parent controller.</value>
		public DocumentController SourceController {
			get { return sourceController; }
			internal set {
				UnsubscribeControllerEvents ();
				sourceController = value;
				SubscribeControllerEvents ();
			}
		}

		public DocumentViewContentCollection AttachedViews { get; } = new DocumentViewContentCollection ();

		/// <summary>
		/// Title of the view. This is the text shown in tabs and selectors.
		/// </summary>
		public string Title {
			get => title ?? SourceController?.TabPageLabel;
			set {
				title = value;
				UpdateTitle ();
			}
		}

		public string AccessibilityDescription {
			get { return accessibilityDescription ?? SourceController?.AccessibilityDescription; }
			set { accessibilityDescription = value; UpdateTitle (); }
		}

		public Xwt.Drawing.Image Icon {
			get => icon;
			set {
				icon = value;
				UpdateTitle ();
			}
		}

		public DocumentView ActiveViewInHierarchy {
			get => activeChildView;
			set {
				if (value != activeChildView) {
					activeChildView = value;
					ActiveViewInHierarchyChanged?.Invoke (this, EventArgs.Empty);
					if (Parent?.ActiveView == this)
						Parent.ActiveViewInHierarchy = value;
				}
			}
		}

		/// <summary>
		/// Container that contains this view
		/// </summary>
		/// <value>The parent.</value>
		public DocumentViewContainer Parent { get; internal set; }

		internal IShellDocumentViewItem ShellView {
			get {
				return shellView;
			}
		}

		object ICommandDelegator.GetDelegatedCommandTarget () => SourceController;

		public void SetActive ()
		{
			if (Parent != null)
				Parent.ActiveView = this;
		}

		public void Dispose ()
		{
			OnDispose ();
		}

		void UpdateTitle ()
		{
			if (shellView != null)
				shellView.SetTitle (title, icon, accessibilityDescription);
		}

		internal virtual void AttachToView (IShellDocumentViewItem shellView)
		{
			this.shellView = shellView;
			UpdateTitle ();
			shellView.SetDelegatedCommandTarget (this);
		}

		internal virtual void DetachFromView ()
		{
			shellView = null;
		}

		protected virtual void OnDispose ()
		{
			if (Parent != null)
				Parent.Views.Remove (this);
			else if (shellView != null)
				throw new InvalidOperationException ("Can't dispose the root view of a document");
		}

		internal virtual IEnumerable<DocumentController> GetActiveControllerHierarchy ()
		{
			if (SourceController != null)
				yield return SourceController;
		}

		internal virtual IEnumerable<DocumentController> GetAllControllers ()
		{
			if (SourceController != null)
				yield return SourceController;
		}

		void SubscribeControllerEvents ()
		{
			if (SourceController != null) {
				SourceController.AccessibilityDescriptionChanged += SourceController_AccessibilityDescriptionChanged;
				SourceController.DocumentTitleChanged += SourceController_AccessibilityDescriptionChanged;
			}
		}

		void UnsubscribeControllerEvents ()
		{
			if (SourceController != null) {
				SourceController.AccessibilityDescriptionChanged -= SourceController_AccessibilityDescriptionChanged;
				SourceController.DocumentTitleChanged -= SourceController_AccessibilityDescriptionChanged;
			}
		}

		void SourceController_AccessibilityDescriptionChanged (object sender, EventArgs e)
		{
			UpdateTitle ();
		}
	}
}
