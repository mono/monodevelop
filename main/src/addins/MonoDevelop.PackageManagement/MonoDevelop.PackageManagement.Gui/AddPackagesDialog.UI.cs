//
// AddPackagesDialog.UI.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ExtendedTitleBarDialog = MonoDevelop.Components.ExtendedTitleBarDialog;
using Mono.Unix;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	public partial class AddPackagesDialog : ExtendedTitleBarDialog
	{
		ComboBox packageSourceComboBox;
		SearchTextEntry packageSearchEntry;
		ListView packagesListView;
		VBox packageInfoVBox;
		Label packageNameLabel;
		Label packageVersionLabel;
		LinkLabel packageIdLink;
		Label packageDescription;
		Label packageAuthor;
		Label packagePublishedDate;
		Label packageDownloads;
		LinkLabel packageLicenseLink;
		LinkLabel packageProjectPageLink;
		Label packageDependenciesList;
		HBox packageDependenciesListHBox;
		Label packageDependenciesNoneLabel;
		CheckBox showPrereleaseCheckBox;
		Label packageId;
		Button addPackagesButton;
		FrameBox loadingSpinnerFrame;
		HBox errorMessageHBox;
		Label errorMessageLabel;
		Label loadingSpinnerLabel;
		FrameBox noPackagesFoundFrame;
		Color lineBorderColor = Color.FromBytes (163, 166, 171);
		Color packageInfoBackgroundColor = Color.FromBytes (227, 231, 237);

		void Build ()
		{
			Title = Catalog.GetString ("Add Packages");
			Width = 820;
			Height = 520;
			Padding = new WidgetSpacing ();

			// Top part of dialog:
			// Package sources and search.
			var topHBox = new HBox ();
			topHBox.Margin = new WidgetSpacing (8, 5, 6, 5);

			packageSourceComboBox = new ComboBox ();
			packageSourceComboBox.MinWidth = 200;
			topHBox.PackStart (packageSourceComboBox);

			packageSearchEntry = new SearchTextEntry ();
			packageSearchEntry.WidthRequest = 187;
			topHBox.PackEnd (packageSearchEntry);

			this.HeaderContent = topHBox;

			// Middle of dialog:
			// Packages and package information.
			var mainVBox = new VBox ();
			Content = mainVBox;

			var middleHBox = new HBox ();
			middleHBox.Spacing = 0;
			var middleFrame = new FrameBox ();
			middleFrame.Content = middleHBox;
			middleFrame.BorderWidth = new WidgetSpacing (0, 0, 0, 1);
			middleFrame.BorderColor = lineBorderColor;
			mainVBox.PackStart (middleFrame, true, true);

			// Error information.
			var packagesListVBox = new VBox ();
			packagesListVBox.Spacing = 0;
			errorMessageHBox = new HBox ();
			errorMessageHBox.Margin = new WidgetSpacing ();
			errorMessageHBox.BackgroundColor = Colors.Orange;
			errorMessageHBox.Visible = false;
			var errorImage = new ImageView ();
			errorImage.Margin = new WidgetSpacing (10, 0, 0, 0);
			errorImage.Image = ImageService.GetIcon (Stock.Warning, Gtk.IconSize.Menu);
			errorImage.HorizontalPlacement = WidgetPlacement.End;
			errorMessageHBox.PackStart (errorImage);
			errorMessageLabel = new Label ();
			errorMessageLabel.TextColor = Colors.White;
			errorMessageLabel.Margin = new WidgetSpacing (5, 5, 5, 5);
			errorMessageLabel.Wrap = WrapMode.Word;
			errorMessageHBox.PackStart (errorMessageLabel, true);
			packagesListVBox.PackStart (errorMessageHBox);

			// Packages list.
			middleHBox.PackStart (packagesListVBox, true, true);
			packagesListView = new ListView ();
			packagesListView.BorderVisible = false;
			packagesListView.HeadersVisible = false;
			packagesListVBox.PackStart (packagesListView, true, true);

			// Loading spinner.
			var loadingSpinnerHBox = new HBox ();
			loadingSpinnerHBox.HorizontalPlacement = WidgetPlacement.Center;
			var loadingSpinner = new Spinner ();
			loadingSpinner.Animate = true;
			loadingSpinner.MinWidth = 20;
			loadingSpinnerHBox.PackStart (loadingSpinner);

			loadingSpinnerLabel = new Label ();
			loadingSpinnerLabel.Text = Catalog.GetString ("Loading package list...");
			loadingSpinnerHBox.PackEnd (loadingSpinnerLabel);

			loadingSpinnerFrame = new FrameBox ();
			loadingSpinnerFrame.Visible = false;
			loadingSpinnerFrame.BackgroundColor = Colors.White;
			loadingSpinnerFrame.Content = loadingSpinnerHBox;
			loadingSpinnerFrame.BorderWidth = new WidgetSpacing ();
			packagesListVBox.PackStart (loadingSpinnerFrame, true, true);

			// No packages found label.
			var noPackagesFoundHBox = new HBox ();
			noPackagesFoundHBox.HorizontalPlacement = WidgetPlacement.Center;

			var noPackagesFoundLabel = new Label ();
			noPackagesFoundLabel.Text = Catalog.GetString ("No matching packages found.");
			noPackagesFoundHBox.PackEnd (noPackagesFoundLabel);

			noPackagesFoundFrame = new FrameBox ();
			noPackagesFoundFrame.Visible = false;
			noPackagesFoundFrame.BackgroundColor = Colors.White;
			noPackagesFoundFrame.Content = noPackagesFoundHBox;
			noPackagesFoundFrame.BorderWidth = new WidgetSpacing ();
			packagesListVBox.PackStart (noPackagesFoundFrame, true, true);

			// Package information
			packageInfoVBox = new VBox ();
			var packageInfoFrame = new FrameBox ();
			packageInfoFrame.BackgroundColor = packageInfoBackgroundColor;
			packageInfoFrame.BorderWidth = new WidgetSpacing ();
			packageInfoFrame.Content = packageInfoVBox;
			packageInfoVBox.Margin = new WidgetSpacing (15, 12, 15, 12);
			var packageInfoContainerVBox = new VBox ();
			packageInfoContainerVBox.WidthRequest = 240;
			packageInfoContainerVBox.PackStart (packageInfoFrame, true, true);

			var packageInfoScrollView = new ScrollView ();
			packageInfoScrollView.BorderVisible = false;
			packageInfoScrollView.HorizontalScrollPolicy = ScrollPolicy.Never;
			packageInfoScrollView.Content = packageInfoContainerVBox;
			packageInfoScrollView.BackgroundColor = packageInfoBackgroundColor;
			var packageInfoScrollViewFrame = new FrameBox ();
			packageInfoScrollViewFrame.BackgroundColor = packageInfoBackgroundColor;
			packageInfoScrollViewFrame.BorderWidth = new WidgetSpacing (1, 0, 0, 0);
			packageInfoScrollViewFrame.BorderColor = lineBorderColor;
			packageInfoScrollViewFrame.Content = packageInfoScrollView;
			middleHBox.PackEnd (packageInfoScrollViewFrame);

			// Package name and version.
			var packageNameHBox = new HBox ();
			packageInfoVBox.PackStart (packageNameHBox);

			packageNameLabel = new Label ();
			packageNameLabel.Ellipsize = EllipsizeMode.End;
			Font packageInfoSmallFont = packageNameLabel.Font.WithScaledSize (0.8);
			packageNameHBox.PackStart (packageNameLabel, true);

			packageVersionLabel = new Label ();
			packageVersionLabel.TextAlignment = Alignment.End;
			packageNameHBox.PackEnd (packageVersionLabel);

			// Package description.
			packageDescription = new Label ();
			packageDescription.Wrap = WrapMode.Word;
			packageDescription.Font = packageNameLabel.Font.WithScaledSize (0.9);
			packageDescription.BackgroundColor = packageInfoBackgroundColor;
			packageInfoVBox.PackStart (packageDescription);

			// Package id.
			var packageIdHBox = new HBox ();
			packageIdHBox.MarginTop = 7;
			packageInfoVBox.PackStart (packageIdHBox);

			var packageIdLabel = new Label ();
			packageIdLabel.Font = packageInfoSmallFont;
			packageIdLabel.Markup = Catalog.GetString ("<b>Id</b>");
			packageIdHBox.PackStart (packageIdLabel);

			packageId = new Label ();
			packageId.Ellipsize = EllipsizeMode.End;
			packageId.TextAlignment = Alignment.End;
			packageId.Font = packageInfoSmallFont;
			packageIdLink = new LinkLabel ();
			packageIdLink.Ellipsize = EllipsizeMode.End;
			packageIdLink.TextAlignment = Alignment.End;
			packageIdLink.Font = packageInfoSmallFont;
			packageIdHBox.PackEnd (packageIdLink, true);
			packageIdHBox.PackEnd (packageId, true);

			// Package author
			var packageAuthorHBox = new HBox ();
			packageInfoVBox.PackStart (packageAuthorHBox);

			var packageAuthorLabel = new Label ();
			packageAuthorLabel.Markup = Catalog.GetString ("<b>Author</b>");
			packageAuthorLabel.Font = packageInfoSmallFont;
			packageAuthorHBox.PackStart (packageAuthorLabel);

			packageAuthor = new Label ();
			packageAuthor.TextAlignment = Alignment.End;
			packageAuthor.Ellipsize = EllipsizeMode.End;
			packageAuthor.Font = packageInfoSmallFont;
			packageAuthorHBox.PackEnd (packageAuthor, true);

			// Package published
			var packagePublishedHBox = new HBox ();
			packageInfoVBox.PackStart (packagePublishedHBox);

			var packagePublishedLabel = new Label ();
			packagePublishedLabel.Markup = Catalog.GetString ("<b>Published</b>");
			packagePublishedLabel.Font = packageInfoSmallFont;
			packagePublishedHBox.PackStart (packagePublishedLabel);

			packagePublishedDate = new Label ();
			packagePublishedDate.Font = packageInfoSmallFont;
			packagePublishedHBox.PackEnd (packagePublishedDate);

			// Package downloads
			var packageDownloadsHBox = new HBox ();
			packageInfoVBox.PackStart (packageDownloadsHBox);

			var packageDownloadsLabel = new Label ();
			packageDownloadsLabel.Markup = Catalog.GetString ("<b>Downloads</b>");
			packageDownloadsLabel.Font = packageInfoSmallFont;
			packageDownloadsHBox.PackStart (packageDownloadsLabel);

			packageDownloads = new Label ();
			packageDownloads.Font = packageInfoSmallFont;
			packageDownloadsHBox.PackEnd (packageDownloads);

			// Package license.
			var packageLicenseHBox = new HBox ();
			packageInfoVBox.PackStart (packageLicenseHBox);

			var packageLicenseLabel = new Label ();
			packageLicenseLabel.Markup = Catalog.GetString ("<b>License</b>");
			packageLicenseLabel.Font = packageInfoSmallFont;
			packageLicenseHBox.PackStart (packageLicenseLabel);

			packageLicenseLink = new LinkLabel ();
			packageLicenseLink.Text = Catalog.GetString ("View License");
			packageLicenseLink.Font = packageInfoSmallFont;
			packageLicenseHBox.PackEnd (packageLicenseLink);

			// Package project page.
			var packageProjectPageHBox = new HBox ();
			packageInfoVBox.PackStart (packageProjectPageHBox);

			var packageProjectPageLabel = new Label ();
			packageProjectPageLabel.Markup = Catalog.GetString ("<b>Project Page</b>");
			packageProjectPageLabel.Font = packageInfoSmallFont;
			packageProjectPageHBox.PackStart (packageProjectPageLabel);

			packageProjectPageLink = new LinkLabel ();
			packageProjectPageLink.Text = Catalog.GetString ("Visit Page");
			packageProjectPageLink.Font = packageInfoSmallFont;
			packageProjectPageHBox.PackEnd (packageProjectPageLink);

			// Package dependencies
			var packageDependenciesHBox = new HBox ();
			packageInfoVBox.PackStart (packageDependenciesHBox);

			var packageDependenciesLabel = new Label ();
			packageDependenciesLabel.Markup = Catalog.GetString ("<b>Dependencies</b>");
			packageDependenciesLabel.Font = packageInfoSmallFont;
			packageDependenciesHBox.PackStart (packageDependenciesLabel);

			packageDependenciesNoneLabel = new Label ();
			packageDependenciesNoneLabel.Text = Catalog.GetString ("None");
			packageDependenciesNoneLabel.Font = packageInfoSmallFont;
			packageDependenciesHBox.PackEnd (packageDependenciesNoneLabel);

			// Package dependencies list.
			packageDependenciesListHBox = new HBox ();
			packageDependenciesListHBox.Visible = false;
			packageInfoVBox.PackStart (packageDependenciesListHBox);

			packageDependenciesList = new Label ();
			packageDependenciesList.Wrap = WrapMode.WordAndCharacter;
			packageDependenciesList.Margin = new WidgetSpacing (5);
			packageDependenciesList.Font = packageInfoSmallFont;
			packageDependenciesListHBox.PackStart (packageDependenciesList, true);

			// Bottom part of dialog:
			// Show pre-release packages and Close/Add to Project buttons.
			var bottomHBox = new HBox ();
			bottomHBox.Margin = new WidgetSpacing (8, 5, 14, 10);
			bottomHBox.Spacing = 5;
			mainVBox.PackStart (bottomHBox);

			showPrereleaseCheckBox = new CheckBox ();
			showPrereleaseCheckBox.Label = Catalog.GetString ("Show pre-release packages");
			bottomHBox.PackStart (showPrereleaseCheckBox);

			addPackagesButton = new Button ();
			addPackagesButton.MinWidth = 120;
			addPackagesButton.MinHeight = 25;
			addPackagesButton.Label = Catalog.GetString ("Add Package");
			bottomHBox.PackEnd (addPackagesButton);

			var closeButton = new Button ();
			closeButton.MinWidth = 120;
			closeButton.MinHeight = 25;
			closeButton.Label = Catalog.GetString ("Close");
			closeButton.Clicked += (sender, e) => Close ();
			bottomHBox.PackEnd (closeButton);

			packageSearchEntry.SetFocus ();
			packageInfoVBox.Visible = false;
		}
	}
}

