// 
// BaseTypeViewSource.cs
//  
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	public class BaseTypeViewSource: RemoteFrameObject, IObjectValueSource
	{
		EvaluationContext ctx;
		object type;
		object obj;
		IObjectSource objectSource;
		
		public BaseTypeViewSource (EvaluationContext ctx, IObjectSource objectSource, object type, object obj)
		{
			this.ctx = ctx;
			this.type = type;
			this.obj = obj;
			this.objectSource = objectSource;
		}
		
		public static ObjectValue CreateBaseTypeView (EvaluationContext ctx, IObjectSource objectSource, object type, object obj)
		{
			BaseTypeViewSource src = new BaseTypeViewSource (ctx, objectSource, type, obj);
			src.Connect ();
			string tname = ctx.Adapter.GetDisplayTypeName (ctx, type);
			ObjectValue val = ObjectValue.CreateObject (src, new ObjectPath ("base"), tname, "{" + tname + "}", ObjectValueFlags.Type|ObjectValueFlags.ReadOnly|ObjectValueFlags.NoRefresh, null);
			val.ChildSelector = "";
			return val;
		}
		
		#region IObjectValueSource implementation

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			EvaluationContext cctx = ctx.WithOptions (options);
			return cctx.Adapter.GetObjectValueChildren (cctx, objectSource, type, obj, index, count, false);
		}

		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			throw new NotSupportedException();
		}

		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			throw new NotSupportedException();
		}
		
		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			throw new System.NotImplementedException ();
		}
		
		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			throw new System.NotImplementedException ();
		}
		
		#endregion
	}
}
