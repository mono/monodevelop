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

namespace DebuggerServer
{
	class ArrayAdaptor: ICollectionAdaptor
	{
		TargetArrayObject array;
		EvaluationContext ctx;
		
		public ArrayAdaptor (EvaluationContext ctx, TargetArrayObject array)
		{
			this.ctx = ctx;
			this.array = array;
		}
		
		public TargetArrayBounds GetBounds ()
		{
			return array.GetArrayBounds (ctx.Thread);
		}
		
		public TargetObject GetElement (int[] indices)
		{
			return array.GetElement (ctx.Thread, indices);
		}
		
		public void SetElement (int[] indices, TargetObject val)
		{
			array.SetElement (ctx.Thread, indices, val);
		}
		
		public TargetType ElementType {
			get {
				return array.Type.ElementType;
			}
		}
		
		public ObjectValue CreateElementValue (ArrayElementGroup grp, ObjectPath path, int[] indices)
		{
			TargetObject elem = array.GetElement (ctx.Thread, indices);
			return Util.CreateObjectValue (ctx, grp, path, elem, ObjectValueFlags.ArrayElement);
		}
	}
}
