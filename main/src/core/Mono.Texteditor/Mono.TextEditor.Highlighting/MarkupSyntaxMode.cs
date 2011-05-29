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
using System.Text;

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
				Tag result = new Tag ();
				string[] commands = text.Split (' ', '\t');
				result.Command = commands[0].ToUpper ();
				for (int i = 1; i < commands.Length; i++) {
					string[] argument = commands[i].Split ('=');
					if (argument.Length == 2)
						result.Arguments[argument[0]] = argument[1].Trim ('"');
				}
				return result;
			}
		}

		class TextChunk : Chunk
		{
			string text;
			
			public ChunkStyle ChunkStyle {
				get;
				set;
			}
			
			public override ChunkStyle GetChunkStyle (ColorSheme style)
			{
				return ChunkStyle;
			}
			
			public TextChunk (ChunkStyle style, int offset)
			{
				this.Offset = offset;
				this.ChunkStyle = style;
			}
			
			public TextChunk (ChunkStyle style, int offset, string text) : this (style, offset)
			{
				if (text == null)
					throw new ArgumentNullException ("text");
				this.text = text;
				this.Length = text.Length;
			}
			
			public override char GetCharAt (Document doc, int offset)
			{
				if (string.IsNullOrEmpty (text))
					return base.GetCharAt (doc, offset);
				return text [offset - this.Offset];
			}
			
			public override string GetText (Document doc)
			{
				if (string.IsNullOrEmpty (text))
					return base.GetText (doc);
				return text;
			}
			
			public override string ToString ()
			{
				return string.Format("[TextChunk: ChunkStyle={0}, Text={1}, Offset={2}, Length={3}]", ChunkStyle, text, Offset, Length);
			}
		}
		
		static ChunkStyle GetChunkStyle (ColorSheme style, IEnumerable<Tag> tagStack)
		{
			ChunkStyle result = new ChunkStyle ();
			if (style == null)
				style = new DefaultStyle (null);
			result.CairoColor = style.Default.CairoColor;
			
			foreach (Tag tag in tagStack) {
				//System.Console.WriteLine("'" + tag.Command + "'");
				switch (tag.Command) {
				case "B":
					result.ChunkProperties |= ChunkProperties.Bold;
					break;
				case "SPAN":
					string val;
					if (tag.Arguments.TryGetValue ("style", out val)) {
						ChunkStyle chunkStyle = style.GetChunkStyle (val);
						if (chunkStyle != null) {
							result.CairoColor = chunkStyle.CairoColor;
							result.ChunkProperties |= chunkStyle.ChunkProperties;
						} else {
							throw new Exception ("Style " + val + " not found.");
						}
					}
					if (tag.Arguments.TryGetValue ("foreground", out val))
						result.CairoColor = style.GetColorFromString (val);
					if (tag.Arguments.TryGetValue ("background", out val))
						result.CairoBackgroundColor = style.GetColorFromString (val);
					break;
				case "A":
					result.Link = tag.Arguments["ref"];
					break;
				case "I":
					result.ChunkProperties |= ChunkProperties.Italic;
					break;
				case "U":
					result.ChunkProperties |= ChunkProperties.Underline;
					break;
				}
			}
			return result;
		}
		
		public override string GetTextWithoutMarkup (Document doc, ColorSheme style, int offset, int length)
		{
			StringBuilder result = new StringBuilder ();
			
			int curOffset = offset;
			int endOffset =  offset + length;
			
			while (curOffset < endOffset) {
				LineSegment curLine = doc.GetLineByOffset (curOffset);
				for (Chunk chunk = GetChunks (doc, style, curLine, curOffset, System.Math.Min (endOffset - curOffset, curLine.EndOffset - curOffset)); chunk != null; chunk = chunk.Next) {
					result.Append (chunk.GetText (doc));
				}
				curOffset = curLine.EndOffset;
			}
			return result.ToString ();
		}
		
		public override Chunk GetChunks (Document doc, ColorSheme style, LineSegment line, int offset, int length)
		{
			int endOffset = System.Math.Min (offset + length, doc.Length);
			Stack<Tag> tagStack = new Stack<Tag> ();
			TextChunk curChunk = new TextChunk (new ChunkStyle (), offset);
			Chunk startChunk = curChunk;
			Chunk endChunk = curChunk;
			bool inTag = true, inSpecial = false;
			int specialBegin = -1;
			StringBuilder tagBuilder = new StringBuilder ();
			StringBuilder specialBuilder = new StringBuilder ();
			for (int i = offset; i < endOffset; i++) {
				char ch = doc.GetCharAt (i);
				switch (ch) {
				case '<':
					curChunk.Length = i - curChunk.Offset;
					if (curChunk.Length > 0) {
						curChunk.ChunkStyle = GetChunkStyle (style, tagStack);
						endChunk = endChunk.Next = curChunk;
						curChunk = new TextChunk (new ChunkStyle (), offset);
					}
					tagBuilder.Length = 0;
					specialBuilder.Length = 0;
					inTag = true;
					break;
				case '&':
					curChunk.Length = i - curChunk.Offset;
					if (curChunk.Length > 0) {
						curChunk.ChunkStyle = GetChunkStyle (style, tagStack);
						endChunk = endChunk.Next = curChunk;
						curChunk = new TextChunk (new ChunkStyle (), offset);
					}
					
					inSpecial = true;
					specialBuilder.Length = 0;
					tagBuilder.Length = 0;
					specialBegin = i;
					break;
				case ';':
					if (inSpecial) {
						string specialText = specialBuilder.ToString ();
						switch (specialText) {
						case "lt":
							endChunk = endChunk.Next = new TextChunk (GetChunkStyle (style, tagStack), specialBegin, "<");
							break;
						case "gt": 
							endChunk = endChunk.Next = new TextChunk (GetChunkStyle (style, tagStack), specialBegin, ">");
							break;
						case "amp": 
							endChunk = endChunk.Next = new TextChunk (GetChunkStyle (style, tagStack), specialBegin, "&");
							break;
						}
						curChunk.Offset = i + 1;
						inSpecial = false;
						specialBuilder.Length = 0;
						tagBuilder.Length = 0;
					}
					break;
				case '>':
					if (!inTag)
						break;
					string tagText = tagBuilder.ToString ();
					tagBuilder.Length = 0;
					if (tagText.StartsWith ("/")) {
						if (tagStack.Count > 0)
							tagStack.Pop ();
					} else {
						tagStack.Push (Tag.Parse (tagText));
					}
					curChunk.Offset = i + 1;
					inTag = false;
					specialBuilder.Length = 0;
					tagBuilder.Length = 0;
					break;
				default:
					if (inSpecial) {
						specialBuilder.Append (ch);
					} else {
						tagBuilder.Append (ch);
					}
					break;
				}
			}
			curChunk.Length = endOffset - curChunk.Offset;
			if (curChunk.Length > 0) {
				curChunk.ChunkStyle = GetChunkStyle (style, tagStack);
				endChunk = endChunk.Next = curChunk;
			}
			endChunk.Next = null;
			return startChunk;
		}
	}
}
