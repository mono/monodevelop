// NewConfigurationDialog.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects
{
	partial class NewConfigurationDialog : Gtk.Dialog
	{
		ItemConfigurationCollection<ItemConfiguration> configurations;

		public NewConfigurationDialog (ItemConfigurationCollection<ItemConfiguration> configurations): this (null, configurations)
		{
		}

		public NewConfigurationDialog (IConfigurationTarget item, ItemConfigurationCollection<ItemConfiguration> configurations)
		{
			this.Build();
			this.configurations = configurations;
			HashSet<string> configs = new HashSet<string> ();
			HashSet<string> platforms = new HashSet<string> ();
			foreach (ItemConfiguration conf in configurations) {
				if (configs.Add (conf.Name))
					comboName.AppendText (conf.Name);
				string plat = MultiConfigItemOptionsPanel.GetPlatformName (conf.Platform);
				if (platforms.Add (plat))
				    comboPlatform.AppendText (plat);
			}
			comboPlatform.Entry.Text = MultiConfigItemOptionsPanel.GetPlatformName ("");
			if (!(item is Solution)) {
				createChildrenCheck.Active = false;
				createChildrenCheck.Visible = false;
				DefaultHeight = 0;
			}

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			comboName.SetCommonAccessibilityAttributes ("NewConfiguration.Name", label1,
			                                            GettextCatalog.GetString ("Select or enter the name of the new configuration"));

			comboPlatform.SetCommonAccessibilityAttributes ("NewConfiguration.Platform",
			                                                label2,
			                                                GettextCatalog.GetString ("Select or enter the platform for the new configuration"));

			createChildrenCheck.SetCommonAccessibilityAttributes ("NewConfiguration.CreateCheck", "",
			                                                      GettextCatalog.GetString ("Check to create configurations for all the solution items"));
		}

		public string ConfigName {
			get {
				string plat = MultiConfigItemOptionsPanel.GetPlatformId (comboPlatform.Entry.Text.Trim ());
				if (string.IsNullOrEmpty (plat))
					return comboName.Entry.Text.Trim ();
				else
					return comboName.Entry.Text.Trim () + "|" + plat;
			}
			set {
				int i = value.LastIndexOf ('|');
				if (i == -1) {
					comboName.Entry.Text = value;
					comboPlatform.Entry.Text = string.Empty;
				} else {
					comboName.Entry.Text = value.Substring (0, i);
					comboPlatform.Entry.Text = MultiConfigItemOptionsPanel.GetPlatformName (value.Substring (i+1));
				}
			}
		}
		
		public bool CreateChildren {
			get { return createChildrenCheck.Active; }
		}
		
		protected virtual void OnOkbutton1Clicked (object sender, System.EventArgs e)
		{
			if (comboName.Entry.Text.Trim ().Length == 0 || comboName.Entry.Text.IndexOf ('|') != -1) {
				MessageService.ShowWarning (this, GettextCatalog.GetString ("Please enter a valid configuration name."));
			} else if (configurations [ConfigName] != null) {
				MessageService.ShowWarning (this, GettextCatalog.GetString ("A configuration with the name '{0}' already exists.", ConfigName));
			} else
				Respond (Gtk.ResponseType.Ok);
		}
	}
}
