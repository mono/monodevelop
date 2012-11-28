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
using System.Linq;

namespace Mono.TextEditor
{
	public abstract class SemanticRule
	{
		public abstract void Analyze (TextDocument doc, DocumentLine line, Chunk startChunk, int startOffset, int endOffset);
	}
	
	public class HighlightUrlSemanticRule : SemanticRule
	{
		const string urlRegexStr = @"(http|ftp)s?\:\/\/[\w\d\._/\-~%@()+:?&=#!]*[\w\d/]";
		
		public static readonly Regex UrlRegex  = new Regex (urlRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		public static readonly Regex MailRegex = new Regex (@"[\w\d._%+-]+@[\w\d.-]+\.\w{2,4}", RegexOptions.Compiled);
		string syntax;
		
		public HighlightUrlSemanticRule (string syntax)
		{
			this.syntax = syntax;
		}
		
		bool inUpdate = false;
		public override void Analyze (TextDocument doc, DocumentLine line, Chunk startChunk, int startOffset, int endOffset)
		{
			if (endOffset <= startOffset || startOffset >= doc.TextLength || inUpdate)
				return;
			inUpdate = true;
			try {
				string text = doc.GetTextAt (startOffset, endOffset - startOffset);
				int startColumn = startOffset - line.Offset;
				var markers = new List <UrlMarker> (line.Markers.Where (m => m is UrlMarker).Cast<UrlMarker> ());
				markers.ForEach (m => doc.RemoveMarker (m, false));
				foreach (System.Text.RegularExpressions.Match m in UrlRegex.Matches (text)) {
					doc.AddMarker (line, new UrlMarker (doc, line, m.Value, UrlType.Url, syntax, startColumn + m.Index, startColumn + m.Index + m.Length), false);
				}
				foreach (System.Text.RegularExpressions.Match m in MailRegex.Matches (text)) {
					doc.AddMarker (line, new UrlMarker (doc, line, m.Value, UrlType.Email, syntax, startColumn + m.Index, startColumn + m.Index + m.Length), false);
				}
			} finally {
				inUpdate = false;
			}
		}
		
	}
}
