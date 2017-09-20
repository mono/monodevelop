// AboutMonoDevelopTabPage.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//   Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2009 RemObjects Software
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
//
//
using System;
using System.IO;

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	class AboutMonoDevelopTabPage: VBox
	{
		Xwt.Drawing.Image imageSep;

		public AboutMonoDevelopTabPage ()
		{
			BorderWidth = 0;

			var aboutFile = BrandingService.GetFile (AboutDialogImage.Name);
			if (aboutFile != null)
				imageSep = Xwt.Drawing.Image.FromFile (aboutFile);
			else
				imageSep = Xwt.Drawing.Image.FromResource (AboutDialogImage.Name);

			var iv = new ImageView (imageSep);
			iv.SetCommonAccessibilityAttributes ("AboutImage", BrandingService.ApplicationLongName, "");
			PackStart (iv, false, false, 0);

			Xwt.VBox infoBox = new Xwt.VBox () {
				CanGetFocus = false
			};
			Xwt.FrameBox mbox = new Xwt.FrameBox (infoBox) {
				CanGetFocus = false
			};

			infoBox.Spacing = 6;
			infoBox.Margin = 12;
			PackStart (mbox.ToGtkWidget (), false, false, 0);

			infoBox.PackStart (new Xwt.Label () {
				Markup = string.Format ("<b>{0}</b>", GettextCatalog.GetString ("Version")),
				MarginTop = 6,
			});
			infoBox.PackStart (new Xwt.Label () {
				Text = IdeVersionInfo.MonoDevelopVersion,
				MarginLeft = 12
			});

			if (BrandingService.LicenseTermsUrl != null) {
				infoBox.PackStart (new Xwt.Label () {
					Markup = string.Format ("<b>{0}</b>", GettextCatalog.GetString ("License")),
					MarginTop = 6,
				});
		       
				var linkLabel = new Xwt.LinkLabel {
					Markup = "<span underline='true'>License Terms</span>",
					Cursor = Xwt.CursorType.Hand,
					MarginLeft = 12,
					CanGetFocus = true,
					Uri = new Uri(BrandingService.LicenseTermsUrl),
				};
				if (IdeTheme.UserInterfaceTheme == Theme.Light)
					linkLabel.Markup = string.Format ("<span color='#5C2D91'>{0}</span>", linkLabel.Markup);
				
				infoBox.PackStart (linkLabel);

				if (BrandingService.PrivacyStatementUrl != null) {
					linkLabel = new Xwt.LinkLabel {
						Markup = string.Format ("<span underline='true'>{0}</span>", GettextCatalog.GetString ("Privacy Statement")),
						Cursor = Xwt.CursorType.Hand,
						MarginLeft = 12,
						CanGetFocus = true,
						Uri = new Uri(BrandingService.PrivacyStatementUrl),
					};

					if (IdeTheme.UserInterfaceTheme == Theme.Light)
						linkLabel.Markup = string.Format ("<span color='#5C2D91'>{0}</span>", linkLabel.Markup);

					infoBox.PackStart (linkLabel);
				}
			}

			infoBox.PackStart (new Xwt.Label () {
				Markup = string.Format ("<b>{0}</b>", GettextCatalog.GetString ("Copyright")),
				MarginTop = 6,
			});

			infoBox.PackStart (new Xwt.Label () {
				Text = (DateTime.Now.Year == 2016 ? "© 2016" : "© 2016–" + DateTime.Now.Year) + " Microsoft Corp.",
				MarginLeft = 12
			});
			infoBox.PackStart (new Xwt.Label () {
				Text = "© 2004–" + DateTime.Now.Year + " Xamarin Inc.",
				MarginLeft = 12
			});
			infoBox.PackStart (new Xwt.Label () {
				Text = "© 2004–" + DateTime.Now.Year + " MonoDevelop contributors",
				MarginLeft = 12
			});

			this.ShowAll ();
		}
	}
}
