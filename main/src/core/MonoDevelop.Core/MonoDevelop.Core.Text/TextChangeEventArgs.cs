//
// TextChangeEventArgs.cs
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
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.Core.Text
{
	[DebuggerDisplay("({offset}, {removedText.Length}, {insertedText.Text})")]
	public sealed class TextChange
	{
		readonly int offset;
		int newOffset;
		readonly ITextSource removedText;
		readonly ITextSource insertedText;

		public TextChange (int offset, string removedText, string insertedText)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException (nameof (offset), offset, "offset must not be negative");
			this.offset = offset;
			this.newOffset = offset;
			this.removedText = removedText != null ? new StringTextSource (removedText) : StringTextSource.Empty;
			this.insertedText = insertedText != null ? new StringTextSource (insertedText) : StringTextSource.Empty;
		}

		public TextChange (int offset, ITextSource removedText, ITextSource insertedText)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException (nameof (offset), offset, "offset must not be negative");
			this.offset = offset;
			this.newOffset = offset;
			this.removedText = removedText ?? StringTextSource.Empty;
			this.insertedText = insertedText ?? StringTextSource.Empty;
		}

		public TextChange(int offset, int newOffset, string removedText, string insertedText)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset must not be negative");
			this.offset = offset;
			this.newOffset = newOffset;
			this.removedText = removedText != null ? new StringTextSource(removedText) : StringTextSource.Empty;
			this.insertedText = insertedText != null ? new StringTextSource(insertedText) : StringTextSource.Empty;
		}

		public TextChange (int offset, int newOffset, ITextSource removedText, ITextSource insertedText)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException (nameof (offset), offset, "offset must not be negative");
			this.offset = offset;
			this.newOffset = newOffset;
			this.removedText = removedText ?? StringTextSource.Empty;
			this.insertedText = insertedText ?? StringTextSource.Empty;
		}

		/// <summary>
		/// The offset at which the change occurs.
		/// </summary>
		public int Offset {
			get { return offset; }
		}

		/// <summary>
		/// The offset at which the change occurs relative to the new buffer.
		/// </summary>
		public int NewOffset
		{
			get { return newOffset; }
		}

		/// <summary>
		/// The text that was removed.
		/// </summary>
		public ITextSource RemovedText {
			get { return removedText; }
		}

		/// <summary>
		/// The number of characters removed.
		/// </summary>
		public int RemovalLength {
			get { return removedText.Length; }
		}

		/// <summary>
		/// The text that was inserted.
		/// </summary>
		public ITextSource InsertedText {
			get { return insertedText; }
		}

		/// <summary>
		/// The number of characters inserted.
		/// </summary>
		public int InsertionLength {
			get { return insertedText.Length; }
		}

		/// <summary>
		/// InsertionLength - RemovalLength
		/// </summary>
		public int ChangeDelta {
			get {
				return InsertionLength - RemovalLength;
			}
		}
	}

	/// <summary>
	/// Describes a change of the document text.
	/// This class is thread-safe.
	/// </summary>
	[Serializable]
	public class TextChangeEventArgs : EventArgs
	{
		public IReadOnlyList<TextChange> TextChanges { get; }

		/// <summary>
		/// Creates a new TextChangeEventArgs object.
		/// </summary>
		[Obsolete ("Use TextChangeEventArgs (int offset, int newOffset, string removedText, string insertedText)")]
		public TextChangeEventArgs (int offset, string removedText, string insertedText) : this(offset, offset, removedText, insertedText) {}

		/// <summary>
		/// Creates a new TextChangeEventArgs object.
		/// </summary>
		[Obsolete ("Use TextChangeEventArgs (int offset, int newOffset, ITextSource removedText, ITextSource insertedText)")]
		public TextChangeEventArgs (int offset, ITextSource removedText, ITextSource insertedText) : this (offset, offset, removedText, insertedText) {}

		public TextChangeEventArgs (int offset, int newOffset, string removedText, string insertedText)
		{
			TextChanges = new List<TextChange> () {
				new TextChange (offset, newOffset, removedText, insertedText)
			};
		}

		/// <summary>
		/// Creates a new TextChangeEventArgs object.
		/// </summary>
		public TextChangeEventArgs (int offset, int newOffset, ITextSource removedText, ITextSource insertedText)
		{
			TextChanges = new List<TextChange> () {
				new TextChange (offset, newOffset, removedText, insertedText)
			};
		}

		/// <summary>
		/// Creates a new TextChangeEventArgs object.
		/// </summary>
		public TextChangeEventArgs (IReadOnlyList<TextChange> textChanges)
		{
			if (textChanges == null)
				throw new ArgumentNullException (nameof (textChanges));
			TextChanges = textChanges;
		}

		/// <summary>
		/// Gets the new offset where the specified offset moves after this document change.
		/// </summary>
		public virtual int GetNewOffset(int offset)
		{
			int changeDelta = 0;
			for (int i = 0; i < TextChanges.Count; ++i) {
				var change = TextChanges[i];
				if (offset <= change.Offset + change.RemovalLength) {
					if (offset >= change.Offset) {
						changeDelta = changeDelta - (offset - change.Offset) + change.InsertionLength;
					}
					break;
				}

				changeDelta += change.ChangeDelta;
			}

			return offset + changeDelta;
		}

		/// <summary>
		/// Creates TextChangeEventArgs for the reverse change.
		/// </summary>
		public virtual TextChangeEventArgs Invert()
		{
			var invertedChanges = new List<TextChange> (TextChanges.Count);
			for (int i = TextChanges.Count - 1; i >= 0; i--) {
				var c = TextChanges [i];
				invertedChanges.Add (new TextChange(c.Offset, c.InsertedText, c.RemovedText));
			}
			return new TextChangeEventArgs(invertedChanges);
		}

		public override string ToString ()
		{
			return string.Format ("[TextChangeEventArgs: #TextChanges={0}]", TextChanges.Count);
		}

		static string Escape (string text)
		{
			if (text == null)
				return null;
			var sb = StringBuilderCache.Allocate ();
			foreach (var ch in text) {
				switch (ch) {
				case '\r':
					sb.Append ("\\r");
					break;
				case '\n':
					sb.Append ("\\n");
					break;
				case '\t':
					sb.Append ("\\t");
					break;
				default:
					sb.Append (ch);
					break;
				}
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}
	}
}

