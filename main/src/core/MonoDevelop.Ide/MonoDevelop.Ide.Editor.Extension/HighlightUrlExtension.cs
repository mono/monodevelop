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
using MonoDevelop.Core;
using System.Threading;
using YamlDotNet.Core.Tokens;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.Editor.Extension
{
	class HighlightUrlExtension : TextEditorExtension
	{
		SegmentTree<TextMarkerSegment> scannedSegmentTree = new SegmentTree<TextMarkerSegment> ();
		const string urlRegexStr = @"(http|ftp)s?\:\/\/[\w\d\.,;_/\-~%@()+:?&^=#!]*[\w\d/]";
		public static readonly Regex UrlRegex = new Regex (urlRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		public static readonly Regex MailRegex = new Regex (@"[\w\d._%+-]+@[\w\d.-]+\.\w+", RegexOptions.Compiled);

		CancellationTokenSource src = new CancellationTokenSource ();

		protected override void Initialize ()
		{
			base.Initialize ();
			Editor.LineShown += Editor_LineShown;
			Editor.TextChanged += Editor_TextChanged;
		}

		public override void Dispose ()
		{
			Editor.LineShown -= Editor_LineShown;
			Editor.TextChanged -= Editor_TextChanged;
			src.Cancel ();
			DisposeUrlTextMarker ();
			base.Dispose ();
		}

		void DisposeUrlTextMarker ()
		{
			foreach (var marker in scannedSegmentTree) {
				foreach (var u in marker.UrlTextMarker)
					Editor.RemoveMarker (u);
			}
			scannedSegmentTree.Clear ();
		}

		void Editor_LineShown (object sender, LineEventArgs e)
		{
			var matches = new List<Tuple<UrlType, Match>> ();
			var input = Editor;
			var line = e.Line;
			var lineOffset = line.Offset;
			var lineEndOffset = lineOffset + line.Length;
			if (lineEndOffset > input.Length || line.Length <= 0)
				return;
			if (scannedSegmentTree.GetSegmentsAt (lineOffset).Any ())
				return;
			var o = 0;
			string lineText = input.GetTextAt (lineOffset, line.Length);
			
			var match = UrlRegex.Match (lineText);
			while (match.Success) {
				matches.Add (Tuple.Create (UrlType.Url, match));
				o = match.Index + match.Length;
				var len = line.Length - o;
				if (len <= 0)
					break;
				match = UrlRegex.Match (lineText, o, len);
			}

			o = 0;
			match = MailRegex.Match (lineText);
			while (match.Success) {
				matches.Add (Tuple.Create (UrlType.Email, match));
				o = match.Index + match.Length;
				var len = line.Length - o;
				if (len <= 0)
					break;
				match = MailRegex.Match (lineText, o, len);
			}
			var newSegment = new TextMarkerSegment (line);
			scannedSegmentTree.Add (newSegment);
			foreach (var m in matches) {
				var startCol = m.Item2.Index;
				var url = m.Item2.Value;
				var marker = Editor.TextMarkerFactory.CreateUrlTextMarker (Editor, url, m.Item1, "url", startCol, startCol + m.Item2.Length);
				Editor.AddMarker (line, marker);
				newSegment.UrlTextMarker.Add (marker);
			}
		}

		void Editor_TextChanged (object sender, TextChangeEventArgs e)
		{
			foreach (var change in e.TextChanges) {
				var startLine = Editor.GetLineByOffset (change.NewOffset);
				int startLineOffset = startLine.Offset;

				var segments = scannedSegmentTree.GetSegmentsOverlapping (change.NewOffset, change.RemovalLength).ToList ();
				foreach (var seg in segments) {
					foreach (var u in seg.UrlTextMarker) {
						Editor.RemoveMarker (u);
					}
					scannedSegmentTree.Remove (seg);
				}
			}

			scannedSegmentTree.UpdateOnTextReplace (sender, e);
		}

		class TextMarkerSegment : TreeSegment
		{
			List<IUrlTextLineMarker> urlTextMarker = new List<IUrlTextLineMarker> ();

			public List<IUrlTextLineMarker> UrlTextMarker {
				get {
					return urlTextMarker;
				}
			}

			public TextMarkerSegment (int offset, int length) : base (offset, length)
			{
			}

			public TextMarkerSegment (ISegment segment) : base (segment)
			{
			}
		}
	}
}
