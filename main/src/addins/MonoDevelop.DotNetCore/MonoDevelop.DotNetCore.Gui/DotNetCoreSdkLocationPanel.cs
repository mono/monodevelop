//
// DotNetCoreSdkLocationPanel.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.Gui
{
	class DotNetCoreSdkLocationPanel : OptionsPanel
	{
		DotNetCoreSdkLocationWidget widget;

		public override Control CreatePanelWidget ()
		{
			widget = new DotNetCoreSdkLocationWidget (this);
			return widget.ToGtkWidget ();
		}

		public FilePath LoadSdkLocationSetting ()
		{
			return DotNetCoreRuntime.FileName;
		}

		public void SaveSdkLocationSetting (FilePath location)
		{
			if (location == DotNetCoreRuntime.FileName) {
				return;
			}

			var path = new DotNetCorePath (location);
			DotNetCoreSdkPaths sdkPaths = GetSdkPaths (path);

			DotNetCoreSdk.Update (sdkPaths);
			DotNetCoreRuntime.Update (path);

			// All open .NET Core projects need to be re-evaluated so the correct
			// SDK MSBuild imports are used.
			ReevaluateAllOpenDotNetCoreProjects ().Ignore ();
		}

		public DotNetCoreSdkPaths SdkPaths { get; private set; }
		public DotNetCorePath DotNetCorePath { get; private set; }
		public DotNetCoreVersion[] RuntimeVersions { get; private set; }

		public void ValidateSdkLocation (FilePath location)
		{
			if (!location.IsNullOrEmpty) {
				DotNetCorePath = new DotNetCorePath (location);
				SdkPaths = GetSdkPaths (DotNetCorePath);
				RuntimeVersions = DotNetCoreRuntimeVersions.GetInstalledVersions (DotNetCorePath.FileName).ToArray ();
			} else {
				DotNetCorePath = null;
				SdkPaths = new DotNetCoreSdkPaths ();
				RuntimeVersions = Array.Empty<DotNetCoreVersion> ();
			}
		}

		static DotNetCoreSdkPaths GetSdkPaths (DotNetCorePath path)
		{
			var sdkPaths = new DotNetCoreSdkPaths ();
			sdkPaths.FindMSBuildSDKsPath (path.FileName);
			return sdkPaths;
		}

		async Task ReevaluateAllOpenDotNetCoreProjects ()
		{
			if (!IdeApp.Workspace.IsOpen)
				return;

			var progressMonitor = new ProgressMonitor ();
			foreach (var project in IdeApp.Workspace.GetAllItems<DotNetProject> ()) {
				if (project.HasFlavor<DotNetCoreProjectExtension> ()) {
					await project.ReevaluateProject (progressMonitor);
				}
			}
		}

		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
	}
}
