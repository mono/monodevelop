//
// DotNetCoreRuntimeOptionsPanelWidget.cs
//
// Author:
//       Lluis Sanchez Gual
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.Gui
{
	partial class DotNetCoreRuntimeOptionsPanelWidget
	{
		readonly List<TargetFramework> frameworks;
		readonly DotNetProject project;
		readonly DotNetCoreProjectExtension dotNetCoreProject;

		public DotNetCoreRuntimeOptionsPanelWidget (DotNetProject project)
		{
			Build ();

			this.project = project;
			if (project == null) {
				Sensitive = false;
				return;
			}

			dotNetCoreProject = project.GetFlavor<DotNetCoreProjectExtension> ();
			var supportedTargetFrameworks = new DotNetCoreProjectSupportedTargetFrameworks (project);
			frameworks = supportedTargetFrameworks.GetFrameworks ().ToList ();

			bool notInstalled = false;
			if (!frameworks.Any (fx => fx.Id == project.TargetFramework.Id)) {
				frameworks.Add (project.TargetFramework);
				notInstalled = true;
			}

			//sort by id ascending, version descending
			frameworks.Sort ((x, y) => {
				var cmp = string.CompareOrdinal (x.Id.Identifier, y.Id.Identifier);
				if (cmp != 0)
					return cmp;
				return string.CompareOrdinal (y.Id.Version, x.Id.Version);
			});

			for (int i = 0; i < frameworks.Count; i++) {
				var fx = frameworks[i];
				if (project.TargetFramework.Id == fx.Id) {
					if (notInstalled)
						runtimeVersionCombo.AppendText (GettextCatalog.GetString ("{0} (Not installed)", fx.GetDisplayName ()));
					else
						runtimeVersionCombo.AppendText (fx.GetDisplayName ());
					runtimeVersionCombo.Active = i;
				} else {
					runtimeVersionCombo.AppendText (fx.GetDisplayName ());
				}
			}

			Sensitive = frameworks.Count > 1;
		}

		public void Store ()
		{
			if (project == null || runtimeVersionCombo.Active == -1)
				return;

			TargetFramework framework = frameworks [runtimeVersionCombo.Active];

			if (framework != project.TargetFramework) {
				project.TargetFramework = frameworks [runtimeVersionCombo.Active];
			}
		}
	}
}
