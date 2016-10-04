//
// AddPlatformImplementationDialog.UI.cs
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

using Xwt;
using MonoDevelop.Core;

namespace MonoDevelop.Packaging.Gui
{
	partial class AddPlatformImplementationDialog : Dialog
	{
		DialogButton okButton;
		CheckBox androidCheckBox;
		CheckBox iosCheckBox;
		CheckBox useSharedProjectCheckBox;

		void Build ()
		{
			Title = GettextCatalog.GetString ("Add Platform Implementation");
			Width = 420;
			Height = 220;
			Padding = new WidgetSpacing (20, 20, 20, 20);

			var mainVBox = new VBox ();
			Content = mainVBox;

			// Platforms selection.
			var platformsVBox = new VBox ();
			platformsVBox.Spacing = 0;
			mainVBox.PackStart (platformsVBox);

			var platformsLabel = new Label ();
			platformsLabel.Text = GettextCatalog.GetString ("Select the platform implementations you would like to add:");
			platformsLabel.MarginBottom = 6;
			platformsVBox.PackStart (platformsLabel);

			androidCheckBox = new CheckBox ();
			androidCheckBox.Label = "Android";
			platformsVBox.PackStart (androidCheckBox);

			iosCheckBox = new CheckBox ();
			iosCheckBox.Label = "iOS";
			platformsVBox.PackStart (iosCheckBox);

			// Use shared project.
			var sharedProjectVBox = new VBox ();
			sharedProjectVBox.Spacing = 0;
			sharedProjectVBox.MarginTop = 20;
			mainVBox.PackStart (sharedProjectVBox);

			var useSharedProjectLabel = new Label ();
			useSharedProjectLabel.Text = GettextCatalog.GetString ("Create a Shared Project from the Portable Class Library:");
			useSharedProjectLabel.MarginBottom = 6;
			sharedProjectVBox.PackStart (useSharedProjectLabel);

			useSharedProjectCheckBox = new CheckBox ();
			useSharedProjectCheckBox.Label = GettextCatalog.GetString ("Create Shared Project");
			sharedProjectVBox.PackStart (useSharedProjectCheckBox);

			var cancelButton = new DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);

			okButton = new DialogButton (Command.Ok);
			Buttons.Add (okButton);
		}
	}
}
