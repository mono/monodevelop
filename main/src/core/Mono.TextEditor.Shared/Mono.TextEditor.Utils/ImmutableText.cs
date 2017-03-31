//
// ImmutableText.cs
//
// A pruned and optimized version of javolution.text.Text
// Ported from IntelliJ IDEA File Version: 5.3, January 10, 2007. 
//
// Author:
//       Jean-Marie Dautelle <jean-marie@dautelle.com>
//       Wilfried Middleton
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

#region Java copyright notice
/*
 * Javolution - Java(tm) Solution for Real-Time and Embedded Systems
 * Copyright (c) 2012, Javolution (http://javolution.org/)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *    1. Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *
 *    2. Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

using System;
using System.Globalization;
using System.IO;
using System.Text;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor.Utils
{
	/// <summary>
	/// <p> This class represents an immutable character sequence with 
	/// fast {@link #concat concatenation}, {@link #insert insertion} and 
	/// {@link #delete deletion} capabilities (O[Log(n)]) instead of 
	/// O[n] for StringBuffer/StringBuilder).</p>
	/// 
	/// <p><i> Implementation Note: To avoid expensive copy operations , 
	/// {@link ImmutableText} instances are broken down into smaller immutable 
	/// sequences, they form a minimal-depth binary tree.
	/// The tree is maintained balanced automatically through <a 
	/// href="http://en.wikipedia.org/wiki/Tree_rotation">tree rotations</a>. 
	/// Insertion/deletions are performed in <code>O[Log(n)]</code>
	/// instead of <code>O[n]</code> for 
	/// <code>StringBuffer/StringBuilder</code>.</i></p>
	/// </summary>
	sealed class ImmutableText : ITextSource
	{
		/// <summary>Holds the default size for primitive blocks of characters.</summary>
		const int BLOCK_SIZE = 1 << 6;

		/// <summary>Holds the mask used to ensure a block boundary cesures.</summary>
		const int BLOCK_MASK = ~(BLOCK_SIZE - 1);

		static readonly LeafNode EMPTY_NODE = new Leaf8BitNode (new byte [0]);

		public static readonly ImmutableText Empty = new ImmutableText (EMPTY_NODE, null);

		readonly Node root;

		/// <summary>
		/// Returns the length of this text.
		/// </summary>
		/// <value>the number of characters (16-bits Unicode) composing this text.</value>
		public int Length {
			get {
				return root.Length;
			}
		}

		public ITextSourceVersion Version {
			get;
			internal set;
		}

		Encoding encoding;
		public Encoding Encoding {
			get {
				return encoding ?? Encoding.UTF8;
			}
			internal set {
				encoding = value;
			}
		}

		public string Text {
			get {
				return ToString ();
			}
		}

		volatile InnerLeaf myLastLeaf;

		/// <summary>
		/// Gets/Sets a single character.
		/// Runs in O(lg N) for random access. Sequential read-only access benefits from a special optimization and runs in amortized O(1).
		/// </summary>
		public char this [int index] {
			get {
				if (root is LeafNode) {
					return root [index];
				}

				var leaf = myLastLeaf;
				if (leaf == null || index < leaf.offset || index >= leaf.offset + leaf.leafNode.Length) {
					myLastLeaf = leaf = FindLeaf (index, 0);
				}
				return leaf.leafNode [index - leaf.offset];
			}
		}

		ImmutableText (Node node, Encoding encoding)
		{
			root = node;
			this.encoding = encoding;
		}

		public ImmutableText (string str)
		{
			root = CreateLeafNode (str.ToCharArray ());
		}

		public ImmutableText (char [] str)
		{
			root = CreateLeafNode (str);
		}

		/// <summary>
		/// Concatenates the specified text to the end of this text. 
		/// This method is very fast (faster even than 
		/// <code>StringBuffer.append(String)</code>) and still returns
		/// a text instance with an internal binary tree of minimal depth!
		/// </summary>
		/// <param name="that">that the text that is concatenated.</param>
		/// <returns><code>this + that</code></returns>
		public ImmutableText Concat (ImmutableText that)
		{
			return that.Length == 0 ? this : Length == 0 ? that : new ImmutableText (ConcatNodes (EnsureChunked ().root, that.EnsureChunked ().root), encoding);
		}

		/// <summary>
		/// Returns the text having the specified text inserted at 
		/// the specified location.
		/// </summary>
		/// <param name="index">index the insertion position.</param>
		/// <param name="txt">txt the text being inserted.</param>
		/// <returns>subtext(0, index).concat(txt).concat(subtext(index))</returns>
		/// <exception cref="IndexOutOfRangeException">if <code>(index &lt; 0) || (index &gt; this.Length)</code></exception>
		public ImmutableText InsertText (int index, ImmutableText txt)
		{
			return GetText (0, index).Concat (txt).Concat (SubText (index));
		}

		public ImmutableText InsertText (int index, string txt)
		{
			return InsertText (index, new ImmutableText (txt));
		}

		/// <summary>
		/// Returns the text without the characters between the specified indexes.
		/// </summary>
		/// <returns><code>subtext(0, start).concat(subtext(end))</code></returns>
		public ImmutableText RemoveText (int start, int count)
		{
			if (count == 0)
				return this;
			var end = start + count;
			if (end > Length)
				throw new IndexOutOfRangeException ();
			return EnsureChunked ().GetText (0, start).Concat (SubText (end));
		}

		/// <summary>
		/// Returns a portion of this text.
		/// </summary>
		/// <returns>the sub-text starting at the specified start position and ending just before the specified end position.</returns>
		public ImmutableText GetText (int start, int count)
		{
			var end = start + count;
			if ((start < 0) || (start > end) || (end > Length)) {
				throw new IndexOutOfRangeException (" start :" + start + " end :" + end + " needs to be between 0 <= " + Length);
			}
			if ((start == 0) && (end == Length)) {
				return this;
			}
			if (start == end) {
				return Empty;
			}

			return new ImmutableText (root.SubNode (start, end), encoding);
		}

		/// <summary>
		/// Copies the whole content of the immutable text into the specified array.
		/// Runs in O(N).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public void CopyTo (char [] destination, int destinationIndex)
		{
			CopyTo (0, destination, destinationIndex, Length);
		}

		/// <summary>
		/// Copies the a part of the immutable text into the specified array.
		/// Runs in O(lg N + M).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
		{
			VerifyRange (sourceIndex, count);
			VerifyArrayWithRange (destination, destinationIndex, count);
			root.CopyTo (sourceIndex, destination, destinationIndex, count);
		}

		/// <summary>
		/// Creates an array and copies the contents of the rope into it.
		/// Runs in O(N).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public char [] ToArray ()
		{
			char [] arr = new char [Length];
			CopyTo (0, arr, 0, arr.Length);
			return arr;
		}

		/// <summary>
		/// Creates an array and copies the contents of the rope into it.
		/// Runs in O(N).
		/// </summary>
		/// <remarks>
		/// This method counts as a read access and may be called concurrently to other read accesses.
		/// </remarks>
		public char [] ToArray (int offset, int length)
		{
			VerifyRange (offset, length);
			char [] arr = new char [length];
			CopyTo (offset, arr, 0, length);
			return arr;
		}

		void VerifyRange (int startIndex, int length)
		{
			if (startIndex < 0 || startIndex > Length) {
				throw new ArgumentOutOfRangeException (nameof (startIndex), startIndex, "0 <= startIndex <= " + Length.ToString (CultureInfo.InvariantCulture));
			}
			if (length < 0 || startIndex + length > Length) {
				throw new ArgumentOutOfRangeException (nameof (length), length, "0 <= length, startIndex(" + startIndex + ")+length(" + length + ") <= " + Length.ToString (CultureInfo.InvariantCulture));
			}
		}

		static void VerifyArrayWithRange (char [] array, int arrayIndex, int count)
		{
			if (array == null)
				throw new ArgumentNullException (nameof (array));
			if (arrayIndex < 0 || arrayIndex > array.Length) {
				throw new ArgumentOutOfRangeException (nameof (arrayIndex), arrayIndex, "0 <= arrayIndex <= " + array.Length.ToString (CultureInfo.InvariantCulture));
			}
			if (count < 0 || arrayIndex + count > array.Length) {
				throw new ArgumentOutOfRangeException (nameof (count), count, "0 <= length, arrayIndex(" + arrayIndex + ")+count <= " + array.Length.ToString (CultureInfo.InvariantCulture));
			}
		}

		public override string ToString ()
		{
			return root.ToString ();
		}

		public string ToString (int offset, int length)
		{
			char [] data = new char [length];
			CopyTo (offset, data, 0, length);
			return new string (data);
		}

		public void WriteTextTo (TextWriter output)
		{
			WriteTextTo (output, 0, Length);
		}

		public void WriteTextTo (TextWriter output, int start, int count)
		{
			int index = 0;
			while (index < count) {
				output.Write (this [start + index]);
				index++;
			}
		}

		public override bool Equals (object obj)
		{
			if (this == obj)
				return true;

			var that = obj as ImmutableText;
			if (that == null)
				return false;

			int len = Length;
			if (len != that.Length)
				return false;

			for (int i = 0; i < len; i++) {
				if (this [i] != that [i])
					return false;
			}
			return true;
		}

		int hash;
		public override int GetHashCode ()
		{
			int h = hash;
			if (h == 0) {
				for (int off = 0; off < Length; off++) {
					h = 31 * h + this [off];
				}
				hash = h;
			}
			return h;
		}

		#region Helper methods

		ImmutableText SubText (int start)
		{
			return GetText (start, Length - start);
		}

		static LeafNode CreateLeafNode (char [] str)
		{
			byte [] bytes = ToBytesIfPossible (str);
			if (bytes != null)
				return new Leaf8BitNode (bytes);
			return new WideLeafNode (str);
		}

		#region orinal version
		//static byte [] ToBytesIfPossible (char [] seq)
		//{
		//	byte [] bytes = new byte [seq.Length];
		//	for (int i = 0; i < bytes.Length; i++) {
		//		char c = seq [i];
		//		if ((c & 0xff00) != 0)
		//			return null;
		//		bytes [i] = (byte)c;
		//	}
		//	return bytes;
		//}
		#endregion

		unsafe static byte [] ToBytesIfPossible (char [] seq)
		{
			var bytes = new byte [seq.Length];
			fixed (byte* bBegin = bytes) {
				fixed (char* cBegin = seq) {
					var bPtr = bBegin;
					var bEnd = bBegin + bytes.Length;

					var cPtr = (ushort*)cBegin;
					while (bPtr != bEnd) {
						var c = *cPtr++;
						if (c > 0xFF)
							return null;
						*(bPtr++) = (byte)c;
					}
				}
			}
			return bytes;
		}

		/// <summary>
		/// When first loaded, ImmutableText contents are stored as a single large array. This saves memory but isn't
		/// modification-friendly as it disallows slightly changed texts to retain most of the internal structure of the
		/// original document. Whoever retains old non-chunked version will use more memory than really needed.
		/// </summary>
		/// <returns>A copy of this text better prepared for small modifications to fully enable structure-sharing capabilities</returns>
		ImmutableText EnsureChunked ()
		{
			if (Length > BLOCK_SIZE && root is LeafNode) {
				return new ImmutableText (NodeOf ((LeafNode)root, 0, Length), encoding);
			}
			return this;
		}

		static Node NodeOf (LeafNode node, int offset, int length)
		{
			if (length <= BLOCK_SIZE) {
				return node.SubNode (offset, offset + length);
			}
			// Splits on a block boundary.
			int half = ((length + BLOCK_SIZE) >> 1) & BLOCK_MASK;
			return new CompositeNode (NodeOf (node, offset, half), NodeOf (node, offset + half, length - half));
		}

		static Node ConcatNodes (Node node1, Node node2)
		{
			// All Text instances are maintained balanced:
			//   (head < tail * 2) & (tail < head * 2)
			int length = node1.Length + node2.Length;
			if (length <= BLOCK_SIZE) { // Merges to primitive.
				var mergedArray = new char [node1.Length + node2.Length];
				node1.CopyTo (0, mergedArray, 0, node1.Length);
				node2.CopyTo (0, mergedArray, node1.Length, node2.Length);
				return CreateLeafNode (mergedArray);
			} else { // Returns a composite.
				Node head = node1;
				Node tail = node2;
				var compositeTail = tail as CompositeNode;
				if (((head.Length << 1) < tail.Length) && compositeTail != null) {
					// head too small, returns (head + tail/2) + (tail/2)
					if (compositeTail.head.Length > compositeTail.tail.Length) {
						// Rotates to concatenate with smaller part.
						compositeTail = (CompositeNode)compositeTail.RotateRight ();
					}
					head = ConcatNodes (head, compositeTail.head);
					tail = compositeTail.tail;
				} else {
					var compositeHead = head as CompositeNode;
					if (((tail.Length << 1) < head.Length) && compositeHead != null) {
						// tail too small, returns (head/2) + (head/2 concat tail)
						if (compositeHead.tail.Length > compositeHead.head.Length) {
							// Rotates to concatenate with smaller part.
							compositeHead = (CompositeNode)compositeHead.RotateLeft ();
						}
						tail = ConcatNodes (compositeHead.tail, tail);
						head = compositeHead.head;
					}
				}

				return new CompositeNode (head, tail);
			}
		}

		InnerLeaf FindLeaf (int index, int offset)
		{
			Node node = root;
			while (true) {
				if (index >= node.Length)
					throw new IndexOutOfRangeException ();

				var leafNode = node as LeafNode;
				if (leafNode != null)
					return new InnerLeaf (leafNode, offset);

				var composite = (CompositeNode)node;
				if (index < composite.head.Length) {
					node = composite.head;
				} else {
					offset += composite.head.Length;
					index -= composite.head.Length;
					node = composite.tail;
				}
			}
		}

		public char GetCharAt (int offset)
		{
			return this [offset];
		}

		TextReader ITextSource.CreateReader ()
		{
			return new ImmutableTextTextReader (this);
		}

		TextReader ITextSource.CreateReader (int offset, int length)
		{
			return new ImmutableTextTextReader (GetText (offset, length));
		}

		ITextSource ITextSource.CreateSnapshot ()
		{
			return this;
		}

		ITextSource ITextSource.CreateSnapshot (int offset, int length)
		{
			return GetText (offset, length);
		}

		string ITextSource.GetTextAt (int offset, int length)
		{
			return ToString (offset, length);
		}

		class InnerLeaf
		{
			internal readonly LeafNode leafNode;
			internal readonly int offset;

			public InnerLeaf (LeafNode leafNode, int offset)
			{
				this.leafNode = leafNode;
				this.offset = offset;
			}
		}

		#endregion

		#region Tree

		abstract class Node
		{
			public abstract int Length { get; }

			public abstract char this [int index] {
				get;
			}

			public abstract void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count);

			public abstract Node SubNode (int start, int end);

			public override string ToString ()
			{
				int len = Length;
				char [] data = new char [len];
				CopyTo (0, data, 0, len);
				return new string (data);
			}

			public Node subSequence (int start, int end)
			{
				return SubNode (start, end);
			}
		}

		abstract class LeafNode : Node
		{
		}

		sealed class WideLeafNode : LeafNode
		{
			readonly char [] data;

			public override int Length {
				get {
					return data.Length;
				}
			}

			public override char this [int index] {
				get {
					return data [index];
				}
			}

			public WideLeafNode (char [] data)
			{
				this.data = data;
			}

			public override void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
			{
				Array.Copy (data, sourceIndex, destination, destinationIndex, count);
			}

			public override Node SubNode (int start, int end)
			{
				if (start == 0 && end == Length) {
					return this;
				}
				var subArray = new char [end - start];
				Array.Copy (data, start, subArray, 0, subArray.Length);
				return CreateLeafNode (subArray);
			}

			public override string ToString ()
			{
				return new string (data);
			}
		}

		sealed class Leaf8BitNode : LeafNode
		{
			readonly byte [] data;

			public override int Length {
				get {
					return data.Length;
				}
			}

			public override char this [int index] {
				get {
					return (char)data [index];
				}
			}

			public Leaf8BitNode (byte [] data)
			{
				this.data = data;
			}

			#region original version
			//public override void GetChars(int start, int end, char[] dest, int destPos) 
			//{
			//	for (int i=start;i<end;i++) {
			//		dest[destPos++] = (char)data[i];
			//	}
			//}
			#endregion

			public unsafe override void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
			{
				fixed (byte* bPtr = data)
				{
					fixed (char* cPtr = destination)
					{
						var b = bPtr + sourceIndex;
						var endPtr = b + count;
						var endPtr4 = endPtr - count % 4;

						var c = (short*)cPtr + destinationIndex;

						while (b != endPtr4) {
							*(c++) = *(b++);
							*(c++) = *(b++);
							*(c++) = *(b++);
							*(c++) = *(b++);
						}

						while (b != endPtr) {
							*(c++) = *(b++);
						}
					}
				}
			}

			public override Node SubNode (int start, int end)
			{
				if (start == 0 && end == Length) {
					return this;
				}
				int length = end - start;
				byte [] chars = new byte [length];
				Array.Copy (data, start, chars, 0, length);
				return new Leaf8BitNode (chars);
			}
		}

		sealed class CompositeNode : Node
		{
			readonly int count;
			internal readonly Node head;
			internal readonly Node tail;

			public override int Length {
				get {
					return count;
				}
			}

			public override char this [int index] {
				get {
					int headLength = head.Length;
					return index < headLength ? head [index] : tail [index - headLength];
				}
			}

			public CompositeNode (Node head, Node tail)
			{
				count = head.Length + tail.Length;
				this.head = head;
				this.tail = tail;
			}

			internal Node RotateRight ()
			{
				// See: http://en.wikipedia.org/wiki/Tree_rotation
				var P = head as CompositeNode;
				if (P == null) {
					return this; // Head not a composite, cannot rotate.
				}
				var A = P.head;
				var B = P.tail;
				var C = tail;
				return new CompositeNode (A, new CompositeNode (B, C));
			}

			internal Node RotateLeft ()
			{
				// See: http://en.wikipedia.org/wiki/Tree_rotation
				var Q = tail as CompositeNode;
				if (Q == null) {
					return this; // Tail not a composite, cannot rotate.
				}
				var B = Q.head;
				var C = Q.tail;
				var A = head;
				return new CompositeNode (new CompositeNode (A, B), C);
			}

			public override void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
			{
				int cesure = head.Length;
				if (sourceIndex + count <= cesure) {
					head.CopyTo (sourceIndex, destination, destinationIndex, count);
				} else if (sourceIndex >= cesure) {
					tail.CopyTo (sourceIndex - cesure, destination, destinationIndex, count);
				} else { // Overlaps head and tail.
					var headChunkSize = cesure - sourceIndex;
					head.CopyTo (sourceIndex, destination, destinationIndex, headChunkSize);
					tail.CopyTo (0, destination, destinationIndex + headChunkSize, count - headChunkSize);
				}
			}

			public override Node SubNode (int start, int end)
			{
				int cesure = head.Length;
				if (end <= cesure) {
					return head.SubNode (start, end);
				}
				if (start >= cesure) {
					return tail.SubNode (start - cesure, end - cesure);
				}
				if ((start == 0) && (end == count)) {
					return this;
				}
				// Overlaps head and tail.
				return ConcatNodes (head.SubNode (start, cesure), tail.SubNode (0, end - cesure));
			}
		}

		#endregion
	}
}