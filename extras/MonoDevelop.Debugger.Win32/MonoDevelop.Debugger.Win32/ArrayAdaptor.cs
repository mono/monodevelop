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
using Mono.Debugging.Client;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	class ArrayAdaptor: ICollectionAdaptor
	{
		CorEvaluationContext ctx;
		CorArrayValue array;
		CorValRef obj;

		public ArrayAdaptor (EvaluationContext ctx, CorValRef obj, CorArrayValue array)
		{
			this.ctx = (CorEvaluationContext) ctx;
			this.array = array;
			this.obj = obj;
		}
		
		public int[] GetDimensions ()
		{
			if (array != null)
				return array.GetDimensions ();
			else
				return new int[0];
		}
		
		public object GetElement (int[] indices)
		{
			return new CorValRef (delegate {
				if (array != null)
					return array.GetElement (indices);
				else
					return null;
			});
		}
		
		public void SetElement (int[] indices, object val)
		{
			CorValRef it = (CorValRef) GetElement (indices);
			it.SetValue (ctx, (CorValRef) val);
		}
		
		public object ElementType {
			get {
				return obj.Val.ExactType.FirstTypeParameter;
			}
		}
	}
}
