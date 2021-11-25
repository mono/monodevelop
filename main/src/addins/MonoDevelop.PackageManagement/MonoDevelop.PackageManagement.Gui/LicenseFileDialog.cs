//
// LicenseFileDialog.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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

using System.ComponentModel;
using NuGet.PackageManagement.UI;
using Xwt;
using Xwt.Formats;

namespace MonoDevelop.PackageManagement
{
	sealed class LicenseFileDialog : Dialog
	{
		RichTextView textView;
		LicenseFileText licenseFileText;

		public LicenseFileDialog (LicenseFileText licenseFileText)
		{
			this.licenseFileText = licenseFileText;

			Build ();
			LoadText ();

			licenseFileText.PropertyChanged += LicenseFileTextPropertyChanged;
			licenseFileText.LoadLicenseFile ();
		}

		void Build ()
		{
			Height = 450;
			Width = 450;
			Title = licenseFileText.LicenseHeader;

			var scrollView = new ScrollView ();
			scrollView.HorizontalScrollPolicy = ScrollPolicy.Never;
			Content = scrollView;

			textView = new RichTextView ();
			textView.ReadOnly = true;
			scrollView.Content = textView;

			var okCommand = new Command ("Ok", Core.GettextCatalog.GetString ("OK"));
			Buttons.Add ();
			DefaultCommand = okCommand;
		}

		void LicenseFileTextPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			LoadText ();

			// Need to refresh the dialog after the license text has been loaded. Otherwise when using the
			// native toolkit the vertical scrollbar is not enabled unless you re-size the dialog.
			OnReallocate ();
		}

		void LoadText ()
		{
			textView.LoadText (licenseFileText.LicenseText, TextFormat.Plain);
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (disposing) {
				licenseFileText.PropertyChanged -= LicenseFileTextPropertyChanged;
			}
		}
	}
}
