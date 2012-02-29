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
		public static readonly XDocument Content;
		
		public static readonly string HeaderTextSize = "x-large";
		public static readonly string HeaderTextColor = "#4e6d9f";
		public static readonly string BackgroundColor = "white";
		public static readonly string TextColor = "black";
		public static readonly string TextSize = "medium";
		public static readonly string LinkColor = "#5a7ac7";
		public static readonly int Spacing = 20;
		public static readonly int LogoHeight = 90;
		
		static WelcomePageBranding ()
		{
			try {
				using (var stream = BrandingService.GetStream ("WelcomePageContent.xml")) {
					if (stream != null) {
						Content = XDocument.Load (stream);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error while reading welcome page contents.", ex);
			}
			if (Content == null) {
				using (var stream = typeof (WelcomePageBranding).Assembly.GetManifestResourceStream ("WelcomePageContent.xml")) {
					Content = XDocument.Load (stream);
				}
			}
			
			try {
				HeaderTextSize = BrandingService.GetString ("WelcomePage", "HeaderTextSize") ?? HeaderTextSize;
				HeaderTextColor = BrandingService.GetString ("WelcomePage", "HeaderTextColor") ?? HeaderTextColor;
				BackgroundColor = BrandingService.GetString ("WelcomePage", "BackgroundColor") ?? BackgroundColor;
				TextColor = BrandingService.GetString ("WelcomePage", "TextColor") ?? TextColor;
				TextSize = BrandingService.GetString ("WelcomePage", "TextSize") ?? TextSize;
				LinkColor = BrandingService.GetString ("WelcomePage", "LinkColor") ?? LinkColor;
				Spacing = BrandingService.GetInt ("WelcomePage", "Spacing") ?? Spacing;
				LogoHeight = BrandingService.GetInt ("WelcomePage", "LogoHeight") ?? LogoHeight;
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading welcome page branding.", e);
			}
		}
		
		public static Gdk.Pixbuf GetLogoImage ()
		{
			using (var stream = BrandingService.GetStream ("WelcomePage_Logo.png", true))
				return new Gdk.Pixbuf (stream);
		}
		
		public static Gdk.Pixbuf GetTopBorderImage ()
		{
			using (var stream = BrandingService.GetStream ("WelcomePage_TopBorderRepeat.png", true))
				return new Gdk.Pixbuf (stream);
		}
	}
}