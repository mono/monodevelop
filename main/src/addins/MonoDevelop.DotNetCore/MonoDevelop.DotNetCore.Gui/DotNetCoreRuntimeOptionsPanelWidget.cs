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
		readonly List<TargetFramework> knownFrameworks;
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

			if (project.HasMultipleTargetFrameworks) {
				runtimeVersionCombo.AppendText (GettextCatalog.GetString ("(Multiple Frameworks)"));
				runtimeVersionCombo.Active = 0;
				Sensitive = false;
			} else {
				dotNetCoreProject = project.GetFlavor<DotNetCoreProjectExtension> ();
				var supportedTargetFrameworks = new DotNetCoreProjectSupportedTargetFrameworks (project);
				var installedFrameworks = supportedTargetFrameworks.GetFrameworks ().ToList ();
				knownFrameworks = supportedTargetFrameworks.GetKnownFrameworks ()
					.Concat (installedFrameworks)
					.Distinct ()
					.ToList ();

				if (!knownFrameworks.Any (fx => fx.Id == project.TargetFramework.Id)) {
					knownFrameworks.Add (project.TargetFramework);
				}

				//sort by id ascending, version descending
				knownFrameworks.Sort ((x, y) => {
					var cmp = string.CompareOrdinal (x.Id.Identifier, y.Id.Identifier);
					if (cmp != 0)
						return cmp;
					return string.CompareOrdinal (y.Id.Version, x.Id.Version);
				});

				for (int i = 0; i < knownFrameworks.Count; i++) {
					var fx = knownFrameworks[i];
					if (installedFrameworks.Any (f => f.Id == fx.Id)) {
						runtimeVersionCombo.AppendText (fx.GetDisplayName ());
					} else {
						runtimeVersionCombo.AppendText (GettextCatalog.GetString ("{0} (Not installed)", fx.GetDisplayName ()));
					}

					if (project.TargetFramework.Id == fx.Id) {
						runtimeVersionCombo.Active = i;
					}
				}

				Sensitive = knownFrameworks.Count > 1;
			}
		}

		public void Store ()
		{
			if (project == null || runtimeVersionCombo.Active == -1 || project.HasMultipleTargetFrameworks)
				return;

			TargetFramework framework = knownFrameworks [runtimeVersionCombo.Active];

			if (framework != project.TargetFramework) {
				project.TargetFramework = knownFrameworks [runtimeVersionCombo.Active];
			}
		}
	}
}
