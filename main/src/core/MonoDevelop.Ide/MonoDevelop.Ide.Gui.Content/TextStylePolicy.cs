//
// TextStylePolicy.cs
//
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Ide.Gui.Content
{
	public enum EolMarker {
		Default, // Environment.NewLine
		Mac,     // '\r'
		Unix,    // '\n'
		Windows  // '\r\n'
	}
	
	public sealed class TextStylePolicy : IEquatable<TextStylePolicy>
	{
		public TextStylePolicy (int fileWidth, int tabWidth, bool tabsToSpaces, bool noTabsAfterNonTabs, bool removeTrailingWhitespace)
		{
			FileWidth = fileWidth;
			TabWidth = tabWidth;
			TabsToSpaces = tabsToSpaces;
			NoTabsAfterNonTabs = noTabsAfterNonTabs;
			RemoveTrailingWhitespace = removeTrailingWhitespace;
			EolMarker = EolMarker.Default;
		}
		
		public TextStylePolicy ()
		{
			FileWidth = 120;
			TabWidth = 4;
		}
		
		[ItemProperty]
		public int FileWidth { get; private set; }
		
		[ItemProperty]
		public int TabWidth { get; private set; }
		
		[ItemProperty]
		public bool TabsToSpaces { get; private set; }
		
		[ItemProperty]
		public bool NoTabsAfterNonTabs { get; private set; }
		
		[ItemProperty]
		public bool RemoveTrailingWhitespace { get; private set; }
		
		[ItemProperty]
		public EolMarker EolMarker { get; private set; }
		
		public string GetEolMarker ()
		{
			switch (EolMarker) {
			case EolMarker.Mac:
				return "\r";
			case EolMarker.Unix:
				return "\n";
			case EolMarker.Windows:
				return "\r\n";
			}
			return Environment.NewLine;
		}
		
		public bool Equals (TextStylePolicy other)
		{
			return other != null && other.FileWidth == FileWidth && other.TabWidth == TabWidth
				&& other.TabsToSpaces == TabsToSpaces && other.NoTabsAfterNonTabs == NoTabsAfterNonTabs
				&& other.RemoveTrailingWhitespace == RemoveTrailingWhitespace;
		}
	}
}
