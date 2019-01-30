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
	public class DocumentViewItem : ICommandDelegator, IDisposable
	{
		string title;
		string accessibilityDescription;
		Xwt.Drawing.Image icon;
		DocumentViewItem activeChildView;
		IShellDocumentViewItem shellView;

		/// <summary>
		/// The controller that was used to create this view
		/// </summary>
		/// <value>The parent controller.</value>
		public DocumentController SourceController { get; internal set; }

		public DocumentViewContentCollection AttachedViews { get; } = new DocumentViewContentCollection ();

		public event EventHandler ViewShown;

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

		public string Title {
			get => title;
			set {
				title = value;
				UpdateTitle ();
			}
		}

		public Xwt.Drawing.Image Icon {
			get => icon;
			set {
				icon = value;
				UpdateTitle ();
			}
		}

		internal IShellDocumentViewItem ShellView {
			get {
				return shellView;
			}
		}

		public DocumentViewContainer Parent { get; internal set; }

		public string AccessibilityDescription {
			get { return accessibilityDescription; }
			set { accessibilityDescription = value; UpdateTitle (); }
		}

		void UpdateTitle ()
		{
			if (shellView != null)
				shellView.SetTitle (title, icon, accessibilityDescription);
		}

		public void SetActive ()
		{
			if (Parent != null)
				Parent.ActiveView = this;
		}

		public void Dispose ()
		{
			OnDispose ();
		}

		protected virtual void OnDispose ()
		{
			if (Parent != null)
				Parent.Views.Remove (this);
			else if (shellView != null)
				throw new InvalidOperationException ("Can't dispose the root view of a document");
		}

		object ICommandDelegator.GetDelegatedCommandTarget () => SourceController;

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

		public DocumentViewItem ActiveViewInHierarchy {
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

		public EventHandler ActiveViewInHierarchyChanged;
	}
}
