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
using Xwt;

namespace MonoDevelop.PackageManagement
{
	public partial class AddPackageSourceDialog : Dialog
	{
		TextEntry packageSourceNameTextEntry;
		TextEntry packageSourceUrlTextEntry;
		TextEntry packageSourceUserNameTextEntry;
		PasswordEntry packageSourcePasswordTextEntry;
		DialogButton addPackageSourceButton;
		DialogButton savePackageSourceButton;

		void Build ()
		{
			Width = 400;
			Title = GettextCatalog.GetString ("Add Package Source");
			int labelWidth = 80;

			var mainVBox = new VBox ();
			Content = mainVBox;

			// Package source name.
			var packageSourceNameHBox = new HBox ();
			mainVBox.PackStart (packageSourceNameHBox);

			var packageSourceNameLabel = new Label ();
			packageSourceNameLabel.Text = GettextCatalog.GetString ("Name");
			packageSourceNameLabel.TextAlignment = Alignment.End;
			packageSourceNameLabel.WidthRequest = labelWidth;
			packageSourceNameHBox.PackStart (packageSourceNameLabel);

			packageSourceNameTextEntry = new TextEntry ();
			packageSourceNameHBox.PackEnd (packageSourceNameTextEntry, true);

			// Package source URL.
			var packageSourceUrlHBox = new HBox ();
			mainVBox.PackStart (packageSourceUrlHBox);

			var packageSourceUrlLabel = new Label ();
			packageSourceUrlLabel.Text = GettextCatalog.GetString ("URL");
			packageSourceUrlLabel.TextAlignment = Alignment.End;
			packageSourceUrlLabel.WidthRequest = labelWidth;
			packageSourceUrlHBox.PackStart (packageSourceUrlLabel);

			packageSourceUrlTextEntry = new TextEntry ();
			packageSourceUrlHBox.PackEnd (packageSourceUrlTextEntry, true);

			// Package source username.
			var packageSourceUserNameHBox = new HBox ();
			mainVBox.PackStart (packageSourceUserNameHBox);

			var packageSourceUserNameLabel = new Label ();
			packageSourceUserNameLabel.Text = GettextCatalog.GetString ("Username");
			packageSourceUserNameLabel.TextAlignment = Alignment.End;
			packageSourceUserNameLabel.WidthRequest = labelWidth;
			packageSourceUserNameHBox.PackStart (packageSourceUserNameLabel);

			packageSourceUserNameTextEntry = new TextEntry ();
			packageSourceUserNameTextEntry.PlaceholderText = GettextCatalog.GetString ("Private sources only");
			packageSourceUserNameHBox.PackEnd (packageSourceUserNameTextEntry, true);

			// Package source password.
			var packageSourcePasswordHBox = new HBox ();
			mainVBox.PackStart (packageSourcePasswordHBox);

			var packageSourcePasswordLabel = new Label ();
			packageSourcePasswordLabel.Text = GettextCatalog.GetString ("Password");
			packageSourcePasswordLabel.TextAlignment = Alignment.End;
			packageSourcePasswordLabel.WidthRequest = labelWidth;
			packageSourcePasswordHBox.PackStart (packageSourcePasswordLabel);

			packageSourcePasswordTextEntry = new PasswordEntry ();
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

