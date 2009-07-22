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
using Mono.Debugger;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace DebuggerServer
{
	class ArrayAdaptor: ICollectionAdaptor
	{
		TargetArrayObject array;
		MdbEvaluationContext ctx;
		
		public ArrayAdaptor (MdbEvaluationContext ctx, TargetArrayObject array)
		{
			this.ctx = ctx;
			this.array = array;
		}
		
		public int[] GetDimensions ()
		{
			TargetArrayBounds bounds = array.GetArrayBounds (ctx.Thread);
			if (bounds.IsMultiDimensional) {
				int[] dims = new int [bounds.LowerBounds.Length];
				for (int n=0; n<bounds.Rank; n++)
					dims [n] = bounds.UpperBounds [n] - bounds.LowerBounds [n] + 1;
				return dims;
			} else if (!bounds.IsUnbound)
				return new int[] { bounds.Length };
			else
				return new int [0];
		}
		
		public object GetElement (int[] indices)
		{
			return array.GetElement (ctx.Thread, indices);
		}
		
		public void SetElement (int[] indices, object val)
		{
			array.SetElement (ctx.Thread, indices, (TargetObject) val);
		}
		
		public object ElementType {
			get {
				return array.Type.ElementType;
			}
		}
		
		public ObjectValue CreateElementValue (ArrayElementGroup grp, ObjectPath path, int[] indices)
		{
			TargetObject elem = array.GetElement (ctx.Thread, indices);
			return ctx.Adapter.CreateObjectValue (ctx, grp, path, elem, ObjectValueFlags.ArrayElement);
		}
	}
}
