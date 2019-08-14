//
// NewConfigurationDialog.cs
//
// Author:
//       Cody Russell <coruss@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Xwt;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	class NewConfigurationDialog : Xwt.Dialog
	{
		public string ConfigName {
			get {
				string plat = MultiConfigItemOptionsPanel.GetPlatformId (comboPlatform.TextEntry.Text.Trim ());
				if (string.IsNullOrEmpty (plat))
					return comboName.TextEntry.Text.Trim ();
				else
					return comboName.TextEntry.Text.Trim () + "|" + plat;
			}
			set {
				int i = value.LastIndexOf ('|');
				if (i == -1) {
					comboName.TextEntry.Text = value;
					comboPlatform.TextEntry.Text = string.Empty;
				} else {
					comboName.TextEntry.Text = value.Substring (0, i);
					comboPlatform.TextEntry.Text = MultiConfigItemOptionsPanel.GetPlatformName (value.Substring (i + 1));
				}
			}
		}

		public bool CreateChildren {
			get => createChildrenCheck.Active;
		}

		public NewConfigurationDialog (ItemConfigurationCollection<ItemConfiguration> configurations)
			: this (null, configurations)
		{
		}

		public NewConfigurationDialog (IConfigurationTarget item, ItemConfigurationCollection<ItemConfiguration> configurations)
		{
			Build ();

			this.configurations = configurations;

			SetupConfigs (item);

			comboName.SetFocus ();
		}

		void Build ()
		{
			Padding = 6;
			Resizable = false;
			Title = GettextCatalog.GetString ("New Configuration");

			var mainVBox = new VBox () { Spacing = 6 };
			var table = new Table { DefaultColumnSpacing = 6, DefaultRowSpacing = 6 };

			var label = new Label { Text = GettextCatalog.GetString ("Name:") };
			comboName = new ComboBoxEntry ();
			comboName.TextEntry.Changed += ComboTextChanged;
			comboName.Accessible.LabelWidget = label;
			table.Add (label, 0, 0);
			table.Add (comboName, 1, 0);

			label = new Label { Text = GettextCatalog.GetString ("Platform:") };
			comboPlatform = new ComboBoxEntry ();
			comboPlatform.TextEntry.Changed += ComboTextChanged;
			comboPlatform.Accessible.LabelWidget = label;
			comboPlatform.WidthRequest = 250;
			table.Add (label, 0, 1);
			table.Add (comboPlatform, 1, 1);

			popover = new InformationPopoverWidget ();
			popover.Visible = false;
			table.Add (popover, 2, 0);

			createChildrenCheck = new CheckBox { Label = GettextCatalog.GetString ("Create configurations for all solution items") };

			mainVBox.PackStart (table, true, true);
			mainVBox.PackStart (createChildrenCheck, true);

			var cancelButton = new DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);

			okButton = new DialogButton (Command.Ok);
			Buttons.Add (okButton);

			DefaultCommand = okButton.Command;

			Content = mainVBox;

			ValidateText ();
		}

		void ComboTextChanged (object sender, EventArgs e)
		{
			ValidateText ();
		}

		void ValidateText ()
		{
			var name = comboName.TextEntry.Text.Trim ();
			var isOk = false;

			if (name.Length == 0 || name.IndexOf ('|') != -1) {
				isOk = false;
				popover.Message = GettextCatalog.GetString ("Please enter a valid configuration name.");
				popover.Severity = Tasks.TaskSeverity.Warning;
				popover.Show ();
			} else if (configurations[ConfigName] != null) {
				isOk = false;
				popover.Message = GettextCatalog.GetString ("A configuration with the name '{0}' already exists.", ConfigName);
				popover.Severity = Tasks.TaskSeverity.Warning;
				popover.Show ();
			} else {
				isOk = true;
				popover.Hide ();
			}

			okButton.Sensitive = isOk;
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);

			if (disposing) {
				comboName.TextEntry.Changed -= ComboTextChanged;
				comboPlatform.TextEntry.Changed -= ComboTextChanged;
			}
		}

		void SetupConfigs (IConfigurationTarget item)
		{
			var configs = new HashSet<string> ();
			var platforms = new HashSet<string> ();

			foreach (var conf in configurations) {
				if (configs.Add (conf.Name))
					comboName.Items.Add (conf.Name);
				var plat = MultiConfigItemOptionsPanel.GetPlatformName (conf.Platform);
				if (platforms.Add (plat))
					comboPlatform.Items.Add (plat);
			}

			comboPlatform.TextEntry.Text = MultiConfigItemOptionsPanel.GetPlatformName ("");
			if (!(item is Solution)) {
				createChildrenCheck.Active = false;
				createChildrenCheck.Visible = false;
			}
		}

		ItemConfigurationCollection<ItemConfiguration> configurations;
		ComboBoxEntry comboName;
		ComboBoxEntry comboPlatform;
		CheckBox createChildrenCheck;
		DialogButton okButton;
		InformationPopoverWidget popover;
	}
}
