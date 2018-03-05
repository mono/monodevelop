//
// StringInternPool.cs
//
// Author:
//       Marius <>
//
// Copyright (c) 2017 
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
using System.Text;

namespace MonoDevelop.Core
{
	public sealed class StringInternPool
	{
		readonly StringTable table = new StringTable ();

		public string Add (string chars)
		{
			return table.Add (chars);
		}

		public string Add (string chars, int start, int len)
		{
			return table.Add (chars, start, len);
		}

		public string Add (StringBuilder chars)
		{
			return table.Add (chars);
		}

		public string Add (char [] chars, int start, int len)
		{
			return table.Add (chars, start, len);
		}

		public string Add (char chars)
		{
			return table.Add (chars);
		}

		public static string AddShared (StringBuilder chars)
		{
			return StringTable.AddShared (chars);
		}

		public static string AddShared (string chars)
		{
			return StringTable.AddShared (chars);
		}

		public static string AddShared (string chars, int start, int len)
		{
			return StringTable.AddShared (chars, start, len);
		}
	}
}
