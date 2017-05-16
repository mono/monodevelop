//
// GtkDotNetCoreProjectTemplateWizardPageWidget.cs
//
// Author:
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

using System;
using System.Linq;
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.DotNetCore.Templating;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DotNetCore.Gui
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class GtkDotNetCoreProjectTemplateWizardPageWidget : Gtk.Bin
	{
		DotNetCoreProjectTemplateWizardPage wizardPage;
		Color backgroundColor;
		ImageView backgroundImageView;
		Xwt.Drawing.Image backgroundImage;

		public GtkDotNetCoreProjectTemplateWizardPageWidget ()
		{
			this.Build ();

			backgroundImage = Xwt.Drawing.Image.FromResource ("preview-netcore.png");
			backgroundImageView = new ImageView (backgroundImage);
			backgroundImageView.Xalign = 1.0f;
			backgroundImageView.Yalign = 0.5f;
			backgroundLargeImageVBox.PackStart (backgroundImageView, true, true, 0);

			backgroundColor = Styles.NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor.ToGdkColor ();
			leftBorderEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTopEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTableEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationBottomEventBox.ModifyBg (StateType.Normal, backgroundColor);
			backgroundLargeImageEventBox.ModifyBg (StateType.Normal, backgroundColor);
		}

		internal GtkDotNetCoreProjectTemplateWizardPageWidget (DotNetCoreProjectTemplateWizardPage wizardPage)
			: this ()
		{
			this.wizardPage = wizardPage;
			PopulateTargetFrameworks ();
			targetFrameworkComboBox.Changed += TargetFrameworkComboBoxChanged;
		}

		void PopulateTargetFrameworks ()
		{
			foreach (TargetFramework framework in wizardPage.TargetFrameworks) {
				targetFrameworkComboBox.AppendText (framework.GetDisplayName ());
			}

			targetFrameworkComboBox.Active = wizardPage.SelectedTargetFrameworkIndex;
		}

		void TargetFrameworkComboBoxChanged (object sender, EventArgs e)
		{
			wizardPage.SelectedTargetFrameworkIndex = targetFrameworkComboBox.Active;
		}
	}
}
