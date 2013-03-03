//
// HtmlWriter.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System;
using System.Text;

namespace Mono.TextEditor.Utils
{
	/// <summary>
	/// This class is used for converting a highlighted document to html.
	/// </summary>
	public static class HtmlWriter
	{
		public static string GenerateHtml (TextDocument doc, Mono.TextEditor.Highlighting.ISyntaxMode mode, Mono.TextEditor.Highlighting.ColorScheme style, ITextEditorOptions options)
		{
			var htmlText = new StringBuilder ();

			htmlText.Append (@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN""><HTML><BODY>");

			var selection = new TextSegment (0, doc.TextLength);
			int startLineNumber = doc.OffsetToLineNumber (selection.Offset);
			int endLineNumber = doc.OffsetToLineNumber (selection.EndOffset);
			htmlText.Append ("<FONT face = '" + options.Font.Family + "'>");
			bool first = true;
			foreach (var line in doc.GetLinesBetween (startLineNumber, endLineNumber)) {
				if (!first) {
					htmlText.Append ("<BR/>");
				} else {
					first = false;
				}

				if (mode == null) {
					AppendHtmlText (htmlText, doc, options, System.Math.Max (selection.Offset, line.Offset), System.Math.Min (line.EndOffset, selection.EndOffset));
					continue;
				}

				foreach (var chunk in mode.GetChunks (style, line, line.Offset, line.Length)) {
					int start = System.Math.Max (selection.Offset, chunk.Offset);
					int end = System.Math.Min (chunk.EndOffset, selection.EndOffset);
					var chunkStyle = style.GetChunkStyle (chunk);
					if (start < end) {
						htmlText.Append ("<SPAN style = '");
						if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal)
							htmlText.Append ("font-weight:" + ((int)chunkStyle.FontWeight) + ";");
						if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal)
							htmlText.Append ("font-style:" + chunkStyle.FontStyle.ToString ().ToLower () + ";");
						htmlText.Append ("color:" + ((HslColor)chunkStyle.Foreground).ToPangoString () + ";");
						htmlText.Append ("' >");
						AppendHtmlText (htmlText, doc, options, start, end);
						htmlText.Append ("</SPAN>");
					}
				}
			}
			htmlText.Append ("</FONT>");
			htmlText.Append ("</BODY></HTML>");
			return htmlText.ToString ();
		}

		static void AppendHtmlText (StringBuilder htmlText, TextDocument doc, ITextEditorOptions options, int start, int end)
		{
			for (int i = start; i < end; i++) {
				char ch = doc.GetCharAt (i);
				switch (ch) {
				case ' ':
					htmlText.Append ("&nbsp;");
					break;
				case '\t':
					for (int i2 = 0; i2 < options.TabSize; i2++)
						htmlText.Append ("&nbsp;");
					break;
				case '<':
					htmlText.Append ("&lt;");
					break;
				case '>':
					htmlText.Append ("&gt;");
					break;
				case '&':
					htmlText.Append ("&amp;");
					break;
				case '"':
					htmlText.Append ("&quot;");
					break;
				default:
					htmlText.Append (ch);
					break;
				}
			}
		}
	}
}

