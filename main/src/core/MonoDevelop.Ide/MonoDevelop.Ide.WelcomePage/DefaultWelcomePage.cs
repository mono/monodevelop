//
// DefaultWelcomePage.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using Gtk;

namespace MonoDevelop.Ide.WelcomePage
{
	public class DefaultWelcomePage: WelcomePageWidget
	{
		protected override void BuildContent (Container parent)
		{
			LogoImage = Gdk.Pixbuf.LoadFromResource ("WelcomePage_Logo.png");
			TopBorderImage = Gdk.Pixbuf.LoadFromResource ("WelcomePage_TopBorderRepeat.png");

			var mainAlignment = new Gtk.Alignment (0.5f, 0.5f, 0f, 1f);

			var mainCol = new WelcomePageColumn ();
			mainAlignment.Add (mainCol);

			var row1 = new WelcomePageRow ();
			row1.PackStart (new WelcomePageButtonBar (
				new WelcomePageBarButton ("MonoDevelop.com", "http://www.monodevelop.com", "link-cloud.png"),
				new WelcomePageBarButton (GettextCatalog.GetString ("Documentation"), "http://www.go-mono.com/docs", "link-info.png"),
				new WelcomePageBarButton (GettextCatalog.GetString ("Support"), "http://monodevelop.com/index.php?title=Help_%26_Contact", "link-heart.png"),
				new WelcomePageBarButton (GettextCatalog.GetString ("Q&A"), "http://stackoverflow.com/questions/tagged/monodevelop", "link-chat.png")
				)
			);
			mainCol.PackStart (row1, false, false, 0);

			var row2 = new WelcomePageRow (
				new WelcomePageColumn (
				new WelcomePageRecentProjectsList (GettextCatalog.GetString ("Solutions"))
				),
				new WelcomePageColumn (
					new WelcomePageNewsFeed (GettextCatalog.GetString ("Xamarin News"), "http://software.xamarin.com/Service/News", "NewsLinks")
				),
				new WelcomePageColumn (
					new WelcomePageTipOfTheDaySection ()
				)
			);
			mainCol.PackStart (row2, false, false, 0);

			parent.Add (mainAlignment);
		}
	}
}

