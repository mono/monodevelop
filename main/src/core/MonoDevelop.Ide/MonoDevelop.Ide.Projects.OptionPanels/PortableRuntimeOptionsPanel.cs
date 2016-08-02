//
// PortableRuntimeOptionsPanel.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
// Copyright (c) Microsoft Inc.
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
using System.Text;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class PortableRuntimeOptionsPanel : ItemOptionsPanel
	{
		PortableRuntimeOptionsPanelWidget widget;
		
		public override Control CreatePanelWidget ()
		{
			return (widget = new PortableRuntimeOptionsPanelWidget ((DotNetProject) ConfiguredProject, ItemConfigurations));
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}

	class PortableRuntimeOptionsPanelWidget : Gtk.VBox
	{
		DotNetProject project;
		TargetFramework target;

		public PortableRuntimeOptionsPanelWidget (DotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			this.project = project;
			this.target = project.TargetFramework;

			Spacing = 6;

			var frameworkPickerButton = new Button (GettextCatalog.GetString ("Change Targets..."));
			PackStart (frameworkPickerButton);
			frameworkPickerButton.Clicked += PickFramework;

			ShowAll ();
		}

		void PickFramework (object sender, EventArgs e)
		{
			var dlg = new PortableRuntimeSelectorDialog (target);
			try {
				var result = MessageService.RunCustomDialog (dlg, (Gtk.Window)Toplevel);
				if (result == (int)Gtk.ResponseType.Ok) {
					target = dlg.TargetFramework;
				}
			} finally {
				dlg.Destroy ();
			}
		}

		public void Store ()
		{
			if (target != null && target != project.TargetFramework) {
				project.TargetFramework = target;
				IdeApp.ProjectOperations.SaveAsync (project);
			}
		}
	}
}
