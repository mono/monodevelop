//
// TextPasteHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Threading.Tasks;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor.Extension
{
	/// <summary>
	/// The text paste handler can do formattings to a text that is about to be pasted
	/// into the text document.
	/// </summary>
	public abstract class TextPasteHandler
	{
		/// <summary>
		/// Formats plain text that is inserted at a specified offset.
		/// </summary>
		/// <returns>
		/// The text that will get inserted at that position.
		/// </returns>
		/// <param name="offset">The offset where the text will be inserted.</param>
		/// <param name="text">The text to be inserted.</param>
		/// <param name="copyData">Additional data in case the text was copied from a Mono.TextEditor.</param>
		public abstract string FormatPlainText(int offset, string text, byte[] copyData);

		/// <summary>
		/// Gets the copy data for a specific segment inside the document. This can contain meta data about the text pasted.
		/// For example 'text pasted from string'.
		/// </summary>
		/// <param name = "offset">The copy offset.</param>
		/// <param name = "length">The length of the copied text.</param>
		public abstract byte[] GetCopyData(int offset, int length);

		/// <summary>
		/// This is called after text was pasted. This is useful for creating an additional undo step for the paste command.
		/// </summary>
		/// <param name="offset">The offset the text was pasted at.</param>
		/// <param name="length">The length of the text pasted.</param>
		public abstract Task PostFomatPastedText (int offset, int length);
	}
}

