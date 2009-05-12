// ArrayValueReference.cs
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
using System.Text;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Evaluation
{
	public class ArrayValueReference<TValue, TType>: ValueReference<TValue, TType>
	{
		TValue arr;
		int[] indices;
		ICollectionAdaptor<TValue, TType> adaptor;

		public ArrayValueReference (EvaluationContext<TValue,TType> ctx, TValue arr, int[] indices)
			: base (ctx)
		{
			this.arr = arr;
			this.indices = indices;
			adaptor = ctx.Adapter.CreateArrayAdaptor (ctx, arr);
		}

		public override TValue Value {
			get {
				TValue elem = adaptor.GetElement (indices);
				return Context.Adapter.GetRealObject (Context, elem);
			}
			set {
				adaptor.SetElement (indices, value);
			}
		}
		
		public override string Name {
			get {
				StringBuilder sb = new StringBuilder ();
				sb.Append ('[');
				for (int n=0; n<indices.Length; n++) {
					if (n > 0) sb.Append (", ");
					sb.Append (indices [n]);
				}
				sb.Append (']');
				return sb.ToString ();
			}
		}
		
		public override TType Type {
			get {
				return adaptor.ElementType;
			}
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.ArrayElement;
			}
		}
	}
}
