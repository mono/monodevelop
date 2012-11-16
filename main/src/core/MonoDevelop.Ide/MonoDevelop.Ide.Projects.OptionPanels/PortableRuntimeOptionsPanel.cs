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
		static TargetFramework NetPortableProfile1;
		static TargetFramework NetPortableProfile2;
		static TargetFramework NetPortableProfile3;
		static TargetFramework NetPortableProfile4;

		Dictionary<CheckButton, List<TargetFramework>> checkboxes = new Dictionary<CheckButton, List<TargetFramework>> ();
		Dictionary<CheckButton, ComboBox> comboboxes = new Dictionary<CheckButton, ComboBox> ();
		PortableDotNetProject project;
		TargetFramework target;

		static void InitProfiles ()
		{
			// Profile 1 (.NETFramework + Silverlight + WindowsPhone + Xbox)
			NetPortableProfile1 = Runtime.SystemAssemblyService.GetTargetFramework (new TargetFrameworkMoniker (".NETPortable", "4.0", "Profile1"));
			SupportedFramework NetFramework = new SupportedFramework (NetPortableProfile1, ".NETFramework", ".NET Framework", "*", new Version (4, 0), "4");
			SupportedFramework Silverlight = new SupportedFramework (NetPortableProfile1, "Silverlight", "Silverlight", "", new Version (4, 0), "4");
			SupportedFramework WindowsPhone = new SupportedFramework (NetPortableProfile1, "Silverlight", "Windows Phone", "WindowsPhone*", new Version (4, 0), "7");
			SupportedFramework Xbox = new SupportedFramework (NetPortableProfile1, "Xbox", "Xbox 360", "*", new Version (4, 0), "");
			
			NetPortableProfile1.SupportedFrameworks.Add (NetFramework);
			NetPortableProfile1.SupportedFrameworks.Add (Silverlight);
			NetPortableProfile1.SupportedFrameworks.Add (WindowsPhone);
			NetPortableProfile1.SupportedFrameworks.Add (Xbox);

			// Profile 2 (.NETFramework + Silverlight + WindowsPhone)
			NetPortableProfile2 = Runtime.SystemAssemblyService.GetTargetFramework (new TargetFrameworkMoniker (".NETPortable", "4.0", "Profile2"));
			NetFramework = new SupportedFramework (NetPortableProfile2, ".NETFramework", ".NET Framework", "*", new Version (4, 0), "4");
			Silverlight = new SupportedFramework (NetPortableProfile2, "Silverlight", "Silverlight", "", new Version (4, 0), "4");
			WindowsPhone = new SupportedFramework (NetPortableProfile2, "Silverlight", "Windows Phone", "WindowsPhone*", new Version (4, 0), "7");
			
			NetPortableProfile2.SupportedFrameworks.Add (NetFramework);
			NetPortableProfile2.SupportedFrameworks.Add (Silverlight);
			NetPortableProfile2.SupportedFrameworks.Add (WindowsPhone);

			// Profile 3 (.NETFramework + Silverlight)
			NetPortableProfile3 = Runtime.SystemAssemblyService.GetTargetFramework (new TargetFrameworkMoniker (".NETPortable", "4.0", "Profile3"));
			NetFramework = new SupportedFramework (NetPortableProfile3, ".NETFramework", ".NET Framework", "*", new Version (4, 0), "4");
			Silverlight = new SupportedFramework (NetPortableProfile3, "Silverlight", "Silverlight", "", new Version (4, 0), "4");
			
			NetPortableProfile3.SupportedFrameworks.Add (NetFramework);
			NetPortableProfile3.SupportedFrameworks.Add (Silverlight);

			// Profile 4 (Silverlight + WindowsPhone)
			NetPortableProfile4 = Runtime.SystemAssemblyService.GetTargetFramework (new TargetFrameworkMoniker (".NETPortable", "4.0", "Profile4"));
			Silverlight = new SupportedFramework (NetPortableProfile4, "Silverlight", "Silverlight", "", new Version (4, 0), "4");
			WindowsPhone = new SupportedFramework (NetPortableProfile4, "Silverlight", "Windows Phone", "WindowsPhone*", new Version (4, 0), "7");

			NetPortableProfile4.SupportedFrameworks.Add (Silverlight);
			NetPortableProfile4.SupportedFrameworks.Add (WindowsPhone);
		}
		
		public PortableRuntimeOptionsPanelWidget (PortableDotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			this.target = project.TargetFramework;
			this.project = project;
			this.Build ();

			// Aggregate all SupportedFrameworks from .NETPortable TargetFrameworks
			SortedDictionary<string, List<SupportedFramework>> frameworks = new SortedDictionary<string, List<SupportedFramework>> ();
			foreach (var fx in GetPortableTargetFrameworks ()) {
				foreach (var sfx in fx.SupportedFrameworks) {
					List<SupportedFramework> list;
					
					if (!frameworks.TryGetValue (sfx.DisplayName, out list)) {
						list = new List<SupportedFramework> ();
						frameworks.Add (sfx.DisplayName, list);
					}
					
					list.Add (sfx);
				}
			}

			// Now create a list of config options from our supported frameworks
			var options = new List<SortedDictionary<string, List<TargetFramework>>> ();
			foreach (var fx in frameworks) {
				var dict = new SortedDictionary<string, List<TargetFramework>> ();
				List<SupportedFramework> versions = fx.Value;
				List<TargetFramework> targets;
				string label;

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

				options.Add (dict);
			}

			// Add multi-option combo boxes first
			foreach (var opt in options) {
				if (opt.Count > 1)
					AddMultiOptionCombo (opt);
			}

			// Now add the single-option check boxes
			foreach (var opt in options) {
				if (opt.Count == 1) {
					var kvp = opt.FirstOrDefault ();

					AddSingleOptionCheckbox (kvp.Key, kvp.Value);
				}
			}
		}

		IEnumerable<TargetFramework> GetPortableTargetFrameworks ()
		{
			int count = 0;

			foreach (var fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				if (fx.Hidden || fx.Id.Identifier != ".NETPortable" || !project.TargetRuntime.IsInstalled (fx))
					continue;

				yield return fx;
				count++;
			}

			if (count > 0)
				yield break;

			if (NetPortableProfile1 == null)
				InitProfiles ();

			yield return NetPortableProfile1;
			yield return NetPortableProfile2;
			yield return NetPortableProfile3;
			yield return NetPortableProfile4;
		}

		void AddMultiOptionCombo (SortedDictionary<string, List<TargetFramework>> options)
		{
			var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) { LeftPadding = 18 };
			var model = new ListStore (new Type[] { typeof (string), typeof (object) });
			var renderer = new CellRendererText ();
			var combo = new ComboBox (model);
			var check = new CheckButton ();
			List<TargetFramework> targets;
			var hbox = new HBox ();
			int current = 0;
			int active = -1;
			string label;

			foreach (var kvp in options) {
				label = kvp.Key;

				if (current + 1 < options.Count)
					label += " or later";

				targets = kvp.Value;
				if (active == -1) {
					foreach (var target in targets) {
						if (target.Id.Equals (project.TargetFramework.Id)) {
							active = current;
							break;
						}
					}
				}

				model.AppendValues (label, targets);
				current++;
			}

			combo.PackStart (renderer, true);
			combo.AddAttribute (renderer, "text", 0);

			check.Show ();
			combo.Show ();

			if (active != -1) {
				combo.Active = active;
				check.Active = true;
			} else {
				check.Active = false;
				combo.Active = 0;
			}

			combo.Changed += (sender, e) => {
				if (check.Active)
					TargetFrameworkChanged (check, combo);
			};
			check.Toggled += (sender, e) => {
				TargetFrameworkChanged (check, combo);
			};

			comboboxes.Add (check, combo);

			hbox.PackStart (check, false, false, 0);
			hbox.PackStart (combo, false, true, 0);
			hbox.Show ();

			alignment.Add (hbox);
			alignment.Show ();

			vbox1.PackStart (alignment, false, false, 0);
		}

		void AddSingleOptionCheckbox (string label, List<TargetFramework> targetFrameworks)
		{
			var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) { LeftPadding = 18 };
			var check = new CheckButton (label);

			foreach (var fx in targetFrameworks) {
				if (fx.Id.Equals (project.TargetFramework.Id)) {
					check.Active = true;
					break;
				}
			}

			check.Toggled += (sender, e) => {
				TargetFrameworkChanged (check, targetFrameworks);
			};

			checkboxes.Add (check, targetFrameworks);

			check.Show ();
			alignment.Add (check);
			alignment.Show ();

			vbox1.PackStart (alignment, false, false, 0);
		}

		List<TargetFramework> GetTargetFrameworks (ComboBox combo)
		{
			TreeIter iter;

			if (!combo.GetActiveIter (out iter))
				return new List<TargetFramework> ();

			return (List<TargetFramework>) combo.Model.GetValue (iter, 1);
		}

		TargetFramework GetTargetFramework (CheckButton checkbox, List<TargetFramework> initial)
		{
			var list = new List<TargetFramework> (initial);
			int nchecked = 0;

			foreach (var kvp in comboboxes) {
				var combo = kvp.Value;
				var check = kvp.Key;

				if (check.Active)
					nchecked++;

				if (!check.Active || check == checkbox)
					continue;

				var filtered = new List<TargetFramework> ();
				foreach (var target in GetTargetFrameworks (combo)) {
					if (list.Contains (target))
						filtered.Add (target);
				}
				list = filtered;
			}

			foreach (var kvp in checkboxes) {
				var targets = kvp.Value;
				var check = kvp.Key;

				if (check.Active)
					nchecked++;
				
				if (!check.Active || check == checkbox)
					continue;
				
				var filtered = new List<TargetFramework> ();
				foreach (var target in targets) {
					if (list.Contains (target))
						filtered.Add (target);
				}
				list = filtered;
			}

			// Choose the TargetFramework with the smallest subset of supported frameworks
			TargetFramework smallest = this.target;
			int min = Int32.MaxValue;

			foreach (var target in list) {
				if (target.SupportedFrameworks.Count < min) {
					min = target.SupportedFrameworks.Count;
					smallest = target;
				}
			}

			return smallest;
		}

		void TargetFrameworkChanged (CheckButton check, List<TargetFramework> targetFrameworks)
		{
			if (!check.Active)
				targetFrameworks = new List<TargetFramework> (GetPortableTargetFrameworks ());

			target = GetTargetFramework (check, targetFrameworks);
		}

		void TargetFrameworkChanged (CheckButton check, ComboBox combo)
		{
			List<TargetFramework> targetFrameworks;

			if (!check.Active)
				targetFrameworks = new List<TargetFramework> (GetPortableTargetFrameworks ());
			else
				targetFrameworks = GetTargetFrameworks (combo);

			target = GetTargetFramework (check, targetFrameworks);
		}
		
		public void Store ()
		{
			if (target != null && target != project.TargetFramework) {
				project.TargetFramework = target;
				IdeApp.ProjectOperations.Save (project);
			}
		}
	}
}
