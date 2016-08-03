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

		string[] KnownNetStandardVersions = new [] {
			"netstandard1.0",
			"netstandard1.1",
			"netstandard1.2",
			"netstandard1.3",
			"netstandard1.4",
			"netstandard1.5",
			"netstandard1.6",
		};

		readonly ComboBox netStandardCombo;
		readonly Entry targetFrameworkEntry;
		readonly RadioButton netstandardRadio;
		readonly RadioButton pclRadio;
		readonly Button frameworkPickerButton;

		public PortableRuntimeOptionsPanelWidget (DotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			this.project = project;
			this.target = project.TargetFramework;

			Spacing = 6;

			PackStart (new Label { Markup = string.Format ("<b>{0}</b>", GettextCatalog.GetString ("Target Framework")), Xalign = 0f });

			var fxAlignment = new Alignment (0f, 0f, 1f, 1f) { LeftPadding = 12 };
			PackStart (fxAlignment);
			var radioBox = new VBox { Spacing = 10 };
			fxAlignment.Add (radioBox);

			var netstandardPickerHbox = new HBox { Spacing = 10 };
			radioBox.PackStart (netstandardPickerHbox);
			netstandardRadio = new RadioButton (GettextCatalog.GetString (".NET Standard Platform:"));
			netstandardPickerHbox.PackStart (netstandardRadio, false, false, 0);
			netstandardPickerHbox.PackStart (netStandardCombo = ComboBox.NewText (), false, false, 0);

			var netstandardDesc = new Label { Markup = GettextCatalog.GetString ("Your library will be compatible with all frameworks that support the selected <a href='{0}'>.NET Standard</a> version.", "netstandard"), Xalign = 0f };
			radioBox.PackStart (new Alignment (0f, 0f, 1f, 1f) { Child = netstandardDesc, LeftPadding = 24 });

			var pclPickerHbox = new HBox { Spacing = 10 };
			radioBox.PackStart (pclPickerHbox);
			pclRadio = new RadioButton (netstandardRadio, GettextCatalog.GetString (".NET Portable:"));
			pclPickerHbox.PackStart (pclRadio, false, false, 0);
			pclPickerHbox.PackStart (targetFrameworkEntry = new Entry { IsEditable = false, WidthChars = 20 }, false, false, 0);
			frameworkPickerButton = new Button (GettextCatalog.GetString ("Change..."));
			pclPickerHbox.PackStart (frameworkPickerButton, false, false, 0);

			var pclDesc = new Label { Markup = GettextCatalog.GetString ("Your library will be compatible with the frameworks supported by the selected <a href='{0}'>PCL profile</a>.", "pcl"), Xalign = 0f };
			radioBox.PackStart (new Alignment (0f, 0f, 1f, 1f) { Child = pclDesc, LeftPadding = 24 });

			frameworkPickerButton.Clicked += PickFramework;

			foreach (var val in KnownNetStandardVersions) {
				netStandardCombo.AppendText (val);
			};
			netStandardCombo.Active = KnownNetStandardVersions.Length - 1;

			targetFrameworkEntry.Text = string.Format ("PCL {0} - {1}", target.Id.Version, target.Id.Profile);

			// both toggle when we switch between them, only need to subscribe to one event
			netstandardRadio.Toggled += RadioToggled;

			UpdateSensitivity ();

			ShowAll ();
		}

		void RadioToggled (object sender, EventArgs e)
		{
			UpdateSensitivity ();
		}

		void UpdateSensitivity()
		{
			bool pcl = pclRadio.Active;

			netStandardCombo.Sensitive = !pcl;
			targetFrameworkEntry.Sensitive = pcl;
			frameworkPickerButton.Sensitive = pcl;
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
