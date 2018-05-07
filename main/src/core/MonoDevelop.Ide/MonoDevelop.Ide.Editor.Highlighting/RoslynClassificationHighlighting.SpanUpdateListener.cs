//
// TextViewMargin.SpanUpdateListener.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.TypeSystem;
using Roslyn.Utilities;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	partial class RoslynClassificationHighlighting
	{
		internal class SpanUpdateListener : IDisposable
		{
			readonly TextEditor textEditor;

			public bool HasUpdatedMultilineSpan { get; set; }

			public SpanUpdateListener (TextEditor textEditor)
			{
				this.textEditor = textEditor;
				textEditor.TextChanged += Document_TextChanged;
				textEditor.TextChanging += Document_TextChanging;
			}

			public void Dispose()
			{
				textEditor.TextChanged -= Document_TextChanged;
				textEditor.TextChanging -= Document_TextChanging;
			}

			List<HighlightedLine> lines = new List<HighlightedLine> ();

			void Document_TextChanging (object sender, Core.Text.TextChangeEventArgs e)
			{
				HasUpdatedMultilineSpan = false;
				foreach (var change in e.TextChanges) {
					if (change.Offset == 0 && change.RemovalLength == textEditor.Length) {
						lines.Clear ();
						return;
					}
					// only the last line state is needed
					var highlightedLastLine = textEditor.Implementation.GetHighlightedLine (textEditor.GetLineByOffset (change.Offset + change.RemovalLength));
					lines.Add (highlightedLastLine);
				}
			}

			void Document_TextChanged (object sender, Core.Text.TextChangeEventArgs e)
			{
				int i = 0;
				foreach (var change in e.TextChanges) {
					if (i >= lines.Count)
						break; // should never happen
					var oldHighlightedLine = lines [i++];

					// compare last line states
					var curLine = textEditor.GetLineByOffset (change.NewOffset + change.InsertionLength);
					var highlightedLastLine = textEditor.Implementation.GetHighlightedLine (curLine);
					if (!UpdateLineHighlight (curLine.LineNumber, oldHighlightedLine, highlightedLastLine))
						break;
				}
				lines.Clear ();
			}

			bool UpdateLineHighlight (int lineNumber, HighlightedLine oldLine, HighlightedLine newLine)
			{
				if (oldLine != null && ShouldUpdateSpan (oldLine, newLine)) {
					textEditor.Implementation.PurgeLayoutCacheAfter (lineNumber);
					textEditor.Implementation.QueueDraw ();
					HasUpdatedMultilineSpan = true;
					return false;
				}
				return true;
			}

			static bool ShouldUpdateSpan (HighlightedLine line1, HighlightedLine line2)
			{
				if (line1.IsContinuedBeyondLineEnd != line2.IsContinuedBeyondLineEnd)
					return true;
				if (line1.IsContinuedBeyondLineEnd == true) {
					return line1.Segments.Last ().ScopeStack.Peek () != line2.Segments.Last ().ScopeStack.Peek ();
				}
				return false;
			}
		}
	}
}