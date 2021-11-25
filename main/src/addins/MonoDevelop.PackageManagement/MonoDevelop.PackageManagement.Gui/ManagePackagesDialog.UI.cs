//
// ManagePackagesDialog.UI.cs
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

using ExtendedTitleBarDialog = MonoDevelop.Components.ExtendedTitleBarDialog;
using InformationPopoverWidget = MonoDevelop.Components.InformationPopoverWidget;
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.PackageManagement
{
	internal partial class ManagePackagesDialog : ExtendedTitleBarDialog
	{
		ComboBox packageSourceComboBox;
		SearchTextEntry packageSearchEntry;
		ListView packagesListView;
		VBox packageInfoVBox;
		HBox packageNameHBox;
		Label packageNameLabel;
		LinkLabel packageIdLink;
		Label packageDescription;
		Label packageAuthor;
		Label packagePublishedDate;
		Label packageDownloads;
		Label packageLicenseLabel;
		LinkLabel packageLicenseLink;
		InformationPopoverWidget packageLicenseMetadataWarningInfoPopoverWidget;
		Label packageLicenseMetadataLinkLabel;
		LinkLabel packageProjectPageLink;
		Label packageDependenciesList;
		HBox packageDependenciesHBox;
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
		Label noPackagesFoundLabel;
		ComboBox packageVersionComboBox;
		HBox packageVersionsHBox;
		Label packageVersionsLabel;
		CustomButtonLabel browseLabel;
		CustomButtonLabel installedLabel;
		CustomButtonLabel updatesLabel;
		CustomButtonLabel consolidateLabel;
		HBox tabGroup;
		VBox projectsListViewVBox;
		Label projectsListViewLabel;
		ListView projectsListView;
		HBox currentPackageVersionHBox;
		Label currentPackageVersionLabel;
		Label currentPackageVersion;
		InformationPopoverWidget currentPackageVersionInfoPopoverWidget;
		Button closeButton;
		int packageInfoFontSize = 11;

		void Build ()
		{
			Title = GettextCatalog.GetString ("Manage NuGet Packages – Solution");
			Width = 840;
			Height = 528;
			Padding = new WidgetSpacing ();

			if (Platform.IsWindows) {
				packageInfoFontSize = 9;
			}

			// Top part of dialog:
			// Package sources and search.
			var topHBox = new HBox ();
			topHBox.Margin = new WidgetSpacing (8, 5, 6, 5);

			// HACK: VoiceOver does not work when using Accessible.Label so workaround this by using
			// Accessible.LabelWidget and hide the label since we do not need it.
			var packageSourceLabel = new Label ();
			packageSourceLabel.Text = GettextCatalog.GetString ("Package source");
			packageSourceLabel.Visible = false;
			topHBox.PackStart (packageSourceLabel);

			packageSourceComboBox = new ComboBox ();
			packageSourceComboBox.Name = "packageSourceComboBox";
			packageSourceComboBox.MinWidth = 200;
			// Does not work:
			//packageSourceComboBox.Accessible.Label = GettextCatalog.GetString ("Package source");
			packageSourceComboBox.Accessible.LabelWidget = packageSourceLabel;
			topHBox.PackStart (packageSourceComboBox);

			tabGroup = new HBox ();

			int tabLabelMinWidth = 60;
			browseLabel = new CustomButtonLabel ();
			browseLabel.Text = GettextCatalog.GetString ("Browse");
			browseLabel.Tag = browseLabel.Text;
			browseLabel.MinWidth = tabLabelMinWidth;
			browseLabel.MarginLeft = 10;
			tabGroup.PackStart (browseLabel);

			installedLabel = new CustomButtonLabel ();
			installedLabel.Text = GettextCatalog.GetString ("Installed");
			installedLabel.Tag = installedLabel.Text;
			installedLabel.MinWidth = tabLabelMinWidth;
			tabGroup.PackStart (installedLabel);

			updatesLabel = new CustomButtonLabel ();
			updatesLabel.Text = GettextCatalog.GetString ("Updates");
			updatesLabel.Tag = updatesLabel.Text;
			updatesLabel.MinWidth = tabLabelMinWidth;
			tabGroup.PackStart (updatesLabel);

			consolidateLabel = new CustomButtonLabel ();
			consolidateLabel.Text = GettextCatalog.GetString ("Consolidate");
			consolidateLabel.Tag = consolidateLabel.Text;
			consolidateLabel.MinWidth = tabLabelMinWidth;
			tabGroup.PackStart (consolidateLabel);

			topHBox.PackStart (tabGroup);

			packageSearchEntry = new SearchTextEntry ();
			packageSearchEntry.Name = "managePackagesDialogSearchEntry";
			packageSearchEntry.WidthRequest = 187;
			packageSearchEntry.PlaceholderText = GettextCatalog.GetString ("Search");
			packageSearchEntry.Accessible.Label = GettextCatalog.GetString ("Package Search");
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
			middleFrame.BorderColor = Styles.LineBorderColor;
			mainVBox.PackStart (middleFrame, true, true);

			// Error information.
			var packagesListVBox = new VBox ();
			packagesListVBox.Spacing = 0;
			errorMessageHBox = new HBox ();
			errorMessageHBox.Margin = new WidgetSpacing ();
			errorMessageHBox.BackgroundColor = Styles.ErrorBackgroundColor;
			errorMessageHBox.Visible = false;
			var errorImage = new ImageView ();
			errorImage.Margin = new WidgetSpacing (10, 0, 0, 0);
			errorImage.Image = ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.Warning, Gtk.IconSize.Menu);
			errorImage.HorizontalPlacement = WidgetPlacement.End;
			errorMessageHBox.PackStart (errorImage);
			errorMessageLabel = new Label ();
			errorMessageLabel.TextColor = Styles.ErrorForegroundColor;
			errorMessageLabel.Margin = new WidgetSpacing (5, 5, 5, 5);
			errorMessageLabel.Wrap = WrapMode.Word;
			errorMessageHBox.PackStart (errorMessageLabel, true);
			packagesListVBox.PackStart (errorMessageHBox);

			// Packages list.
			middleHBox.PackStart (packagesListVBox, true, true);
			packagesListView = new ListView ();
			packagesListView.BorderVisible = false;
			packagesListView.HeadersVisible = false;
			packagesListView.Accessible.Label = GettextCatalog.GetString ("Packages");
			packagesListVBox.PackStart (packagesListView, true, true);

			// Loading spinner.
			var loadingSpinnerHBox = new HBox ();
			loadingSpinnerHBox.HorizontalPlacement = WidgetPlacement.Center;
			var loadingSpinner = new Spinner ();
			loadingSpinner.Animate = true;
			loadingSpinner.MinWidth = 20;
			loadingSpinnerHBox.PackStart (loadingSpinner);

			loadingSpinnerLabel = new Label ();
			loadingSpinnerLabel.Text = GettextCatalog.GetString ("Loading package list...");
			loadingSpinnerHBox.PackEnd (loadingSpinnerLabel);

			loadingSpinnerFrame = new FrameBox ();
			loadingSpinnerFrame.Visible = false;
			loadingSpinnerFrame.BackgroundColor = Styles.BackgroundColor;
			loadingSpinnerFrame.Content = loadingSpinnerHBox;
			loadingSpinnerFrame.BorderWidth = new WidgetSpacing ();
			packagesListVBox.PackStart (loadingSpinnerFrame, true, true);

			// No packages found label.
			var noPackagesFoundHBox = new HBox ();
			noPackagesFoundHBox.HorizontalPlacement = WidgetPlacement.Center;

			noPackagesFoundLabel = new Label ();
			noPackagesFoundLabel.Text = GettextCatalog.GetString ("No matching packages found.");
			noPackagesFoundHBox.PackEnd (noPackagesFoundLabel);

			noPackagesFoundFrame = new FrameBox ();
			noPackagesFoundFrame.Visible = false;
			noPackagesFoundFrame.BackgroundColor = Styles.BackgroundColor;
			noPackagesFoundFrame.Content = noPackagesFoundHBox;
			noPackagesFoundFrame.BorderWidth = new WidgetSpacing ();
			packagesListVBox.PackStart (noPackagesFoundFrame, true, true);

			// Package information
			packageInfoVBox = new VBox ();
			var packageInfoFrame = new FrameBox ();
			packageInfoFrame.BackgroundColor = Styles.PackageInfoBackgroundColor;
			packageInfoFrame.BorderWidth = new WidgetSpacing ();
			packageInfoFrame.Content = packageInfoVBox;
			packageInfoVBox.Margin = new WidgetSpacing (15, 12, 15, 12);
			var packageInfoContainerVBox = new VBox ();
			packageInfoContainerVBox.WidthRequest = 328;
			packageInfoContainerVBox.PackStart (packageInfoFrame, true, true);

			var packageInfoScrollView = new ScrollView ();
			packageInfoScrollView.BorderVisible = false;
			packageInfoScrollView.HorizontalScrollPolicy = ScrollPolicy.Never;
			packageInfoScrollView.Content = packageInfoContainerVBox;
			packageInfoScrollView.BackgroundColor = Styles.PackageInfoBackgroundColor;
			var packageInfoScrollViewFrame = new FrameBox ();
			packageInfoScrollViewFrame.BackgroundColor = Styles.PackageInfoBackgroundColor;
			packageInfoScrollViewFrame.BorderWidth = new WidgetSpacing (1, 0, 0, 0);
			packageInfoScrollViewFrame.BorderColor = Styles.LineBorderColor;
			packageInfoScrollViewFrame.Content = packageInfoScrollView;

			// Package name and version.
			packageNameHBox = new HBox ();
			packageInfoVBox.PackStart (packageNameHBox);

			packageNameLabel = new Label ();
			packageNameLabel.Ellipsize = EllipsizeMode.End;
			Font packageInfoSmallFont = packageNameLabel.Font.WithSize (packageInfoFontSize);
			Font packageInfoBoldFont = packageInfoSmallFont.WithWeight (FontWeight.Bold);
			packageNameLabel.Font = packageInfoSmallFont;
			packageNameHBox.PackStart (packageNameLabel, true);

			// Projects list view label.
			projectsListViewLabel = new Label ();
			projectsListViewLabel.Wrap = WrapMode.Word;
			projectsListViewLabel.BackgroundColor = Styles.PackageInfoBackgroundColor;
			packageInfoVBox.PackStart (projectsListViewLabel);

			// Projects list view.
			projectsListViewVBox = new VBox ();
			projectsListViewVBox.Margin = new WidgetSpacing ();
			packageInfoVBox.PackStart (projectsListViewVBox, true, true);

			// Package description.
			packageDescription = new Label ();
			packageDescription.Wrap = WrapMode.Word;
			packageDescription.Font = packageNameLabel.Font.WithSize (packageInfoFontSize);
			packageDescription.BackgroundColor = Styles.PackageInfoBackgroundColor;
			packageInfoVBox.PackStart (packageDescription);

			// Package id.
			var packageIdHBox = new HBox ();
			packageIdHBox.MarginTop = 7;
			packageInfoVBox.PackStart (packageIdHBox);

			var packageIdLabel = new Label ();
			packageIdLabel.Font = packageInfoBoldFont;
			packageIdLabel.Text = GettextCatalog.GetString ("ID");
			packageIdHBox.PackStart (packageIdLabel);

			packageId = new Label ();
			packageId.Ellipsize = EllipsizeMode.End;
			packageId.TextAlignment = Alignment.End;
			packageId.Font = packageInfoSmallFont;
			packageId.Accessible.LabelWidget = packageIdLabel;
			packageIdLink = new LinkLabel ();
			packageIdLink.Ellipsize = EllipsizeMode.End;
			packageIdLink.TextAlignment = Alignment.End;
			packageIdLink.Font = packageInfoSmallFont;
			packageIdLink.Accessible.LabelWidget = packageIdLabel;
			packageIdHBox.PackEnd (packageIdLink, true);
			packageIdHBox.PackEnd (packageId, true);

			// Package author
			var packageAuthorHBox = new HBox ();
			packageInfoVBox.PackStart (packageAuthorHBox);

			var packageAuthorLabel = new Label ();
			packageAuthorLabel.Text = GettextCatalog.GetString ("Author");
			packageAuthorLabel.Font = packageInfoBoldFont;
			packageAuthorHBox.PackStart (packageAuthorLabel);

			packageAuthor = new Label ();
			packageAuthor.TextAlignment = Alignment.End;
			packageAuthor.Ellipsize = EllipsizeMode.End;
			packageAuthor.Font = packageInfoSmallFont;
			packageAuthor.Accessible.LabelWidget = packageAuthorLabel;
			packageAuthorHBox.PackEnd (packageAuthor, true);

			// Package published
			var packagePublishedHBox = new HBox ();
			packageInfoVBox.PackStart (packagePublishedHBox);

			var packagePublishedLabel = new Label ();
			packagePublishedLabel.Text = GettextCatalog.GetString ("Published");
			packagePublishedLabel.Font = packageInfoBoldFont;
			packagePublishedHBox.PackStart (packagePublishedLabel);

			packagePublishedDate = new Label ();
			packagePublishedDate.Font = packageInfoSmallFont;
			packagePublishedDate.Accessible.LabelWidget = packagePublishedLabel;
			packagePublishedHBox.PackEnd (packagePublishedDate);

			// Package downloads
			var packageDownloadsHBox = new HBox ();
			packageInfoVBox.PackStart (packageDownloadsHBox);

			var packageDownloadsLabel = new Label ();
			packageDownloadsLabel.Text = GettextCatalog.GetString ("Downloads");
			packageDownloadsLabel.Font = packageInfoBoldFont;
			packageDownloadsHBox.PackStart (packageDownloadsLabel);

			packageDownloads = new Label ();
			packageDownloads.Font = packageInfoSmallFont;
			packageDownloads.Accessible.LabelWidget = packageDownloadsLabel;
			packageDownloadsHBox.PackEnd (packageDownloads);

			// Package license.
			var packageLicenseHBox = new HBox ();
			packageInfoVBox.PackStart (packageLicenseHBox);

			packageLicenseLabel = new Label ();
			packageLicenseLabel.Text = GettextCatalog.GetString ("License");
			packageLicenseLabel.Font = packageInfoBoldFont;
			packageLicenseHBox.PackStart (packageLicenseLabel, vpos: WidgetPlacement.Start);

			packageLicenseLink = new LinkLabel ();
			packageLicenseLink.Text = GettextCatalog.GetString ("View License");
			packageLicenseLink.Font = packageInfoSmallFont;
			packageLicenseLink.TextAlignment = Alignment.End;
			packageLicenseHBox.PackStart (packageLicenseLink, true);

			packageLicenseMetadataLinkLabel = new Label ();
			packageLicenseMetadataLinkLabel.Wrap = WrapMode.Word;
			packageLicenseMetadataLinkLabel.Font = packageInfoSmallFont;
			packageLicenseMetadataLinkLabel.Accessible.LabelWidget = packageLicenseLabel;
			packageLicenseMetadataLinkLabel.TextAlignment = Alignment.End;
			packageLicenseHBox.PackStart (packageLicenseMetadataLinkLabel, true, vpos: WidgetPlacement.Start);

			packageLicenseMetadataWarningInfoPopoverWidget = new InformationPopoverWidget ();
			packageLicenseMetadataWarningInfoPopoverWidget.Severity = Ide.Tasks.TaskSeverity.Warning;
			packageLicenseHBox.PackStart (packageLicenseMetadataWarningInfoPopoverWidget, vpos: WidgetPlacement.Start, hpos: WidgetPlacement.End);

			// Package project page.
			var packageProjectPageHBox = new HBox ();
			packageInfoVBox.PackStart (packageProjectPageHBox);

			var packageProjectPageLabel = new Label ();
			packageProjectPageLabel.Text = GettextCatalog.GetString ("Project Page");
			packageProjectPageLabel.Font = packageInfoBoldFont;
			packageProjectPageHBox.PackStart (packageProjectPageLabel);

			packageProjectPageLink = new LinkLabel ();
			packageProjectPageLink.Text = GettextCatalog.GetString ("Visit Page");
			packageProjectPageLink.Font = packageInfoSmallFont;
			packageProjectPageLink.Accessible.Label = GettextCatalog.GetString ("Visit Project Page");
			packageProjectPageHBox.PackEnd (packageProjectPageLink);

			// Package dependencies
			packageDependenciesHBox = new HBox ();
			packageInfoVBox.PackStart (packageDependenciesHBox);

			var packageDependenciesLabel = new Label ();
			packageDependenciesLabel.Text = GettextCatalog.GetString ("Dependencies");
			packageDependenciesLabel.Font = packageInfoBoldFont;
			packageDependenciesHBox.PackStart (packageDependenciesLabel);

			packageDependenciesNoneLabel = new Label ();
			packageDependenciesNoneLabel.Text = GettextCatalog.GetString ("None");
			packageDependenciesNoneLabel.Font = packageInfoSmallFont;
			packageDependenciesNoneLabel.Accessible.LabelWidget = packageDependenciesLabel;
			packageDependenciesHBox.PackEnd (packageDependenciesNoneLabel);

			// Package dependencies list.
			packageDependenciesListHBox = new HBox ();
			packageDependenciesListHBox.Visible = false;
			packageInfoVBox.PackStart (packageDependenciesListHBox);

			packageDependenciesList = new Label ();
			packageDependenciesList.Wrap = WrapMode.WordAndCharacter;
			packageDependenciesList.Margin = new WidgetSpacing (5);
			packageDependenciesList.Font = packageInfoSmallFont;
			packageDependenciesList.Accessible.LabelWidget = packageDependenciesLabel;
			packageDependenciesListHBox.PackStart (packageDependenciesList, true);

			// Current package version.
			currentPackageVersionHBox = new HBox ();
			currentPackageVersionHBox.Spacing = 15;
			currentPackageVersionHBox.Visible = false;
			currentPackageVersionHBox.BackgroundColor = Styles.PackageInfoBackgroundColor;
			currentPackageVersionHBox.Margin = new WidgetSpacing (15, 0, 15, 0);
			currentPackageVersionLabel = new Label ();
			currentPackageVersionLabel.BoundsChanged += PackageVersionLabelBoundsChanged;
			currentPackageVersionLabel.Font = packageInfoSmallFont;
			currentPackageVersionLabel.Text = GettextCatalog.GetString ("Current Version:");
			currentPackageVersionLabel.TextAlignment = Alignment.End;
			currentPackageVersionHBox.PackStart (currentPackageVersionLabel);

			var currentPackageVersionWithInfoPopoverHBox = new HBox ();
			currentPackageVersionWithInfoPopoverHBox.Margin = new WidgetSpacing ();
			currentPackageVersionWithInfoPopoverHBox.Spacing = 0;

			currentPackageVersion = new Label ();
			currentPackageVersion.Font = packageInfoSmallFont;
			currentPackageVersion.Accessible.LabelWidget = currentPackageVersionLabel;
			currentPackageVersionWithInfoPopoverHBox.PackStart (currentPackageVersion);

			currentPackageVersionInfoPopoverWidget = new InformationPopoverWidget ();
			currentPackageVersionInfoPopoverWidget.Severity = Ide.Tasks.TaskSeverity.Information;
			currentPackageVersionInfoPopoverWidget.Margin = new WidgetSpacing (5, 0, 0, 2);
			currentPackageVersionInfoPopoverWidget.Accessible.LabelWidget = currentPackageVersionLabel;
			currentPackageVersionWithInfoPopoverHBox.PackStart (currentPackageVersionInfoPopoverWidget);

			currentPackageVersionHBox.PackStart (currentPackageVersionWithInfoPopoverHBox);

			// Package versions.
			packageVersionsHBox = new HBox ();
			packageVersionsHBox.Visible = false;
			packageVersionsHBox.BackgroundColor = Styles.PackageInfoBackgroundColor;
			packageVersionsHBox.Margin = new WidgetSpacing (15, 0, 15, 12);
			packageVersionsLabel = new Label ();
			packageVersionsLabel.Font = packageInfoSmallFont;
			packageVersionsLabel.Text = GettextCatalog.GetString ("New Version:");
			packageVersionsLabel.TextAlignment = Alignment.End;
			packageVersionsHBox.PackStart (packageVersionsLabel);

			packageVersionComboBox = new ComboBox ();
			packageVersionComboBox.Name = "packageVersionComboBox";
			packageVersionComboBox.Accessible.LabelWidget = packageVersionsLabel;
			packageVersionsHBox.Spacing = 15;
			packageVersionsHBox.PackStart (packageVersionComboBox, true, true);

			var packageInfoAndVersionsVBox = new VBox ();
			packageInfoAndVersionsVBox.Margin = new WidgetSpacing ();
			packageInfoAndVersionsVBox.BackgroundColor = Styles.PackageInfoBackgroundColor;
			packageInfoAndVersionsVBox.PackStart (packageInfoScrollViewFrame, true, true);
			packageInfoAndVersionsVBox.PackStart (currentPackageVersionHBox, false, false);
			packageInfoAndVersionsVBox.PackStart (packageVersionsHBox, false, false);
			middleHBox.PackEnd (packageInfoAndVersionsVBox);

			// Bottom part of dialog:
			// Show pre-release packages and Close/Add to Project buttons.
			var bottomHBox = new HBox ();
			bottomHBox.Margin = new WidgetSpacing (8, 5, 14, 10);
			bottomHBox.Spacing = 5;
			mainVBox.PackStart (bottomHBox);

			showPrereleaseCheckBox = new CheckBox ();
			showPrereleaseCheckBox.Name = "managePackagesDialogShowPreReleaseCheckBox";
			showPrereleaseCheckBox.Label = GettextCatalog.GetString ("Show pre-release packages");
			bottomHBox.PackStart (showPrereleaseCheckBox);

			addPackagesButton = new Button ();
			addPackagesButton.Name = "managePackagesDialogAddPackageButton";
			addPackagesButton.MinWidth = 120;
			addPackagesButton.MinHeight = 25;
			addPackagesButton.Label = GettextCatalog.GetString ("Add Package");
			bottomHBox.PackEnd (addPackagesButton);

			closeButton = new Button ();
			closeButton.Name = "managePackagesDialogCloseButton";
			closeButton.MinWidth = 120;
			closeButton.MinHeight = 25;
			closeButton.Label = GettextCatalog.GetString ("Close");
			bottomHBox.PackEnd (closeButton);

			packageSearchEntry.SetFocus ();
			packageInfoVBox.Visible = false;
		}

		double? maxPackageVersionLabelWidth;

		void PackageVersionLabelBoundsChanged (object sender, EventArgs e)
		{
			if (!viewModel.IsUpdatesPageSelected)
				return;

			double currentPackageVersionLabelWidth = currentPackageVersionLabel.Size.Width;
			double packageVersionsLabelWidth = packageVersionsLabel.Size.Width;

			if (currentPackageVersionLabelWidth > packageVersionsLabelWidth) {
				packageVersionsLabel.WidthRequest = currentPackageVersionLabelWidth;
				maxPackageVersionLabelWidth = currentPackageVersionLabelWidth;
			} else if (packageVersionsLabelWidth > currentPackageVersionLabelWidth) {
				currentPackageVersionLabel.WidthRequest = packageVersionsLabelWidth;
				maxPackageVersionLabelWidth = packageVersionsLabelWidth;
			}
		}
	}

	sealed class CustomButtonLabel : Canvas
	{
		readonly TextLayout layout = new TextLayout ();
		Size preferredSize;

		public string Text {
			get { return layout.Text; }
			set {
				layout.Markup = null;
				layout.Text = value;
				Accessible.Title = layout.Text;
				preferredSize = layout.GetSize ();
				OnPreferredSizeChanged ();
			}
		}

		public string Markup {
			get { return layout.Markup; }
			set {
				layout.Markup = value;
				Accessible.Title = layout.Text;
				preferredSize = layout.GetSize ();
				OnPreferredSizeChanged ();
			}
		}

		public CustomButtonLabel ()
		{
			CanGetFocus = true;
			Accessible.Role = Xwt.Accessibility.Role.Button;
		}

		protected override Size OnGetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
		{
			return preferredSize;
		}

		protected override void OnDraw (Context ctx, Rectangle dirtyRect)
		{
			if (HasFocus) {
				ctx.SetColor (Ide.Gui.Styles.BaseSelectionBackgroundColor);
			}
			var actualSize = Size;
			var x = Math.Max (0, (actualSize.Width - preferredSize.Width) / 2);
			var y = Math.Max (0, (actualSize.Height - preferredSize.Height) / 2);
			x += x % 2; // align pixels
			ctx.DrawTextLayout (layout, x, y);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				layout.Dispose ();
			}
			base.Dispose (disposing);
		}
	}
}

