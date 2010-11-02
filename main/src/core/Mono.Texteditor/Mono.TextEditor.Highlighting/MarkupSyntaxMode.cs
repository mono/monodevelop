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
				result.Command = commands[0];
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
			
			public override ChunkStyle GetChunkStyle (Style style)
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
		
		static ChunkStyle GetChunkStyle (Style style, IEnumerable<Tag> tagStack)
		{
			ChunkStyle result = new ChunkStyle ();
			if (style == null)
				style = new DefaultStyle (null);
			result.Color = style.Default.Color;
			
			foreach (Tag tag in tagStack) {
				//System.Console.WriteLine("'" + tag.Command + "'");
				switch (tag.Command.ToUpper ()) {
				case "B":
					result.ChunkProperties |= ChunkProperties.Bold;
					break;
				case "SPAN":
					if (tag.Arguments.ContainsKey ("style")) {
						ChunkStyle chunkStyle = style.GetChunkStyle (tag.Arguments["style"]);
						if (chunkStyle != null) {
							result.Color = chunkStyle.Color;
							result.ChunkProperties |= chunkStyle.ChunkProperties;
						} else {
							throw new Exception ("Style " + tag.Arguments["style"] + " not found.");
						}
					}
					if (tag.Arguments.ContainsKey ("foreground")) 
						result.Color = style.GetColorFromString (tag.Arguments["foreground"]);
					if (tag.Arguments.ContainsKey ("background")) 
						result.BackgroundColor = style.GetColorFromString (tag.Arguments["background"]);
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
		
		public override string GetTextWithoutMarkup (Document doc, Style style, int offset, int length)
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
		
		public override Chunk GetChunks (Document doc, Style style, LineSegment line, int offset, int length)
		{
			int endOffset = System.Math.Min (offset + length, doc.Length);
			Stack<Tag> tagStack = new Stack<Tag> ();
			TextChunk curChunk = new TextChunk (new ChunkStyle (), offset);
			Chunk startChunk = curChunk;
			Chunk endChunk = curChunk;
			bool inTag = true, inSpecial = false;
			int tagBegin = -1, specialBegin = -1;
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
							curChunk.ChunkStyle = GetChunkStyle (style, tagStack);
							endChunk = endChunk.Next = curChunk;
							curChunk = new TextChunk (new ChunkStyle (), offset);
						}
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
					}
					break;
				case '>':
					if (!inTag)
						break;
					string tagText = doc.GetTextBetween (tagBegin + 1, i);
					if (tagText.StartsWith ("/")) {
						if (tagStack.Count > 0)
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
				curChunk.ChunkStyle = GetChunkStyle (style, tagStack);
				endChunk = endChunk.Next = curChunk;
			}
			endChunk.Next = null;
			return startChunk;
		}
	}
}
