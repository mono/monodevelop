// 
// CodeFormattingPanelWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Components;
using System.Linq;
using RefactoringEssentials.CSharp.Diagnostics;
using Xwt.Backends;
using Xwt;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class RunConfigurationsPanel: OptionsPanel
	{
		List<RunConfigInfo> configs = new List<RunConfigInfo> ();
		Dictionary<RunConfigInfo, RunConfigurationOptionsDialogSection> sections = new Dictionary<RunConfigInfo, RunConfigurationOptionsDialogSection> ();
		RunConfigurationsPanelWidget widget;
		XwtControl control;

		public override void Initialize (OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);

			Project = (Project)dataObject;

			if (!Project.SupportsRunConfigurations ())
				return;
			
			foreach (var rc in Project.RunConfigurations)
				configs.Add (new RunConfigInfo { ProjectConfig = rc, EditedConfig = Project.CloneRunConfiguration (rc, rc.Name) });
		
			foreach (var c in configs)
				AddPanel (c);
			ParentDialog.ExpandChildren (this);
		}

		public override bool IsVisible ()
		{
			return Project.SupportsRunConfigurations ();
		}

		public Project Project { get; set; }

		public List<RunConfigInfo> Configurations {
			get { return configs; }
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
		}

		public void RefreshList ()
		{
			if (widget != null)
				widget.RefreshList ();
		}

		void AddPanel (RunConfigInfo configInfo)
		{
			configInfo.Project = Project;
			var sec = new RunConfigurationOptionsDialogSection (configInfo);
			sec.Fill = true;
			sections [configInfo] = sec;
			ParentDialog.AddChildSection (this, sec, configInfo);
		}
		
		void RemovePanel (RunConfigInfo rc)
		{
			var section = sections [rc];
			sections.Remove (rc);
			ParentDialog.RemoveSection (section);
		}
		
		internal void RemoveConfiguration (ProjectRunConfiguration editedConfig)
		{
			var c = configs.First (ci => ci.EditedConfig == editedConfig);
			configs.Remove (c);
			RemovePanel (c);
		}

		internal void AddConfiguration (ProjectRunConfiguration editedConfig)
		{
			var c = new RunConfigInfo { EditedConfig = editedConfig };
			configs.Add (c);
			AddPanel (c);
		}

		internal void ReplaceConfiguration (ProjectRunConfiguration oldConf, ProjectRunConfiguration newConf)
		{
			var i = configs.FindIndex (ci => ci.EditedConfig == oldConf);
			var oldc = configs [i];
			var newc = new RunConfigInfo { EditedConfig = newConf };
			configs [i] = newc;
			RemovePanel (oldc);
			AddPanel (newc);
		}

		internal void ShowConfiguration (ProjectRunConfiguration editedConfig)
		{
			var rc = configs.First (ci => ci.EditedConfig == editedConfig);
			var section = sections [rc];
			ParentDialog.ShowPage (section);
		}

		public override Control CreatePanelWidget ()
		{
			widget = new RunConfigurationsPanelWidget (this, ParentDialog);
			return control = new XwtControl (widget);
		}
		
		public override void ApplyChanges ()
		{
			foreach (var c in configs.Where (co => co.ProjectConfig == null)) {
				c.ProjectConfig = Project.CloneRunConfiguration (c.EditedConfig);
				Project.RunConfigurations.Add (c.ProjectConfig);
			}
			foreach (var c in Project.RunConfigurations.Where (co => !configs.Any (mc => mc.EditedConfig.Name == co.Name)).ToArray ())
				Project.RunConfigurations.Remove (c);
		}
	}

	class RunConfigInfo
	{
		public Project Project { get; set; }
		public ProjectRunConfiguration ProjectConfig { get; set; }
		public ProjectRunConfiguration EditedConfig { get; set; }
	}
	
	class RunConfigurationOptionsDialogSection : OptionsDialogSection
	{
		public RunConfigurationOptionsDialogSection (RunConfigInfo configInfo): base (typeof(RunConfigurationPanel))
		{
			this.RunConfiguration = configInfo.EditedConfig;
			Label = configInfo.EditedConfig.Name;
			HeaderLabel = GettextCatalog.GetString ("Run Configuration: {0}", configInfo.EditedConfig.Name);
			Icon = "md-prefs-play";
		}
		
		//this is used by the options dialog to look up the icon as needed, at required scales
		public SolutionItemRunConfiguration RunConfiguration { get; private set; }
	}
	
	partial class RunConfigurationsPanelWidget: Xwt.VBox
	{
		RunConfigurationsPanel panel;
		RunConfigurationsList list;

		Xwt.Button removeButton;
		Xwt.Button copyButton;
		Xwt.Button renameButton;

		public RunConfigurationsPanelWidget (RunConfigurationsPanel panel, OptionsDialog dialog)
		{
			this.panel = panel;

			this.Margin = 6;
			Spacing = 6;

			list = new RunConfigurationsList ();
			this.PackStart (list, true);

			var box = new Xwt.HBox ();
			box.Spacing = 6;

			var btn = new Xwt.Button (GettextCatalog.GetString ("New"));
			btn.Clicked += OnAddConfiguration;
			box.PackStart (btn, false);

			copyButton = new Xwt.Button (GettextCatalog.GetString ("Duplicate"));
			copyButton.Clicked += OnCopyConfiguration;
			box.PackStart (copyButton, false);

			renameButton = new Xwt.Button (GettextCatalog.GetString ("Rename"));
			renameButton.Clicked += OnRenameConfiguration;
			box.PackStart (renameButton, false);

			removeButton = new Xwt.Button (GettextCatalog.GetString ("Remove"));
			removeButton.Clicked += OnRemoveConfiguration;
			box.PackEnd (removeButton, false);

			Fill ();

			this.PackStart (box, false);

			list.SelectionChanged += (sender, e) => UpdateButtons ();
			list.RowActivated += (sender, e) => panel.ShowConfiguration ((ProjectRunConfiguration)list.SelectedConfiguration);
			UpdateButtons ();
		}

		void Fill ()
		{
			list.Fill (panel.Configurations.Select (c => c.EditedConfig).ToArray ());
		}

		public void RefreshList ()
		{
			Fill ();
			UpdateButtons ();
		}

		void UpdateButtons ()
		{
			var selection = list.SelectedConfiguration != null;
			removeButton.Sensitive = selection;
			copyButton.Sensitive = selection;
			renameButton.Sensitive = selection;
			if (selection) {
				if (((ProjectRunConfiguration)list.SelectedConfiguration).IsDefaultConfiguration) {
					removeButton.Sensitive = false;
					renameButton.Sensitive = false;
				}
			}
		}

		void OnAddConfiguration (object sender, EventArgs e)
		{
			var okCommand = new Command (GettextCatalog.GetString ("Create"));
			using (var dlg = new RunConfigurationNameDialog (ParentWindow, "", okCommand, panel.Configurations.Select (c => c.EditedConfig.Name))) {
				dlg.Title = GettextCatalog.GetString ("New Configuration");
				if (dlg.Run () == okCommand) {
					var config = panel.Project.CreateRunConfiguration (dlg.NewName);
					panel.AddConfiguration (config);
					Fill ();
				}
			}
		}

		void OnCopyConfiguration (object sender, EventArgs e)
		{
			var config = (ProjectRunConfiguration)list.SelectedConfiguration;
			var okCommand = new Command (GettextCatalog.GetString ("Create"));
			using (var dlg = new RunConfigurationNameDialog (ParentWindow, config.Name, okCommand, panel.Configurations.Select (c => c.EditedConfig.Name))) {
				dlg.Title = GettextCatalog.GetString ("Duplicate Configuration");
				if (dlg.Run () == okCommand) {
					var copy = panel.Project.CloneRunConfiguration (config, dlg.NewName);
					panel.AddConfiguration (copy);
					Fill ();
				}
			}
		}

		void OnRenameConfiguration (object sender, EventArgs e)
		{
			var config = (ProjectRunConfiguration)list.SelectedConfiguration;
			var okCommand = new Command (GettextCatalog.GetString ("Rename"));
			using (var dlg = new RunConfigurationNameDialog (ParentWindow, config.Name, okCommand, panel.Configurations.Select (c => c.EditedConfig.Name))) {
				dlg.Title = GettextCatalog.GetString ("Rename Configuration");
				if (dlg.Run () != Command.Cancel) {
					var copy = panel.Project.CloneRunConfiguration (config, dlg.NewName);
					panel.ReplaceConfiguration (config, copy);
					Fill ();
				}
			}
		}

		void OnRemoveConfiguration (object sender, EventArgs e)
		{
			var config = (ProjectRunConfiguration)list.SelectedConfiguration;
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to remove the configuration '{0}'?", config.Name), AlertButton.Remove)) {
				panel.RemoveConfiguration (config);
				Fill ();
			}
		}
	}

	class RunConfigurationNameDialog: Xwt.Dialog
	{
		VBox mainBox;
		TextEntry entry;
		Label errorLabel;
		IEnumerable<string> invalidNames;
		DialogButton okButton;

		public RunConfigurationNameDialog (Xwt.WindowFrame parent, string name, Command action, IEnumerable<string> invalidNames)
		{
			TransientFor = parent;
			Resizable = false;
			Width = 300;
			this.invalidNames = invalidNames;
			mainBox = new VBox ();
			var box = new HBox ();
			mainBox.PackStart (box, true);
			box.PackStart (new Label (GettextCatalog.GetString ("Name")), false);
			entry = new TextEntry ();
			entry.Text = name ?? "";
			box.PackStart (entry, true);
			Content = mainBox;

			Buttons.Add (new DialogButton (Command.Cancel));
			Buttons.Add (okButton = new DialogButton (action));
			this.DefaultCommand = okButton.Command;
			entry.Activated += Entry_Activated;
			entry.Changed += (s, o) => UpdateControls ();
		}

		void Entry_Activated (object sender, EventArgs e)
		{
			OnCommandActivated (okButton.Command);
		}

		void UpdateControls ()
		{
			okButton.Sensitive = entry.Text.Length > 0;
		}

		protected override void OnShown ()
		{
			UpdateControls ();
			base.OnShown ();
		}

		public string NewName {
			get { return entry.Text; }
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd != Command.Cancel) {
				if (entry.Text.Length == 0) {
					ShowError (GettextCatalog.GetString ("The name can't be empty"));
					return;
				}
				if (invalidNames.Contains (entry.Text)) {
					ShowError (GettextCatalog.GetString ("This name is already in use"));
					return;
				}
			}
			base.OnCommandActivated (cmd);
		}

		void ShowError (string msg)
		{
			if (errorLabel == null) {
				errorLabel = new Label ();
				mainBox.PackStart (errorLabel, false);
			}
			errorLabel.Text = msg;
			errorLabel.TextColor = Xwt.Drawing.Colors.Red;
		}
	}
}
