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
		const string urlRegexStr = @"(http|ftp)s?\:\/\/(([\d]{1,3}\.[\d]{1,3}\.[\d]{1,3}\.[\d]{1,3})|([\w\-]+\.)+(((af|ax|al|dz|as|ad|ao|ai|aq|ag|am|aw|au|at|az|bs|bh|bd|bb|by|be|bz|bj|bm|bt|bo|ba|bw|bv|br|io|bn|bg|bf|kh|cm|ca|cv|ky|cf|td|cl|cn|cx|cc|km|cg|cd|ck|cr|ci|hr|cu|cy|cz|dk|dj|dm|do|ec|eg|sv|gq|er|ee|et|fk|fo|fj|fi|fr|gf|pf|tf|ga|gm|ge|de|gh|gi|gr|gl|gd|gp|gu|gt| gg|gn|gw|gy|ht|hm|va|hn|hk|hu|is|id|ir|iq|ie|im|il|it|jm|jp|je|jo|kz|ke|ki|kp|kr|kw|kg|la|lv|lb|ls|lr|ly|li|lt|lu|mo|mk|mg|mw|my|mv|ml|mt|mh|mq|mr|yt|mx|fm|md|mc|mn|ms|ma|mz|mm|nr|np|nl|an|nc|nz|ni|ng|nu|nf|mp|no|om|pk|pw|ps|pa|pg|py|pe|ph|pn|pl|pt|qa|re|ro|ru|rw|sh|kn|lc|pm|vc|ws|sm|st|sa|sn|cs|sc|sl|sg|sk|si|sb|so|za|gs|es|lk|sd|sr|sj|sz|se|ch|sy|tw|tj|tz|th|tl|tg|tk|to|tt|tn|tr|tm|tc|tv|ug|ua|gb|us|um|uy|uz|vu|ve|vn|vg|vi|wf|eh|ye|zm|zw|uk|com|edu|gov|int|mil|net|org|biz|info|name|pro|aero|coop|museum|arpa|co|in|ne|bi|na|pr|ae|mu|ar))))(:[\d]{1,4})?($|(\/([a-zA-Z0-9_\.\?=/#%&\+-])*)*|\/)";
		
		static Regex urlRegex = new Regex (urlRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static Regex mailRegex = new Regex (@"[\w\d._%+-]+@[\w\d.-]+\.\w{2,4}", RegexOptions.Compiled);
		string syntax;
		
		public HighlightUrlSemanticRule (string syntax)
		{
			this.syntax = syntax;
		}
		
		public override void Analyze (Document doc, LineSegment line, List<Chunk> chunks, int startOffset, int endOffset)
		{
			string text = doc.GetTextAt (startOffset, endOffset - startOffset);
			int startColumn = startOffset - line.Offset;
			line.RemoveMarker (typeof(UrlMarker));
			foreach (System.Text.RegularExpressions.Match m in urlRegex.Matches (text)) {
				line.AddMarker (new UrlMarker (line, m.Value, UrlType.Url, syntax, startColumn + m.Index, startColumn + m.Index + m.Length));
			}
			foreach (System.Text.RegularExpressions.Match m in mailRegex.Matches (text)) {
				line.AddMarker (new UrlMarker (line, m.Value, UrlType.Email, syntax, startColumn + m.Index, startColumn + m.Index + m.Length));
			}
		}
		
	}
}
