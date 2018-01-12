//
// GtkProjectNuGetBuildOptionsPanelWidget.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;

namespace MonoDevelop.Packaging.Gui
{
	partial class GtkProjectNuGetBuildOptionsPanelWidget
	{
		bool projectHasMetadata;

		public GtkProjectNuGetBuildOptionsPanelWidget ()
		{
			Build ();
			UpdateMissingMetadataLabelVisibility ();
			packOnBuildButton.Toggled += PackOnBuildButtonToggled;
			GtkNuGetPackageMetadataOptionsPanelWidget.OnProjectHasMetadataChanged = OnProjectHasMetadataChanged;

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			packOnBuildButton.SetCommonAccessibilityAttributes ("NugetBuildOptionsPanel.PackOnBuild", "",
			                                                    GettextCatalog.GetString ("Check to create a NuGet package when building"));
		}

		public bool PackOnBuild {
			get { return packOnBuildButton.Active; }
			set {
				packOnBuildButton.Active = value;
				UpdateMissingMetadataLabelVisibility ();
			}
		}

		public bool ProjectHasMetadata {
			get { return projectHasMetadata; }
			set {
				projectHasMetadata = value;
				UpdateMissingMetadataLabelVisibility ();
			}
		}

		void PackOnBuildButtonToggled (object sender, EventArgs e)
		{
			UpdateMissingMetadataLabelVisibility ();
		}

		void UpdateMissingMetadataLabelVisibility ()
		{
			if (packOnBuildButton.Active) {
				missingMetadataLabel.Visible = !ProjectHasMetadata;
			} else {
				missingMetadataLabel.Visible = false;
			}
		}

		void OnProjectHasMetadataChanged (bool hasMetadata)
		{
			ProjectHasMetadata = hasMetadata;
		}

		protected override void OnDestroyed ()
		{
			GtkNuGetPackageMetadataOptionsPanelWidget.OnProjectHasMetadataChanged = null;
			base.OnDestroyed ();
		}
	}
}
