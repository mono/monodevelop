//
// SemanticRule.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Mono.TextEditor
{
	public abstract class SemanticRule
	{
		public abstract void Analyze (Document doc, LineSegment line, List<Chunk> chunks, int startOffset, int endOffset);
	}
	
	public class HighlightUrlSemanticRule : SemanticRule
	{
		const string urlRegexStr = "https?://"
				+ "?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?" //user@
				+ @"(([0-9]{1,3}\.){3}[0-9]{1,3}" // IP- 199.194.52.184
				+ "|" // allows either IP or domain
				+ @"([0-9a-z_!~*'()-]+\.)*" // tertiary domain(s)- www.
				+ @"([0-9a-z][0-9a-z-]{0,61})?[0-9a-z]\." // second level domain
				+ "[a-z]{2,6})" // first level domain- .com or .museum
				+ "(:[0-9]{1,4})?" // port number- :80
				+ "((/?)|" // a slash isn't required if there is no file name
				+ "(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)"; 
		
		static Regex urlRegex = new Regex (urlRegexStr, RegexOptions.Compiled);
		static Regex mailRegex = new Regex (@"[\w\d._%+-]+@[\w\d.-]+\.\w{2,4}", RegexOptions.Compiled);
		string syntax;
		
		public HighlightUrlSemanticRule (string syntax)
		{
			this.syntax = syntax;
		}
		
		public override void Analyze (Document doc, LineSegment line, List<Chunk> chunks, int startOffset, int endOffset)
		{
			string text = doc.Buffer.GetTextAt (startOffset, endOffset - startOffset);
			int startColumn = startOffset - line.Offset;
			line.RemoveMarker (typeof(UrlMarker));
			foreach (System.Text.RegularExpressions.Match m in urlRegex.Matches (text)) {
				line.AddMarker (new UrlMarker (line, m.Value, syntax, startColumn + m.Index, startColumn + m.Index + m.Length));
			}
			foreach (System.Text.RegularExpressions.Match m in mailRegex.Matches (text)) {
				line.AddMarker (new UrlMarker (line, m.Value, syntax, startColumn + m.Index, startColumn + m.Index + m.Length));
			}
		}
		
	}
}
