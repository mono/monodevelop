// RawViewSource.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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

namespace Mono.Debugging.Evaluation
{
	public class RawViewSource: RemoteFrameObject, IObjectValueSource
	{
		object obj;
		EvaluationContext ctx;
		IObjectSource objectSource;

		public RawViewSource (EvaluationContext ctx, IObjectSource objectSource, object obj)
		{
			this.ctx = ctx;
			this.obj = obj;
			this.objectSource = objectSource;
		}

		public static ObjectValue CreateRawView (EvaluationContext ctx, IObjectSource objectSource, object obj)
		{
			RawViewSource src = new RawViewSource (ctx, objectSource, obj);
			src.Connect ();
			ObjectValue val = ObjectValue.CreateObject (src, new ObjectPath ("Raw View"), "", "", ObjectValueFlags.Group|ObjectValueFlags.ReadOnly|ObjectValueFlags.NoRefresh, null);
			val.ChildSelector = "";
			return val;
		}
		
		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			EvaluationContext cctx = ctx.WithOptions (options);
			return cctx.Adapter.GetObjectValueChildren (cctx, objectSource, cctx.Adapter.GetValueType (cctx, obj), obj, index, count, false);
		}
		
		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			throw new NotSupportedException ();
		}
		
		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			throw new NotSupportedException ();
		}
		
		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			throw new System.NotImplementedException ();
		}
		
		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			throw new System.NotImplementedException ();
		}
	}
}
