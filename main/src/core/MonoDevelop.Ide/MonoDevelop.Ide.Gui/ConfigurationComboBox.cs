//
// ConfigurationComboBox.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.ObjectModel;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui
{
	internal class ConfigurationComboBox: ToolbarComboBox
	{
		bool updating;
		
		public ConfigurationComboBox () 
		{
			Combo.Changed += new EventHandler (OnChanged);
			IdeApp.Workspace.ConfigurationsChanged += OnConfigurationsChanged;
			IdeApp.Workspace.ActiveConfigurationChanged += OnActiveConfigurationChanged;
			Reset ();
		}
		
		void Reset ()
		{
			((Gtk.ListStore)Combo.Model).Clear ();
			Combo.AppendText ("dummy");
			Combo.Active = -1;
			Combo.Sensitive = false;
		}
		
		void RefreshCombo ()
		{
			((Gtk.ListStore)Combo.Model).Clear ();
			int active = -1;
			int n=0;
			ReadOnlyCollection<string> configs = IdeApp.Workspace.GetConfigurations ();
			foreach (string conf in configs) {
				Combo.AppendText (conf);
				if (conf == IdeApp.Workspace.ActiveConfiguration)
					active = n;
				n++;
			}
			Combo.Sensitive = n > 0;
			if (active >= 0)
				Combo.Active = active;
			else if (configs.Count > 0)
				IdeApp.Workspace.ActiveConfiguration = configs [0];
			Combo.ShowAll ();
		}

		void OnConfigurationsChanged (object sender, EventArgs e)
		{
			RefreshCombo ();
		}
		
		void OnActiveConfigurationChanged (object sender, EventArgs e)
		{
			if (updating)
				return;

			string knownConfig = null;
			
			Gtk.TreeIter it;
			if (Combo.Model.GetIterFirst (out it)) {
				do {
					string cs = (string) Combo.Model.GetValue (it, 0);
					if (knownConfig == null)
						knownConfig = cs;
					if (IdeApp.Workspace.ActiveConfiguration == cs) {
						updating = true;
						Combo.SetActiveIter (it);
						updating = false;
						return;
					}
				}
				while (Combo.Model.IterNext (ref it));
			}

			// Configuration not found. Set one that is known.
			IdeApp.Workspace.ActiveConfiguration = knownConfig;
		}
		
		protected void OnChanged (object sender, EventArgs args)
		{
			if (updating)
				return;
			if (IdeApp.Workspace.IsOpen) {
				Gtk.TreeIter iter;
				if (Combo.GetActiveIter (out iter)) {
					string cs = (string) Combo.Model.GetValue (iter, 0);
					updating = true;
					IdeApp.Workspace.ActiveConfiguration = cs;
					updating = false;
				}
			}
		}
	}
}
