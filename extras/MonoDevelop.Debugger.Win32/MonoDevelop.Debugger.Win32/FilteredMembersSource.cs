using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace MonoDevelop.Debugger.Evaluation
{
	public class FilteredMembersSource<TValue, TType>: RemoteFrameObject, IObjectValueSource
		where TValue: class
		where TType: class
	{
		TValue obj;
		TType type;
		EvaluationContext<TValue, TType> ctx;
		BindingFlags bindingFlags;

		public FilteredMembersSource (EvaluationContext<TValue, TType> ctx, TType type, TValue obj, BindingFlags bindingFlags)
		{
			this.ctx = ctx;
			this.obj = obj;
			this.type = type;
			this.bindingFlags = bindingFlags;
		}

		public static ObjectValue CreateNode (EvaluationContext<TValue, TType> ctx, TType type, TValue obj, BindingFlags bindingFlags)
		{
			FilteredMembersSource<TValue, TType> src = new FilteredMembersSource<TValue, TType> (ctx, type, obj, bindingFlags);
			src.Connect ();
			string label;
			if ((bindingFlags & BindingFlags.NonPublic) != 0)
				label = "Non-public members";
			else
				label = "Static members";
			return ObjectValue.CreateObject (src, new ObjectPath (label), "", "", ObjectValueFlags.ReadOnly, null);
		}

		public ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			List<ObjectValue> list = new List<ObjectValue> ();
			foreach (ValueReference<TValue, TType> val in ctx.Adapter.GetMembers (ctx, type, obj, bindingFlags)) {
				list.Add (val.CreateObjectValue ());
			}
			if ((bindingFlags & BindingFlags.NonPublic) == 0) {
				BindingFlags newFlags = bindingFlags | BindingFlags.NonPublic;
				newFlags &= ~BindingFlags.Public;
				list.Add (CreateNode (ctx, type, obj, newFlags));
			}
			return list.ToArray ();
		}

		public string SetValue (ObjectPath path, string value)
		{
			throw new NotSupportedException ();
		}
	}
}
