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
			targetFrameworkLabel.WidthRequest = -1;

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
		}

		void AuthenticationsComboBoxChanged (object sender, EventArgs e)
		{
			wizardPage.SelectedAuthenticationIndex = authenticationComboBox.Active;
		}

		protected virtual void Build ()
		{
			MonoDevelop.Components.Gui.Initialize (this);
			MonoDevelop.Components.BinContainer.Attach (this);

			Name = "MonoDevelop.DotNetCore.Gui.GtkDotNetCoreProjectTemplateWizardPageWidget";

			mainHBox = new HBox ();
			mainHBox.Name = "mainHBox";

			leftBorderEventBox = new EventBox ();
			leftBorderEventBox.WidthRequest = 30;
			leftBorderEventBox.Name = "leftBorderEventBox";
			mainHBox.Add (leftBorderEventBox);

			var w1 = (Box.BoxChild)mainHBox [leftBorderEventBox];
			w1.Position = 0;
			w1.Expand = false;

			configurationVBox = new VBox ();
			configurationVBox.WidthRequest = 440;
			configurationVBox.Name = "configurationVBox";

			configurationTopEventBox = new EventBox ();
			configurationTopEventBox.Name = "configurationTopEventBox";
			configurationVBox.Add (configurationTopEventBox);

			var w2 = (Box.BoxChild)configurationVBox [configurationTopEventBox];
			w2.Position = 0;

			configurationTableEventBox = new EventBox ();
			configurationTableEventBox.Name = "configurationTableEventBox";

			var showFrameworkSelection = wizardPage.TargetFrameworks.Count > 1;
			var showAuthenticationSelection = wizardPage.SupportedAuthentications.Count > 0;

			uint tableRows =  (uint)(showFrameworkSelection && showAuthenticationSelection ? 4 : 2);
			configurationTable = new Table (tableRows, 3, false);
			configurationTable.Name = "configurationTable";
			configurationTable.RowSpacing = 7;
			configurationTable.ColumnSpacing = 6;

			if (showFrameworkSelection)
				AddFrameworkSelection ();

			if (showAuthenticationSelection)
				AddAuthenticationSelection ((uint)(showFrameworkSelection ? 2 : 0));

			configurationTableEventBox.Add (configurationTable);
			configurationVBox.Add (configurationTableEventBox);

			var w7 = (Box.BoxChild)configurationVBox [configurationTableEventBox];
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;

			configurationBottomEventBox = new EventBox ();
			configurationBottomEventBox.Name = "configurationBottomEventBox";
			configurationVBox.Add (configurationBottomEventBox);

			var w8 = (Box.BoxChild)configurationVBox [configurationBottomEventBox];
			w8.Position = 2;
			mainHBox.Add (configurationVBox);

			var w9 = (Box.BoxChild)mainHBox [configurationVBox];
			w9.Position = 1;

			backgroundLargeImageEventBox = new EventBox ();
			backgroundLargeImageEventBox.Name = "backgroundLargeImageEventBox";

			backgroundLargeImageVBox = new VBox ();
			backgroundLargeImageVBox.Name = "backgroundLargeImageVBox";
			backgroundLargeImageEventBox.Add (backgroundLargeImageVBox);
			mainHBox.Add (backgroundLargeImageEventBox);

			var w11 = (Box.BoxChild)mainHBox [backgroundLargeImageEventBox];
			w11.Position = 2;

			Add (mainHBox);

			if (Child != null) {
				Child.ShowAll ();
			}

			Hide ();
		}

		void AddFrameworkSelection()
		{
			targetFrameworkComboBox = ComboBox.NewText ();
			targetFrameworkComboBox.WidthRequest = 250;
			targetFrameworkComboBox.Name = "targetFrameworkComboBox";
			configurationTable.Add (targetFrameworkComboBox);

			var w3 = (Table.TableChild)configurationTable [targetFrameworkComboBox];
			w3.TopAttach = 1;
			w3.BottomAttach = 2;
			w3.LeftAttach = 1;
			w3.RightAttach = 2;
			w3.XOptions = (AttachOptions)4;
			w3.YOptions = (AttachOptions)4;

			targetFrameworkInformationLabel = new Label ();
			targetFrameworkInformationLabel.Name = "targetFrameworkInformationLabel";
			targetFrameworkInformationLabel.Xpad = 5;
			targetFrameworkInformationLabel.Xalign = 0F;
			targetFrameworkInformationLabel.LabelProp = GettextCatalog.GetString ("Select the target framework for your project.");
			targetFrameworkInformationLabel.Justify = (Justification)1;
			configurationTable.Add (targetFrameworkInformationLabel);

			var w4 = (Table.TableChild)configurationTable [targetFrameworkInformationLabel];
			w4.LeftAttach = 1;
			w4.RightAttach = 2;
			w4.XOptions = (AttachOptions)4;
			w4.YOptions = (AttachOptions)4;

			targetFrameworkLabel = new Label ();
			targetFrameworkLabel.WidthRequest = 132;
			targetFrameworkLabel.Name = "targetFrameworkLabel";
			targetFrameworkLabel.Xpad = 5;
			targetFrameworkLabel.Xalign = 1F;
			targetFrameworkLabel.LabelProp = GettextCatalog.GetString ("Target Framework:");
			targetFrameworkLabel.Justify = (Justification)1;
			configurationTable.Add (targetFrameworkLabel);

			var w5 = (Table.TableChild)configurationTable [targetFrameworkLabel];
			w5.TopAttach = 1;
			w5.BottomAttach = 2;
			w5.XOptions = (AttachOptions)4;
			w5.YOptions = (AttachOptions)4;
		}

		void AddAuthenticationSelection(uint primaryRow)
		{
			authenticationComboBox = ComboBox.NewText ();
			authenticationComboBox.WidthRequest = 250;
			authenticationComboBox.Name = "authenticationComboBox";
			configurationTable.Add (authenticationComboBox);

			var authenticationComboBoxCell = (Table.TableChild)configurationTable [authenticationComboBox];
			authenticationComboBoxCell.TopAttach = primaryRow;
			authenticationComboBoxCell.BottomAttach = primaryRow + 1;
			authenticationComboBoxCell.LeftAttach = 1;
			authenticationComboBoxCell.RightAttach = 2;
			authenticationComboBoxCell.XOptions = (AttachOptions)4;
			authenticationComboBoxCell.YOptions = (AttachOptions)4;

			authenticationInformationLabel = new Label ();
			authenticationInformationLabel.Name = "authenticationInformationLabel";
			authenticationInformationLabel.Xpad = 5;
			authenticationInformationLabel.Xalign = 0F;
			authenticationInformationLabel.LabelProp = GettextCatalog.GetString ("TODO: Make this dynamic.");
			authenticationInformationLabel.Justify = (Justification)1;
			configurationTable.Add (authenticationInformationLabel);

			var authenticationInformationLabelCell = (Table.TableChild)configurationTable [authenticationInformationLabel];
			authenticationInformationLabelCell.TopAttach = primaryRow + 1;
			authenticationInformationLabelCell.BottomAttach = primaryRow + 2;
			authenticationInformationLabelCell.LeftAttach = 1;
			authenticationInformationLabelCell.RightAttach = 2;
			authenticationInformationLabelCell.XOptions = (AttachOptions)4;
			authenticationInformationLabelCell.YOptions = (AttachOptions)4;

			authenticationLabel = new Label ();
			authenticationLabel.WidthRequest = 132;
			authenticationLabel.Name = "authenticationLabel";
			authenticationLabel.Xpad = 5;
			authenticationLabel.Xalign = 1F;
			authenticationLabel.LabelProp = GettextCatalog.GetString ("Authentication:");
			authenticationLabel.Justify = (Justification)1;
			configurationTable.Add (authenticationLabel);

			var authenticationLabelCell = (Table.TableChild)configurationTable [authenticationLabel];
			authenticationLabelCell.TopAttach = primaryRow;
			authenticationLabelCell.BottomAttach = primaryRow + 1;
			authenticationLabelCell.LeftAttach = 0;
			authenticationLabelCell.RightAttach = 1;
			authenticationLabelCell.XOptions = (AttachOptions)4;
			authenticationLabelCell.YOptions = (AttachOptions)4;
		}
	}
}
