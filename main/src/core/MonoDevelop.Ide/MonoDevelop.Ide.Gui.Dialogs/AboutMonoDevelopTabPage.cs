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
using System.Text;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

using Gdk;
using Gtk;
using GLib;
using Pango;
using System.Reflection;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	class AboutMonoDevelopTabPage: VBox
	{
		Pixbuf imageSep;

		public AboutMonoDevelopTabPage ()
		{
			BorderWidth = 0;

			using (var stream = BrandingService.GetStream ("AboutImage.png", true))
				imageSep = new Pixbuf (stream);
			PackStart (new Gtk.Image (imageSep), false, false, 0);

			Xwt.VBox infoBox = new Xwt.VBox ();
			infoBox.Spacing = 6;
			infoBox.Margin = 12;
			PackStart (infoBox.ToGtkWidget (), false, false, 0);

			infoBox.PackStart (new Xwt.Label () {
				Text = GettextCatalog.GetString ("Version"),
				Font = infoBox.Font.WithWeight (Xwt.Drawing.FontWeight.Bold)
			});
			infoBox.PackStart (new Xwt.Label () {
				Text = IdeVersionInfo.MonoDevelopVersion,
				MarginLeft = 12
			});

			infoBox.PackStart (new Xwt.Label () {
				Text = GettextCatalog.GetString ("License"),
				Font = infoBox.Font.WithWeight (Xwt.Drawing.FontWeight.Bold)
			});
			infoBox.PackStart (new Xwt.Label () {
				Text = GettextCatalog.GetString ("Released under the GNU Lesser General Public License."),
				MarginLeft = 12
			});

			infoBox.PackStart (new Xwt.Label () {
				Text = GettextCatalog.GetString ("Copyright"),
				Font = infoBox.Font.WithWeight (Xwt.Drawing.FontWeight.Bold)
			});
			var cbox = new Xwt.HBox () {
				Spacing = 0,
				MarginLeft = 12
			};
			cbox.PackStart (new Xwt.Label ("© 2011-2012 "));
			cbox.PackStart (new Xwt.LinkLabel () {
				Text = string.Format ("Xamarin Inc."),
				Uri = new Uri ("http://www.xamarin.com")
			});
			infoBox.PackStart (cbox);
			infoBox.PackStart (new Xwt.Label () {
				Text = "© 2004-2012 MonoDevelop contributors",
				MarginLeft = 12
			});

			cbox = new Xwt.HBox () {
				Spacing = 0,
				MarginLeft = 12
			};
			cbox.PackStart (new Xwt.Label ("Some icons by "));
			cbox.PackStart (new Xwt.LinkLabel () {
				Text = string.Format ("Yusuke Kamiyamane"),
				Uri = new Uri ("http://p.yusukekamiyamane.com")
			});
			infoBox.PackStart (cbox);

			cbox = new Xwt.HBox () {
				Spacing = 0,
				MarginLeft = 12
			};
			cbox.PackStart (new Xwt.Label ("Some icons from the "));
			cbox.PackStart (new Xwt.LinkLabel () {
				Text = string.Format ("Silk icon set"),
				Uri = new Uri ("http://www.famfamfam.com/")
			});
			infoBox.PackStart (cbox);

			this.ShowAll ();
		}
	}
}