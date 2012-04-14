// 
// TextSegment.cs
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

namespace Mono.TextEditor
{
	/// <summary>
	/// An (Offset, Length) pair representing a text span.
	/// </summary>
	public struct TextSegment : IEquatable<TextSegment>, ICSharpCode.NRefactory.Editor.ISegment
	{
		public static readonly TextSegment Invalid = new TextSegment (-1, 0);

		readonly int offset;

		/// <summary>
		///  Gets the start offset of the segment. 
		/// </summary>
		/// <value>
		/// The offset.
		/// </value>
		public int Offset {
			get {
				return offset;
			}
		}

		readonly int length;

		/// <summary>
		/// Gets the length of the segment. 
		/// </summary>
		/// <value>
		/// The length.
		/// </value>
		public int Length {
			get {
				return length;
			}
		}

		/// <summary>
		/// Gets the end offset of the segment. 
		/// </summary>
		/// <remarks>
		/// EndOffset = Offset + Length;
		/// </remarks>
		/// <value>
		/// The end offset.
		/// </value>
		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is empty.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is empty; otherwise, <c>false</c>.
		/// </value>
		public bool IsEmpty {
			get {
				return Length == 0;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is invalid.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is invalid; otherwise, <c>false</c>.
		/// </value>
		public bool IsInvalid {
			get {
				return Offset < 0;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mono.TextEditor.TextSegment"/> struct.
		/// </summary>
		/// <param name='offset'>
		/// The offset of the segment.
		/// </param>
		/// <param name='length'>
		/// The length of the segment.
		/// </param>
		public TextSegment (int offset, int length)
		{
			this.offset = offset;
			this.length = length;
		}

		public TextSegment (ICSharpCode.NRefactory.Editor.ISegment nrefactorySegment) : this (nrefactorySegment.Offset, nrefactorySegment.Length)
		{
		}

		public static bool operator == (TextSegment left, TextSegment right)
		{
			return Equals (left, right);
		}

		public static bool operator != (TextSegment left, TextSegment right)
		{
			return !Equals (left, right);
		}

		public static bool Equals (TextSegment left, TextSegment right)
		{
			return left.Offset == right.Offset && left.Length == right.Length;
		}

		/// <summary>
		/// Determines whether this instance contains the specified offset. 
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance contains the specified offset (upper bound exclusive); otherwise, <c>false</c>.
		/// </returns>
		/// <param name='offset'>
		/// The offset.
		/// </param>
		public bool Contains (int offset)
		{
			return Offset <= offset && offset < EndOffset;
		}

		/// <summary>
		/// Determines whether this instance contains the specified segment. 
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance contains the specified segment (upper bound inclusive); otherwise, <c>false</c>.
		/// </returns>
		/// <param name='segment'>
		/// The segment.
		/// </param>
		public bool Contains (TextSegment segment)
		{
			return Offset <= segment.Offset && segment.EndOffset <= EndOffset;
		}

		/// <summary>
		/// Determines whether this instance is inside the specified offset. 
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance is inside the specified offset (upper bound inclusive); otherwise, <c>false</c>.
		/// </returns>
		/// <param name='offset'>
		/// The offset offset.
		/// </param>
		public bool IsInside (int offset)
		{
			return Offset <= offset && offset <= EndOffset;
		}

		/// <summary>
		/// Determines whether the specified <see cref="Mono.TextEditor.TextSegment"/> is equal to the current <see cref="Mono.TextEditor.TextSegment"/>.
		/// </summary>
		/// <param name='other'>
		/// The <see cref="Mono.TextEditor.TextSegment"/> to compare with the current <see cref="Mono.TextEditor.TextSegment"/>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="Mono.TextEditor.TextSegment"/> is equal to the current
		/// <see cref="Mono.TextEditor.TextSegment"/>; otherwise, <c>false</c>.
		/// </returns>
		public bool Equals (TextSegment other)
		{
			return Equals (this, other);
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="Mono.TextEditor.TextSegment"/>.
		/// </summary>
		/// <param name='obj'>
		/// The <see cref="System.Object"/> to compare with the current <see cref="Mono.TextEditor.TextSegment"/>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="System.Object"/> is equal to the current
		/// <see cref="Mono.TextEditor.TextSegment"/>; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals (object obj)
		{
			return obj is TextSegment && Equals (this, (TextSegment)obj);
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="Mono.TextEditor.TextSegment"/> object.
		/// </summary>
		/// <returns>
		/// A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.
		/// </returns>
		public override int GetHashCode ()
		{
			return Offset ^ Length;
		}

		public DocumentRegion GetRegion (TextDocument document)
		{
			if (document == null)
				throw new System.ArgumentNullException ("document");
			return new DocumentRegion (document.OffsetToLocation (Offset), document.OffsetToLocation (EndOffset));
		}

		public static TextSegment FromBounds (int startOffset, int endOffset)
		{
			if (startOffset > endOffset)
				throw new ArgumentOutOfRangeException ("endOffset", "endOffset < startOffset");
			return new TextSegment (startOffset, endOffset - startOffset);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Mono.TextEditor.TextSegment"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the current <see cref="Mono.TextEditor.TextSegment"/>.
		/// </returns>
		public override string ToString ()
		{
			return string.Format ("[TextSegment: Offset={0}, Length={1}]", Offset, Length);
		}
	}
}
