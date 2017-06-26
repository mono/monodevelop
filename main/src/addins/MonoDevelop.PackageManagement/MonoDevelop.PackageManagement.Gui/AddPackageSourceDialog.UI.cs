//
// AddPackageSourceDialog.UI.cs
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
using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;
using Xwt;

namespace MonoDevelop.PackageManagement
{
	internal partial class AddPackageSourceDialog : Dialog
	{
		TextEntry packageSourceNameTextEntry;
		TextEntry packageSourceUrlTextEntry;
		TextEntry packageSourceUserNameTextEntry;
		PasswordEntry packageSourcePasswordTextEntry;
		DialogButton addPackageSourceButton;
		DialogButton savePackageSourceButton;
		Button browseButton;

		void Build ()
		{
			Width = 400;
			Title = GettextCatalog.GetString ("Add Package Source");
			int labelWidth = 80;

			var mainVBox = new VBox ();
			mainVBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			Content = mainVBox;

			// Package source name.
			var packageSourceNameHBox = new HBox ();
			packageSourceNameHBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			mainVBox.PackStart (packageSourceNameHBox);

			var packageSourceNameLabel = new Label ();
			packageSourceNameLabel.Text = GettextCatalog.GetString ("Name");
			packageSourceNameLabel.TextAlignment = Alignment.End;
			packageSourceNameLabel.WidthRequest = labelWidth;
			packageSourceNameHBox.PackStart (packageSourceNameLabel);

			packageSourceNameTextEntry = new TextEntry ();
			packageSourceNameTextEntry.SetCommonAccessibilityAttributes ("PackageSourceDialog.name", packageSourceNameLabel,
			                                                             GettextCatalog.GetString ("Enter the name for this package source"));
			packageSourceNameHBox.PackEnd (packageSourceNameTextEntry, true);

			// Package source URL.
			var packageSourceUrlHBox = new HBox ();
			packageSourceUrlHBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			mainVBox.PackStart (packageSourceUrlHBox);

			var packageSourceUrlLabel = new Label ();
			packageSourceUrlLabel.Text = GettextCatalog.GetString ("Location");
			packageSourceUrlLabel.TextAlignment = Alignment.End;
			packageSourceUrlLabel.WidthRequest = labelWidth;
			packageSourceUrlHBox.PackStart (packageSourceUrlLabel);

			packageSourceUrlTextEntry = new TextEntry ();
			packageSourceUrlTextEntry.SetCommonAccessibilityAttributes ("PackageSourceDialog.url", packageSourceUrlLabel,
			                                                            GettextCatalog.GetString ("Enter the URL for this package source"));
			packageSourceUrlTextEntry.PlaceholderText = GettextCatalog.GetString ("URL or folder");
			packageSourceUrlHBox.PackStart (packageSourceUrlTextEntry, true);

			browseButton = new Button ();
			browseButton.Label = GettextCatalog.GetString ("_Browse...");
			packageSourceUrlHBox.PackStart (browseButton);

			// Package source username.
			var packageSourceUserNameHBox = new HBox ();
			packageSourceUserNameHBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			mainVBox.PackStart (packageSourceUserNameHBox);

			var packageSourceUserNameLabel = new Label ();
			packageSourceUserNameLabel.Text = GettextCatalog.GetString ("Username");
			packageSourceUserNameLabel.TextAlignment = Alignment.End;
			packageSourceUserNameLabel.WidthRequest = labelWidth;
			packageSourceUserNameHBox.PackStart (packageSourceUserNameLabel);

			packageSourceUserNameTextEntry = new TextEntry ();
			packageSourceUserNameTextEntry.SetCommonAccessibilityAttributes ("PackageSourceDialog.username", packageSourceUserNameLabel,
			                                                                 GettextCatalog.GetString ("Enter the username (if required) for this package source"));
			packageSourceUserNameTextEntry.PlaceholderText = GettextCatalog.GetString ("Private sources only");
			packageSourceUserNameHBox.PackEnd (packageSourceUserNameTextEntry, true);

			// Package source password.
			var packageSourcePasswordHBox = new HBox ();
			packageSourcePasswordHBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			mainVBox.PackStart (packageSourcePasswordHBox);

			var packageSourcePasswordLabel = new Label ();
			packageSourcePasswordLabel.Text = GettextCatalog.GetString ("Password");
			packageSourcePasswordLabel.TextAlignment = Alignment.End;
			packageSourcePasswordLabel.WidthRequest = labelWidth;
			packageSourcePasswordHBox.PackStart (packageSourcePasswordLabel);

			packageSourcePasswordTextEntry = new PasswordEntry ();
			packageSourcePasswordTextEntry.SetCommonAccessibilityAttributes ("PackageSourceDialog.password", packageSourcePasswordLabel,
			                                                                 GettextCatalog.GetString ("Enter the password (if required) for this package source"));
			packageSourcePasswordTextEntry.PlaceholderText = GettextCatalog.GetString ("Private sources only");
			packageSourcePasswordHBox.PackEnd (packageSourcePasswordTextEntry, true);

			// Buttons at bottom of dialog.
			var cancelButton = new DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);

			addPackageSourceButton = new DialogButton (Command.Ok);
			addPackageSourceButton.Label = GettextCatalog.GetString ("Add Source");
			addPackageSourceButton.Sensitive = false;
			Buttons.Add (addPackageSourceButton);

			savePackageSourceButton = new DialogButton (Command.Apply);
			savePackageSourceButton.Label = GettextCatalog.GetString ("Save");
			savePackageSourceButton.Visible = false;
			Buttons.Add (savePackageSourceButton);
		}
	}
}

