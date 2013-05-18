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
		string name;
		public string Name {
			get {
				return this.name;
			}
		}
		
		[NodeAttribute("_displayName", "Name of the font displayed to the user.")]
		string displayName;
		public string DisplayName {
			get {
				return this.displayName;
			}
		}
		
		[NodeAttribute("default", "Default font to use.")]
		string fontDescription;

		[NodeAttribute("defaultMac", "Default mac font to use.")]
		string fontDescriptionMac;

		[NodeAttribute("defaultWindows", "Default windows font to use.")]
		string fontDescriptionWindows;

		public string FontDescription {
			get {
				if (MonoDevelop.Core.Platform.IsWindows)
					return string.IsNullOrEmpty (fontDescriptionWindows) ? fontDescription : fontDescriptionWindows;
				if (MonoDevelop.Core.Platform.IsMac)
					return string.IsNullOrEmpty (fontDescriptionMac) ? fontDescription : fontDescriptionMac;
				return fontDescription;
			}
		}


	}
}

