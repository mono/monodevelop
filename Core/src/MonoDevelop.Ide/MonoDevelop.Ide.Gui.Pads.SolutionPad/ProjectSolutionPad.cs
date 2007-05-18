//
// ProjectSolutionPad.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Resources;

using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Projects;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class ProjectSolutionPad : TreeViewPad
	{
		static ProjectSolutionPad instance = null;
		public static ProjectSolutionPad Instance {
			get {
				return instance;
			}
		}
		
		ToolButton openButton;
		ToggleToolButton showHiddenButton;
		
		public bool ShowAllFiles {
			get {
				return showHiddenButton.Active;
			}
			set {
				showHiddenButton.Active = value;
			}
		}
		
		protected override Toolbar CreateToolbar ()
		{
			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.Menu;
						
			openButton = new ToggleToolButton ();
			openButton.Label = GettextCatalog.GetString ("Open");
			openButton.IconWidget = new Gtk.Image (Services.Resources.GetBitmap (MonoDevelop.Core.Gui.Stock.OpenFileIcon));
			
			openButton.IsImportant = true;
			openButton.Clicked += new EventHandler (OpenButtonClicked);
			//showHiddenButton.SetTooltip (tips, GettextCatalog.GetString ("Toggle show all files"), GettextCatalog.GetString ("Show Successful Tests"));
			toolbar.Insert (openButton, -1);
			
			showHiddenButton = new ToggleToolButton ();
			showHiddenButton.Label = GettextCatalog.GetString ("Show Hidden");
			showHiddenButton.Active = false;
			//showHiddenButton.IconWidget = Context.GetIcon (Stock.PropertiesIcon);
			showHiddenButton.IsImportant = true;
			showHiddenButton.Toggled += new EventHandler (ShowHiddenButtonToggled);
			//showHiddenButton.SetTooltip (tips, GettextCatalog.GetString ("Toggle show all files"), GettextCatalog.GetString ("Show Successful Tests"));
			toolbar.Insert (showHiddenButton, -1);
			
			return toolbar;
		}
		
		public ProjectSolutionPad()
		{
			Debug.Assert (instance == null);
			instance = this;
			ProjectService.SolutionOpened += delegate(object sender, SolutionEventArgs e) {
				FillTree (e.Solution);
			};
			
			ProjectService.SolutionClosed += delegate(object sender, EventArgs e) {
				Clear ();
			};
			
			Runtime.Properties.PropertyChanged += delegate(object sender, PropertyEventArgs e) {
				if (e.OldValue != e.NewValue && e.Key == "MonoDevelop.Core.Gui.ProjectBrowser.ShowExtensions") {
					RedrawContent ();
				}
			};
			
			FillTree (ProjectService.Solution);
		}
		
		void OpenButtonClicked (object sender, EventArgs e)
		{
			ITreeNavigator navigator = base.GetSelectedNode ();
			FileNode fileNode = navigator.DataItem as FileNode;
			if (fileNode != null) {
				IdeApp.Workbench.OpenDocument (fileNode.FileName);
			} else {
				navigator.Expanded = true;
			}
		}
		
		void ShowHiddenButtonToggled (object sender, EventArgs e)
		{
			FillTree (ProjectService.Solution);
		}
		
		void FillTree (Solution solution)
		{
			Clear ();
			if (solution != null)
				LoadTree (solution);
		}
	}
}
