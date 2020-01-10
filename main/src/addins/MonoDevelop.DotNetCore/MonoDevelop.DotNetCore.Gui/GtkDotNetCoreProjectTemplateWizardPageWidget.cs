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
		EventBox configurationBottomEventBox;
		EventBox backgroundLargeImageEventBox;
		VBox backgroundLargeImageVBox;

		public GtkDotNetCoreProjectTemplateWizardPageWidget ()
		{
			this.Build ();

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

		protected virtual void Build ()
		{
			MonoDevelop.Components.Gui.Initialize (this);
			BinContainer.Attach (this);

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

			configurationTable = new Table (2, 3, false);
			configurationTable.Name = "configurationTable";
			configurationTable.RowSpacing = 7;
			configurationTable.ColumnSpacing = 6;

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
	}
}
