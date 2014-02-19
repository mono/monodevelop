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
using Mono.Unix;
using ExtendedTitleBarDialog = MonoDevelop.Components.ExtendedTitleBarDialog;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	public partial class AddPackagesDialog : ExtendedTitleBarDialog
	{
		ComboBox packageSourceComboBox;
		TextEntry packageSearchEntry;
		ListView packagesListView;
		VBox packageInfoVBox;
		Label packageNameLabel;
		Label packageVersionLabel;
		LinkLabel packageIdLink;
		RichTextView packageDescription;
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
		Frame loadingSpinnerFrame;
		HBox errorMessageHBox;
		Label errorMessageLabel;

		void Build ()
		{
			Title = Catalog.GetString ("Add Packages");
			Width = 640;
			Height = 480;
			Padding = new WidgetSpacing ();

			// Top part of dialog:
			// Package sources and search.
			var topHBox = new HBox ();
			topHBox.Margin = new WidgetSpacing (5, 5, 5, 5);

			packageSourceComboBox = new ComboBox ();
			packageSourceComboBox.MinWidth = 200;
			topHBox.PackStart (packageSourceComboBox);

			packageSearchEntry = new TextEntry ();
			topHBox.PackEnd (packageSearchEntry);

			this.HeaderContent = topHBox;

			// Middle of dialog:
			// Packages and package information.
			var mainVBox = new VBox ();
			Content = mainVBox;

			var middleHBox = new HBox ();
			mainVBox.PackStart (middleHBox, true, true);

			// Error information.
			var packagesListVBox = new VBox ();
			errorMessageHBox = new HBox ();
			errorMessageHBox.Margin = new WidgetSpacing ();
			errorMessageHBox.BackgroundColor = Colors.Orange;
			errorMessageHBox.Visible = false;
			errorMessageLabel = new Label ();
			errorMessageLabel.TextColor = Colors.White;
			errorMessageLabel.Margin = new WidgetSpacing (5, 5, 5, 5);
			errorMessageLabel.Wrap = WrapMode.Word;
			errorMessageHBox.PackStart (errorMessageLabel, true);
			packagesListVBox.PackStart (errorMessageHBox);

			// Packages list.
			middleHBox.PackStart (packagesListVBox, true, true);
			packagesListView = new ListView ();
			packagesListView.HeadersVisible = false;
			packagesListVBox.PackStart (packagesListView, true, true);

			// Loading spinner.
			var loadingSpinnerHBox = new HBox ();
			loadingSpinnerHBox.HorizontalPlacement = WidgetPlacement.Center;
			var loadingSpinner = new Spinner ();
			loadingSpinner.Animate = true;
			loadingSpinnerHBox.PackStart (loadingSpinner);

			var loadingLabel = new Label ();
			loadingLabel.Text = Catalog.GetString ("Loading package list...");
			loadingSpinnerHBox.PackEnd (loadingLabel);

			loadingSpinnerFrame = new Frame ();
			loadingSpinnerFrame.Visible = false;
			loadingSpinnerFrame.BackgroundColor = Colors.White;
			loadingSpinnerFrame.Content = loadingSpinnerHBox;
			packagesListVBox.PackStart (loadingSpinnerFrame, true, true);

			// Package information
			packageInfoVBox = new VBox ();
			packageInfoVBox.Margin = new WidgetSpacing (5, 10, 10, 10);
			var packageInfoContainerVBox = new VBox ();
			packageInfoContainerVBox.WidthRequest = 260;
			packageInfoContainerVBox.PackStart (packageInfoVBox, true, true);

			var packageInfoScrollView = new ScrollView ();
			packageInfoScrollView.BorderVisible = false;
			packageInfoScrollView.HorizontalScrollPolicy = ScrollPolicy.Never;
			packageInfoScrollView.Content = packageInfoContainerVBox;
			middleHBox.PackEnd (packageInfoScrollView);

			// Package name and version.
			var packageNameHBox = new HBox ();
			packageInfoVBox.PackStart (packageNameHBox);

			packageNameLabel = new Label ();
			packageNameHBox.PackStart (packageNameLabel);

			packageVersionLabel = new Label ();
			packageNameHBox.PackEnd (packageVersionLabel);

			// Package description.
			packageDescription = new RichTextView ();
			packageDescription.Sensitive = false;
			packageInfoVBox.PackStart (packageDescription);

			// Package id.
			var packageIdHBox = new HBox ();
			packageInfoVBox.PackStart (packageIdHBox);

			var packageIdLabel = new Label ();
			packageIdLabel.Markup = Catalog.GetString ("<b>Id</b>");
			packageIdHBox.PackStart (packageIdLabel);

			packageId = new Label ();
			packageIdLink = new LinkLabel ();
			packageIdHBox.PackEnd (packageIdLink);
			packageIdHBox.PackEnd (packageId);

			// Package author
			var packageAuthorHBox = new HBox ();
			packageInfoVBox.PackStart (packageAuthorHBox);

			var packageAuthorLabel = new Label ();
			packageAuthorLabel.Markup = Catalog.GetString ("<b>Author</b>");
			packageAuthorHBox.PackStart (packageAuthorLabel);

			packageAuthor = new Label ();
			packageAuthorHBox.PackEnd (packageAuthor);

			// Package published
			var packagePublishedHBox = new HBox ();
			packageInfoVBox.PackStart (packagePublishedHBox);

			var packagePublishedLabel = new Label ();
			packagePublishedLabel.Markup = Catalog.GetString ("<b>Published</b>");
			packagePublishedHBox.PackStart (packagePublishedLabel);

			packagePublishedDate = new Label ();
			packagePublishedHBox.PackEnd (packagePublishedDate);

			// Package downloads
			var packageDownloadsHBox = new HBox ();
			packageInfoVBox.PackStart (packageDownloadsHBox);

			var packageDownloadsLabel = new Label ();
			packageDownloadsLabel.Markup = Catalog.GetString ("<b>Downloads</b>");
			packageDownloadsHBox.PackStart (packageDownloadsLabel);

			packageDownloads = new Label ();
			packageDownloadsHBox.PackEnd (packageDownloads);

			// Package license.
			var packageLicenseHBox = new HBox ();
			packageInfoVBox.PackStart (packageLicenseHBox);

			var packageLicenseLabel = new Label ();
			packageLicenseLabel.Markup = Catalog.GetString ("<b>License</b>");
			packageLicenseHBox.PackStart (packageLicenseLabel);

			packageLicenseLink = new LinkLabel ();
			packageLicenseLink.Text = Catalog.GetString ("View License");
			packageLicenseHBox.PackEnd (packageLicenseLink);

			// Package project page.
			var packageProjectPageHBox = new HBox ();
			packageInfoVBox.PackStart (packageProjectPageHBox);

			var packageProjectPageLabel = new Label ();
			packageProjectPageLabel.Markup = Catalog.GetString ("<b>Project Page</b>");
			packageProjectPageHBox.PackStart (packageProjectPageLabel);

			packageProjectPageLink = new LinkLabel ();
			packageProjectPageLink.Text = Catalog.GetString ("Visit Page");
			packageProjectPageHBox.PackEnd (packageProjectPageLink);

			// Package dependencies
			var packageDependenciesHBox = new HBox ();
			packageInfoVBox.PackStart (packageDependenciesHBox);

			var packageDependenciesLabel = new Label ();
			packageDependenciesLabel.Markup = Catalog.GetString ("<b>Dependencies</b>");
			packageDependenciesHBox.PackStart (packageDependenciesLabel);

			packageDependenciesNoneLabel = new Label ();
			packageDependenciesNoneLabel.Text = Catalog.GetString ("None");
			packageDependenciesHBox.PackEnd (packageDependenciesNoneLabel);

			// Package dependencies list.
			packageDependenciesListHBox = new HBox ();
			packageDependenciesListHBox.Visible = false;
			packageInfoVBox.PackStart (packageDependenciesListHBox);

			packageDependenciesList = new Label ();
			packageDependenciesListHBox.PackEnd (packageDependenciesList);

			// Bottom part of dialog:
			// Show pre-release packages and Close/Add to Project buttons.
			var bottomHBox = new HBox ();
			bottomHBox.Margin = new WidgetSpacing (5, 5, 5, 5);
			mainVBox.PackStart (bottomHBox);

			showPrereleaseCheckBox = new CheckBox ();
			showPrereleaseCheckBox.Label = Catalog.GetString ("Show pre-release packages");
			bottomHBox.PackStart (showPrereleaseCheckBox);

			addPackagesButton = new Button ();
			addPackagesButton.Label = Catalog.GetString ("Add Package");
			bottomHBox.PackEnd (addPackagesButton);

			var closeButton = new Button ();
			closeButton.Label = Catalog.GetString ("Close");
			closeButton.Clicked += (sender, e) => Close ();
			bottomHBox.PackEnd (closeButton);

			packageSearchEntry.SetFocus ();
			packageInfoVBox.Visible = false;
		}
	}
}

