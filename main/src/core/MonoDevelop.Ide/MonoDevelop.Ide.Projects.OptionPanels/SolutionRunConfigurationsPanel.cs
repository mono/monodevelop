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
	class SolutionRunConfigurationsPanel: OptionsPanel
	{
		List<SolutionRunConfigInfo> configs = new List<SolutionRunConfigInfo> ();
		Dictionary<SolutionRunConfigInfo, SolutionRunConfigurationOptionsDialogSection> sections = new Dictionary<SolutionRunConfigInfo, SolutionRunConfigurationOptionsDialogSection> ();
		SolutionRunConfigurationsPanelWidget widget;
		XwtControl control;

		public override void Initialize (OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);

			Solution = (Solution)dataObject;

			foreach (var rc in Solution.MultiStartupRunConfigurations)
				configs.Add (new SolutionRunConfigInfo { ProjectConfig = rc, EditedConfig = new MultiItemSolutionRunConfiguration (rc) });
		
			foreach (var c in configs)
				AddPanel (c);
			ParentDialog.ExpandChildren (this);
		}

		public Solution Solution { get; set; }

		public List<SolutionRunConfigInfo> Configurations {
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

		void AddPanel (SolutionRunConfigInfo configInfo)
		{
			configInfo.Solution = Solution;
			var sec = new SolutionRunConfigurationOptionsDialogSection (configInfo);
			sec.Fill = true;
			sections [configInfo] = sec;
			ParentDialog.AddChildSection (this, sec, configInfo);
		}
		
		void RemovePanel (SolutionRunConfigInfo rc)
		{
			var section = sections [rc];
			sections.Remove (rc);
			ParentDialog.RemoveSection (section);
		}
		
		internal void RemoveConfiguration (MultiItemSolutionRunConfiguration editedConfig)
		{
			var c = configs.First (ci => ci.EditedConfig == editedConfig);
			configs.Remove (c);
			RemovePanel (c);
		}

		internal void AddConfiguration (MultiItemSolutionRunConfiguration editedConfig)
		{
			var c = new SolutionRunConfigInfo { EditedConfig = editedConfig };
			configs.Add (c);
			AddPanel (c);
		}

		internal void ReplaceConfiguration (MultiItemSolutionRunConfiguration oldConf, MultiItemSolutionRunConfiguration newConf)
		{
			var i = configs.FindIndex (ci => ci.EditedConfig == oldConf);
			var oldc = configs [i];
			var newc = new SolutionRunConfigInfo { EditedConfig = newConf };
			configs [i] = newc;
			RemovePanel (oldc);
			AddPanel (newc);
		}

		internal void ShowConfiguration (MultiItemSolutionRunConfiguration editedConfig)
		{
			var rc = configs.First (ci => ci.EditedConfig == editedConfig);
			var section = sections [rc];
			ParentDialog.ShowPage (section);
		}

		public override Control CreatePanelWidget ()
		{
			widget = new SolutionRunConfigurationsPanelWidget (this, ParentDialog);
			return control = new XwtControl (widget);
		}
		
		public override void ApplyChanges ()
		{
			foreach (var c in configs.Where (co => co.ProjectConfig == null)) {
				c.ProjectConfig = new MultiItemSolutionRunConfiguration (c.EditedConfig);
				Solution.MultiStartupRunConfigurations.Add (c.ProjectConfig);
			}
			foreach (var c in Solution.MultiStartupRunConfigurations.Where (co => !configs.Any (mc => mc.EditedConfig.Name == co.Name)).ToArray ())
				Solution.MultiStartupRunConfigurations.Remove (c);
		}
	}

	class SolutionRunConfigInfo
	{
		public Solution Solution { get; set; }
		public MultiItemSolutionRunConfiguration ProjectConfig { get; set; }
		public MultiItemSolutionRunConfiguration EditedConfig { get; set; }
	}
	
	class SolutionRunConfigurationOptionsDialogSection : OptionsDialogSection
	{
		public SolutionRunConfigurationOptionsDialogSection (SolutionRunConfigInfo configInfo): base (typeof(SolutionRunConfigurationPanel))
		{
			RunConfiguration = configInfo.EditedConfig;
			Label = configInfo.EditedConfig.Name;
			HeaderLabel = GettextCatalog.GetString ("Run Configuration: {0}", configInfo.EditedConfig.Name);
			Icon = "md-prefs-play";
		}
		
		//this is used by the options dialog to look up the icon as needed, at required scales
		public MultiItemSolutionRunConfiguration RunConfiguration { get; private set; }
	}
	
	class SolutionRunConfigurationsPanelWidget: Xwt.VBox
	{
		SolutionRunConfigurationsPanel panel;
		RunConfigurationsList list;

		Xwt.Button removeButton;
		Xwt.Button copyButton;
		Xwt.Button renameButton;

		public SolutionRunConfigurationsPanelWidget (SolutionRunConfigurationsPanel panel, OptionsDialog dialog)
		{
			this.panel = panel;

			Margin = 6;
			Spacing = 6;

			list = new RunConfigurationsList ();
			PackStart (list, true);

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

			PackStart (box, false);

			list.SelectionChanged += (sender, e) => UpdateButtons ();
			list.RowActivated += (sender, e) => panel.ShowConfiguration ((MultiItemSolutionRunConfiguration)list.SelectedConfiguration);
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
		}

		void OnAddConfiguration (object sender, EventArgs e)
		{
			var okCommand = new Command (GettextCatalog.GetString ("Create"));
			using (var dlg = new RunConfigurationNameDialog (ParentWindow, "", okCommand, panel.Configurations.Select (c => c.EditedConfig.Name))) {
				dlg.Title = GettextCatalog.GetString ("New Configuration");
				if (dlg.Run () == okCommand) {
					var config = new MultiItemSolutionRunConfiguration (dlg.NewName, dlg.NewName);
					panel.AddConfiguration (config);
					Fill ();
				}
			}
		}

		void OnCopyConfiguration (object sender, EventArgs e)
		{
			var config = (MultiItemSolutionRunConfiguration)list.SelectedConfiguration;
			var okCommand = new Command (GettextCatalog.GetString ("Create"));
			using (var dlg = new RunConfigurationNameDialog (ParentWindow, config.Name, okCommand, panel.Configurations.Select (c => c.EditedConfig.Name))) {
				dlg.Title = GettextCatalog.GetString ("Duplicate Configuration");
				if (dlg.Run () == okCommand) {
					var copy = new MultiItemSolutionRunConfiguration (config, dlg.NewName);
					panel.AddConfiguration (copy);
					Fill ();
				}
			}
		}

		void OnRenameConfiguration (object sender, EventArgs e)
		{
			var config = (MultiItemSolutionRunConfiguration)list.SelectedConfiguration;
			var okCommand = new Command (GettextCatalog.GetString ("Rename"));
			using (var dlg = new RunConfigurationNameDialog (ParentWindow, config.Name, okCommand, panel.Configurations.Select (c => c.EditedConfig.Name))) {
				dlg.Title = GettextCatalog.GetString ("Rename Configuration");
				if (dlg.Run () != Command.Cancel) {
					var copy = new MultiItemSolutionRunConfiguration (config, dlg.NewName);
					panel.ReplaceConfiguration (config, copy);
					Fill ();
				}
			}
		}

		void OnRemoveConfiguration (object sender, EventArgs e)
		{
			var config = (MultiItemSolutionRunConfiguration)list.SelectedConfiguration;
			if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to remove the configuration '{0}'?", config.Name), AlertButton.Remove)) {
				panel.RemoveConfiguration (config);
				Fill ();
			}
		}
	}
}
