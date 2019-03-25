//
// GtkShellDocumentViewContent.cs
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
using System.Threading.Tasks;
using MonoDevelop.Components;
using System.Threading;
using MonoDevelop.Ide.Gui.Content;
using Gtk;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.Gui.Shell
{
	class GtkShellDocumentViewContent: GtkShellDocumentViewItem, IShellDocumentViewContent
	{
		GtkDocumentToolbar toolbar;
		PathBar pathBar;
		Func<CancellationToken,Task<Control>> contentLoader;
		VBox box;
		IPathedDocument pathDoc;
		IPathedDocument pathDocPending;
		Gtk.Widget viewControl;

		public event EventHandler ContentInserted;

		public IShellDocumentToolbar GetToolbar ()
		{
			if (toolbar == null) {
				toolbar = new GtkDocumentToolbar ();
				box.PackStart (toolbar.Container, false, false, 0);
				box.ReorderChild (toolbar.Container, 0);
				toolbar.Visible = true;
				HidePathBar ();
			}
			return toolbar;
		}

		public void SetContentLoader (Func<CancellationToken,Task<Control>> contentLoader)
		{
			this.contentLoader = contentLoader;
		}

		protected override async Task OnLoad (CancellationToken cancellationToken)
		{
			if (box == null) {
				box = new VBox ();
				box.Accessible.SetShouldIgnore (true);
				Add (box);
				box.Show ();
			}
			var control = await contentLoader (cancellationToken);
			if (cancellationToken.IsCancellationRequested)
				return;
			if (viewControl != null) {
				box.Remove (viewControl);
				viewControl = null;
			}
			if (control != null) {
				viewControl = control.GetNativeWidget<Gtk.Widget> ();
				box.PackStart (viewControl, true, true, 0);
			}
			if (pathDocPending != null) {
				ShowPathBar (pathDocPending);
				pathDocPending = null;
			}
			ContentInserted?.Invoke (this, EventArgs.Empty);
		}

		/*		public void OnDeactivated ()
				{
					if (pathBar != null)
						pathBar.HideMenu ();
				}

				public void OnActivated ()
				{
					if (subViewToolbar != null)
						subViewToolbar.Tabs [subViewToolbar.ActiveTab].Activate ();
				}*/

		public void ShowPathBar (IPathedDocument pathDoc)
		{
			if (box == null) {
				pathDocPending = pathDoc;
				return;
			}

			DetachPathedDocument ();

			this.pathDoc = pathDoc;
			pathDoc.PathChanged += HandlePathChange;

			// If a toolbar is already being shown, we don't show the pathbar yet
			if (toolbar != null && toolbar.Visible)
				return;

			if (pathBar == null) {
				pathBar = new PathBar (pathDoc.CreatePathWidget);
				box.PackStart (pathBar, false, true, 0);
				box.ReorderChild (pathBar, 0);
				pathBar.Show ();
			}
			pathBar.SetPath (pathDoc.CurrentPath);
		}

		public void HidePathBar ()
		{
			if (pathBar != null) {
				DetachPathedDocument ();
				box.Remove (pathBar);
				pathBar.Destroy ();
				pathBar = null;
			}
		}

		void DetachPathedDocument ()
		{
			if (pathDoc != null) {
				pathDoc.PathChanged -= HandlePathChange;
				pathDoc = null;
			}
		}

		void HandlePathChange (object sender, DocumentPathChangedEventArgs args)
		{
			pathBar.SetPath (pathDoc.CurrentPath);
		}

		public override void DetachFromView ()
		{
			if (viewControl != null) {
				box.Remove (viewControl);
				viewControl = null;
			}
			base.DetachFromView ();
		}

		public void ReloadContent ()
		{
			throw new NotImplementedException ();
		}
	}
}
