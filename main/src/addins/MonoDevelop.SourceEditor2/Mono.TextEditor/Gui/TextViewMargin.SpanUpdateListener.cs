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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using Mono.TextEditor.Highlighting;

using MonoDevelop.Components.AtkCocoaHelper;

using Gdk;
using Gtk;
using System.Timers;
using System.Diagnostics;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Collections.Immutable;
using System.Threading;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Mono.TextEditor
{
	partial class TextViewMargin
	{
		internal class SpanUpdateListener : IDisposable
		{
			readonly MonoTextEditor textEditor;

			public bool HasUpdatedMultilineSpan { get; set; }

			public SpanUpdateListener (MonoTextEditor textEditor)
			{
				this.textEditor = textEditor;
				textEditor.Document.TextBuffer.Changing += OnTextBufferChanging;
				textEditor.Document.TextBuffer.ChangedLowPriority += OnTextBufferChangedLowPriority;
			}

			public void Dispose()
			{
				textEditor.Document.TextBuffer.Changing -= OnTextBufferChanging;
				textEditor.Document.TextBuffer.ChangedLowPriority -= OnTextBufferChangedLowPriority;
			}

			Dictionary<int, HighlightedLine> lines = new Dictionary<int, HighlightedLine> ();

			void OnTextBufferChanging (object sender, TextContentChangingEventArgs e)
			{
				HasUpdatedMultilineSpan = false;

				int startLine = this.textEditor.TextViewLines.FirstVisibleLine.Start.GetContainingLine ().LineNumber + 1;
				int endLine = this.textEditor.TextViewLines.LastVisibleLine.Start.GetContainingLine ().LineNumber + 1;

				for (int curLine = startLine; curLine <= endLine; curLine++) {
					var layout = textEditor.TextViewMargin.GetLayout (textEditor.GetLine (curLine));
					lines[curLine] = layout.HighlightedLine;
				}
			}

			void OnTextBufferChangedLowPriority (object sender, TextContentChangedEventArgs e)
			{
				foreach (var change in e.Changes) {
					var oldLineNumber = e.Before.GetLineNumberFromPosition (change.OldPosition) + 1;
					lines.TryGetValue(oldLineNumber, out var oldHighlightedLine);
					var curLine = textEditor.GetLineByOffset (change.NewPosition);
					var curLayout = textEditor.TextViewMargin.GetLayout (curLine);
					if (!UpdateLineHighlight (curLine.LineNumber, oldHighlightedLine, curLayout.HighlightedLine))
						break;
				}
				lines.Clear ();
			}

			bool UpdateLineHighlight (int lineNumber, HighlightedLine oldLine, HighlightedLine newLine)
			{
				if (oldLine == null || ShouldUpdateSpan (oldLine, newLine)) {
					textEditor.TextViewMargin.PurgeLayoutCacheAfter (lineNumber);
					textEditor.QueueDraw ();
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