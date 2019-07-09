//
// CSharpBreakpointSpanResolver.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;

using MonoDevelop.Debugger;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.CSharp.Debugger
{
	[ExportDocumentControllerExtension (MimeType = "text/x-csharp")]
	class CSharpBreakpointSpanResolver : DocumentControllerExtension, IBreakpointSpanResolver
	{
		public override Task<bool> SupportsController (DocumentController controller)
		{
			return Task.FromResult (controller.GetContent<ITextBuffer> () != null);
		}

		public async Task<Span> GetBreakpointSpanAsync (ITextBuffer buffer, int position, CancellationToken cancellationToken)
		{
			var document = buffer.AsTextContainer ()?.GetOpenDocumentInCurrentContext ();
			var tree = document != null ? await document.GetSyntaxTreeAsync (cancellationToken) : null;

			if (tree != null && BreakpointSpans.TryGetBreakpointSpan (tree, Math.Max (0, Math.Min (position, tree.Length - 1)), cancellationToken, out var span))
				return new Span (span.Start, span.Length);

			return buffer.CurrentSnapshot.GetLineFromPosition (Math.Max (0, Math.Min (position, buffer.CurrentSnapshot.Length - 1))).Extent.Span;
		}
	}
}
