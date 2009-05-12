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
using MonoDevelop.Debugger.Evaluation;

namespace MonoDevelop.Debugger.Win32
{
	class ArrayAdaptor: ICollectionAdaptor<CorValue, CorType>
	{
		CorArrayValue array;
		EvaluationContext<CorValue, CorType> ctx;

		public ArrayAdaptor (EvaluationContext<CorValue, CorType> ctx, CorArrayValue array)
		{
			this.ctx = ctx;
			this.array = array;
		}
		
		public int[] GetDimensions ()
		{
			return array.GetDimensions ();
		}
		
		public CorValue GetElement (int[] indices)
		{
			return array.GetElement (indices);
		}
		
		public void SetElement (int[] indices, CorValue val)
		{
			CorValue aval = array.GetElement (indices);
			CorGenericValue gval = aval.CastToGenericValue ();
			CorGenericValue g = val.CastToGenericValue ();
			if (gval != null && g != null)
				gval.SetValue (g.GetValue ());
		}
		
		public CorType ElementType {
			get {
				return array.ExactType;
			}
		}

		public ObjectValue CreateElementValue (ArrayElementGroup<CorValue, CorType> grp, ObjectPath path, int[] indices)
		{
			CorValue elem = array.GetElement (indices);
			return ctx.Adapter.CreateObjectValue (ctx, grp, path, elem, ObjectValueFlags.ArrayElement);
		}
	}
}
