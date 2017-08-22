// 
// ISyntaxMode.cs
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
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public sealed class HighlightedLine
	{
		public ISegment TextSegment { get; }
		/// <summary>
		/// The segment offsets are 0 at line start regardless of where the line is inside the document.
		/// </summary>
		public IReadOnlyList<ColoredSegment> Segments { get; private set; }
		public HighlightedLine (ISegment textSegment, IReadOnlyList<ColoredSegment> segments)
		{
			TextSegment = textSegment;
			Segments = segments;
		}
	}

	/// <summary>
	/// The basic interface for all syntax modes
	/// </summary>
	public interface ISyntaxHighlighting : IDisposable
	{
		/// <summary>
		/// Gets colorized segments (aka chunks) from offset to offset + length.
		/// </summary>
		/// <param name='line'>
		/// The starting line at (offset). This is the same as Document.GetLineByOffset (offset).
		/// </param>
		Task<HighlightedLine> GetHighlightedLineAsync (IDocumentLine line, CancellationToken cancellationToken);
		Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken);

		event EventHandler<LineEventArgs> HighlightingStateChanged;
	}

	public sealed class DefaultSyntaxHighlighting : ISyntaxHighlighting
	{
		public static readonly DefaultSyntaxHighlighting Instance = new DefaultSyntaxHighlighting ();

		DefaultSyntaxHighlighting ()
		{
		}

		public Task<HighlightedLine> GetHighlightedLineAsync (IDocumentLine line, CancellationToken cancellationToken)
		{
			return Task.FromResult (new HighlightedLine (line, new [] { new ColoredSegment (0, line.Length, ScopeStack.Empty) }));
		}

		public Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken)
		{
			return Task.FromResult (ScopeStack.Empty);
		}

		public event EventHandler<LineEventArgs> HighlightingStateChanged { add { } remove { } }

		public void Dispose()
		{
		}
	}
}