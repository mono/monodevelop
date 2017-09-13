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
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Gui.Content
{
	public enum EolMarker {
		Native = 0, // Environment.NewLine
		Mac = 1,    // '\r'
		Unix = 2,   // '\n'
		Windows = 3 // '\r\n'
	}
	
	[PolicyType ("Text file formatting")]
	public sealed class TextStylePolicy : IEquatable<TextStylePolicy>
	{
		public TextStylePolicy (int fileWidth, int tabWidth, int indentWidth, bool tabsToSpaces, bool noTabsAfterNonTabs, bool removeTrailingWhitespace, EolMarker eolMarker)
		{
			FileWidth = fileWidth;
			TabWidth = tabWidth;
			IndentWidth = indentWidth;
			TabsToSpaces = tabsToSpaces;
			NoTabsAfterNonTabs = noTabsAfterNonTabs;
			RemoveTrailingWhitespace = removeTrailingWhitespace;
			EolMarker = eolMarker;
		}
		
		public TextStylePolicy()
		{
			FileWidth = 120;
			TabWidth = 4;
			IndentWidth = 4;
			RemoveTrailingWhitespace = true;
		}

		public TextStylePolicy(TextStylePolicy other)
		{
			FileWidth = other.FileWidth;
			TabWidth = other.TabWidth;
			IndentWidth = other.IndentWidth;
			TabsToSpaces = other.TabsToSpaces;
			NoTabsAfterNonTabs = other.NoTabsAfterNonTabs;
			RemoveTrailingWhitespace = other.RemoveTrailingWhitespace;
			EolMarker = other.EolMarker;
		}

		[ItemProperty]
		public int FileWidth { get; private set; }
		
		[ItemProperty]
		public int TabWidth { get; private set; }
		
		[ItemProperty]
		public bool TabsToSpaces { get; private set; }
		
		[ItemProperty]
		public int IndentWidth { get; private set; }
		
		[ItemProperty]
		public bool RemoveTrailingWhitespace { get; private set; }
		
		[ItemProperty]
		public bool NoTabsAfterNonTabs { get; private set; }
		
		[ItemProperty]
		public EolMarker EolMarker { get; private set; }

		public TextStylePolicy WithTabsToSpaces(bool tabToSpaces)
		{
			if (tabToSpaces == TabsToSpaces)
				return this;
			return new TextStylePolicy(this) {
				TabsToSpaces = tabToSpaces
			};
		}

		public TextStylePolicy WithTabWidth(int tabWidth)
		{
			if (tabWidth == TabWidth)
				return this;
			return new TextStylePolicy(this) {
				TabWidth = tabWidth
			};
		}

		public static string GetEolMarker (EolMarker eolMarker)
		{
			switch (eolMarker) {
			case EolMarker.Mac:
				return "\r";
			case EolMarker.Unix:
				return "\n";
			case EolMarker.Windows:
				return "\r\n";
			}
			return Environment.NewLine;
		}

	
		public string GetEolMarker ()
		{
			return GetEolMarker (EolMarker);
		}

	
		public bool Equals (TextStylePolicy other)
		{
			return other != null && other.FileWidth == FileWidth && other.TabWidth == TabWidth
				&& other.TabsToSpaces == TabsToSpaces && other.NoTabsAfterNonTabs == NoTabsAfterNonTabs
				&& other.RemoveTrailingWhitespace == RemoveTrailingWhitespace && other.EolMarker == EolMarker && other.IndentWidth == IndentWidth;
		}
	}
}