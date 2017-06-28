//
// GtkCrossPlatformLibraryProjectTemplateWizardPageWidget.cs
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
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Packaging.Templating;

namespace MonoDevelop.Packaging.Gui
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class GtkCrossPlatformLibraryProjectTemplateWizardPageWidget : Gtk.Bin
	{
		CrossPlatformLibraryTemplateWizardPage wizardPage;
		Color backgroundColor;
		EventBoxTooltip nameTooltip;
		ImageView backgroundImageView;
		Xwt.Drawing.Image backgroundImage;

		public GtkCrossPlatformLibraryProjectTemplateWizardPageWidget ()
		{
			this.Build ();

			// Do not use a width request for the configuration box so the left hand side of the
			// wizard page can expand to fit its contents. Also specify a width for the name
			// text box to use more of the left hand side of the wizard.
			configurationVBox.WidthRequest = -1;
			nameTextBox.WidthRequest = 280;

			backgroundImage = Xwt.Drawing.Image.FromResource ("preview-multiplatform-library.png");
			backgroundImageView = new ImageView (backgroundImage);
			backgroundImageView.Xalign = 1.0f;
			backgroundImageView.Yalign = 0.5f;
			backgroundLargeImageVBox.PackStart (backgroundImageView, true, true, 0);

			var separatorColor = Styles.NewProjectDialog.ProjectConfigurationSeparatorColor.ToGdkColor ();
			targetPlatformsSeparator.ModifyBg (StateType.Normal, separatorColor);
			sharedCodeSeparator.ModifyBg (StateType.Normal, separatorColor);

			backgroundColor = Styles.NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor.ToGdkColor ();
			leftBorderEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTopEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTableEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationBottomEventBox.ModifyBg (StateType.Normal, backgroundColor);
			backgroundLargeImageEventBox.ModifyBg (StateType.Normal, backgroundColor);
		}

		internal GtkCrossPlatformLibraryProjectTemplateWizardPageWidget (CrossPlatformLibraryTemplateWizardPage wizardPage)
			: this ()
		{
			this.wizardPage = wizardPage;

			nameTextBox.TextInserted += NameTextInserted;
			nameTextBox.Changed += NameTextChanged;

			descriptionTextBox.Text = wizardPage.Description;
			descriptionTextBox.Changed += DescriptionTextChanged;

			nameTextBox.ActivatesDefault = true;
			descriptionTextBox.ActivatesDefault = true;

			nameTextBox.TruncateMultiline = true;
			descriptionTextBox.TruncateMultiline = true;

			androidCheckButton.Active = wizardPage.IsAndroidChecked;
			androidCheckButton.Sensitive = wizardPage.IsAndroidEnabled;
			androidCheckButton.Toggled += AndroidCheckButtonToggled;

			iOSCheckButton.Active = wizardPage.IsIOSChecked;
			iOSCheckButton.Sensitive = wizardPage.IsIOSEnabled;
			iOSCheckButton.Toggled += IOSCheckButtonToggled;

			portableClassLibraryRadioButton.Active = wizardPage.IsPortableClassLibrarySelected;
			portableClassLibraryRadioButton.Toggled += PortableClassLibraryRadioButtonToggled;

			targetPlatformsVBox.Sensitive = !wizardPage.IsPortableClassLibrarySelected;

			sharedProjectRadioButton.Active = wizardPage.IsSharedProjectSelected;
			sharedProjectRadioButton.Toggled += SharedProjectRadioButtonToggled;
		}

		protected override void OnFocusGrabbed ()
		{
			nameTextBox.GrabFocus ();
		}

		void NameTextInserted (object o, TextInsertedArgs args)
		{
			if (args.Text.IndexOf ('\r') >= 0) {
				var textBox = (Entry)o;
				textBox.Text = textBox.Text.Replace ("\r", string.Empty);
			}
		}

		void NameTextChanged (object sender, EventArgs e)
		{
			// Use name as description by default.
			if (wizardPage.LibraryName == wizardPage.Description)
				descriptionTextBox.Text = nameTextBox.Text;

			wizardPage.LibraryName = nameTextBox.Text;

			if (wizardPage.HasLibraryNameError ()) {
				if (nameTooltip == null) {
					nameTooltip = ShowErrorTooltip (nameEventBox, wizardPage.LibraryNameError);
				}
			} else {
				if (nameTooltip != null) {
					HideTooltip (nameEventBox, nameTooltip);
					nameTooltip = null;
				}
			}
		}

		void DescriptionTextChanged (object sender, EventArgs e)
		{
			wizardPage.Description = descriptionTextBox.Text;
		}

		void AndroidCheckButtonToggled (object sender, EventArgs e)
		{
			wizardPage.IsAndroidChecked = androidCheckButton.Active;
		}

		void IOSCheckButtonToggled (object sender, EventArgs e)
		{
			wizardPage.IsIOSChecked = iOSCheckButton.Active;
		}

		void PortableClassLibraryRadioButtonToggled (object sender, EventArgs e)
		{
			wizardPage.IsPortableClassLibrarySelected = portableClassLibraryRadioButton.Active;
			targetPlatformsVBox.Sensitive = !wizardPage.IsPortableClassLibrarySelected;
		}

		void SharedProjectRadioButtonToggled (object sender, EventArgs e)
		{
			wizardPage.IsSharedProjectSelected = sharedProjectRadioButton.Active;
		}

		public override void Dispose ()
		{
			Dispose (nameTooltip);
			Dispose (backgroundImage);
		}

		void Dispose (IDisposable disposable)
		{
			if (disposable != null) {
				disposable.Dispose ();
			}
		}

		EventBoxTooltip ShowErrorTooltip (EventBox eventBox, string tooltipText)
		{
			eventBox.ModifyBg (StateType.Normal, backgroundColor);
			Xwt.Drawing.Image image = ImageService.GetIcon ("md-error", IconSize.Menu);

			eventBox.Add (new ImageView (image));
			eventBox.ShowAll ();

			return new EventBoxTooltip (eventBox) {
				ToolTip = tooltipText,
				Severity = TaskSeverity.Error
			};
		}

		void HideTooltip (EventBox eventBox, EventBoxTooltip tooltip)
		{
			Dispose (tooltip);
			eventBox.Foreach (eventBox.Remove);
		}
	}
}
