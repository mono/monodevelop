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
		List<IUrlTextLineMarker> markers = new List<IUrlTextLineMarker> ();
		const string urlRegexStr = @"(http|ftp)s?\:\/\/[\w\d\.,;_/\-~%@()+:?&^=#!]*[\w\d/]";
		public static readonly Regex UrlRegex = new Regex (urlRegexStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		public static readonly Regex MailRegex = new Regex (@"[\w\d._%+-]+@[\w\d.-]+\.\w+", RegexOptions.Compiled);

		Dictionary<int, CancellationTokenSource> src = new Dictionary<int, CancellationTokenSource> ();
		CancellationTokenSource updateAllSrc = new CancellationTokenSource ();
		Task updateAllTask;

		protected override void Initialize ()
		{
			base.Initialize ();
			Editor.TextChanged += Editor_TextChanged;
			RunUpdateAll ();
		}

		public override void Dispose ()
		{
			updateAllSrc.Cancel ();
			Editor.TextChanged -= Editor_TextChanged;
			foreach (var kv in src) {
				kv.Value.Dispose ();
			}
			DisposeUrlTextMarker ();
			base.Dispose ();
		}

		void DisposeUrlTextMarker ()
		{
			foreach (var u in markers)
				Editor.RemoveMarker (u);
			markers.Clear ();
		}

		void Editor_TextChanged (object sender, TextChangeEventArgs e)
		{
			if (!updateAllTask.IsCompleted) {
				RunUpdateAll ();
				return;
			}
			for (int i = 0; i < e.TextChanges.Count; ++i) {
				var change = e.TextChanges[i];
				var startLine = Editor.GetLineByOffset (change.NewOffset);
				var line = startLine;
				int endOffset = change.NewOffset + change.InsertionLength;	
			
				var input = Editor.CreateSnapshot ();
				var lineOffset = line.Offset;
				if (src.TryGetValue (lineOffset, out CancellationTokenSource cts))
					cts.Cancel ();
				cts = new CancellationTokenSource ();
				src [lineOffset] = cts;
				var token = cts.Token;
				RunUpdateTask (input, startLine, endOffset, token);
			}
		}

		private void RunUpdateAll ()
		{
			updateAllSrc.Cancel ();
			DisposeUrlTextMarker ();
			updateAllSrc = new CancellationTokenSource ();
			updateAllTask = RunUpdateTask (Editor.CreateSnapshot (), Editor.GetLine (1), Editor.Length, updateAllSrc.Token);
		}

		Task RunUpdateTask (ITextSource input, IDocumentLine startLine, int endOffset, CancellationToken token)
		{
			return Task.Run (delegate {
				var matches = new List<(UrlType, Match, IDocumentLine)> ();
				var line = startLine;
				int o = 0;
				while (line != null && line.Offset < endOffset) {
					if (token.IsCancellationRequested)
						return;
					string lineText = input.GetTextAt (line.Offset, line.Length);
					var match = UrlRegex.Match (lineText);
					while (match.Success) {
						if (token.IsCancellationRequested)
							return;
						matches.Add ((UrlType.Url, match, line));
						o = match.Index + match.Length;
						var len = line.Length - o;
						if (len <= 0)
							break;
						match = UrlRegex.Match (lineText, o, len);
					}

					o = 0;
					match = MailRegex.Match (lineText);
					while (match.Success) {
						if (token.IsCancellationRequested)
							return;
						matches.Add ((UrlType.Email, match, line));
						o = match.Index + match.Length;
						var len = line.Length - o;
						if (len <= 0)
							break;
						match = MailRegex.Match (lineText, o, len);
					}
					line = line.NextLine;
				}

				Runtime.RunInMainThread (delegate {
					if (token.IsCancellationRequested)
						return;
					line = startLine;
					while (line != null && line.Offset < endOffset) {
						foreach (var u in Editor.GetLineMarkers (line).OfType<IUrlTextLineMarker> ()) {
							Editor.RemoveMarker (u);
							markers.Remove (u);
						}
						line = line.NextLine;
					}

					foreach (var m in matches) {
						var startCol = m.Item2.Index;
						var url = m.Item2.Value;
						var marker = Editor.TextMarkerFactory.CreateUrlTextMarker (Editor, url, m.Item1, "url", startCol, startCol + m.Item2.Length);
						markers.Add (marker);
						Editor.AddMarker (m.Item3, marker);
					}
					src.Remove (startLine.Offset);
				});
			});
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
