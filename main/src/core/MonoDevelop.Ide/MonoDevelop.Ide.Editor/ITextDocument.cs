//
// ITextDocument.cs
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
using MonoDevelop.Core.Text;
using System.Text;
using MonoDevelop.Core;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor
{
	public interface ITextDocument : IReadonlyTextDocument
	{
		/// <summary>
		/// Gets/Sets the text of the whole document..
		/// </summary>
		new string Text { get; set; } // hides ITextSource.Text to add the setter

		/// <summary>
		/// Gets or Sets a character at the specified position in the document.
		/// </summary>
		/// <paramref name="offset">The index of the character to get.</paramref>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to TextLength-1).</exception>
		/// <returns>The character at the specified position.</returns>
		/// <remarks>This is the same as Text[offset], but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		new char this [int offset] { get; set; }

		new bool IsReadOnly { get; set; }

		new FilePath FileName { get; set; }

		new string MimeType { get; set; }

		new Encoding Encoding { get; set; }

		void InsertText (int offset, string text);

		void InsertText (int offset, ITextSource text);

		void RemoveText (int offset, int length);

		void ReplaceText (int offset, int length, string value);

		void ReplaceText (int offset, int length, ITextSource value);

		void ApplyTextChanges (IEnumerable<Microsoft.CodeAnalysis.Text.TextChange> changes);

		bool IsInAtomicUndo {
			get;
		}

		IDisposable OpenUndoGroup();

		/// <summary>
		/// This event is called directly before a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the change (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanging;

		/// <summary>
		/// This event is called directly after a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the event handler (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanged;

		event EventHandler FileNameChanged;
		event EventHandler MimeTypeChanged;

		/// <summary>
		/// Creates an immutable snapshot of this document.
		/// </summary>
		IReadonlyTextDocument CreateDocumentSnapshot();

//		event EventHandler<LineEventArgs> LineChanged;
//		event EventHandler<LineEventArgs> LineInserted;
//		event EventHandler<LineEventArgs> LineRemoved;
	}

	public static class DocumentExtensions
	{
		public static void RemoveText (this ITextDocument document, ISegment segment)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			document.RemoveText (segment.Offset, segment.Length);
		}

		public static void ReplaceText (this ITextDocument document, ISegment segment, string value)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			document.ReplaceText (segment.Offset, segment.Length, value);
		}

		public static void ReplaceText (this ITextDocument document, ISegment segment, ITextSource textSource)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			document.ReplaceText (segment.Offset, segment.Length, textSource);
		}

		public static void Save (this ITextDocument document)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			document.WriteTextTo (document.FileName); 
		}
	}
}