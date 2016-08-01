//
// RunConfigurationOptionsPanel.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide.Execution;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	public class RunConfigurationPanel: OptionsPanel
	{
		RunConfigInfo config;
		RunConfigurationEditor editor;
		Gtk.VBox box;
		Gtk.CheckButton userConf;

		public RunConfigurationPanel ()
		{
		}

		public override void Initialize (OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);

			config = (RunConfigInfo)dataObject;
			editor = RunConfigurationService.CreateEditorForConfiguration (config.EditedConfig);

			box = new Gtk.VBox ();
			box.Spacing = 12;
			userConf = new Gtk.CheckButton (GettextCatalog.GetString ("User-specific configuration"));
			box.PackEnd (userConf, false, false, 0);
			box.PackEnd (new Gtk.HSeparator (), false, false, 0);
			box.ShowAll ();

			editor.Changed += Editor_Changed;
		}

		public override bool ValidateChanges ()
		{
			return editor.Validate ();
		}

		public override void ApplyChanges ()
		{
			if (editor != null) {
				editor.Save ();
				config.EditedConfig.StoreInUserFile = userConf.Active;
				config.ProjectConfig.CopyFrom (config.EditedConfig, false);
			}
		}

		public override Control CreatePanelWidget ()
		{
			if (editor != null) {
				editor.Load (config.Project, config.EditedConfig);
				var c = editor.CreateControl ();
				box.PackStart (c.GetNativeWidget<Gtk.Widget> (), true, true, 0);
				userConf.Active = config.EditedConfig.StoreInUserFile;
				return box;
			}
			else
				return new Gtk.Label ("");
		}

		void Editor_Changed (object sender, EventArgs e)
		{
			editor.Save ();
			var panel = ParentDialog.GetPanel<RunConfigurationsPanel> ("General");
			panel.RefreshList ();
		}
	}
}

