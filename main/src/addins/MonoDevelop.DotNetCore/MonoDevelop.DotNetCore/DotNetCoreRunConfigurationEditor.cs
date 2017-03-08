//
// DotNetCoreRunConfigurationEditor.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Ide.Projects.OptionPanels;
using MonoDevelop.Projects;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreRunConfigurationEditor : RunConfigurationEditor
	{
		DotNetCoreRunConfigurationEditorWidget widget;

		public DotNetCoreRunConfigurationEditor ()
		{
			widget = new DotNetCoreRunConfigurationEditorWidget ();
		}

		public override Control CreateControl ()
		{
			return new XwtControl (widget);
		}

		public override void Load (Project project, SolutionItemRunConfiguration config)
		{
			widget.LoadCore (project, (DotNetCoreRunConfiguration)config);
			widget.Changed += (sender, e) => NotifyChanged ();
		}

		public override void Save ()
		{
			widget.SaveCore ();
		}

		public override bool Validate ()
		{
			return widget.ValidateCore ();
		}
	}

	class DotNetCoreRunConfigurationEditorWidget : DotNetRunConfigurationEditorWidget
	{
		DotNetCoreRunConfiguration config;
		CheckBox launchBrowser;
		TextEntry launchUrl;
		TextEntry applicationUrl;
		Xwt.ImageView applicationUrlWarning;
		XwtBoxTooltip applicationUrlWarningTooltip;


		public void LoadCore (Project project, DotNetCoreRunConfiguration config)
		{
			this.config = config;
			base.Load (project, config);
			var coreProject = project.GetFlavor<DotNetCoreProjectExtension> ();
			if (coreProject == null || !coreProject.IsWeb)
				return;
			var table = new Table ();
			table.Margin = 12;

			table.Add (launchBrowser = new CheckBox (GettextCatalog.GetString ("Launch URL:")), 0, 0);
			table.Add (launchUrl = new TextEntry () { PlaceholderText = GettextCatalog.GetString ("Absolute or relative URL") }, 1, 0, hexpand: true);

			table.Add (new Label (GettextCatalog.GetString ("App URL:")), 0, 1);

			applicationUrlWarning = new Xwt.ImageView (ImageService.GetIcon (Ide.Gui.Stock.Warning, Gtk.IconSize.Menu));
			applicationUrlWarningTooltip = new XwtBoxTooltip (applicationUrlWarning) {
				ToolTip = GettextCatalog.GetString ("Invalid URL"),
				Severity = Ide.Tasks.TaskSeverity.Warning
			};

			var applicationUrlBox = new HBox ();
			applicationUrlBox.PackStart (applicationUrl = new TextEntry () { PlaceholderText = GettextCatalog.GetString ("The URL of the application") }, true, true);
			applicationUrlBox.PackStart (applicationUrlWarningTooltip);

			table.Add (applicationUrlBox, 1, 1, hexpand: true);
			Add (table, GettextCatalog.GetString ("ASP.NET Core"));

			launchBrowser.Active = config.LaunchBrowser;
			launchUrl.Text = config.LaunchUrl;
			applicationUrl.Text = config.ApplicationURL;

			UpdateUI ();

			launchBrowser.Toggled += delegate { NotifyChanged (); UpdateUI (); };
			launchUrl.Changed += delegate { NotifyChanged (); };
			applicationUrl.Changed += delegate { NotifyChanged (); UpdateUI (); };
		}

		void UpdateUI ()
		{
			launchUrl.Sensitive = launchBrowser.Active;
			applicationUrlWarningTooltip.Visible = !IsValidUrl (applicationUrl.Text);
		}

		bool IsValidUrl (string url)
		{
			Uri dummy;
			return Uri.TryCreate (url, UriKind.Absolute, out dummy);
		}

		public void SaveCore ()
		{
			base.Save ();
			if (applicationUrl != null) {//applicationUrl != null means it's web project, hence set values
				config.LaunchBrowser = launchBrowser.Active;
				config.LaunchUrl = launchUrl.Text;
				config.ApplicationURL = applicationUrl.Text;
			}
		}

		public bool ValidateCore ()
		{
			if (!base.Validate ())
				return false;
			return applicationUrl == null || IsValidUrl (applicationUrl.Text);//applicationUrl == null means it's not web project, hence it's valid config
		}
	}
}
