// 
// FilteredMembersSource.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using System.Diagnostics;

namespace Mono.Debugging.Evaluation
{
	public class FilteredMembersSource: RemoteFrameObject, IObjectValueSource
	{
		object obj;
		object type;
		EvaluationContext ctx;
		BindingFlags bindingFlags;
		IObjectSource objectSource;

		public FilteredMembersSource (EvaluationContext ctx, IObjectSource objectSource, object type, object obj, BindingFlags bindingFlags)
		{
			this.ctx = ctx;
			this.obj = obj;
			this.type = type;
			this.bindingFlags = bindingFlags;
			this.objectSource = objectSource;
		}

		public static ObjectValue CreateNonPublicsNode (EvaluationContext ctx, IObjectSource objectSource, object type, object obj, BindingFlags bindingFlags)
		{
			return CreateNode (ctx, objectSource, type, obj, bindingFlags, "Non-public members");
		}
		
		public static ObjectValue CreateStaticsNode (EvaluationContext ctx, IObjectSource objectSource, object type, object obj, BindingFlags bindingFlags)
		{
			return CreateNode (ctx, objectSource, type, obj, bindingFlags, "Static members");
		}
		
		static ObjectValue CreateNode (EvaluationContext ctx, IObjectSource objectSource, object type, object obj, BindingFlags bindingFlags, string label)
		{
			FilteredMembersSource src = new FilteredMembersSource (ctx, objectSource, type, obj, bindingFlags);
			src.Connect ();
			ObjectValue val = ObjectValue.CreateObject (src, new ObjectPath (label), "", "", ObjectValueFlags.Group|ObjectValueFlags.ReadOnly|ObjectValueFlags.NoRefresh, null);
			val.ChildSelector = "";
			return val;
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			EvaluationContext cctx = ctx.WithOptions (options);
			var names = new ObjectValueNameTracker (cctx);
			object tdataType = null;
			TypeDisplayData tdata = null;
			List<ObjectValue> list = new List<ObjectValue> ();
			foreach (ValueReference val in cctx.Adapter.GetMembersSorted (cctx, objectSource, type, obj, bindingFlags)) {
				object decType = val.DeclaringType;
				if (decType != null && decType != tdataType) {
					tdataType = decType;
					tdata = cctx.Adapter.GetTypeDisplayData (cctx, decType);
				}
				DebuggerBrowsableState state = tdata.GetMemberBrowsableState (val.Name);
				if (state == DebuggerBrowsableState.Never)
					continue;
				ObjectValue oval = val.CreateObjectValue (options);
				names.Disambiguate (val, oval);
				list.Add (oval);
			}
			if ((bindingFlags & BindingFlags.NonPublic) == 0) {
				BindingFlags newFlags = bindingFlags | BindingFlags.NonPublic;
				newFlags &= ~BindingFlags.Public;
				list.Add (CreateNonPublicsNode (cctx, objectSource, type, obj, newFlags));
			}
			return list.ToArray ();
		}
		
		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			throw new NotSupportedException ();
		}

		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			throw new NotSupportedException ();
		}
		
		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			throw new System.NotImplementedException ();
		}
		
		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			throw new System.NotImplementedException ();
		}
	}
}
