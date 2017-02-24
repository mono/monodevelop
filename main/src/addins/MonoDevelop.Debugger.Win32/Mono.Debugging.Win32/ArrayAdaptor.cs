// ArrayAdaptor.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Evaluation;

namespace Mono.Debugging.Win32
{
	class ArrayAdaptor: ICollectionAdaptor
	{
		readonly CorEvaluationContext ctx;
		readonly CorValRef<CorArrayValue> valRef;

		public ArrayAdaptor (EvaluationContext ctx, CorValRef<CorArrayValue> valRef)
		{
			this.ctx = (CorEvaluationContext) ctx;
			this.valRef = valRef;
		}

		public int[] GetLowerBounds ()
		{
			var array = valRef.Val;
			if (array != null && array.HasBaseIndicies) {
				return array.GetBaseIndicies ();
			} else {
				return new int[GetDimensions ().Length];
			}
		}
		
		public int[] GetDimensions ()
		{
			var array = valRef.Val;
			return array != null ? array.GetDimensions () : new int[0];
		}
		
		public object GetElement (int[] indices)
		{
			return new CorValRef (delegate {
				var array = valRef.Val;
				return array != null ? array.GetElement (indices) : null;
			});
		}

		public Array GetElements (int[] indices, int count)
		{
			// FIXME: the point of this method is to be more efficient than getting 1 array element at a time...
			var elements = new ArrayList ();

			int[] idx = new int[indices.Length];
			for (int i = 0; i < indices.Length; i++)
				idx[i] = indices[i];

			for (int i = 0; i < count; i++) {
				elements.Add (GetElement ((int[])idx.Clone ()));
				idx[idx.Length - 1]++;
			}

			return elements.ToArray ();
		}
		
		public void SetElement (int[] indices, object val)
		{
			CorValRef it = (CorValRef) GetElement (indices);
			valRef.Invalidate ();
			it.SetValue (ctx, (CorValRef) val);
		}
		
		public object ElementType {
			get {
				return valRef.Val.ExactType.FirstTypeParameter;
			}
		}
	}
}
