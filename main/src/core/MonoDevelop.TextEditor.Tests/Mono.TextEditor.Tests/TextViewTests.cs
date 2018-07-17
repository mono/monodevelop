//
// TextViewTests.cs
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

using System.IO;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Mono.TextEditor.Utils;
using System.Reflection;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	public class TextViewTests
	{
		class TestSyntaxMode : ISyntaxHighlighting
		{
			#pragma warning disable 67 // unused
			public event EventHandler<MonoDevelop.Ide.Editor.LineEventArgs> HighlightingStateChanged;
			#pragma warning restore 67

			public void Dispose ()
			{
			}

			public Task<HighlightedLine> GetHighlightedLineAsync (IDocumentLine line, CancellationToken cancellationToken)
			{
				var segments = new List<ColoredSegment> ();
				for (int i = 0; i < line.Length;i++){
					if (i > 0 && i == line.Length - 1) {
						segments.Add (new ColoredSegment (i, 2, ScopeStack.Empty));
						break;
					}
					segments.Add (new ColoredSegment (i, 1, ScopeStack.Empty));
				}

				return Task.FromResult (new HighlightedLine (line, segments));
			}

			public Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken)
			{
				return Task.FromResult (ScopeStack.Empty);
			}
		}
	}
}