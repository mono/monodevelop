//
// StringBuilderCache.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Threading;
using System.Collections.Immutable;
using System.Security.Cryptography;

namespace MonoDevelop.Core
{
	/// <summary>
	/// This is a pool for storing StringBuilder objects.
	/// </summary>
	public static class StringBuilderCache
	{
		static ImmutableQueue<CachedStringBuilder> pool = ImmutableQueue<CachedStringBuilder>.Empty;

		public static CachedStringBuilder New ()
		{
			pool = pool.Dequeue (out CachedStringBuilder result);
			if (result != null) {
				result.Cached = false;
				return result;
			}
			return new CachedStringBuilder ();
		}

		internal static void Put (CachedStringBuilder sb)
		{
			sb.Clear ();
			sb.Cached = true;
			pool = pool.Enqueue (sb);
		}
	}

	/// <summary>
	/// A string builder which puts itself into the object pool on a ToString() call.
	/// </summary>
	public class CachedStringBuilder
	{
		readonly StringBuilder sb = new StringBuilder ();

		internal bool Cached { get; set; }

		public int Capacity { get => sb.Capacity; set => sb.Capacity = value; }
		public int Length { get => sb.Length; set => sb.Length = value; }

		public int MaxCapacity { get => sb.MaxCapacity; }

		public char this [int index] { get => sb [index]; set => sb [index] = value; }

		public CachedStringBuilder Append (long value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (ulong value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (uint value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (ushort value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (string value, int startIndex, int count)
		{
			sb.Append (value, startIndex, count);
			return this;
		}

		public CachedStringBuilder Append (string value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (float value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (sbyte value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (object value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (int value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (char [] value, int startIndex, int charCount)
		{
			sb.Append (value, startIndex, charCount);
			return this;
		}

		public CachedStringBuilder Append (double value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (decimal value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (char [] value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (char value, int repeatCount)
		{
			sb.Append (value, repeatCount);
			return this;
		}

		public unsafe CachedStringBuilder Append (char* value, int valueCount)
		{
			sb.Append (value, valueCount);
			return this;
		}

		public CachedStringBuilder Append (char value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (byte value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (bool value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder Append (short value)
		{
			sb.Append (value);
			return this;
		}

		public CachedStringBuilder AppendFormat (string format, object arg0, object arg1, object arg2)
		{
			sb.AppendFormat (format, arg0, arg1, arg2);
			return this;
		}

		public CachedStringBuilder AppendFormat (string format, params object [] args)
		{
			sb.AppendFormat (format, args);
			return this;
		}

		public CachedStringBuilder AppendFormat (string format, object arg0, object arg1)
		{
			sb.AppendFormat (format, arg0, arg1);
			return this;
		}

		public CachedStringBuilder AppendFormat (string format, object arg0)
		{
			sb.AppendFormat (format, arg0);
			return this;
		}

		public CachedStringBuilder AppendFormat (IFormatProvider provider, string format, object arg0, object arg1, object arg2)
		{
			sb.AppendFormat (provider, format, arg0, arg1, arg2);
			return this;
		}

		public CachedStringBuilder AppendFormat (IFormatProvider provider, string format, object arg0, object arg1)
		{
			sb.AppendFormat (provider, format, arg0, arg1);
			return this;
		}

		public CachedStringBuilder AppendFormat (IFormatProvider provider, string format, object arg0)
		{
			sb.AppendFormat (provider, format, arg0);
			return this;
		}

		public CachedStringBuilder AppendFormat (IFormatProvider provider, string format, params object [] args)
		{
			sb.AppendFormat (provider, format, args);
			return this;
		}

		public CachedStringBuilder AppendLine ()
		{
			sb.AppendLine ();
			return this;
		}

		public CachedStringBuilder AppendLine (string value)
		{
			sb.AppendLine (value);
			return this;
		}

		public CachedStringBuilder Clear ()
		{
			sb.Clear ();
			return this;
		}

		public void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count)
		{
			sb.CopyTo (sourceIndex, destination, destinationIndex, count);
		}

		public int EnsureCapacity (int capacity)
		{
			return sb.EnsureCapacity(capacity);
		}

		public bool Equals (StringBuilder sb)
		{
			return this.sb.Equals (sb);
		}

		public bool Equals (CachedStringBuilder csb)
		{
			return this.sb.Equals (csb.sb);
		}

		public CachedStringBuilder Insert (int index, uint value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, ushort value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, string value, int count)
		{
			sb.Insert (index, value, count);
			return this;
		}

		public CachedStringBuilder Insert (int index, string value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, float value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, sbyte value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, ulong value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, object value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, double value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, int value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, decimal value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, char [] value, int startIndex, int charCount)
		{
			sb.Insert (index, value, startIndex, charCount);
			return this;
		}

		public CachedStringBuilder Insert (int index, char [] value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, char value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, byte value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, bool value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, long value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Insert (int index, short value)
		{
			sb.Insert (index, value);
			return this;
		}

		public CachedStringBuilder Remove (int startIndex, int length)
		{
			sb.Remove (startIndex, length);
			return this;
		}

		public CachedStringBuilder Replace (string oldValue, string newValue, int startIndex, int count)
		{
			sb.Replace (oldValue, newValue, startIndex, count);
			return this;
		}

		public CachedStringBuilder Replace (char oldChar, char newChar)
		{
			sb.Replace (oldChar, newChar);
			return this;
		}

		public CachedStringBuilder Replace (char oldChar, char newChar, int startIndex, int count)
		{
			sb.Replace (oldChar, newChar, startIndex, count);
			return this;
		}

		public CachedStringBuilder Replace (string oldValue, string newValue)
		{
			sb.Replace (oldValue, newValue);
			return this;
		}

		public override string ToString ()
		{
			if (Cached)
				throw new InvalidOperationException ("Object is cached.");
			var result = sb.ToString ();
			StringBuilderCache.Put (this);
			return result;
		}

		public string ToString (int startIndex, int length)
		{
			if (Cached)
				throw new InvalidOperationException ("Object is cached.");
			var result = sb.ToString (startIndex, length);
			StringBuilderCache.Put (this);
			return result;
		}
	}
}