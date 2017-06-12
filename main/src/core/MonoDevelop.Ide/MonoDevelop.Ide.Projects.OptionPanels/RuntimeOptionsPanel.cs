//
// RuntimeOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class RuntimeOptionsPanel : ItemOptionsPanel
	{
		RuntimeOptionsPanelWidget widget;

		public override Control CreatePanelWidget()
		{
			return (widget = new RuntimeOptionsPanelWidget ((DotNetProject)ConfiguredProject, ItemConfigurations));
		}
		
		public override void ApplyChanges()
		{
			widget.Store ();
		}
	}

	partial class RuntimeOptionsPanelWidget : Gtk.Bin 
	{
		readonly List<TargetFramework> frameworks;
		readonly DotNetProject project;

		public RuntimeOptionsPanelWidget (DotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			Build ();
			
			this.project = project;
			if (project == null) {
				Sensitive = false;
				return;
			}
			
			frameworks = Runtime.SystemAssemblyService.GetTargetFrameworks ()
				.Where (fx => project.TargetRuntime.IsInstalled (fx) && project.SupportsFramework (fx))
				.ToList ();
			
			bool notInstalled = false;
			
			if (!frameworks.Any (fx => fx.Id == project.TargetFramework.Id)) {
				notInstalled = true;
				frameworks.Add (project.TargetFramework);
			}

			//sort by id ascending, version descending, profile ascending
			frameworks.Sort ((x, y) => {
				var cmp = string.CompareOrdinal (x.Id.Identifier, y.Id.Identifier);
				if (cmp != 0)
					return cmp;
				cmp = string.CompareOrdinal (y.Id.Version, x.Id.Version);
				if (cmp != 0)
					return cmp;
				return string.CompareOrdinal (x.Id.Profile, y.Id.Profile);
			});
			
			for (int i = 0; i < frameworks.Count; i++) {
				var fx = frameworks[i];
				if (project.TargetFramework.Id == fx.Id) {
					string name = notInstalled? GettextCatalog.GetString ("{0} (Not installed)", fx.Name) : fx.Name;
					runtimeVersionCombo.AppendText (name);
					runtimeVersionCombo.Active = i;
				} else {
					runtimeVersionCombo.AppendText (fx.Name);
				}
			}
			
			Sensitive = frameworks.Count > 1;
		}

		public void Store ()
		{
			if (project == null || runtimeVersionCombo.Active == -1)
				return;
			project.TargetFramework = frameworks [runtimeVersionCombo.Active];
		}
	}
}
