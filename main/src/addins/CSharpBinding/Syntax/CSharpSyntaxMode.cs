// 
// SyntaxMode.cs
//  
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor.Highlighting;
using Mono.TextEditor;
using System.Xml;




namespace MonoDevelop.CSharpBinding
{
	
	
	public class CSharpSyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode
	{
		
		public CSharpSyntaxMode ()
		{
			ResourceXmlProvider provider = new ResourceXmlProvider (typeof (IXmlProvider).Assembly, "CSharpSyntaxMode.xml");
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = new List<Span> (baseMode.Spans);
				this.matches = baseMode.Matches;
				this.prevMarker = new List<Marker> (baseMode.PrevMarker);
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.table = baseMode.Table;
			}
		}
		
		public override Chunk GetChunks (Mono.TextEditor.Document doc, Mono.TextEditor.Highlighting.Style style, Mono.TextEditor.LineSegment line, int offset, int length)
		{
			return new CSharpChunkParser (doc, style, this, line).GetChunks (offset, length);
		}
		
		protected class CSharpChunkParser : ChunkParser
		{
			public CSharpChunkParser (Document doc, Style style, SyntaxMode mode, LineSegment line) : base (doc, style, mode, line)
			{
			}
		}
	}
}
