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
using MonoDevelop.Ide.Execution;
using MonoDevelop.Ide.Projects.OptionPanels;
using MonoDevelop.Projects;
using Xwt;

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
			widget.Save ();
		}

		public override bool Validate ()
		{
			return widget.Validate ();
		}
	}

	class DotNetCoreRunConfigurationEditorWidget : DotNetRunConfigurationEditorWidget
	{
		DotNetCoreRunConfiguration config;
		CheckBox launchBrowser;
		public void LoadCore (Project project, DotNetCoreRunConfiguration config)
		{
			this.config = config;
			base.Load (project, config);
			var coreProject = project.GetFlavor<DotNetCoreProjectExtension> ();
			if (coreProject == null || !coreProject.IsWeb)
				return;
			var mainBox = new VBox ();
			mainBox.Margin = 12;
			mainBox.PackStart (launchBrowser = new CheckBox (GettextCatalog.GetString ("Launch browser")));
			Add (mainBox, GettextCatalog.GetString ("ASP.NET Core"));


			launchBrowser.Toggled += delegate {
				NotifyChanged ();
			};
		}

		public void SaveCore ()
		{
			base.Save ();
			config.LaunchBrowser = launchBrowser.Active;
		}

		public bool ValidateCore ()
		{
			if (!base.Validate ())
				return false;
			return true;
		}
	}
}
