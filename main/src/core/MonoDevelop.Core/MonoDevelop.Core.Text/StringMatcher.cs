// 
// CompletionMatcher.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Core.Text
{
	public abstract class StringMatcher
	{
		public static StringMatcher GetMatcher (string filter, bool matchWordStartsOnly)
		{
			if (matchWordStartsOnly)
				return new BacktrackingStringMatcher (filter);
			else
				return new LaneStringMatcher (filter);
		}
		
		public abstract bool CalcMatchRank (string name, out int matchRank);
		public abstract bool IsMatch (string name);
		public abstract int[] GetMatch (string text);

		/// <summary>
		/// Makes a copy of this instance.
		/// </summary>
		/// <returns>The clone.</returns>
		/// <remarks>
		/// StringMatcher is not guaranteed to be thread safe. In multi-threading scenarios, the Clone
		/// method can be used to create copies of a string matcher to be used by different threads.
		/// </remarks>
		public virtual StringMatcher Clone ()
		{
			return (StringMatcher)MemberwiseClone ();
		}
	}
}

