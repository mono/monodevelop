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
		DialogButton yesButton;
		Spinner spinner;

		void Build (bool installing)
		{
			var mainVBox = new VBox ();
			mainVBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			Content = mainVBox;

			var label = new Label ();
			label.Margin = new WidgetSpacing (10, 10, 10, 0);
			label.Text = GetMainLabelText (installing);

			mainVBox.PackStart (label);

			var middleHBox = new HBox ();
			middleHBox.Accessible.Role = Xwt.Accessibility.Role.Filler;
			mainVBox.PackStart (middleHBox);

			var questionLabel = new Label ();
			questionLabel.Margin = 10;
			questionLabel.Text = GetQuestionLabelText (installing);
			middleHBox.PackStart (questionLabel);

			spinner = new Spinner ();
			middleHBox.PackStart (spinner);
			spinner.Accessible.Identifier = "busySpinner";
			spinner.Accessible.Description =  GettextCatalog.GetString ("Busy indicator shown whilst waiting stopping for NuGet package processing to stop");
			spinner.Visible = false;

			var noButton = new DialogButton (Command.No);
			yesButton = new DialogButton (Command.Yes);
			Buttons.Add (noButton);
			Buttons.Add (yesButton);
		}

		static string GetMainLabelText (bool installing)
		{
			if (installing)
				return GettextCatalog.GetString ("Unable to close the solution when NuGet packages are being installed.");

			return  GettextCatalog.GetString ("Unable to close the solution when NuGet packages are being uninstalled.");
		}

		static string GetQuestionLabelText (bool installing)
		{
			if (installing)
				return GettextCatalog.GetString ("Stop installing NuGet packages?");

			return GettextCatalog.GetString ("Stop uninstalling NuGet packages?");
		}

		/// <summary>
		/// Do not immediately close the dialog if the Yes button is clicked.
		/// The dialog will wait until the NuGet package action has stopped.
		/// </summary>
		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd != Command.Yes)
				base.OnCommandActivated (cmd);
		}
	}
}
