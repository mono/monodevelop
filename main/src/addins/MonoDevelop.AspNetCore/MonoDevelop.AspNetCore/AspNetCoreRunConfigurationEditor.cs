//
// AspNetCoreRunConfigurationEditor.cs
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

namespace MonoDevelop.AspNetCore
{
	class AspNetCoreRunConfigurationEditor : RunConfigurationEditor
	{
		AspNetCoreRunConfigurationEditorWidget widget;

		public AspNetCoreRunConfigurationEditor ()
		{
			widget = new AspNetCoreRunConfigurationEditorWidget ();
		}

		public override Control CreateControl ()
		{
			return new XwtControl (widget);
		}

		public override void Load (Project project, SolutionItemRunConfiguration config)
		{
			widget.LoadCore (project, (AspNetCoreRunConfiguration)config);
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

	class AspNetCoreRunConfigurationEditorWidget : DotNetRunConfigurationEditorWidget
	{
		AspNetCoreRunConfiguration config;
		CheckBox launchBrowser;
		TextEntry launchUrl;
		TextEntry applicationUrl;
		XwtBoxTooltip applicationUrlWarningTooltip;

		public AspNetCoreRunConfigurationEditorWidget ()
			: base (false)
		{

		}

		public void LoadCore (Project project, AspNetCoreRunConfiguration config)
		{
			this.config = config;
			base.Load (project, config);
			var mainBox = new VBox ();
			mainBox.Margin = 24;

			var appUrlTable = new Table ();
			appUrlTable.Add (new Label (GettextCatalog.GetString ("App URL:")), 0, 0);
			var applicationUrlBox = new HBox ();
			applicationUrlWarningTooltip = new XwtBoxTooltip (new Xwt.ImageView (ImageService.GetIcon (Ide.Gui.Stock.Warning, Gtk.IconSize.Menu))) {
				ToolTip = GettextCatalog.GetString ("Invalid URL"),
				Severity = Ide.Tasks.TaskSeverity.Warning
			};
			applicationUrlBox.PackStart (applicationUrl = new TextEntry (), true, true);
			applicationUrlBox.PackStart (applicationUrlWarningTooltip);
			appUrlTable.Add (applicationUrlBox, 1, 0, hexpand: true);
			appUrlTable.Add (new Label (GettextCatalog.GetString ("Where your app should listen for connections")) { Sensitive = false }, 1, 1, hexpand: true);
			mainBox.PackStart (appUrlTable);

			mainBox.PackStart (launchBrowser = new CheckBox (GettextCatalog.GetString ("Open URL in web browser when app starts:")), marginTop: 16);

			var browserTable = new Table ();
			browserTable.MarginLeft = 16;
			var offset = 0;
			//offset = 1; // just so uncommenting Browser Combobox works as expected
			//browserTable.Add (new Label (GettextCatalog.GetString ("Web Browser:")), 0, 0, hpos: WidgetPlacement.End);
			//var browsersCombobox = new ComboBox ();
			//browsersCombobox.Items.Add ("Chrome");
			//browsersCombobox.Items.Add ("Firefox");
			//browsersCombobox.Items.Add ("Opera");
			//browsersCombobox.Items.Add ("Safari");
			//browserTable.Add (browsersCombobox, 1, 0, hpos: WidgetPlacement.Start);
			browserTable.Add (launchUrl = new TextEntry (), 1, offset, hexpand: true);
			browserTable.Add (new Label (GettextCatalog.GetString ("URL:")), 0, offset, hpos: WidgetPlacement.End);
			browserTable.Add (new Label (GettextCatalog.GetString ("Absolute or relative to App URL")) { Sensitive = false }, 1, offset + 1);
			mainBox.PackStart (browserTable);

			Add (mainBox, GettextCatalog.GetString ("ASP.NET Core"));

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
			config.LaunchBrowser = launchBrowser.Active;
			config.LaunchUrl = launchUrl.Text;
			config.ApplicationURL = applicationUrl.Text;
		}

		public bool ValidateCore ()
		{
			if (!base.Validate ())
				return false;
			return IsValidUrl (applicationUrl.Text);
		}
	}
}
