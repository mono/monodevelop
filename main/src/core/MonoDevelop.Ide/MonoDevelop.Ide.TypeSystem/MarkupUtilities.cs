// 
// DocumentationService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	internal static class MarkupUtilities
	{
		static string EscapedLessThan = "&lt;";
		static string EscapedGreaterThan = "&gt;";
		static string EscapedAmpersand = "&amp;";
		static string EscapedApostrophe = "&apos;";
		static string EscapedQuote = "&quot;";

		public static void AppendEscapedString (StringBuilder builder, string toEscape, int start, int count)
		{
			if (toEscape == null)
				return;
			
			for (int i = 0; i < count; i++) {
				char c = toEscape[start + i];
				switch (c) {
				case '<':
					builder.Append (EscapedLessThan);
					break;
				case '>':
					builder.Append (EscapedGreaterThan);
					break;
				case '&':
					builder.Append (EscapedAmpersand);
					break;
				case '\'':
					builder.Append (EscapedApostrophe);
					break;
				case '"':
					builder.Append (EscapedQuote);
					break;
				default:
					builder.Append (c);
					break;
				}
			}
		}

		public static string UnescapeString (string text)
		{
			if (string.IsNullOrEmpty (text))
				return text;
			return text.Replace (EscapedLessThan, "<")
				       .Replace (EscapedGreaterThan, ">")
				       .Replace (EscapedAmpersand, "&")
				       .Replace (EscapedApostrophe, "'")
				       .Replace (EscapedQuote, "\"");
		}
	}
}
