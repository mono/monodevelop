//
// GtkAspNetProjectTemplateWizardPageWidget.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using Gdk;
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace MonoDevelop.AspNet.Projects
{
	[System.ComponentModel.ToolboxItem (true)]
	partial class GtkAspNetProjectTemplateWizardPageWidget : Gtk.Bin
	{
		AspNetProjectTemplateWizardPage wizardPage;
		ImageView backgroundImageView;
		Xwt.Drawing.Image backgroundImage;

		public GtkAspNetProjectTemplateWizardPageWidget ()
		{
			this.Build ();

			if (Platform.IsMac) {
				int labelPaddingHeight = 5;
				if (IsYosemiteOrHigher ())
					labelPaddingHeight--;
				includeLabelPadding.HeightRequest = labelPaddingHeight;
				testingLabelPadding.HeightRequest = labelPaddingHeight;

				int leftPaddingWidth = 28;
				mvcDescriptionLeftHandPadding.WidthRequest = leftPaddingWidth;
				webFormsDescriptionLeftHandPadding.WidthRequest = leftPaddingWidth;
				webApiDescriptionLeftHandPadding.WidthRequest = leftPaddingWidth;
				includeUnitTestProjectDescriptionLeftHandPadding.WidthRequest = leftPaddingWidth;
			}

			// Do not use a width request for the configuration box so the left hand side of the
			// wizard page can expand to fit its contents.
			configurationVBox.WidthRequest = -1;

			backgroundImage = Xwt.Drawing.Image.FromResource ("aspnet-wizard-page.png");
			backgroundImageView = new ImageView (backgroundImage);
			backgroundImageView.Xalign = 1.0f;
			backgroundImageView.Yalign = 1.0f;
			backgroundImageView.SizeRequested += BackgroundImageViewSizeRequested;
			backgroundLargeImageVBox.PackStart (backgroundImageView, true, true, 0);

			var separatorColor = Ide.Gui.Styles.NewProjectDialog.ProjectConfigurationSeparatorColor.ToGdkColor ();
			var backgroundColor = Ide.Gui.Styles.NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor.ToGdkColor ();
			testingSeparator.ModifyBg (StateType.Normal, separatorColor);

			leftBorderEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTopEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTableEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationBottomEventBox.ModifyBg (StateType.Normal, backgroundColor);
			backgroundLargeImageEventBox.ModifyBg (StateType.Normal, backgroundColor);
		}

		public GtkAspNetProjectTemplateWizardPageWidget (AspNetProjectTemplateWizardPage wizardPage)
			: this ()
		{
			WizardPage = wizardPage;
		}

		public override void Dispose ()
		{
			backgroundImageView.SizeRequested -= BackgroundImageViewSizeRequested;
			Dispose (backgroundImage);
			base.Dispose ();
		}

		void Dispose (IDisposable disposable)
		{
			if (disposable != null) {
				disposable.Dispose ();
			}
		}

		public AspNetProjectTemplateWizardPage WizardPage {
			get { return wizardPage; }
			set {
				wizardPage = value;
				LoadWizardPageInfo ();
			}
		}

		void LoadWizardPageInfo ()
		{
			if (wizardPage.AspNetMvcMutable || wizardPage.AspNetMvcEnabled) {
				includeMvcCheck.Active = wizardPage.AspNetMvcEnabled;
				includeMvcCheck.Toggled += (sender, e) => {
					wizardPage.AspNetMvcEnabled = includeMvcCheck.Active;
				};
				mvcVBox.Sensitive = wizardPage.AspNetMvcMutable;
			}

			if (wizardPage.AspNetWebFormsMutable || wizardPage.AspNetWebFormsEnabled) {
				includeWebFormsCheck.Active = wizardPage.AspNetWebFormsEnabled;
				includeWebFormsCheck.Toggled += (sender, e) => {
					wizardPage.AspNetWebFormsEnabled = includeWebFormsCheck.Active;
				};
				webFormsVBox.Sensitive = wizardPage.AspNetWebFormsMutable;
			} else {
				RemoveFromTable (webFormsVBox);
			}

			if (wizardPage.AspNetWebApiMutable || wizardPage.AspNetWebApiEnabled) {
				includeWebApiCheck.Active = wizardPage.AspNetWebApiEnabled;
				includeWebApiCheck.Toggled += (sender, e) => {
					wizardPage.AspNetWebApiEnabled = includeWebApiCheck.Active;
				};
				webApiVBox.Sensitive = wizardPage.AspNetWebApiMutable;
			}

			includeTestProjectCheck.Toggled += (sender, e) => {
				wizardPage.IncludeTestProject = includeTestProjectCheck.Active;
			};
		}

		void RemoveFromTable (Widget widget)
		{
			configurationTable.Remove (widget);
			widget.Destroy ();
		}

		bool IsYosemiteOrHigher ()
		{
			return Platform.OSVersion >= MacSystemInformation.Yosemite;
		}

		void BackgroundImageViewSizeRequested (object o, SizeRequestedArgs args)
		{
			if (args.Requisition.Width <= backgroundImage.Width) {
				// Ensure there is a gap between the image and the left hand side of the wizard
				// if the labels have expanded to fit their text.
				args.Requisition = new Requisition {
					Height = args.Requisition.Height,
					Width = args.Requisition.Width + 30
				};
			}
		}
	}
}

