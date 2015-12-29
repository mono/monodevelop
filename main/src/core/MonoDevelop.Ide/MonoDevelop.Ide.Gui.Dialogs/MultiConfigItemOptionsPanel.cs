// MultiConfigItemOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public abstract class MultiConfigItemOptionsPanel: ItemOptionsPanel, IOptionsPanel
	{
		MultiConfigItemOptionsDialog dialog;
		Gtk.ComboBox configCombo;
		Gtk.ListStore configListStore;
		const int configListStoreConfigNameColumn = 1;
		Gtk.ComboBox platformCombo;
		List<ItemConfiguration> currentConfigs = new List<ItemConfiguration> ();
		List<string> platforms = new List<string> ();
		Gtk.Widget panelWidget;
		
		bool loading;
		bool widgetCreated;
		bool allowMixedConfigurations;
		int lastConfigSelection = -1;
		int lastPlatformSelection = -1;
		
		internal ConfigurationData ConfigurationData {
			get { return dialog.ConfigurationData; }
		}
		
		public override void Initialize (OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
			this.dialog = dialog as MultiConfigItemOptionsDialog;
			if (this.dialog == null)
				throw new System.InvalidOperationException ("MultiConfigItemOptionsPanel can only be used in options dialogs of type MultiConfigItemOptionsDialog. Panel type: " + GetType ());
			this.dialog.ConfigurationData.ConfigurationsChanged += OnConfigurationsChanged;
		}
		
		public ItemConfiguration CurrentConfiguration {
			get {
				if (allowMixedConfigurations)
					throw new System.InvalidOperationException ("The options panel is working in multiple configuration selection mode (AllowMixedConfigurations=true). Use the property CurrentConfigurations to get the list of all selected configurations.");
				return currentConfigs.Count > 0 ? currentConfigs [0] : null;
			}
		}

		public ItemConfiguration[] CurrentConfigurations {
			get {
				if (!allowMixedConfigurations)
					throw new System.InvalidOperationException ("The options panel is working in single configuration selection mode (AllowMixedConfigurations=false). Use the property CurrentConfiguration to get the selected configuration.");
				return currentConfigs.ToArray ();
			}
		}

		// Set to true to allow the user changing data of several configurations
		// at the same time
		public bool AllowMixedConfigurations {
			get {
				return allowMixedConfigurations;
			}
			set {
				allowMixedConfigurations = value;
				if (widgetCreated) {
					SaveConfigurations ();
					FillConfigurations ();
					UpdateSelection ();
				}
			}
		}
		
		Gtk.Widget IOptionsPanel.CreatePanelWidget ()
		{
			Gtk.VBox cbox = new Gtk.VBox (false, 6);
			Gtk.HBox combosBox = new Gtk.HBox (false, 6);
			cbox.PackStart (combosBox, false, false, 0);
			combosBox.PackStart (new Gtk.Label (GettextCatalog.GetString ("Configuration:")), false, false, 0);
			configListStore = new Gtk.ListStore (typeof(string), typeof(string));
			configCombo = new Gtk.ComboBox (configListStore);
			var cell = new Gtk.CellRendererText ();
			configCombo.PackStart (cell, true);
			configCombo.AddAttribute (cell, "text", 0);
			combosBox.PackStart (configCombo, false, false, 0);
			combosBox.PackStart (new Gtk.Label (GettextCatalog.GetString ("Platform:")), false, false, 0);
			platformCombo = Gtk.ComboBox.NewText ();
			combosBox.PackStart (platformCombo, false, false, 0);
			cbox.PackStart (new Gtk.HSeparator (), false, false, 0);
			cbox.ShowAll ();

			cbox.Hidden += OnPageHidden;
			cbox.Shown += OnPageShown;
			
			lastConfigSelection = -1;
			lastPlatformSelection = -1;
			
			FillConfigurations ();
			UpdateSelection ();
			
			configCombo.Changed += OnConfigChanged;
			platformCombo.Changed += OnConfigChanged;
			
			bool oldMixed = allowMixedConfigurations;
			Gtk.Widget child = CreatePanelWidget ();
			
			//HACK: work around bug 469427 - broken themes match on widget names
			if (child.Name.IndexOf ("Panel") > 0)
				child.Name = child.Name.Replace ("Panel", "_");
			
			cbox.PackStart (child, true, true, 0);
			
			if (allowMixedConfigurations != oldMixed) {
				// If mixed mode has changed, update the configuration list
				FillConfigurations ();
				UpdateSelection ();
			}
			widgetCreated = true;
			panelWidget = child;
			
			if (currentConfigs.Count > 0) {
				panelWidget.Sensitive = true;
				LoadConfigData ();
			}
			else
				panelWidget.Sensitive = false;
			
			return cbox;
		}
		
		void FillConfigurations ()
		{
			loading = true;
			configListStore.Clear ();
			
			if (allowMixedConfigurations)
				AppendComboConfig (GettextCatalog.GetString ("All Configurations"));

			HashSet<string> configs = new HashSet<string> ();
			foreach (ItemConfiguration config in FilterConfigurations (dialog.ConfigurationData.Configurations)) {
				if (configs.Add (config.Name))
					AppendComboConfig (config.Name);
			}
			
			loading = false;
		}

		void AppendComboConfig (string configName)
		{
			bool isActive = IdeApp.Workspace.ActiveConfigurationId == configName;
			AppendComboConfig (configName, isActive);
		}

		void AppendComboConfig (string configName, bool isActive = false)
		{
			string displayName = configName;
			if (isActive)
				displayName = configName + " " + GettextCatalog.GetString ("(Active)");

			configListStore.AppendValues (displayName, configName);
		}
		
		protected virtual bool ConfigurationsAreEqual (IEnumerable<ItemConfiguration> configs)
		{
			return false;
		}
		
		protected virtual IEnumerable<ItemConfiguration> FilterConfigurations (IEnumerable<ItemConfiguration> configurations)
		{
			return configurations;
		}
		
		void FillPlatforms ()
		{
			loading = true;
			
			((Gtk.ListStore)platformCombo.Model).Clear ();
			platforms.Clear ();

			string configName = null;
			if (!allowMixedConfigurations || configCombo.Active > 0)
				configName = GetSelectedComboConfig ();

			foreach (ItemConfiguration config in FilterConfigurations (dialog.ConfigurationData.Configurations)) {
				if ((configName == null || config.Name == configName) && !platforms.Contains (config.Platform)) {
					platforms.Add (config.Platform);
					platformCombo.AppendText (GetPlatformName (config.Platform));
				}
			}
			loading = false;
		}

		string GetSelectedComboConfig ()
		{
			Gtk.TreeIter iter;
			if (configCombo.GetActiveIter (out iter)) {
				return (string)configCombo.Model.GetValue (iter, configListStoreConfigNameColumn);
			}
			return null;
		}
		
		void OnConfigChanged (object s, EventArgs a)
		{
			if (loading)
				return;
			
			if (!ValidateChanges ()) {
				loading = true;
				configCombo.Active = lastConfigSelection;
				platformCombo.Active = lastPlatformSelection;
				loading = false;
				return;
			}
			
			if (s == configCombo) {
				FillPlatforms ();
				SelectPlatform (dialog.CurrentPlatform);
			}

			SaveConfigurations ();
			UpdateCurrentConfiguration ();
		}

		void SaveConfigurations ()
		{
			if (widgetCreated && currentConfigs.Count > 0)
				ApplyChanges ();
		}

		void UpdateCurrentConfiguration ()
		{
			lastConfigSelection = configCombo.Active;
			lastPlatformSelection = platformCombo.Active;

			currentConfigs.Clear ();
			
			string configName = dialog.CurrentConfig = GetSelectedComboConfig ();
			if (configName == GettextCatalog.GetString ("All Configurations"))
				configName = null;
			
			string platform = GetPlatformId (dialog.CurrentPlatform = platformCombo.ActiveText);
			
			foreach (ItemConfiguration config in FilterConfigurations (dialog.ConfigurationData.Configurations)) {
				if ((configName == null || config.Name == configName) && config.Platform == platform)
					currentConfigs.Add (config);
			}
			
			if (widgetCreated && currentConfigs.Count > 0) {
				panelWidget.Sensitive = true;
				LoadConfigData ();
			} else if (widgetCreated)
				panelWidget.Sensitive = false;
		}
		
		void OnConfigurationsChanged (object s, EventArgs a)
		{
			if (!widgetCreated)
				return;

			lastConfigSelection = -1;
			SaveConfigurations ();
			FillConfigurations ();
			UpdateSelection ();
		}
		
		void UpdateSelection ()
		{
			if (allowMixedConfigurations && ConfigurationsAreEqual (FilterConfigurations (dialog.ConfigurationData.Configurations))) {
				configCombo.Active = 0;
			} else {
				SelectConfiguration (dialog.CurrentConfig);
			}

			if (lastConfigSelection != configCombo.Active)
				FillPlatforms ();
			
			SelectPlatform (dialog.CurrentPlatform);
			UpdateCurrentConfiguration ();
		}

		void OnPageHidden (object sender, EventArgs e)
		{
			SaveConfigurations ();
		}
		
		void OnPageShown (object s, EventArgs a)
		{
			UpdateSelection ();
		}
		
		void SelectConfiguration (string config)
		{
			loading = true;
			
			Gtk.TreeIter it;
			if (configCombo.Model.GetIterFirst (out it)) {
				do {
					if (config == (string) configCombo.Model.GetValue (it, configListStoreConfigNameColumn)) {
						configCombo.SetActiveIter (it);
						break;
					}
				}
				while (configCombo.Model.IterNext (ref it));
			}
			
			if (configCombo.Active == -1)
				configCombo.Active = 0;
			
			loading = false;
		}
		
		void SelectPlatform (string platform)
		{
			loading = true;
			
			Gtk.TreeIter it;
			if (platformCombo.Model.GetIterFirst (out it)) {
				do {
					if (platform == (string) platformCombo.Model.GetValue (it, 0)) {
						platformCombo.SetActiveIter (it);
						break;
					}
				}
				while (platformCombo.Model.IterNext (ref it));
			}
			
			if (platformCombo.Active == -1)
				platformCombo.Active = 0;
			
			loading = false;
		}
		
		void IOptionsPanel.ApplyChanges ()
		{
			if (currentConfigs.Count > 0)
				ApplyChanges ();
		}
		
		public abstract void LoadConfigData ();
		
		internal static string GetPlatformName (string id)
		{
			if (string.IsNullOrEmpty (id))
				return GettextCatalog.GetString ("Any CPU");
			else
				return id;
		}
		
		internal static string GetPlatformId (string name)
		{
			if (name == GettextCatalog.GetString ("Any CPU"))
				return string.Empty;
			else
				return name;
		}
	}
}
