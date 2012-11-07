//
// WelcomePageFrame.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Xml;
using System.Linq;
using Gdk;
using Gtk;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Desktop;
using System.Reflection;
using System.Xml.Linq;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.WelcomePage
{
	class WelcomePageFrame: EventBox
	{
		WelcomePageProjectBar projectBar;

		public WelcomePageFrame (Gtk.Widget w)
		{
			VBox box = new VBox ();
			box.Show ();
			projectBar = new WelcomePageProjectBar ();
			box.PackStart (projectBar, false, false, 0);

			box.PackStart (w, true, true, 0);
			CanFocus = true;

			Add (box);
			Show ();
			UpdateProjectBar ();
		}

		public void UpdateProjectBar ()
		{
			if (IdeApp.Workspace.IsOpen || IdeApp.Workbench.Documents.Count > 0) {
				projectBar.UpdateContent ();
				projectBar.ShowAll ();
			}
			else
				projectBar.Hide ();
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape && IdeApp.Workspace.IsOpen)
				WelcomePageService.HideWelcomePage (true);
			return base.OnKeyPressEvent (evnt);
		}

		void HandleLastWorkspaceItemClosed (object sender, EventArgs e)
		{
			UpdateProjectBar ();
		}

		void HandleDocumentClosed (object sender, MonoDevelop.Ide.Gui.DocumentEventArgs e)
		{
			UpdateProjectBar ();
		}

		protected override void OnParentSet (Widget previous_parent)
		{
			base.OnParentSet (previous_parent);
			if (Parent == null) {
				IdeApp.Workspace.LastWorkspaceItemClosed -= HandleLastWorkspaceItemClosed;
				IdeApp.Workbench.DocumentClosed -= HandleDocumentClosed;
			} else {
				IdeApp.Workspace.LastWorkspaceItemClosed += HandleLastWorkspaceItemClosed;
				IdeApp.Workbench.DocumentClosed += HandleDocumentClosed;
			}
		}
	}

	class WelcomePageProjectBar: HeaderBox
	{
		Gtk.Label messageLabel;
		Gtk.Button closeButton;
		Gtk.Button backButton;

		public WelcomePageProjectBar ()
		{
			SetPadding (3, 3, 12, 12);
			GradientBackround = true;

			HBox box = new HBox (false, 6);
			box.PackStart (messageLabel = new Gtk.Label () { Xalign = 0 }, true, true, 0);
			backButton = new Gtk.Button ();
			box.PackEnd (backButton, false, false, 0);
			closeButton = new Gtk.Button ();
			box.PackEnd (closeButton, false, false, 0);

			closeButton.Clicked += delegate {
				if (IdeApp.Workspace.IsOpen)
					IdeApp.Workspace.Close ();
				else
					IdeApp.Workbench.CloseAllDocuments (false);
			};
			backButton.Clicked += delegate {
				WelcomePageService.HideWelcomePage (true);
			};
			Add (box);
			UpdateContent ();
		}

		public void UpdateContent ()
		{
			if (IdeApp.Workspace.Items.Count > 0) {
				var sols = IdeApp.Workspace.GetAllSolutions ().ToArray ();
				if (sols.Length == 1) {
					messageLabel.Text = GettextCatalog.GetString ("Solution '{0}' is currently open", sols [0].Name);
					backButton.Label = GettextCatalog.GetString ("Go Back to Solution");
					closeButton.Label = GettextCatalog.GetString ("Close Solution");
				}
				else if (sols.Length > 1) {
					messageLabel.Text = GettextCatalog.GetString ("Solution '{0}' and others are currently open", sols [0].Name);
					backButton.Label = GettextCatalog.GetString ("Go Back to Solutions");
					closeButton.Label = GettextCatalog.GetString ("Close all Solutions");
				}
				else {
					messageLabel.Text = GettextCatalog.GetString ("A workspace is currently open");
					backButton.Label = GettextCatalog.GetString ("Go Back to Workspace");
					closeButton.Label = GettextCatalog.GetString ("Close Workspace");
				}
			} else if (IdeApp.Workbench.Documents.Count> 0) {
				var files = IdeApp.Workbench.Documents.Where (d => d.IsFile).ToArray ();
				if (files.Length == 1) {
					messageLabel.Text = GettextCatalog.GetString ("The file '{0}' is currently open", files[0].FileName.FileName);
					backButton.Label = GettextCatalog.GetString ("Go Back to File");
					closeButton.Label = GettextCatalog.GetString ("Close File");
				} else if (files.Length > 1) {
					messageLabel.Text = GettextCatalog.GetString ("The file '{0}' and other are currently open", files[0].FileName.FileName);
					backButton.Label = GettextCatalog.GetString ("Go Back to Files");
					closeButton.Label = GettextCatalog.GetString ("Close Files");
				} else {
					messageLabel.Text = GettextCatalog.GetString ("Some documents are currently open");
					backButton.Label = GettextCatalog.GetString ("Go Back to Documents");
					closeButton.Label = GettextCatalog.GetString ("Close Documents");
				}
			}
		}
		
		//this is used to style like a tooltip
		bool changeStyle = false;
		
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			if (changeStyle)
				return;
			changeStyle = true;
			var surrogate = new TooltipStyleSurrogate ();
			surrogate.EnsureStyle ();
			this.Style = surrogate.Style;
			surrogate.Destroy ();

			base.OnStyleSet (previous_style);
			changeStyle = false;
		}
		
		class TooltipStyleSurrogate : TooltipWindow {}
	}
}
