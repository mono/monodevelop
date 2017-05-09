//
// ISegment.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Core.Text
{
	/// <summary>
	/// An (Offset,Length)-pair.
	/// </summary>
	public interface ISegment
	{
		/// <summary>
		/// Gets the start offset of the segment.
		/// </summary>
		int Offset { get; }

		/// <summary>
		/// Gets the length of the segment.
		/// </summary>
		/// <remarks>For line segments (IDocumentLine), the length does not include the line delimeter.</remarks>
		int Length { get; }

		/// <summary>
		/// Gets the end offset of the segment.
		/// </summary>
		/// <remarks>EndOffset = Offset + Length;</remarks>
		int EndOffset { get; }
	}

	/// <summary>
	/// An (Offset, Length) pair representing a text span.
	/// </summary>
	public struct TextSegment : IEquatable<TextSegment>, ISegment
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
		/// Initializes a new instance of the <see cref="TextSegment"/> struct.
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

		/// <summary>
		/// Initializes a new instance of the <see cref="TextSegment"/> struct.
		/// </summary>
		public TextSegment (ISegment segment) : this(segment.Offset, segment.Length)
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
		/// Determines whether the specified <see cref="TextSegment"/> is equal to the current <see cref="TextSegment"/>.
		/// </summary>
		/// <param name='other'>
		/// The <see cref="TextSegment"/> to compare with the current <see cref="TextSegment"/>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="TextSegment"/> is equal to the current
		/// <see cref="TextSegment"/>; otherwise, <c>false</c>.
		/// </returns>
		public bool Equals (TextSegment other)
		{
			return Equals (this, other);
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="TextSegment"/>.
		/// </summary>
		/// <param name='obj'>
		/// The <see cref="System.Object"/> to compare with the current <see cref="TextSegment"/>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="System.Object"/> is equal to the current
		/// <see cref="TextSegment"/>; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals (object obj)
		{
			var otherSegment = obj as ISegment;
			if (otherSegment == null)
				return false;
			return Offset == otherSegment.Offset && length == otherSegment.Length;
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="TextSegment"/> object.
		/// </summary>
		/// <returns>
		/// A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.
		/// </returns>
		public override int GetHashCode ()
		{
			return Offset ^ Length;
		}

		public static TextSegment FromBounds (int startOffset, int endOffset)
		{
			if (startOffset > endOffset)
				throw new ArgumentOutOfRangeException ("endOffset", "endOffset < startOffset");
			return new TextSegment (startOffset, endOffset - startOffset);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="TextSegment"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the current <see cref="TextSegment"/>.
		/// </returns>
		public override string ToString ()
		{
			return string.Format ("[TextSegment: Offset={0}, Length={1}]", Offset, Length);
		}
	}

	/// <summary>
	/// An abstract implementation of the ISegment (Offset, Length) pair representing a text span.
	/// </summary>
	public abstract class AbstractSegment : ISegment
	{
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
		/// Initializes a new instance of the <see cref="TextSegment"/> struct.
		/// </summary>
		/// <param name='offset'>
		/// The offset of the segment.
		/// </param>
		/// <param name='length'>
		/// The length of the segment.
		/// </param>
		protected AbstractSegment (int offset, int length)
		{
			if (length < 0)
				throw new System.ArgumentOutOfRangeException (nameof (length), "was " + length);
			this.offset = offset;
			this.length = length;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="TextSegment"/> struct.
		/// </summary>
		protected AbstractSegment (ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			this.offset = segment.Offset;
			this.length = segment.Length;
		}

		public override string ToString ()
		{
			return string.Format ("[AbstractSegment: Offset={0}, Length={1}]", Offset, Length);
		}
	}


	/// <summary>
	/// Extension methods for <see cref="ISegment"/>.
	/// </summary>
	public static class ISegmentExtensions
	{
		/// <summary>
		/// Gets whether <paramref name="segment"/> fully contains the specified segment.
		/// </summary>
		/// <remarks>
		/// Use <c>segment.Contains(offset, 0)</c> to detect whether a segment (end inclusive) contains offset;
		/// use <c>segment.Contains(offset, 1)</c> to detect whether a segment (end exclusive) contains offset.
		/// </remarks>
		public static bool Contains (this ISegment segment, int offset, int length)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return segment.Offset <= offset && offset + length <= segment.EndOffset;
		}

		/// <summary>
		/// Gets whether <paramref name="segment"/> fully contains the specified segment.
		/// </summary>
		public static bool Contains (this ISegment segment, ISegment span)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			if (span == null)
				throw new ArgumentNullException ("span");
			return segment.Offset <= span.Offset && span.EndOffset <= segment.EndOffset;
		}

		/// <summary>
		/// Gets whether the offset is within the <paramref name="segment"/>.
		/// </summary>
		public static bool Contains (this ISegment segment, int offset)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return unchecked((uint)(offset - segment.Offset) < (uint)segment.Length);
		}

		/// <summary>
		/// Gets whether the offset is within the <paramref name="segment"/>.
		/// </summary>
		public static bool IsInside (this ISegment segment, int offset)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return unchecked((uint)(offset - segment.Offset) <= (uint)segment.Length);
		}

		/// <summary>
		/// Determines whether <paramref name="other"/> overlaps this span. Two spans are considered to overlap 
		/// if they have positions in common and neither is empty. Empty spans do not overlap with any 
		/// other span.
		/// </summary>
		public static bool OverlapsWith (this ISegment segment, ISegment other)
		{
			int overlapStart = Math.Max (segment.Offset, other.Offset);
			int overlapEnd = Math.Min (segment.EndOffset, other.EndOffset);
			return overlapStart < overlapEnd;
		}

		public static ISegment AdjustSegment (this ISegment segment, TextChangeEventArgs args)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			var newStartOffset = args.GetNewOffset (segment.Offset);
			var newEndOffset = args.GetNewOffset (segment.EndOffset);
			return new TextSegment (newStartOffset, newEndOffset - newStartOffset);
		}

		public static IEnumerable<ISegment> AdjustSegments (this IEnumerable<ISegment> segments, TextChangeEventArgs args)
		{
			if (segments == null)
				throw new ArgumentNullException ("segments");
			foreach (var segment in segments) {
				yield return segment.AdjustSegment (args);
			}
		}

		public static bool IsInvalid (this ISegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException (nameof (segment));
			return segment.Offset < 0;
		}
	}
}