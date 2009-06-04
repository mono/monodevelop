// CollectionAdaptor.cs
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
using System.Collections.Generic;
using Mono.Debugger;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace DebuggerServer
{
	public class CollectionAdaptor: RemoteFrameObject, ICollectionAdaptor, IObjectValueSource
	{
		EvaluationContext ctx;
		TargetStructObject obj;
		
		TargetPropertyInfo indexerProp;
		
		TargetArrayBounds bounds;
		TargetObject enumerator;
		List<TargetObject> items = new List<TargetObject> ();
		
//		static Dictionary<string,ColInfo> colTypes = new Dictionary<string,ColInfo> ();
		
		class ColInfo {
			public TargetPropertyInfo IndexerProp;
		}
		
		bool done;
		string currentObjType;
		
		public CollectionAdaptor (EvaluationContext ctx, TargetStructObject obj, TargetPropertyInfo indexerProp)
		{
			this.ctx = ctx;
			this.indexerProp = indexerProp;
			this.obj = obj;
		}
		
		public static CollectionAdaptor CreateAdaptor (EvaluationContext ctx, TargetStructObject obj)
		{
			// Disabled for now since it is not stable
			return null;
/*			if (obj is TargetGenericInstanceObject)
				return null;
			if (!ctx.Frame.Language.IsManaged)
				return null;
			
			ColInfo colInfo;
			if (colTypes.TryGetValue (obj.Type.Name, out colInfo)) {
				if (colInfo == null)
					return null;
			}
			else {
				if (!ObjectUtil.IsInstanceOfType (ctx, "System.Collections.ICollection", obj)) {
					colTypes [obj.Type.Name] = null;
					return null;
				}
				colInfo = new ColInfo ();
				foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, obj.Type, false, false, true, true, ReqMemberAccess.All)) {
					if (mem.Member.IsStatic)
						continue;
					if (mem.Member is TargetPropertyInfo) {
						TargetPropertyInfo prop = (TargetPropertyInfo) mem.Member;
						if (prop.CanRead && prop.Getter.ParameterTypes.Length == 1)
							colInfo.IndexerProp = prop;
					}
					if (colInfo.IndexerProp != null)
						break;
				}
			}
			
			if (colInfo.IndexerProp != null) {
				colTypes [obj.Type.Name] = colInfo;
				return new CollectionAdaptor (ctx, obj, colInfo.IndexerProp);
			}
			else {
				colTypes [obj.Type.Name] = null;
				return null;
			}
			*/
		}
		
		public TargetArrayBounds GetBounds ()
		{
			if (bounds == null) {
				TargetObject ob = ObjectUtil.GetPropertyValue (ctx, "Count", obj);
				ob = ObjectUtil.GetRealObject (ctx, ob);
				TargetFundamentalObject fob = ob as TargetFundamentalObject;
				int count;
				if (fob == null)
					count = 0;
				else
					count = Convert.ToInt32 (fob.GetObject (ctx.Thread));
				bounds = TargetArrayBounds.MakeSimpleArray (count);
			}
			return bounds;
		}
		
		public TargetObject GetElement (int[] indices)
		{
			int idx = indices [0];
			
			if (idx >= items.Count) {
				if (enumerator == null) {
					// call GetEnumerator
					enumerator = ObjectUtil.CallMethod (ctx, "GetEnumerator", obj);
				}
			}
			
			while (bounds.Length > items.Count && !done) {
				// call MoveNext
				TargetObject eob = ObjectUtil.GetRealObject (ctx, ObjectUtil.CallMethod (ctx, "MoveNext", enumerator));
				TargetFundamentalObject res = eob as TargetFundamentalObject;
				if (res == null) {
					done = true;
				} else if ((bool) res.GetObject (ctx.Thread)) {
					// call Current
					TargetObject current = ObjectUtil.GetPropertyValue (ctx, "Current", enumerator);
					items.Add (current);
				}
			}
			
			if (idx < items.Count)
				return items [idx];
			else
				return null;
		}
		
		bool CheckDictionary (TargetObject currentObj)
		{
			currentObj = ObjectUtil.GetRealObject (ctx, currentObj);
			
			if (currentObj.Type.Name == "System.Collections.DictionaryEntry" || currentObj.Type.Name.StartsWith ("KeyValuePair")) {
				// If the object type changes, we can handle it as a dictionary entry
				if (currentObjType != null)
					return currentObjType == currentObj.Type.Name;

				currentObjType = currentObj.Type.Name;
				return true;
			}
			return false;
		}
		
		public ObjectValue CreateElementValue (ArrayElementGroup grp, ObjectPath path, int[] indices)
		{
			TargetObject elem = GetElement (indices);
			TargetStructObject telem = elem as TargetStructObject;
			
			if (telem != null && CheckDictionary (telem)) {
				string sidx = indices[0].ToString ();
				TargetObject key = ObjectUtil.GetPropertyValue (ctx, "Key", telem);
				TargetObject value = ObjectUtil.GetPropertyValue (ctx, "Value", telem);
				key = ObjectUtil.GetRealObject (ctx, key);
				value = ObjectUtil.GetRealObject (ctx, value);
				string val = "{[" + Server.Instance.Evaluator.TargetObjectToExpression (ctx, key) + ", " + Server.Instance.Evaluator.TargetObjectToExpression (ctx, value) + "]}";
				ObjectValueFlags setFlags = indexerProp.CanWrite ? ObjectValueFlags.None : ObjectValueFlags.ReadOnly;
				ObjectValue vkey = Util.CreateObjectValue (ctx, this, new ObjectPath (sidx).Append ("key"), key, ObjectValueFlags.ReadOnly | ObjectValueFlags.Property);
				ObjectValue vvalue = Util.CreateObjectValue (ctx, this, new ObjectPath (sidx).Append ("val"), value, setFlags | ObjectValueFlags.Property);
				
				Connect ();
				return ObjectValue.CreateObject (null, new ObjectPath (sidx), elem.Type.Name, val, ObjectValueFlags.ReadOnly, new ObjectValue [] { vkey, vvalue });
			}
			else
				return Util.CreateObjectValue (ctx, grp, path, elem, ObjectValueFlags.ArrayElement);
		}
		
		public void SetElement (int[] indices, TargetObject val)
		{
			TargetFundamentalObject ind = ctx.Frame.Language.CreateInstance (ctx.Thread, indices [0]);
			TargetFundamentalType ft = indexerProp.Setter.ParameterTypes [0] as TargetFundamentalType;
			if (ft == null)
				return;
			TargetObject newInd = TargetObjectConvert.ExplicitFundamentalConversion (ctx, ind, ft);
			if (newInd == null)
				return;
			ObjectUtil.SetPropertyValue (ctx, null, obj, val, newInd);
		}
		
		
		public TargetType ElementType {
			get {
				return indexerProp.Getter.ReturnType;
			}
		}

		#region IObjectValueSource implementation 
		
		public ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			int idx = int.Parse (path [0]);
			TargetStructObject elem = GetElement (new int [] { idx }) as TargetStructObject;
			if (elem == null)
				return new ObjectValue [0];
			
			if (path[1] == "key") {
				TargetObject key = ObjectUtil.GetPropertyValue (ctx, "Key", elem);
				return Util.GetObjectValueChildren (ctx, key, index, count);
			} else {
				TargetObject value = ObjectUtil.GetPropertyValue (ctx, "Value", elem);
				return Util.GetObjectValueChildren (ctx, value, index, count);
			}
		}
		
		public string SetValue (ObjectPath path, string value)
		{
			int idx = int.Parse (path [0]);
			TargetStructObject elem = GetElement (new int [] { idx }) as TargetStructObject;
			if (elem == null || path [1] != "value")
				return value;
			
			TargetObject key = ObjectUtil.GetPropertyValue (ctx, "Key", elem);
			
			TargetObject val;
			try {
				TargetType elemType = indexerProp.Setter.ParameterTypes [1];
				EvaluationOptions ops = new EvaluationOptions ();
				ops.ExpectedType = elemType;
				ops.CanEvaluateMethods = true;
				ValueReference var = Server.Instance.Evaluator.Evaluate (ctx, value, ops);
				val = var.Value;
				val = TargetObjectConvert.Cast (ctx, val, elemType);
				ObjectUtil.SetPropertyValue (ctx, null, obj, val, key);
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerError (ex);
				return value;
			}
			
			try {
				val = ObjectUtil.GetPropertyValue (ctx, null, obj, key);
				return Server.Instance.Evaluator.TargetObjectToExpression (ctx, val);
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerError (ex);
				return value;
			}
		}
		
		#endregion 
	}
}
