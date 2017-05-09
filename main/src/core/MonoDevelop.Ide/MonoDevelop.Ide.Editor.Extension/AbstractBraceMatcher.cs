//
// AbstractBraceMatcher.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Util;
using MonoDevelop.Core.Text;
using System;

namespace MonoDevelop.Ide.Editor
{
	public abstract class AbstractBraceMatcher
	{
		public string MimeType {
			get;
			internal set;
		} 

		public virtual bool CanHandle (TextEditor editor)
		{
			return DesktopService.GetMimeTypeIsSubtype(editor.MimeType, MimeType);
		}

		public abstract Task<BraceMatchingResult?> GetMatchingBracesAsync(IReadonlyTextDocument editor, DocumentContext context, int offset, CancellationToken cancellationToken = default(CancellationToken));
	}

	sealed class DefaultBraceMatcher : AbstractBraceMatcher
	{
		public override bool CanHandle (TextEditor editor)
		{
			return true;
		}

		public override Task<BraceMatchingResult?> GetMatchingBracesAsync (IReadonlyTextDocument editor, DocumentContext context, int offset, CancellationToken cancellationToken)
		{
			BraceMatchingResult? result = null;

			var matching = SimpleBracketMatcher.GetMatchingBracketOffset (editor, offset, cancellationToken);
			if (matching >= 0) {
				int start = Math.Min (offset, matching);
				int end = Math.Max (offset, matching);
				result = new BraceMatchingResult (new TextSegment (start, 1), new TextSegment (end, 1), offset == start);
			}

			return Task.FromResult (result);
		}
	}
}