//
// HighlightUrlExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Extension
{
	class HighlightUrlExtension : TextEditorExtension
	{
		const string urlRegexStr = @"(http|ftp)s?\:\/\/[\w\d\.,;_/\-~%@()+:?&^=#!]*[\w\d/]";

		public static readonly Regex UrlRegex = new Regex (urlRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		public static readonly Regex MailRegex = new Regex (@"[\w\d._%+-]+@[\w\d.-]+\.\w+", RegexOptions.Compiled);

		List<IUrlTextLineMarker> urlTextMarker = new List<IUrlTextLineMarker> ();

		protected override void Initialize ()
		{
			base.Initialize ();
			Editor.TextChanged += Update;
			Update (null, null);
		}

		public override void Dispose ()
		{
			DisposeUrlTextMarker ();
		}

		void DisposeUrlTextMarker ()
		{
			foreach (var marker in urlTextMarker) {
				Editor.RemoveMarker (marker);
			}
			urlTextMarker.Clear ();
		}

		void Update (object sender, TextChangeEventArgs e)
		{
			var startLine = e != null ? Editor.GetLineByOffset (e.Offset) : Editor.GetLine (1);
			var endLine = e != null ? Editor.GetLineByOffset (e.Offset + e.RemovalLength) : Editor.GetLine (Editor.LineCount);
			var stopLine = endLine.NextLine;
			int startLineOffset = startLine.Offset;
			var endLineOffset = endLine.Offset + endLine.Length;

			for (int i = 0; i < urlTextMarker.Count; i++) {
				var marker = urlTextMarker [i];
				var o = marker.Line.Offset;
				if (startLineOffset <= o && o < endLineOffset) {
					Editor.RemoveMarker (marker);
					urlTextMarker.RemoveAt (i);
					i--;
					continue;
				}
			}

			Task.Run (delegate {
				var matches = new List<Tuple<IDocumentLine, UrlType, Match>> ();
				var line = startLine;
				while (line != stopLine) {
					var lineOffset = line.Offset;
					var o = line.Offset;
					var len = line.Length;
					var match = UrlRegex.Match (Editor, o, len);
					while (match.Success) {
						matches.Add (Tuple.Create (line, UrlType.Url, match));
						var delta = line.Offset - o + match.Length;
						o += delta;
						len -= delta;
						if (len < 0)
							break;
						match = UrlRegex.Match (Editor, o, len);
					}

					o = line.Offset;
					len = line.Length;
					match = MailRegex.Match (Editor, o, len);
					while (match.Success) {
						matches.Add (Tuple.Create (line, UrlType.Email, match));
						var delta = line.Offset - o + match.Length;
						o += delta;
						len -= delta;
						if (len < 0)
							break;
						match = MailRegex.Match (Editor, o, len);
					}
					line = line.NextLine;
				}
				Runtime.RunInMainThread (delegate {
					foreach (var m in matches) {
						var startCol = m.Item3.Index - m.Item1.Offset;
						var url = m.Item3.Value;
						var marker = Editor.TextMarkerFactory.CreateUrlTextMarker (Editor, m.Item1, url, m.Item2, "url", startCol, startCol + m.Item3.Length);
						Editor.AddMarker (m.Item1, marker);
						urlTextMarker.Add (marker); 
					}
				});
			});
		}
	}
}
