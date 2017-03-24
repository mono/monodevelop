//
// MruCache.cs
//
// Author:
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
using System;
using System.Collections.Generic;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class MruCache
	{
		const int MaxItems = 42;

		readonly List<string> lastItems = new List<string> (MaxItems);
		readonly object mruLock = new object ();

		public void CommitCompletionData (CompletionData item)
		{
			lock (mruLock) {
				var removed = lastItems.Remove (item.DisplayText);
				if (!removed && lastItems.Count == MaxItems)
					lastItems.RemoveAt (0);

				lastItems.Add (item.DisplayText);
			}
		}

		/// <summary>
		/// Lower is better. 1 == not in list.
		/// </summary>
		public int GetIndex (CompletionData item)
		{
			lock (mruLock) {
				var index = lastItems.IndexOf (item.DisplayText);
				return -index;
			}
		}

		public void Clear ()
		{
			lock (mruLock) {
				lastItems.Clear ();
			}
		}
	}
}