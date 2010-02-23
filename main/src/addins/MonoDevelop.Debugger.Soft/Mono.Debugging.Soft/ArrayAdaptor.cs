// 
// ArrayAdaptor.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Mono.Debugging.Evaluation;
using Mono.Debugger.Soft;

namespace Mono.Debugging.Soft
{
	public class ArrayAdaptor: ICollectionAdaptor
	{
		ArrayMirror array;
		int[] dimensions;
		
		public ArrayAdaptor (ArrayMirror array)
		{
			this.array = array;
		}
		
		public int[] GetDimensions ()
		{
			if (dimensions == null) {
				dimensions = new int [array.Rank];
				for (int n=0; n<array.Rank; n++)
					dimensions [n] = array.GetLength (n);
			}
			return dimensions;
		}
		
		public object GetElement (int[] indices)
		{
			int i = GetIndex (indices);
			return array.GetValues (i, 1) [0];
		}
		
		public void SetElement (int[] indices, object val)
		{
			array.SetValues (GetIndex (indices), new Value[] { (Value) val });
		}
		
		int GetIndex (int[] indices)
		{
			int ts = 1;
			int i = 0;
			int[] dims = GetDimensions ();
			for (int n = indices.Length - 1; n >= 0; n--) {
				i += indices [n] * ts;
				ts *= dims [n];
			}
			return i;
		}
		
		public object ElementType {
			get {
				return array.Type.GetElementType ();
			}
		}
	}
}
