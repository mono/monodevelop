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
using Gdk;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.DotNetCore.Templating;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DotNetCore.Gui
{
	[System.ComponentModel.ToolboxItem (true)]
	partial class GtkDotNetCoreProjectTemplateWizardPageWidget : Gtk.Bin
	{
		DotNetCoreProjectTemplateWizardPage wizardPage;
		Color backgroundColor;
		ImageView backgroundImageView;
		Xwt.Drawing.Image backgroundImage;

		HBox mainHBox;
		EventBox leftBorderEventBox;
		VBox configurationVBox;
		EventBox configurationTopEventBox;
		EventBox configurationTableEventBox;
		Table configurationTable;

		ComboBox targetFrameworkComboBox;
		Label targetFrameworkInformationLabel;
		Label targetFrameworkLabel;

		ComboBox authenticationComboBox;
		Label authenticationInformationLabel;
		Label authenticationLabel;

		EventBox configurationBottomEventBox;
		EventBox backgroundLargeImageEventBox;
		VBox backgroundLargeImageVBox;

		public GtkDotNetCoreProjectTemplateWizardPageWidget (DotNetCoreProjectTemplateWizardPage wizardPage)
		{
			this.wizardPage = wizardPage;

			Build ();

			// Do not use a width request for the configuration box so the left hand side of the
			// wizard page can expand to fit its contents.
			configurationVBox.WidthRequest = -1;

			backgroundImage = Xwt.Drawing.Image.FromResource ("netcore-wizard-page.png");
			backgroundImageView = new ImageView (backgroundImage) {
				Xalign = 1.0f,
				Yalign = 0.5f
			};
			backgroundLargeImageVBox.PackStart (backgroundImageView, true, true, 0);

			backgroundColor = Styles.NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor.ToGdkColor ();
			leftBorderEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTopEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationTableEventBox.ModifyBg (StateType.Normal, backgroundColor);
			configurationBottomEventBox.ModifyBg (StateType.Normal, backgroundColor);
			backgroundLargeImageEventBox.ModifyBg (StateType.Normal, backgroundColor);

			if (wizardPage.TargetFrameworks.Count > 1) {
				PopulateTargetFrameworks ();
				targetFrameworkComboBox.Changed += TargetFrameworkComboBoxChanged;
			}

			if (wizardPage.SupportedAuthentications.Count > 0) {
				PopulateAuthentications ();
				authenticationComboBox.Changed += AuthenticationsComboBoxChanged;
			}
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

		void PopulateAuthentications ()
		{
			foreach (var authentication in wizardPage.SupportedAuthentications) {
				authenticationComboBox.AppendText (authentication.Description);
			}

			authenticationComboBox.Active = wizardPage.SelectedAuthenticationIndex;
			authenticationInformationLabel.LabelProp = wizardPage.SupportedAuthentications [wizardPage.SelectedAuthenticationIndex].Information;
		}

		void AuthenticationsComboBoxChanged (object sender, EventArgs e)
		{
			wizardPage.SelectedAuthenticationIndex = authenticationComboBox.Active;
			authenticationInformationLabel.LabelProp = wizardPage.SupportedAuthentications [wizardPage.SelectedAuthenticationIndex].Information;
		}

		protected virtual void Build ()
		{
			MonoDevelop.Components.Gui.Initialize (this);
			MonoDevelop.Components.BinContainer.Attach (this);

			Name = "MonoDevelop.DotNetCore.Gui.GtkDotNetCoreProjectTemplateWizardPageWidget";

			mainHBox = new HBox {
				Name = "mainHBox"
			};

			leftBorderEventBox = new EventBox {
				WidthRequest = 30,
				Name = "leftBorderEventBox"
			};
			mainHBox.PackStart (leftBorderEventBox, false, true, 0);

			configurationVBox = new VBox {
				WidthRequest = 440,
				Name = "configurationVBox"
			};

			configurationTopEventBox = new EventBox {
				Name = "configurationTopEventBox"
			};
			configurationVBox.PackStart (configurationTopEventBox, true, true, 0);

			var showFrameworkSelection = wizardPage.TargetFrameworks.Count > 1;
			var showAuthenticationSelection = wizardPage.SupportedAuthentications.Count > 0;

			// Create the table of configurable options
			uint tableRows =  (uint)(showFrameworkSelection && showAuthenticationSelection ? 4 : 2);
			configurationTable = new Table (tableRows, 3, false) {
				Name = "configurationTable",
				RowSpacing = 7,
				ColumnSpacing = 6
			};

			if (showFrameworkSelection)
				AddFrameworkSelection ();

			if (showAuthenticationSelection)
				AddAuthenticationSelection ((uint)(showFrameworkSelection ? 2 : 0));

			configurationTableEventBox = new EventBox {
				Name = "configurationTableEventBox"
			};
			configurationTableEventBox.Add (configurationTable);
			configurationVBox.PackStart (configurationTableEventBox, false, false, 0);

			configurationBottomEventBox = new EventBox {
				Name = "configurationBottomEventBox"
			};
			configurationVBox.PackStart (configurationBottomEventBox);
			mainHBox.PackStart (configurationVBox);

			// Add the image
			backgroundLargeImageEventBox = new EventBox {
				Name = "backgroundLargeImageEventBox"
			};
			backgroundLargeImageVBox = new VBox {
				Name = "backgroundLargeImageVBox"
			};
			backgroundLargeImageEventBox.Add (backgroundLargeImageVBox);
			mainHBox.PackStart (backgroundLargeImageEventBox);

			Add (mainHBox);

			if (Child != null) {
				Child.ShowAll ();
			}

			Hide ();
		}

		void AddFrameworkSelection()
		{
			targetFrameworkComboBox = ComboBox.NewText ();
			targetFrameworkComboBox.WidthRequest = 350;
			targetFrameworkComboBox.Name = "targetFrameworkComboBox";
			configurationTable.Attach (targetFrameworkComboBox, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			targetFrameworkInformationLabel = new Label {
				WidthRequest = 350,
				Name = "targetFrameworkInformationLabel",
				Xpad = 5,
				Xalign = 0F,
				LabelProp = GettextCatalog.GetString ("Select the target framework for your project."),
				Justify = Justification.Left,
				Wrap = true
			};
			configurationTable.Attach (targetFrameworkInformationLabel, 1, 2, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			targetFrameworkLabel = new Label {
				Name = "targetFrameworkLabel",
				Xpad = 5,
				Xalign = 1F,
				LabelProp = GettextCatalog.GetString ("Target Framework:"),
				Justify = Justification.Right
			};
			configurationTable.Attach (targetFrameworkLabel, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
		}

		void AddAuthenticationSelection(uint primaryRow)
		{
			authenticationComboBox = ComboBox.NewText ();
			authenticationComboBox.WidthRequest = 350;
			authenticationComboBox.Name = "authenticationComboBox";
			configurationTable.Attach (authenticationComboBox, 1, 2, primaryRow, primaryRow + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			authenticationInformationLabel = new Label {
				WidthRequest = 350,
				Name = "authenticationInformationLabel",
				Xpad = 5,
				Xalign = 0F,
				Justify = Justification.Left,
				Wrap = true
			};
			configurationTable.Attach (authenticationInformationLabel, 1, 2, primaryRow + 1, primaryRow + 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			authenticationLabel = new Label {
				Name = "authenticationLabel",
				Xpad = 5,
				Xalign = 1F,
				LabelProp = GettextCatalog.GetString ("Authentication:"),
				Justify = Justification.Right
			};
			configurationTable.Attach (authenticationLabel, 0, 1, primaryRow, primaryRow + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
		}
	}
}
