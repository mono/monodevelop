// 
// AList.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using System.Collections;

namespace Sharpen
{
	public class AList<T>: List<T>
	{
		public AList ()
		{
		}
		
		public AList (int size): base (size)
		{
		}
		
		public AList (IEnumerable<T> t)
		{
			AddRange (t);
		}
		
		public override bool Equals (object obj)
		{
			if (obj == this)
				return true;
			IList list = obj as IList;
			if (list == null)
				return false;
			if (list.Count != Count)
				return false;
			for (int n=0; n<list.Count; n++) {
				if (!object.Equals (this[n], list[n]))
					return false;
			}
			return true;
		}
		
		public override int GetHashCode ()
		{
			int n = 0;
			foreach (object o in this)
				if (o != null)
					n += o.GetHashCode ();
			return n;
		}
		
		public void RemoveElement (T elem)
		{
			Remove (elem);
		}
		
		public void TrimToSize ()
		{
			Capacity = Count;
		}
		
		public void EnsureCapacity (int c)
		{
			if (c > Capacity && c > Count)
				Capacity = c;
		}
	}
}

