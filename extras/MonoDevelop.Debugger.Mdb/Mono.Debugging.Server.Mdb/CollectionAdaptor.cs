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
		Thread thread;
		TargetStructObject obj;
		
		TargetPropertyInfo indexerProp;
		
		TargetArrayBounds bounds;
		TargetObject enumerator;
		List<TargetObject> items = new List<TargetObject> ();
		
		static Dictionary<string,ColInfo> colTypes = new Dictionary<string,ColInfo> ();
		
		class ColInfo {
			public TargetPropertyInfo IndexerProp;
		}
		
		bool done;
		string currentObjType;
		
		public CollectionAdaptor (Thread thread, TargetStructObject obj, TargetPropertyInfo indexerProp)
		{
			this.thread = thread;
			this.indexerProp = indexerProp;
			this.obj = obj;
		}
		
		public static CollectionAdaptor CreateAdaptor (Thread thread, TargetStructObject obj)
		{
			ColInfo colInfo;
			if (colTypes.TryGetValue (obj.Type.Name, out colInfo)) {
				if (colInfo == null)
					return null;
			}
			else {
				if (!ObjectUtil.IsInstanceOfType (thread, "System.Collections.ICollection", obj)) {
					colTypes [obj.Type.Name] = null;
					return null;
				}
				colInfo = new ColInfo ();
				foreach (MemberReference mem in ObjectUtil.GetTypeMembers (thread, obj.Type, false, false, true, true, false)) {
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
				return new CollectionAdaptor (thread, obj, colInfo.IndexerProp);
			}
			else {
				colTypes [obj.Type.Name] = null;
				return null;
			}
		}
		
		public TargetArrayBounds GetBounds ()
		{
			if (bounds == null) {
				TargetObject ob = ObjectUtil.GetPropertyValue (thread, "Count", obj);
				ob = ObjectUtil.GetRealObject (thread, ob);
				TargetFundamentalObject fob = ob as TargetFundamentalObject;
				int count;
				if (fob == null)
					count = 0;
				else
					count = Convert.ToInt32 (fob.GetObject (thread));
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
					enumerator = ObjectUtil.CallMethod (thread, "GetEnumerator", obj);
				}
			}
			
			while (bounds.Length > items.Count && !done) {
				// call MoveNext
				TargetObject eob = ObjectUtil.GetRealObject (thread, ObjectUtil.CallMethod (thread, "MoveNext", enumerator));
				TargetFundamentalObject res = eob as TargetFundamentalObject;
				if (res == null) {
					done = true;
				} else if ((bool) res.GetObject (thread)) {
					// call Current
					TargetObject current = ObjectUtil.GetPropertyValue (thread, "Current", enumerator);
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
			currentObj = ObjectUtil.GetRealObject (thread, currentObj);
			
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
				TargetObject key = ObjectUtil.GetPropertyValue (thread, "Key", telem);
				TargetObject value = ObjectUtil.GetPropertyValue (thread, "Value", telem);
				key = ObjectUtil.GetRealObject (thread, key);
				value = ObjectUtil.GetRealObject (thread, value);
				string val = "{[" + Server.Instance.Evaluator.TargetObjectToString (thread, key) + ", " + Server.Instance.Evaluator.TargetObjectToString (thread, value) + "]}";
				ObjectValueFlags setFlags = indexerProp.CanWrite ? ObjectValueFlags.None : ObjectValueFlags.ReadOnly;
				ObjectValue vkey = Util.CreateObjectValue (thread, this, new ObjectPath (sidx).Append ("key"), key, ObjectValueFlags.ReadOnly | ObjectValueFlags.Property);
				ObjectValue vvalue = Util.CreateObjectValue (thread, this, new ObjectPath (sidx).Append ("val"), value, setFlags | ObjectValueFlags.Property);
				
				Connect ();
				return ObjectValue.CreateObject (null, new ObjectPath (sidx), elem.Type.Name, val, ObjectValueFlags.ReadOnly, new ObjectValue [] { vkey, vvalue });
			}
			else
				return Util.CreateObjectValue (thread, grp, path, elem, ObjectValueFlags.ArrayElement);
		}
		
		public void SetElement (int[] indices, TargetObject val)
		{
			TargetFundamentalObject ind = thread.CurrentFrame.Language.CreateInstance (thread, indices [0]);
			TargetFundamentalType ft = indexerProp.Setter.ParameterTypes [0] as TargetFundamentalType;
			if (ft == null)
				return;
			TargetObject newInd = TargetObjectConvert.ExplicitFundamentalConversion (thread.CurrentFrame, ind, ft);
			if (newInd == null)
				return;
			ObjectUtil.SetPropertyValue (thread, null, obj, val, newInd);
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
				TargetObject key = ObjectUtil.GetPropertyValue (thread, "Key", elem);
				return Util.GetObjectValueChildren (thread, key, index, count);
			} else {
				TargetObject value = ObjectUtil.GetPropertyValue (thread, "Value", elem);
				return Util.GetObjectValueChildren (thread, value, index, count);
			}
		}
		
		public string SetValue (ObjectPath path, string value)
		{
			int idx = int.Parse (path [0]);
			TargetStructObject elem = GetElement (new int [] { idx }) as TargetStructObject;
			if (elem == null || path [1] != "value")
				return value;
			
			TargetObject key = ObjectUtil.GetPropertyValue (thread, "Key", elem);
			
			TargetObject val;
			try {
				TargetType elemType = indexerProp.Setter.ParameterTypes [1];
				EvaluationOptions ops = new EvaluationOptions ();
				ops.ExpectedType = elemType;
				ops.CanEvaluateMethods = true;
				ValueReference var = Server.Instance.Evaluator.Evaluate (thread.CurrentFrame, value, ops);
				val = var.Value;
				val = TargetObjectConvert.Cast (thread.CurrentFrame, val, elemType);
				ObjectUtil.SetPropertyValue (thread, null, obj, val, key);
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerError (ex);
				return value;
			}
			
			try {
				val = ObjectUtil.GetPropertyValue (thread, null, obj, key);
				return Server.Instance.Evaluator.TargetObjectToString (thread, val);
			} catch (Exception ex) {
				Server.Instance.WriteDebuggerError (ex);
				return value;
			}
		}
		
		#endregion 
	}
}
