// 
// WelcomePageBranding.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using System.Xml.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.WelcomePage
{
	static class WelcomePageBranding
	{
		public static readonly XElement Content;
		public static readonly string HeaderTextSize, HeaderTextColor, BackgroundColor, TextColor, TextSize, LinkColor;
		public static readonly uint Spacing, LogoHeight;
		
		static WelcomePageBranding ()
		{
			try {
				var streams = new Func<System.IO.Stream> [] {
					() => typeof (WelcomePageBranding).Assembly.GetManifestResourceStream ("WelcomePage.xml"),
					() => BrandingService.OpenStream ("WelcomePage.xml"),
				};
				foreach (var sf in streams) {
					using (var s = sf ()) {
						var doc = XDocument.Load (s);
						foreach (var el in doc.Root.Elements ()) {
							switch (el.Name.ToString ()) {
							case "HeaderTextSize":
								HeaderTextSize = (string) el;
								break;
							case "HeaderTextColor":
								HeaderTextColor = (string) el;
								break;
							case "BackgroundColor":
								BackgroundColor = (string) el;
								break;
							case "TextColor":
								TextColor = (string) el;
								break;
							case "TextSize":
								TextSize = (string) el;
								break;
							case "LinkColor":
								LinkColor = (string) el;
								break;
							case "Spacing":
								Spacing = (uint) el;
								break;
							case "LogoHeight":
								LogoHeight = (uint) el;
								break;
							case "Content":
								Content = el;
								break;
							}
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading welcome page data from branding xml.", e);
			}
		}
		
		public static Gdk.Pixbuf GetLogoImage ()
		{
			using (var stream = BrandingService.OpenStream ("WelcomePage_Logo.png"))
				return new Gdk.Pixbuf (stream);
		}
		
		public static Gdk.Pixbuf GetTopBorderImage ()
		{
			using (var stream = BrandingService.OpenStream ("WelcomePage_TopBorderRepeat.png"))
				return new Gdk.Pixbuf (stream);
		}
	}
}