//
// MemoryExtensions.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
namespace System
{
	static class MemoryExtensions
	{
		public static int IndexOf (this ReadOnlySpan<char> span, char value, int startIndex)
		{
			var indexInSlice = span.Slice(startIndex).IndexOf(value);
			return indexInSlice == -1 ? -1 : startIndex + indexInSlice;
		}

		public static int IndexOfAny (this ReadOnlySpan<char> span, Span<char> values, int startIndex)
		{
			var indexInSlice = span.Slice (startIndex).IndexOfAny (values);
			return indexInSlice == -1 ? -1 : startIndex + indexInSlice;
		}

		/// <summary>
		/// Optimized overload of ToString() with takes the original value to not allocate in that case.
		/// In case two different strings with the same length are passed, the method will not work as you think.
		/// </summary>
		/// <returns>The originalValue if the lengths are the same. Otherwise a new string is allocated from span</returns>
		/// <param name="span">The span string.</param>
		/// <param name="originalValue">The original string the span is created from.</param>
		public static string ToStringWithOriginal (this ReadOnlySpan<char> span, string originalValue)
		{
			return span.Length == originalValue.Length ? originalValue : span.ToString ();
		}
	}
}
