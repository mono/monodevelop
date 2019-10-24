// 
// LicenseAcceptanceDialog.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
//   Vsevolod Kukol <sevo@sevo.org>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using NuGet.PackageManagement.UI;
using Xwt;

namespace MonoDevelop.PackageManagement
{
	internal class LicenseAcceptanceDialog : Dialog
	{
		LicenseAcceptanceViewModel viewModel;
		VBox packagesList;
		ScrollView scroll;
		ImageLoader imageLoader;
		static readonly WidgetSpacing rowMargin = 10;

		public LicenseAcceptanceDialog (LicenseAcceptanceViewModel viewModel)
		{
			Height = 350;
			Padding = 0;
			Title = GettextCatalog.GetString ("License Acceptance");
			this.viewModel = viewModel;
			this.imageLoader = new ImageLoader ();
			this.imageLoader.Loaded += HandleImageLoaded;

			var titleLabel = new Label ();
			titleLabel.Text = GettextCatalog.GetPluralString (
				"The following package requires that you accept its license terms before installing:",
				"The following packages require that you accept their license terms before installing:",
				viewModel.HasMultiplePackages ? 2 : 1);
			var descriptionLabel = new Label ();
			descriptionLabel.Wrap = WrapMode.Word;
			descriptionLabel.Markup = GettextCatalog.GetString ("By clicking <b>Accept</b> you agree to the license terms for the packages listed above.\n" +
																	"If you do not agree to the license terms click <b>Decline</b>.");

			packagesList = new VBox ();
			packagesList.Spacing = 0;

			scroll = new ScrollView (packagesList);
			scroll.HorizontalScrollPolicy = ScrollPolicy.Automatic;
			scroll.VerticalScrollPolicy = ScrollPolicy.Automatic;
			scroll.BorderVisible = false;
			scroll.BackgroundColor = Ide.Gui.Styles.BackgroundColor;

			var container = new VBox ();
			container.MarginTop = container.MarginLeft = container.MarginRight = 15;
			container.PackStart (titleLabel);
			container.PackStart (scroll, true, true);
			container.PackEnd (descriptionLabel);

			Content = container;

			var declineButton = new DialogButton (GettextCatalog.GetString ("Decline"), Command.Cancel);
			var acceptButton = new DialogButton (GettextCatalog.GetString ("Accept"), Command.Ok);

			Buttons.Add (declineButton);
			Buttons.Add (acceptButton);

			AddPackages ();
		}

		void HandleImageLoaded (object sender, ImageLoadedEventArgs e)
		{
			if (!e.HasError) {
				Core.Runtime.RunInMainThread (delegate {
					var view = e.State as ImageView;
					if (view != null)
						view.Image = e.Image?.WithSize (IconSize.Large) ?? ImageService.GetIcon ("md-package", Gtk.IconSize.Dnd);
				});
			}
		}

		void AddPackages ()
		{
			foreach (PackageLicenseViewModel package in viewModel.Packages) {
				AddPackage (package);
			}
		}

		void AddPackage (PackageLicenseViewModel package)
		{
			var titleBox = new VBox ();
			titleBox.Spacing = 0;
			titleBox.MarginBottom = 4;
			titleBox.PackStart (new Label {
				Markup = string.Format ("<span weight='bold'>{0}</span> – {1}", package.Id, package.Author),
			});
			AddLicenseInfo (package, titleBox);

			var rowBox = new HBox ();
			rowBox.Margin = rowMargin;

			var icon = new ImageView (ImageService.GetIcon ("md-package", Gtk.IconSize.Dnd));

			if (package.IconUrl != null && !string.IsNullOrEmpty (package.IconUrl.AbsoluteUri))
				imageLoader.LoadFrom (package.IconUrl, icon);

			rowBox.PackStart (icon);
			rowBox.PackStart (titleBox, true);

			packagesList.PackStart (rowBox);
		}

		static void AddLicenseInfo (PackageLicenseViewModel package, VBox parentVBox)
		{
			if (package.LicenseLinks.Any ()) {
				if (package.LicenseLinks.Count == 1) {
					IText textLink = package.LicenseLinks [0];
					if (textLink is LicenseText licenseText) {
						AddLicenseLinkLabel (licenseText.Link, licenseText.Text, parentVBox);
						return;
					} else if (textLink is LicenseFileText licenseFileText) {
						AddFileLicenseLinkLabel (licenseFileText, parentVBox);
						return;
					} else {
						// Warning or free text fallback to showing an expression which will handle this.
					}
				}
				AddLicenseExpressionLabel (package, parentVBox);
			} else {
				AddLicenseLinkLabel (package.LicenseUrl, GettextCatalog.GetString ("View License"), parentVBox);
			}
		}

		static void AddLicenseExpressionLabel (PackageLicenseViewModel package, VBox parentVBox)
		{
			var licenseLabel = new Label ();
			var builder = new LicenseLinkMarkupBuilder ();
			licenseLabel.Markup = builder.GetMarkup (package.LicenseLinks);
			licenseLabel.Wrap = WrapMode.None;

			// Does not work. LabelBackend for Xamarin.Mac implementation does not ILabelEventSink.OnLinkClicked
			licenseLabel.LinkClicked += (sender, e) => IdeServices.DesktopService.ShowUrl (e.Target.AbsoluteUri);

			foreach (WarningText warning in builder.Warnings) {
				AddLicenseWarningLabel (warning, parentVBox);
			}

			var hbox = new HBox ();
			hbox.PackStart (licenseLabel, true);

			parentVBox.PackStart (hbox);
		}

		static void AddLicenseWarningLabel (WarningText warning, VBox parentVBox)
		{
			var hbox = new HBox ();
			var image = new ImageView {
				Image = ImageService.GetIcon ("md-warning", Gtk.IconSize.Menu),
				VerticalPlacement = WidgetPlacement.Start,
			};
			image.Accessible.RoleDescription = GettextCatalog.GetString ("Warning Icon");

			hbox.PackStart (image);

			var label = new Label {
				Text = warning.Text,
				Wrap = WrapMode.None
			};
			image.Accessible.LabelWidget = label;
			hbox.PackStart (label, true);

			parentVBox.PackStart (hbox);
		}

		static void AddFileLicenseLinkLabel (LicenseFileText licenseFileText, VBox parentVBox)
		{
			var licenseLabel = new LinkLabel (GettextCatalog.GetString ("View License"));
			licenseLabel.Uri = licenseFileText.CreateLicenseFileUri (1);
			licenseLabel.Tag = licenseFileText;
			licenseLabel.NavigateToUrl += (sender, e) => ShowFileDialog ((LinkLabel)sender);
			parentVBox.PackStart (licenseLabel);
		}

		static void AddLicenseLinkLabel (Uri licenseUrl, string labelText, VBox parentVBox)
		{
			var licenseLabel = new LinkLabel (labelText);
			licenseLabel.Uri = licenseUrl;
			licenseLabel.LinkClicked += (sender, e) => IdeServices.DesktopService.ShowUrl (e.Target.AbsoluteUri);
			parentVBox.PackStart (licenseLabel);
		}

		public new bool Run (WindowFrame parentWindow)
		{
			return base.Run (parentWindow) == Command.Ok;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
				imageLoader.Dispose ();
			base.Dispose (disposing);
		}

		static void ShowFileDialog (LinkLabel label)
		{
			var licenseFileText = (LicenseFileText)label.Tag;
			var dialog = new LicenseFileDialog (licenseFileText);
			dialog.Run (label.ParentWindow);
		}
	}
}

