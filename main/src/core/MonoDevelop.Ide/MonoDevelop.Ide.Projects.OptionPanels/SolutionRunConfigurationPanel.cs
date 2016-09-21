//
// SolutionRunConfigurationPanel.cs
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
using MonoDevelop.Ide.Gui.Dialogs;
using Xwt;
using MonoDevelop.Projects;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	public class SolutionRunConfigurationPanel: OptionsPanel
	{
		SolutionRunConfigInfo config;
		ListStore store;
		ListView listView;
		DataField<bool> selectedField = new DataField<bool> ();
		DataField<string> projectNameField = new DataField<string> ();
		DataField<SolutionItem> projectField = new DataField<SolutionItem> ();
		DataField<string> runConfigField = new DataField<string> ();
		DataField<ItemCollection> projectRunConfigsField = new DataField<ItemCollection> ();

		public SolutionRunConfigurationPanel ()
		{
		}

		public override void Initialize (OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);

			config = (SolutionRunConfigInfo)dataObject;

			store = new ListStore (selectedField, projectNameField, projectField, runConfigField, projectRunConfigsField);
			listView = new ListView (store);

			var col1 = new ListViewColumn (GettextCatalog.GetString ("Solution Item"));
			var cb = new CheckBoxCellView (selectedField);
			cb.Toggled += SelectionChanged;
			cb.Editable = true;
			col1.Views.Add (cb);
			col1.Views.Add (new TextCellView (projectNameField));
			listView.Columns.Add (col1);

			var configSelView = new ComboBoxCellView (runConfigField);
			configSelView.Editable = true;
			configSelView.ItemsField = projectRunConfigsField;
			var col2 = new ListViewColumn (GettextCatalog.GetString ("Run Configuration"), configSelView);
			listView.Columns.Add (col2);

			foreach (var it in config.Solution.GetAllSolutionItems ().Where (si => si.SupportsExecute ()).OrderBy (si => si.Name)) {
				var row = store.AddRow ();
				var si = config.EditedConfig.Items.FirstOrDefault (i => i.SolutionItem == it);
				var sc = si?.RunConfiguration?.Name ?? it.GetDefaultRunConfiguration ()?.Name;
				var configs = new ItemCollection ();
				foreach (var pc in it.GetRunConfigurations ())
					configs.Add (pc.Name);
				store.SetValues (row, selectedField, si != null, projectNameField, it.Name, projectField, it, runConfigField, sc, projectRunConfigsField, configs);
			}
		}

		public override bool ValidateChanges ()
		{
			return true;
		}

		public override void ApplyChanges ()
		{
			SaveChanges ();
			if (config.ProjectConfig != null)
				config.ProjectConfig.CopyFrom (config.EditedConfig);
		}

		void SaveChanges ()
		{
			config.EditedConfig.Items.Clear ();
			for (int n = 0; n < store.RowCount; n++) {
				if (store.GetValue (n, selectedField)) {
					var proj = store.GetValue (n, projectField);
					var rconf = store.GetValue (n, runConfigField);
					var conf = rconf != null ? proj.GetRunConfigurations ().FirstOrDefault (c => c.Name == rconf) : null;
					config.EditedConfig.Items.Add (new StartupItem (proj, conf));
				}
			}
		}

		public override Control CreatePanelWidget ()
		{
			return new XwtControl (listView);
		}

		void SelectionChanged (object sender, WidgetEventArgs e)
		{
			Xwt.Application.Invoke (delegate {
				SaveChanges ();
				var panel = ParentDialog.GetPanel<SolutionRunConfigurationsPanel> ("General");
				panel.RefreshList ();
			});
		}
	}
}
