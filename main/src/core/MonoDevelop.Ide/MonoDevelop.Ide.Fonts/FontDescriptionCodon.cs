// 
// FontDescriptionCodon.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;

namespace MonoDevelop.Ide.Fonts
{
	[ExtensionNode (Description="A template for color and syntax shemes.")]
	public class FontDescriptionCodon : ExtensionNode
	{
		[NodeAttribute("name", "Name of the font.")]
		public string Name { get; private set; }

		[NodeAttribute("_displayName", "Name of the font displayed to the user.", Localizable=true)]
		public string DisplayName { get; private set; }

		//these fields are assigned by reflection, suppress "never assigned" warning
		#pragma warning disable 649

		[NodeAttribute("default", "Default font to use.")]
		string fontDescription;

		[NodeAttribute("defaultMac", "Default mac font to use.")]
		string fontDescriptionMac;

		[NodeAttribute("defaultMacYosemite", "Default Mac font to use for OSX 10.10 or later.")]
		string fontDescriptionMacYosemite;

		[NodeAttribute("defaultWindows", "Default windows font to use.")]
		string fontDescriptionWindows;

		#pragma warning restore 649

		public string FontDescription {
			get {
				if (Core.Platform.IsWindows)
					return string.IsNullOrEmpty (fontDescriptionWindows) ? fontDescription : fontDescriptionWindows;
				if (Core.Platform.IsMac) {
					if (Core.Platform.OSVersion >= Core.MacSystemInformation.Yosemite) {
						if (!string.IsNullOrEmpty (fontDescriptionMacYosemite)) {
							return fontDescriptionMacYosemite;
						}
					}

					return string.IsNullOrEmpty (fontDescriptionMac) ? fontDescription : fontDescriptionMac;
				}
				return fontDescription;
			}
		}


	}
}

