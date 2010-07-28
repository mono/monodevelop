// GapBuffer.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Text;
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public sealed class GapBuffer : IBuffer
	{
		char[] buffer = new char[0];
		
		int gapBegin  = 0;
		int gapEnd    = 0;
		int gapLength = 0;
		
		const int minGapLength = 16 * 1024;
		const int maxGapLength = 256 * 1024;
		
		public int Length {
			get {
				return buffer.Length - gapLength;
			}
		}
		
		public void Insert (int offset, string text)
		{
			Replace (offset, 0, text);
		}
		
		public void Remove (int offset, int count)
		{
			Replace (offset, count, null);
		}
		
		public string GetTextAt (ISegment segment)
		{
			return GetTextAt (segment.Offset, segment.Length);
		}
		
		public string Text {
			get {
				return GetTextAt (0, Length);
			}
			set {
				buffer = value != null ? value.ToCharArray () : new char[0];
				gapBegin = gapEnd = gapLength = 0;
			}
		}
		
		public char GetCharAt (int offset)
		{
			return buffer[offset < gapBegin ? offset : offset + gapLength];
		}
		
		public string GetTextAt (int offset, int count)
		{
			int end = offset + count;
			if (end < gapBegin) 
				return new string (buffer, offset, count);
			if (offset > gapBegin) 
				return new string (buffer, offset + gapLength, count);
		
			int leftBlockSize = gapBegin - offset;
			int rightBlockSize = end - gapBegin;
			char[] result = new char [leftBlockSize + rightBlockSize];
			Array.Copy (buffer, offset, result, 0, leftBlockSize);
			Array.Copy (buffer, gapEnd, result, leftBlockSize, rightBlockSize);
			return new string (result);
		}
		
		public void Replace (int offset, int count, string text)
		{
			if (!string.IsNullOrEmpty (text)) { 
				PlaceGap (offset, text.Length - count);
				text.CopyTo (0, buffer, offset, text.Length);
				gapBegin += text.Length;
			} else {
				PlaceGap (offset, 0);
			}
			gapEnd   += count; 
			gapLength = gapEnd - gapBegin;
			if (gapLength > maxGapLength) 
				CreateBuffer (gapBegin, minGapLength);
		}
		
		void PlaceGap (int newOffset, int minLength)
		{
			if (gapLength < minLength) {
				if (minLength < maxGapLength) {
					CreateBuffer (newOffset, minLength + (maxGapLength - minLength) / 2);
				} else {
					CreateBuffer (newOffset, minLength + minGapLength);
				}
				return;
			}
			
			int delta = gapBegin - newOffset;
			if (delta > 0) {
				Array.Copy (buffer, newOffset, buffer, gapEnd - delta, delta);
			} else {
				Array.Copy (buffer, gapEnd, buffer, gapBegin, -delta);
			}
			gapBegin -= delta;
			gapEnd   -= delta;
		}
		
		void CreateBuffer (int gapOffset, int gapLength)
		{
			gapLength = System.Math.Max (minGapLength, gapLength);
			
			char[] newBuffer = new char[Length + gapLength];
			if (gapOffset < gapBegin) {
				Array.Copy (buffer, 0, newBuffer, 0, gapOffset);
				Array.Copy (buffer, gapOffset, newBuffer, gapOffset + gapLength, gapBegin - gapOffset);
				Array.Copy (buffer, gapEnd, newBuffer, newBuffer.Length - (buffer.Length - gapEnd), buffer.Length - gapEnd);
			} else {
				Array.Copy (buffer, 0, newBuffer, 0, gapBegin);
				Array.Copy (buffer, gapEnd, newBuffer, gapBegin, gapOffset - gapBegin);
				int lastPartLength = newBuffer.Length - (gapOffset + gapLength);
				Array.Copy (buffer, buffer.Length - lastPartLength, newBuffer, gapOffset + gapLength, lastPartLength);
			}
			
			gapBegin  = gapOffset;
			gapEnd    = gapOffset + gapLength;
			this.gapLength = gapLength;
			buffer    = newBuffer;
		}
		
		unsafe int SearchForwardInternal (string value, int startIndex)
		{
			int valueLen = value.Length;
			if (startIndex >= buffer.Length - valueLen + 1)
				return -1;
			
			int count = buffer.Length;
			fixed (char* thisptr = buffer, valueptr = value) {
				char* ap = thisptr + startIndex;
				char* thisEnd = thisptr + count - valueLen + 1;
				char* gapBeginPtr = thisptr + gapBegin;
				char* gapEndPtr = thisptr + gapEnd;
				char* stopGap = gapBeginPtr - valueLen + 1;
				
				if (ap < gapBeginPtr) {
					while (ap != stopGap) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								if (ap[i] != valueptr[i])
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap++;
					}
					
					while (ap != gapBeginPtr) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								char* p = ap + i;
								if (p >= gapBeginPtr)
									p = p - gapBeginPtr + gapEndPtr;
								if (*p != valueptr[i])
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap++;
					}
				}
				
				if (ap < gapEndPtr)
					ap = gapEndPtr;
				
				while (ap != thisEnd) {
					if (*ap == *valueptr) {
						for (int i = 1; i < valueLen; i++) {
							if (ap[i] != valueptr[i])
								goto NextVal;
						}
						return (int)(ap - thisptr);
					}
					NextVal:
					ap++;
				}
			}
			return -1;
		}
		
		unsafe int SearchForwardInternalIgnoreCase (string value, int startIndex)
		{
			int valueLen = value.Length;
			if (startIndex >= buffer.Length - valueLen + 1)
				return -1;
			
			int count = buffer.Length;
			fixed (char* thisptr = buffer, valueptr = value) {
				char* ap = thisptr + startIndex;
				char* thisEnd = thisptr + count - valueLen + 1;
				char* gapBeginPtr = thisptr + gapBegin;
				char* gapEndPtr = thisptr + gapEnd;
				char* stopGap = gapBeginPtr - valueLen + 1;
				
				if (ap < gapBeginPtr) {
					while (ap != stopGap) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								if (char.ToUpper (ap[i]) != valueptr[i])
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap++;
					}
					
					while (ap != gapBeginPtr) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								char* p = ap + i;
								if (p >= gapBeginPtr)
									p = p - gapBeginPtr + gapEndPtr;
								if (char.ToUpper (*p) != valueptr[i])
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap++;
					}
				}
				
				if (ap < gapEndPtr)
					ap = gapEndPtr;
				
				while (ap != thisEnd) {
					if (*ap == *valueptr) {
						for (int i = 1; i < valueLen; i++) {
							if (char.ToUpper (ap[i]) != valueptr[i])
								goto NextVal;
						}
						return (int)(ap - thisptr);
					}
					NextVal:
					ap++;
				}
			}
			return -1;
		}
		
		public IEnumerable<int> SearchForward (string pattern, int startIndex)
		{
			int idx = startIndex;
			while ((idx = SearchForwardInternal (pattern, idx)) != -1) {
				yield return idx;
				idx += pattern.Length;
			}
		}
		
		public IEnumerable<int> SearchForwardIgnoreCase (string pattern, int startIndex)
		{
			pattern = pattern.ToUpper ();
			int idx = startIndex;
			while ((idx = SearchForwardInternalIgnoreCase (pattern, idx)) != -1) {
				yield return idx;
				idx += pattern.Length;
			}
		}
		
		
		unsafe int SearchBackwardInternal (string value, int startIndex)
		{
			int valueLen = value.Length;
			if (startIndex < valueLen)
				return -1;
			
			int count = buffer.Length;
			fixed (char* thisptr = buffer, v = value) {
				char* valueptr = v + valueLen - 1;
				char* ap = thisptr + startIndex;
				char* thisEnd = thisptr + count - valueLen + 1;
				char* gapBeginPtr = thisptr + gapBegin;
				char* gapEndPtr = thisptr + gapEnd;
				char* stopGap = gapEndPtr - valueLen + 1;
				
				if (ap >= gapEndPtr) {
					while (ap != stopGap) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								if (*(ap - i) != *(valueptr - i))
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap--;
					}
					
					while (ap != gapEndPtr) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								char* p = ap - i;
								if (p >= gapEndPtr)
									p += gapBeginPtr - gapEndPtr;
								if (*p != *(valueptr - i))
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap--;
					}
				}
				
				if (ap > gapBeginPtr)
					ap = gapBeginPtr;
				
				while (ap != thisEnd) {
					if (*ap == *valueptr) {
						for (int i = 1; i < valueLen; i++) {
							if (ap[i] != *(valueptr - i))
								goto NextVal;
						}
						return (int)(ap - thisptr);
					}
					NextVal:
					ap--;
				}
			}
			return -1;
		}
		
		unsafe int SearchBackwardInternalIgnoreCase (string value, int startIndex)
		{
			int valueLen = value.Length;
			if (startIndex < valueLen)
				return -1;
			
			int count = buffer.Length;
			fixed (char* thisptr = buffer, v = value) {
				char* valueptr = v + valueLen - 1;
				char* ap = thisptr + startIndex;
				char* thisEnd = thisptr + count - valueLen + 1;
				char* gapBeginPtr = thisptr + gapBegin;
				char* gapEndPtr = thisptr + gapEnd;
				char* stopGap = gapEndPtr - valueLen + 1;
				
				if (ap >= gapEndPtr) {
					while (ap != stopGap) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								if (char.ToUpper (*(ap - i)) != char.ToUpper (*(valueptr - i)))
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap--;
					}
					
					while (ap != gapEndPtr) {
						if (*ap == *valueptr) {
							for (int i = 1; i < valueLen; i++) {
								char* p = ap - i;
								if (p >= gapEndPtr)
									p += gapBeginPtr - gapEndPtr;
								if (char.ToUpper (*p) != char.ToUpper (*(valueptr - i)))
									goto NextVal;
							}
							return (int)(ap - thisptr);
						}
						NextVal:
						ap--;
					}
				}
				
				if (ap > gapBeginPtr)
					ap = gapBeginPtr;
				
				while (ap != thisEnd) {
					if (*ap == *valueptr) {
						for (int i = 1; i < valueLen; i++) {
							if (char.ToUpper (ap[i]) != char.ToUpper (*(valueptr - i)))
								goto NextVal;
						}
						return (int)(ap - thisptr);
					}
					NextVal:
					ap--;
				}
			}
			return -1;
		}
		
		
		public IEnumerable<int> SearchBackward (string pattern, int startIndex)
		{
			int idx = startIndex;
			while ((idx = SearchBackwardInternal (pattern, idx)) != -1) {
				yield return idx;
				idx -= pattern.Length;
			}
		}
		
		public IEnumerable<int> SearchBackwardIgnoreCase (string pattern, int startIndex)
		{
			pattern = pattern.ToUpper ();
			int idx = startIndex;
			while ((idx = SearchBackwardInternalIgnoreCase (pattern, idx)) != -1) {
				yield return idx;
				idx -= pattern.Length;
			}
		}
		
		/* Boyer-Moore-Horspool-Raita implementation: (but on Intel Core i brute force outpeforms it.

	static int[] ProcessString (string pattern)
	{
		var result = new int[char.MaxValue];
		for (int i = 0; i < result.Length; i++)
			result[i] = pattern.Length - 1;
		for (int i = 0; i < pattern.Length - 1; ++i) {
			result[pattern[i]] = pattern.Length - i - 1;
		}
		return result;
	}
	unsafe static int bmhrSearchBytes (string text, string pattern, int textStart, int[] b)
	{
		int lastIndex = text.Length + pattern.Length - 1;
		if (textStart >= lastIndex)
			return -1;
		
		int m = pattern.Length - 1;
		int mMinusOne = pattern.Length - 2;
		
		var last = pattern[pattern.Length - 1];
		var first = pattern[0];
		
		fixed (char* textPtr = text, pattenrPtr = pattern) {
			char* i = textPtr + textStart + pattern.Length - 1;
			char* endText = textPtr + lastIndex;
			while (i < endText) {
				if (*i == last) {
					//if (*(i - m) == first) {
						char* k = i - 1;
						char* pp = pattenrPtr + mMinusOne;
						
						while (pp >= pattenrPtr && *k == *pp) {
							--k;
							--pp;
						}
						
						if (pp < pattenrPtr)
							return (int)(k - textPtr) + 1;
				//	}
				}
				i += b[*i];
			}
		}
		return -1;
	}
			 * */
	}
}