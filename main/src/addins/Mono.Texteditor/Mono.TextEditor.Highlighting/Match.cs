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
using System.Xml;

namespace Mono.TextEditor
{
	public class Match
	{
		string color;
		string pattern;
		
		public string Color {
			get {
				return color;
			}
		}

		public string Pattern {
			get {
				return pattern;
			}
		}
		
		public System.Text.RegularExpressions.Regex Regex {
			get {
				return new System.Text.RegularExpressions.Regex (pattern, System.Text.RegularExpressions.RegexOptions.Compiled);
			}
		}
		
		public override string ToString ()
		{
			return String.Format ("[Match: Color={0}, Pattern={1}]", color, pattern);
		}

		
		public const string Node = "Match";
		public static Match Read (XmlReader reader)
		{
			Match result = new Match ();
			result.color = reader.GetAttribute ("color");
			result.pattern = reader.ReadElementString ();
			return result;
		}
	}
}
