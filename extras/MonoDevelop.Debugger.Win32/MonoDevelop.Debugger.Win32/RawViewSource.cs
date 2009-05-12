// RawViewSource.cs
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
using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Evaluation
{
	public class RawViewSource<TValue,TType>: RemoteFrameObject, IObjectValueSource
	{
		TValue obj;
		EvaluationContext<TValue, TType> ctx;

		public RawViewSource (EvaluationContext<TValue, TType> ctx, TValue obj)
		{
			this.ctx = ctx;
			this.obj = obj;
		}

		public static ObjectValue CreateRawView (EvaluationContext<TValue, TType> ctx, TValue obj)
		{
			RawViewSource<TValue, TType> src = new RawViewSource<TValue, TType> (ctx, obj);
			src.Connect ();
			return ObjectValue.CreateObject (src, new ObjectPath ("Raw View"), "", "", ObjectValueFlags.ReadOnly, null);
		}
		
		public ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			return ctx.Adapter.GetObjectValueChildren (ctx, obj, index, count, false);
		}
		
		public string SetValue (ObjectPath path, string value)
		{
			throw new NotSupportedException ();
		}
	}
}
