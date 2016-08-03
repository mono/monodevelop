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
			widget = new PortableRuntimeOptionsPanelWidget ((DotNetProject) ConfiguredProject, ItemConfigurations);

			return widget;
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}

	class PortableRuntimeOptionsPanelWidget : Gtk.VBox
	{
		const string netstandardDocsUrl = "https://docs.microsoft.com/en-us/dotnet/articles/standard/library";
		const string pcldDocsUrl = "https://developer.xamarin.com/guides/cross-platform/application_fundamentals/pcl/introduction_to_portable_class_libraries/";

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

		ComboBox netStandardCombo;
		Entry targetFrameworkEntry;
		RadioButton netstandardRadio;
		RadioButton pclRadio;
		Button frameworkPickerButton;

		public PortableRuntimeOptionsPanelWidget (DotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			Build ();

			this.project = project;

			//TODO: read from the project.json
			TargetFramework = project.TargetFramework;
			NetStandardVersion = null;
		}

		void Build ()
		{
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

			var netstandardDesc = new Label { Markup = GettextCatalog.GetString ("Your library will be compatible with all frameworks that support the selected <a href='{0}'>.NET Standard</a> version.", netstandardDocsUrl), Xalign = 0f };
			GtkWorkarounds.SetLinkHandler (netstandardDesc, HandleLink);
			radioBox.PackStart (new Alignment (0f, 0f, 1f, 1f) { Child = netstandardDesc, LeftPadding = 24 });

			var pclPickerHbox = new HBox { Spacing = 10 };
			radioBox.PackStart (pclPickerHbox);
			pclRadio = new RadioButton (netstandardRadio, GettextCatalog.GetString (".NET Portable:"));
			pclPickerHbox.PackStart (pclRadio, false, false, 0);
			pclPickerHbox.PackStart (targetFrameworkEntry = new Entry { IsEditable = false, WidthChars = 20 }, false, false, 0);
			frameworkPickerButton = new Button (GettextCatalog.GetString ("Change..."));
			pclPickerHbox.PackStart (frameworkPickerButton, false, false, 0);

			var pclDesc = new Label { Markup = GettextCatalog.GetString ("Your library will be compatible with the frameworks supported by the selected <a href='{0}'>PCL profile</a>.", pcldDocsUrl), Xalign = 0f };
			GtkWorkarounds.SetLinkHandler (pclDesc, HandleLink);
			radioBox.PackStart (new Alignment (0f, 0f, 1f, 1f) { Child = pclDesc, LeftPadding = 24 });

			frameworkPickerButton.Clicked += PickFramework;

			// both toggle when we switch between them, only need to subscribe to one event
			netstandardRadio.Toggled += RadioToggled;

			UpdateSensitivity ();

			ShowAll ();
		}

		string NetStandardVersion {
			get {
				return netStandardCombo.ActiveText;
			}
			set {
				((ListStore)netStandardCombo.Model).Clear ();

				int selected = -1;

				for (int i = 0; i < KnownNetStandardVersions.Length; i++) {
					var version = KnownNetStandardVersions[i];
					netStandardCombo.AppendText (version);
					if (version == value) {
						selected = i;
					}
				}

				if (value == null) {
					selected = KnownNetStandardVersions.Length - 1;
				} else if (selected < 0) {
					//project uses some version we don't know about, add it
					netStandardCombo.AppendText (value);
					selected = KnownNetStandardVersions.Length;
				}

				netStandardCombo.Active = selected;
			}
		}

		TargetFramework TargetFramework {
			get {
				return target;
			}
			set {
				target = value;
				targetFrameworkEntry.Text = PortableRuntimeSelectorDialog.GetPclShortDisplayName (target, false);
			}
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

		void HandleLink (string url)
		{
			DesktopService.ShowUrl (url);
		}

		void PickFramework (object sender, EventArgs e)
		{
			var dlg = new PortableRuntimeSelectorDialog (target);
			try {
				var result = MessageService.RunCustomDialog (dlg, (Gtk.Window)Toplevel);
				if (result == (int)Gtk.ResponseType.Ok) {
					TargetFramework = dlg.TargetFramework;
				}
			} finally {
				dlg.Destroy ();
			}
		}

		public void Store ()
		{
			//TODO set these in the project
			var fx = TargetFramework;
			var isNetStandard = netstandardRadio.Active;
			var nsVersion = NetStandardVersion;

			//netstandard always used PCL5 framework
			if (isNetStandard) {
				fx = Runtime.SystemAssemblyService.GetTargetFramework (new TargetFrameworkMoniker (TargetFrameworkMoniker.ID_PORTABLE, "v5.0"));
			}

			if (fx != null && fx != project.TargetFramework) {
				project.TargetFramework = fx;
				IdeApp.ProjectOperations.SaveAsync (project);
			}
		}
	}
}
