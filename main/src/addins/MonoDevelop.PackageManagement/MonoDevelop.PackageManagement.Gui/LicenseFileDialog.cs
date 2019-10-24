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

using System;
using System.Collections.Generic;
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

			textView = new RichTextView ();
			Content = textView;

			Buttons.Add (Command.Ok);
			DefaultCommand = Command.Ok;
		}

		void LicenseFileTextPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			LoadText ();
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

		public static bool ShowDialog (Uri uri, IReadOnlyList<IText> licenseLinks, Dialog parent)
		{
			if (!uri.IsFile)
				return false;

			if (uri.Fragment?.Length > 0) {
				if (int.TryParse (uri.Fragment.Substring (1), out int fileNumber)) {
					ShowLicenseFile (fileNumber, licenseLinks, parent);
					return true;
				}
			}
			return false;
		}

		static void ShowLicenseFile (int fileNumber, IReadOnlyList<IText> licenseLinks, Dialog parent)
		{
			LicenseFileText licenseFileText = GetLicenseFile (fileNumber, licenseLinks);
			if (licenseFileText != null) {
				var dialog = new LicenseFileDialog (licenseFileText);
				dialog.Run (parent);
			}
		}

		static LicenseFileText GetLicenseFile (int fileNumber, IReadOnlyList<IText> licenseLinks)
		{
			int currentFileNumber = 0;
			foreach (IText text in licenseLinks) {
				if (text is LicenseFileText licenseFileText) {
					currentFileNumber++;
					if (currentFileNumber == fileNumber) {
						return licenseFileText;
					}
				}
			}
			return null;
		}
	}
}
