// 
// PortableRuntimeOptionsPanel.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class PortableRuntimeOptionsPanel : ItemOptionsPanel
	{
		PortableRuntimeOptionsPanelWidget widget;
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is PortableDotNetProject;
		}
		
		public override Widget CreatePanelWidget ()
		{
			return (widget = new PortableRuntimeOptionsPanelWidget ((PortableDotNetProject) ConfiguredProject, ItemConfigurations));
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PortableRuntimeOptionsPanelWidget : Gtk.Bin
	{
		PortableDotNetProject project;
		
		public PortableRuntimeOptionsPanelWidget (PortableDotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			this.project = project;
			this.Build ();
			
			SortedDictionary<string, List<SupportedFramework>> options = new SortedDictionary<string, List<SupportedFramework>> ();
			
			foreach (var fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				if (fx.Hidden || fx.Id.Identifier != ".NETPortable" || !project.TargetRuntime.IsInstalled (fx))
					continue;
				
				foreach (var sfx in fx.SupportedFrameworks) {
					List<SupportedFramework> list;
					
					if (!options.TryGetValue (sfx.DisplayName, out list)) {
						list = new List<SupportedFramework> ();
						options.Add (sfx.DisplayName, list);
					}
					
					list.Add (sfx);
				}
			}

			if (options.Count == 0) {
				var fx = Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.PORTABLE_4_0);
				var net4 = new SupportedFramework (fx, ".NETFramework", ".NET Framework", "*", new Version (4, 0), "4");
				var sl4 = new SupportedFramework (fx, "Silverlight", "Silverlight", "", new Version (4, 0), "4");
				var wp7 = new SupportedFramework (fx, "Silverlight","Windows Phone", "WindowsPhone*", new Version (4, 0), "7");
				var xbox = new SupportedFramework (fx, "Xbox", "Xbox 360", "*", new Version (4, 0), "");

				fx.SupportedFrameworks.Add (net4);
				fx.SupportedFrameworks.Add (sl4);
				fx.SupportedFrameworks.Add (wp7);
				fx.SupportedFrameworks.Add (xbox);

				options.Add (net4.DisplayName, new List<SupportedFramework> () { net4 });
				options.Add (sl4.DisplayName, new List<SupportedFramework> () { sl4 });
				options.Add (wp7.DisplayName, new List<SupportedFramework> () { wp7 });
				options.Add (xbox.DisplayName, new List<SupportedFramework> () { xbox });
			}
			
			foreach (var opt in options) {
				var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) { LeftPadding = 18 };
				List<SupportedFramework> versions = opt.Value;
				List<TargetFramework> targets;
				CheckButton check;
				ComboBox combo;
				string label;

				var dict = new SortedDictionary<string, List<TargetFramework>> ();
				foreach (var sfx in versions) {
					if (!string.IsNullOrEmpty (sfx.MinimumVersionDisplayName))
						label = sfx.DisplayName + " " + sfx.MinimumVersionDisplayName;
					else
						label = sfx.DisplayName;

					if (!dict.TryGetValue (label, out targets)) {
						targets = new List<TargetFramework> ();
						dict.Add (label, targets);
					}

					targets.Add (sfx.TargetFramework);
				}

				if (dict.Count > 1) {
					var model = new ListStore (new Type[] { typeof (string), typeof (object) });
					int current = 1;

					foreach (var kvp in dict) {
						var display = kvp.Key;

						if (current < dict.Count)
							display += " or later";

						model.AppendValues (display, kvp.Value);
						current++;
					}

					var renderer = new CellRendererText ();

					combo = new ComboBox (model);
					combo.PackStart (renderer, true);
					combo.AddAttribute (renderer, "text", 0);
					combo.Active = 0; // FIXME: select the right one...
					combo.Changed += ComboChanged;
					combo.Show ();

					check = new CheckButton ();
					check.Toggled += CheckToggled;
					check.Active = true;
					check.Show ();

					var checkAlignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f);
					checkAlignment.Add (check);
					checkAlignment.Show ();

					var hbox = new HBox (false, 6);
					hbox.PackStart (checkAlignment, false, false, 0);
					hbox.PackStart (combo, false, true, 0);
					hbox.Show ();

					alignment.Add (hbox);
				} else {
					var kvp = dict.FirstOrDefault ();

					check = new CheckButton (kvp.Key);
					check.Toggled += CheckToggled;
					check.Active = true;
					check.Show ();

					alignment.Add (check);
				}

				alignment.Show ();
				
				vbox1.PackStart (alignment, false, false, 0);
			}
		}

		void CheckToggled (object sender, EventArgs e)
		{

		}

		void ComboChanged (object sender, EventArgs e)
		{

		}
		
		static int CompareFrameworksByVersion (SupportedFramework fx1, SupportedFramework fx2)
		{
			if (fx1.MinimumVersion < fx2.MinimumVersion)
				return -1;
			
			if (fx1.MinimumVersion > fx2.MinimumVersion)
				return 1;
			
			if (fx1.MaximumVersion < fx2.MaximumVersion)
				return -1;
			
			if (fx1.MaximumVersion > fx2.MaximumVersion)
				return 1;
			
			return 0;
		}
		
		public void Store ()
		{
			// no-op for now, until we figure out the logic for setting the target framework based on the checkboxes enabled...
		}
	}
}
