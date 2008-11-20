// MarkupSyntaxMode.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;

namespace Mono.TextEditor.Highlighting
{
	public class MarkupSyntaxMode : SyntaxMode
	{
		class Tag
		{
			public string Command {
				get;
				set;
			}
			public Dictionary<string, string> Arguments {
				get;
				private set;
			}
			
			public Tag ()
			{
				Arguments = new Dictionary<string, string> ();
			}
			
			public static Tag Parse (string text)
			{
				return new Tag () {
					Command = text
				};
			}
		}

		class TextChunk : Chunk
		{
			string text;
			
			public TextChunk (ChunkStyle style, string text)
			{
				this.text = text;
				this.Offset = 0;
				this.Length = text.Length;
				this.Style = style;
			}
			
			public override char GetCharAt (Document doc, int offset)
			{
				return text [offset];
			}	
		}
			
		static ChunkStyle GetChunkStyle (IEnumerable<Tag> tagStack)
		{
			ChunkStyle result = new ChunkStyle ();
			
			foreach (Tag tag in tagStack) {
				//System.Console.WriteLine("'" + tag.Command + "'");
				switch (tag.Command) {
				case "B":
				case "b":
					result.Bold = true;
					break;
				case "I":
				case "i":
					result.Italic = true;
					break;
				case "U":
				case "u":
					result.Underline = true;
					break;
				}
			}
			return result;
		}
		
		public override Chunk[] GetChunks (Document doc, Style style, LineSegment line, int offset, int length)
		{
			int endOffset = System.Math.Min (offset + length, doc.Length);
			List<Chunk> result = new List<Chunk> ();
			Stack<Tag> tagStack = new Stack<Tag> ();
			Chunk curChunk = new Chunk (offset, 0, new ChunkStyle ());
			bool inTag = true, inSpecial = false;
			int tagBegin = -1, specialBegin = -1;
			for (int i = offset; i < endOffset; i++) {
				char ch = doc.GetCharAt (i);
				switch (ch) {
				case '<':
					curChunk.Length = i - curChunk.Offset;
					if (curChunk.Length > 0) {
						curChunk.Style = GetChunkStyle (tagStack);
						result.Add (curChunk);
						curChunk = new Chunk (i, 0, null);
					}
					tagBegin = i;
					inTag = true;
					break;
				case '&':
					inSpecial = true;
					specialBegin = i;
					break;
				case ';':
					if (inSpecial) {
						string specialText = doc.GetTextBetween (specialBegin + 1, i);
						curChunk.Length = specialBegin - curChunk.Offset;
						if (curChunk.Length > 0) {
							curChunk.Style = GetChunkStyle (tagStack);
							result.Add (curChunk);
							curChunk = new Chunk (i, 0, null);
						}
						switch (specialText) {
						case "lt": 
							result.Add (new TextChunk (GetChunkStyle (tagStack), "<"));
							break;
						case "gt": 
							result.Add (new TextChunk (GetChunkStyle (tagStack), ">"));
							break;
						case "amp": 
							result.Add (new TextChunk (GetChunkStyle (tagStack), "&"));
							break;
						}
						curChunk.Offset = i + 1;
						inSpecial = false;
					}
					break;
				case '>':
					if (!inTag)
						break;
					string tagText = doc.GetTextBetween (tagBegin + 1, i);
					if (tagText.StartsWith ("/")) {
						tagStack.Pop ();
					} else {
						tagStack.Push (Tag.Parse (tagText));
					}
					curChunk.Offset = i + 1;
					inTag = false;
					break;
				}
			}
			curChunk.Length = endOffset - curChunk.Offset;
			if (curChunk.Length > 0) {
				curChunk.Style = GetChunkStyle (tagStack);
				result.Add (curChunk);
			}
				
			return result.ToArray ();
		}
	}
}
