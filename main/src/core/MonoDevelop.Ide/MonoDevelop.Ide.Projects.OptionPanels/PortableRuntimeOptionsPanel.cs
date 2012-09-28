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
		public PortableRuntimeOptionsPanelWidget (PortableDotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			this.Build ();
			
			SortedDictionary<string, List<Framework>> options = new SortedDictionary<string, List<Framework>> ();
			
			foreach (var fx in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				if (fx.Hidden || fx.Id.Identifier != ".NETPortable" || !project.TargetRuntime.IsInstalled (fx))
					continue;
				
				foreach (var sfx in fx.SupportedFrameworks) {
					List<Framework> list;
					
					if (!options.TryGetValue (sfx.DisplayName, out list)) {
						list = new List<Framework> ();
						options.Add (sfx.DisplayName, list);
					}
					
					list.Add (sfx);
				}
			}
			
			foreach (var opt in options) {
				var alignment = new Alignment (0.0f, 0.5f, 1.0f, 1.0f) { LeftPadding = 18 };
				List<Framework> versions = opt.Value;
				CheckButton check;
				
				// FIXME: VS11 introduces comboboxes for some of these... which I suspect will need to sort based on version
				//versions.Sort (CompareFrameworksByVersion);
				check = new CheckButton (versions[0].DisplayName + " " + versions[0].MinimumVersionDisplayName);
				check.Sensitive = false; // Desensitize until we support changing these values...
				foreach (var ver in versions) {
					if (ver.TargetFramework == project.TargetFramework) {
						check.Active = true;
						break;
					}
				}
				check.Show ();
				
				alignment.Add (check);
				alignment.Show ();
				
				vbox1.PackStart (alignment, false, false, 0);
			}
		}
		
		static int CompareFrameworksByVersion (Framework fx1, Framework fx2)
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
