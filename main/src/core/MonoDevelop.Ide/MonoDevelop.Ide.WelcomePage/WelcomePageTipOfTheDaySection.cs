//
// WelcomePageTipOfTheDaySection.cs
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
using System.Xml;
using System.Collections.Generic;
using MonoDevelop.Ide.Fonts;

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageTipOfTheDaySection: WelcomePageSection
	{
		private List<string> tips = new List<string> ();
		private int currentTip;
		Gtk.Label label;

		public WelcomePageTipOfTheDaySection (): base (GettextCatalog.GetString ("Did you know?"))
		{
			XmlDocument xmlDocument = new XmlDocument ();
			xmlDocument.Load (System.IO.Path.Combine (System.IO.Path.Combine (PropertyService.DataPath, "options"), "TipsOfTheDay.xml"));

			foreach (XmlNode xmlNode in xmlDocument.DocumentElement.ChildNodes) {
				tips.Add (StringParserService.Parse (xmlNode.InnerText));
			}
			
			if (tips.Count != 0)  
				currentTip = new Random ().Next () % tips.Count;
			else
				currentTip = -1;

			Gtk.VBox box = new Gtk.VBox (false, 12);
			box.Accessible.SetShouldIgnore (true);

			label = new Gtk.Label ();

			label.Accessible.Name = "WelcomePage.TipOfTheDay.TipLabel";
			label.Accessible.Description = "A tip for using MonoDevelop";

			label.Xalign = 0;
			label.Wrap = true;
			label.WidthRequest = 200;
			label.ModifyFont (FontService.SansFont.CopyModified (Gui.Styles.FontScale11));
			label.SetPadding (0, 10);

			label.Text = currentTip != -1 ? tips[currentTip] : "";
			box.PackStart (label, true, true, 0);

			var next = new Gtk.Button (GettextCatalog.GetString ("Next Tip"));
			next.Accessible.Name = "WelcomePage.TipOfTheDay.NextButton";
			next.Accessible.Description = "Show the next tip";

			next.Relief = Gtk.ReliefStyle.Normal;
			next.Clicked += delegate {
				if (tips.Count == 0)
					return;
				currentTip = currentTip + 1;
				if (currentTip >= tips.Count)
					currentTip = 0;
				label.Text = tips[currentTip];
			};

			var al = new Gtk.Alignment (0, 0, 0, 0);
			al.Accessible.SetShouldIgnore (true);
			al.Add (next);
			box.PackStart (al, false, false, 0);
			SetContent (box);

			SetTitledWidget (label);
			SetTitledWidget (next);
		}
	}
}

