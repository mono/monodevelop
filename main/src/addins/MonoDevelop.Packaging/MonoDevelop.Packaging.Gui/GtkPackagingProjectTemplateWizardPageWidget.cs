//
// GtkPackagingProjectTemplateWizardPageWidget.cs
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
	public partial class GtkPackagingProjectTemplateWizardPageWidget : Gtk.Bin
	{
		PackagingProjectTemplateWizardPage wizardPage;
		Color backgroundColor;
		EventBoxTooltip idTooltip;
		ImageView backgroundImageView;
		Xwt.Drawing.Image backgroundImage;

		public GtkPackagingProjectTemplateWizardPageWidget ()
		{
			this.Build ();

			backgroundImage = Xwt.Drawing.Image.FromResource ("preview-nuget.png");
			backgroundImageView = new ImageView (backgroundImage);
			backgroundImageView.Xalign = 1.0f;
			backgroundImageView.Yalign = 0.5f;
			backgroundLargeImageVBox.PackStart (backgroundImageView, true, true, 0);

			var separatorColor = Styles.NewProjectDialog.ProjectConfigurationSeparatorColor.ToGdkColor ();
			separator.ModifyBg (StateType.Normal, separatorColor);

			backgroundColor = Styles.NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor.ToGdkColor ();
			leftBorderEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTopEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTableEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationBottomEventBox.ModifyBg (StateType.Normal, backgroundColor);
			backgroundLargeImageEventBox.ModifyBg (StateType.Normal, backgroundColor);
		}

		internal GtkPackagingProjectTemplateWizardPageWidget (PackagingProjectTemplateWizardPage wizardPage)
			: this ()
		{
			this.wizardPage = wizardPage;

			packageAuthorsTextBox.Text = wizardPage.Authors;

			packageIdTextBox.TextInserted += PackageIdTextInserted;
			packageIdTextBox.Changed += PackageIdTextBoxChanged;
			packageAuthorsTextBox.Changed += PackageAuthorsTextBoxChanged;
			packageDescriptionTextBox.Changed += PackageDescriptionTextChanged;

			packageIdTextBox.ActivatesDefault = true;
			packageAuthorsTextBox.ActivatesDefault = true;
			packageDescriptionTextBox.ActivatesDefault = true;

			packageIdTextBox.TruncateMultiline = true;
			packageAuthorsTextBox.TruncateMultiline = true;
			packageDescriptionTextBox.TruncateMultiline = true;
		}

		protected override void OnFocusGrabbed ()
		{
			packageIdTextBox.GrabFocus ();
		}

		void PackageIdTextInserted (object o, TextInsertedArgs args)
		{
			if (args.Text.IndexOf ('\r') >= 0) {
				var textBox = (Entry)o;
				textBox.Text = textBox.Text.Replace ("\r", string.Empty);
			}
		}

		void PackageIdTextBoxChanged (object sender, EventArgs e)
		{
			// Use id as description by default.
			if (wizardPage.Id == wizardPage.Description)
				packageDescriptionTextBox.Text = packageIdTextBox.Text;

			wizardPage.Id = packageIdTextBox.Text;

			if (wizardPage.HasIdError ()) {
				if (idTooltip == null) {
					idTooltip = ShowErrorTooltip (idEventBox, wizardPage.IdError);
				}
			} else {
				if (idTooltip != null) {
					HideTooltip (idEventBox, idTooltip);
					idTooltip = null;
				}
			}
		}

		void PackageAuthorsTextBoxChanged (object sender, EventArgs e)
		{
			wizardPage.Authors = packageAuthorsTextBox.Text;
		}

		void PackageDescriptionTextChanged (object sender, EventArgs e)
		{
			wizardPage.Description = packageDescriptionTextBox.Text;
		}

		public override void Dispose ()
		{
			Dispose (idTooltip);
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
