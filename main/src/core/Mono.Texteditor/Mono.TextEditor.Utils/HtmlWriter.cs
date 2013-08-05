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
using Mono.TextEditor.Highlighting;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextEditor.Utils
{
	class ColoredSegment
	{
		public string Style { get; set; }
		public string Text { get; set; }

		public ColoredSegment (Chunk chunk, TextDocument doc)
		{
			this.Style = chunk.Style;
			this.Text = doc.GetTextAt (chunk);
		}

		public static List<List<ColoredSegment>> GetChunks (TextEditorData data, TextSegment selectedSegment)
		{
			int startLineNumber = data.OffsetToLineNumber (selectedSegment.Offset);
			int endLineNumber = data.OffsetToLineNumber (selectedSegment.EndOffset);
			var copiedColoredChunks = new List<List<ColoredSegment>> ();
			foreach (var line in data.Document.GetLinesBetween (startLineNumber, endLineNumber)) {
				var offset = System.Math.Max (selectedSegment.Offset, line.Offset);
				var length = System.Math.Min (selectedSegment.EndOffset, line.EndOffset) - offset;
				copiedColoredChunks.Add (
					data.GetChunks (
					line, 
					offset,
					length
				)
					.Select (chunk => new ColoredSegment (chunk, data.Document))
					.ToList ()
				);
			}
			return copiedColoredChunks;
		}
	}
	/// <summary>
	/// This class is used for converting a highlighted document to html.
	/// </summary>
	public static class HtmlWriter
	{
		public static string GenerateHtml (TextEditorData data)
		{
			return GenerateHtml (ColoredSegment.GetChunks (data, new TextSegment (0, data.Length)), data.ColorStyle, data.Options);
		}

		internal static string GenerateHtml (List<List<ColoredSegment>> chunks, Mono.TextEditor.Highlighting.ColorScheme style, ITextEditorOptions options)
		{
			var htmlText = new StringBuilder ();
			htmlText.AppendLine (@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">");
			htmlText.AppendLine ("<HTML>");
			htmlText.AppendLine ("<HEAD>");
			htmlText.AppendLine ("<META HTTP-EQUIV=\"CONTENT-TYPE\" CONTENT=\"text/html; charset=utf-8\">");
			htmlText.AppendLine ("<META NAME=\"GENERATOR\" CONTENT=\"Mono Text Editor\">");
			htmlText.AppendLine ("</HEAD>");
			htmlText.AppendLine ("<BODY>"); 

			htmlText.AppendLine ("<FONT face = '" + options.Font.Family + "'>");
			bool first = true;

			foreach (var line in chunks) {
				if (!first) {
					htmlText.AppendLine ("<BR>");
				} else {
					first = false;
				}

				foreach (var chunk in line) {
					var chunkStyle = style.GetChunkStyle (chunk.Style);
					htmlText.Append ("<SPAN style='");
					if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal)
						htmlText.Append ("font-weight:" + ((int)chunkStyle.FontWeight) + ";");
					if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal)
						htmlText.Append ("font-style:" + chunkStyle.FontStyle.ToString ().ToLower () + ";");
					htmlText.Append ("color:" + ((HslColor)chunkStyle.Foreground).ToPangoString () + ";");
					htmlText.Append ("'>");
					AppendHtmlText (htmlText, chunk.Text, options);
					htmlText.Append ("</SPAN>");
				}
			}
			htmlText.AppendLine ("</FONT>");
            htmlText.AppendLine ("</BODY></HTML>");

			if (Platform.IsWindows)
                return GenerateCFHtml (htmlText.ToString ());

			return htmlText.ToString ();
		}

        static readonly string emptyCFHtmlHeader = GenerateCFHtmlHeader (0, 0, 0, 0);

        static string GenerateCFHtml (string htmlFragment)
        {
            int startHTML = emptyCFHtmlHeader.Length;
            int startFragment = startHTML;
            int endFragment = startFragment + System.Text.Encoding.UTF8.GetByteCount (htmlFragment);
            int endHTML = endFragment;
            return GenerateCFHtmlHeader (startHTML, endHTML, startFragment, endFragment) + htmlFragment;
        }

        /// <summary>
        /// Generates a CF_HTML clipboard format header.
        /// </summary>
        static string GenerateCFHtmlHeader (int startHTML, int endHTML, int startFragment, int endFragment)
        {
            return
                "Version:0.9" + Environment.NewLine +
                    string.Format ("StartHTML: {0:d8}", startHTML) + Environment.NewLine +
                    string.Format ("EndHTML: {0:d8}", endHTML) + Environment.NewLine +
                    string.Format ("StartFragment: {0:d8}", startFragment) + Environment.NewLine +
                    string.Format ("EndFragment: {0:d8}", endFragment) + Environment.NewLine;
        }

		static void AppendHtmlText (StringBuilder htmlText, string text, ITextEditorOptions options)
		{
			foreach (char ch in text) {
				switch (ch) {
				case ' ':
					htmlText.Append ("&nbsp;"); // NOTE: &#32; doesn't work in all programs
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

