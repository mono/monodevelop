// Match.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text.RegularExpressions;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public class Match
	{
		System.Text.RegularExpressions.Regex  regex;
		public string Color {
			get;
			private set;
		}

		public string Pattern {
			get;
			private set;
		}
		
		public System.Text.RegularExpressions.Regex Regex {
			get {
				return regex;
			}
		}
		
		public virtual bool GetIsValid (Style style)
		{
			return style.GetChunkStyle (Color) != null;
		}
		
		public override string ToString ()
		{
			return String.Format ("[Match: Color={0}, Pattern={1}]", Color, Pattern);
		}

		public virtual int TryMatch (string text, int matchOffset)
		{
			
//			System.Text.RegularExpressions.Match match = regex.Match (text, matchOffset);
			string matchStr = text.Substring (matchOffset);
			System.Text.RegularExpressions.Match match = regex.Match (matchStr);
			
			if (match.Success)
				return match.Length;
			return -1;
		}
		
		public const string Node = "Match";
		public static Match Read (XmlReader reader)
		{
			string color   = reader.GetAttribute ("color");
			string pattern = reader.ReadElementString ();
			Match result   = pattern == "CSharpNumber" ? new CSharpNumberMatch () : new Match ();
			result.Color   = color;
			result.Pattern = "^" + pattern;
			result.regex   = new System.Text.RegularExpressions.Regex (result.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
			return result;
		}
	}
	
	public class CSharpNumberMatch : Match
	{
		static bool ReadNonFloatEnd (string text, ref int i)
		{
			if (i >= text.Length)
				return false;
			char ch = Char.ToUpper (text[i]);
			if (ch == 'L') {
				i++;
				if (i < text.Length && Char.ToUpper (text[i]) == 'U')
					i++;
				return true;
			} else if (ch == 'U') {
				i++;
				if (i < text.Length && Char.ToUpper (text[i]) == 'L')
					i++;
				return true;
			}
			return false;
		}
		
		static bool ReadFloatEnd (string text, ref int i)
		{
			if (i >= text.Length)
				return false;
			char ch = Char.ToUpper (text[i]);
			if (ch == 'F' || ch == 'M' || ch == 'D') {
				i++;
				return true;
			}
			return false;
		}
		
		public override bool GetIsValid (Style style)
		{
			return true;
		}
		
		public override int TryMatch (string text, int matchOffset)
		{
			int i = matchOffset;
			if (matchOffset + 1 < text.Length && text[matchOffset] == '0' && Char.ToUpper (text[matchOffset + 1]) == 'X') {
				i += 2; // skip 0x
				while (i < text.Length) {
					char ch = Char.ToUpper (text[i]);
					if (!(Char.IsDigit (ch) || ('A' <= ch && ch <= 'F')))
						break;
					i++;
				}
				ReadNonFloatEnd (text, ref i);
				return i - matchOffset;
			} else {
				if (i >= text.Length || !Char.IsDigit (text[i]))
					return -1;
				i++;
				while (i < text.Length && Char.IsDigit (text[i]))
					i++;
			}
			if (ReadNonFloatEnd (text, ref i))
				return i - matchOffset;
			if (i < text.Length && text[i] == '.') {
				i++;
				if (i >= text.Length) 
					return (i - 1) - matchOffset;
				if (!Char.IsDigit (text[i]))
					return -1;
				i++;
				while (i < text.Length && Char.IsDigit (text[i]))
					i++;
			}
			if (i < text.Length && Char.ToUpper (text[i]) == 'E') {
				i++;
				if (i < text.Length && (text[i] == '-' || text[i] == '+'))
					i++;
				while (i < text.Length && Char.IsDigit (text[i]))
					i++;
			}
			ReadFloatEnd (text, ref i);
			return i - matchOffset;
		}
	}
	
}
