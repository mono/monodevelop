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

namespace Mono.TextEditor.Highlighting
{
	/// <summary>
	/// The basic interface for all syntax modes
	/// </summary>
	public interface ISyntaxMode
	{
		/// <summary>
		/// Gets or sets the document the syntax mode is attached to. To detach it's set to null.
		/// </summary>
		TextDocument Document {
			get;
			set;
		}

		/// <summary>
		/// Gets colorized segments (aka chunks) from offset to offset + length.
		/// </summary>
		/// <param name='style'>
		/// The color scheme used te generate the chunks.
		/// </param>
		/// <param name='line'>
		/// The starting line at (offset). This is the same as Document.GetLineByOffset (offset).
		/// </param>
		/// <param name='offset'>
		/// The starting offset.
		/// </param>
		/// <param name='length'>
		/// The length of the text converted to chunks.
		/// </param>
		IEnumerable<Chunk> GetChunks (ColorScheme style, DocumentLine line, int offset, int length);
	}
}

