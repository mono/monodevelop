//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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

namespace Microsoft.VisualStudio.Text.Projection
{
	/// <summary>
	/// An extension point for the text view construction process to provide
	/// a surface projection buffer for the ITextDataModel.
	/// Razor provides an implementation that returns the top HtmlxProjection
	/// buffer, which projects into C#, CSS, JavaScript, inert and ultimately
	/// RazorCoreCSharp (disk buffer).
	/// </summary>
	public interface IProjectionBufferProvider
	{
		/// <summary>
		/// Given the disk buffer (bottom buffer) return an optional projection buffer
		/// to use as the top buffer (surface buffer) in the buffer graph.
		/// </summary>
		/// <param name="diskBuffer">Buffer that corresponds to the text document on disk.</param>
		/// <param name="projectionBuffer">Buffer that should be used as the surface buffer of the text view.</param>
		/// <returns>true if the projection buffer was provided, false to fall back to the default (no projection).</returns>
		bool TryGetProjectionBuffer (ITextBuffer diskBuffer, out ITextBuffer projectionBuffer);
	}
}