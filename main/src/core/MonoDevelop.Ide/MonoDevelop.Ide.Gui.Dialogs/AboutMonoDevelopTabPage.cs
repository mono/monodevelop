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

			using (var stream = BrandingService.GetStream ("SplashScreen.png", true))
				imageSep = new Pixbuf (stream);
			PackStart (new Gtk.Image (imageSep), false, false, 0);

			VBox infoBox = new VBox (false, 6);
			infoBox.BorderWidth = 12;
			PackStart (infoBox, false, false, 0);

			var label = new Label () {
				Xalign = 0,
				Markup = string.Format (
					"<b>{0}</b>\n    {1}", 
					GettextCatalog.GetString ("Version"), 
					BuildVariables.PackageVersion == BuildVariables.PackageVersionLabel ? BuildVariables.PackageVersionLabel : String.Format ("{0} ({1})", 
				                                                                                                                          BuildVariables.PackageVersionLabel, 
				                                                                                                                          BuildVariables.PackageVersion))
			};
			infoBox.PackStart (label, false, false, 0);
			label = new Label () {
				Xalign = 0,
				Markup = "<b>" + GettextCatalog.GetString ("License") + "</b>\n    " + GettextCatalog.GetString ("Released under the GNU Lesser General Public License.")
			};
			infoBox.PackStart (label, false, false, 0);

			label = new Label () {
				Xalign = 0,
				Markup = string.Format ("<b>Copyright</b>\n    (c) 2004-{0} by MonoDevelop contributors", DateTime.Now.Year)
			};
			infoBox.PackStart (label, false, false, 0);

			this.ShowAll ();
		}
	}
}