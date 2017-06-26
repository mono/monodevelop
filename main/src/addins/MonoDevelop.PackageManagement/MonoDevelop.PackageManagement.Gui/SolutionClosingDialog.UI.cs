//
// SolutionClosingDialog.UI.cs
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

using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.PackageManagement.Gui
{
	partial class SolutionClosingDialog : Dialog
	{
		Button yesButton;
		Spinner spinner;

		void Build ()
		{
			Resizable = false;

			var mainVBox = new VBox ();
			mainVBox.Margin = 14;
			Content = mainVBox;

			var label = new Label ();
			label.Text = GettextCatalog.GetString ("Unable to close the solution when NuGet packages are being processed.");
			mainVBox.PackStart (label);

			var questionLabel = new Label ();
			questionLabel.Text = "Stop processing NuGet packages?";
			mainVBox.PackStart (questionLabel);

			spinner = new Spinner ();
			mainVBox.PackStart (spinner);
			spinner.Visible = false;

			var bottomHBox = new HBox ();
			bottomHBox.Margin = 0;
			//bottomHBox.Margin = new WidgetSpacing (8, 5, 14, 10);
			bottomHBox.Spacing = 5;
			mainVBox.PackStart (bottomHBox);

			yesButton = new Button ();
			//stopButton.MinWidth = 120;
			//stopButton.MinHeight = 25;
			yesButton.Label = GettextCatalog.GetString ("Yes");
			bottomHBox.PackEnd (yesButton);

			var noButton = new Button ();
			//cancelButton.MinWidth = 120;
			//cancelButton.MinHeight = 25;
			noButton.Label = GettextCatalog.GetString ("No");
			noButton.Clicked += (sender, e) => Close ();
			bottomHBox.PackEnd (noButton);
		}
	}
}
