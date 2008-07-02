// Util.cs
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
using System.Text;
using MD = Mono.Debugger;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace DebuggerServer
{
	public static class Util
	{
		public static ObjectValue CreateObjectValue (MD.Thread thread, IObjectValueSource source, ObjectPath path, TargetObject obj, bool editable)
		{
			return CreateObjectValue (thread, source, path, obj, editable, true);
		}
		
		static ObjectValue CreateObjectValue (MD.Thread thread, IObjectValueSource source, ObjectPath path, TargetObject obj, bool editable, bool recurseCurrentObject)
		{
			if (obj == null)
				ObjectValue.CreateObject (null, path, "", null, null);
			
			if (obj.HasAddress && obj.GetAddress (thread).IsNull)
				return ObjectValue.CreateObject (null, path, obj.TypeName, "(null)", null);
			
			switch (obj.Kind) {
				
				case TargetObjectKind.Class:
					TargetClassObject co = obj as TargetClassObject;
					if (co == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else {
						if (recurseCurrentObject) {
							TargetObject ob = co.GetCurrentObject (thread);
							if (ob != null)
								return CreateObjectValue (thread, source, path, ob, editable, false);
						}
						return ObjectValue.CreateObject (source, path, obj.TypeName, TargetObjectToString (thread, obj, false), null);
					}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else if (recurseCurrentObject)
						return CreateObjectValue (thread, source, path, oob.GetDereferencedObject (thread), editable, false);
					else
						return ObjectValue.CreateObject (source, path, obj.TypeName, TargetObjectToString (thread, obj, false), null);
					
				case TargetObjectKind.Array:
					TargetArrayObject array = (TargetArrayObject) obj;
					TargetArrayBounds bounds = array.GetArrayBounds (thread);
					if (bounds.IsMultiDimensional)
						return ObjectValue.CreateArray (source, path, obj.TypeName, 0, null);
					else
						return ObjectValue.CreateArray (source, path, obj.TypeName, bounds.Length, null);
					
				case TargetObjectKind.Fundamental:
					TargetFundamentalObject fob = (TargetFundamentalObject) obj;
					object val = fob.GetObject (thread);
					return ObjectValue.CreatePrimitive (source, path, obj.TypeName, ObjectToString (val), editable);
					
				case TargetObjectKind.Enum:
					TargetEnumObject enumobj = (TargetEnumObject) obj;
					return CreateObjectValue (thread, source, path, enumobj.GetValue (thread), editable);
					
				default:
					return ObjectValue.CreateUnknown (source, path, obj.TypeName);
			}
		}
		
		public static ObjectValue[] GetObjectValueChildren (MD.Thread thread, IObjectValueSource source, TargetObject obj, ObjectPath path, int firstItemIndex, int count)
		{
			return GetObjectValueChildren (thread, source, obj, path, firstItemIndex, count, false);
		}
		
		static ObjectValue[] GetObjectValueChildren (MD.Thread thread, IObjectValueSource source, TargetObject obj, ObjectPath path, int firstItemIndex, int count, bool recurseCurrentObject)
		{
			switch (obj.Kind)
			{
				case MD.Languages.TargetObjectKind.Array:
					
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return new ObjectValue [0];
					
					TargetArrayBounds bounds = arr.GetArrayBounds (thread);
					if (bounds.IsMultiDimensional) {
						// Calculate the initial index
						int[] indices = new int [bounds.Rank];
						int[] rankSizes = new int [bounds.Rank];
						rankSizes [bounds.Rank-1] = 1;
						for (int n=bounds.Rank-2; n>=0; n--) {
							rankSizes [n] = rankSizes [n+1] * (bounds.UpperBounds [n+1] - bounds.LowerBounds [n+1] +1);
						}
						int rest = firstItemIndex;
						for (int n=0; n<bounds.Rank; n++) {
							indices [n] = rest / rankSizes [n];
							rest -= indices [n] * rankSizes [n];
							indices [n] += bounds.LowerBounds [n];
						}
						// Get the values
						
						ObjectValue[] values = new ObjectValue [count];
						bool outOfRange = false;
						
						for (int n=0; n<count; n++) {
							string idx = GetIndicesString (indices);
							if (outOfRange)
								values [n] = ObjectValue.CreateUnknown (idx);
							else {
								TargetObject elem = arr.GetElement (thread, indices);
								values [n] = CreateObjectValue (thread, source, path.Append (idx), elem, true);
							}
							
							// Increment the indices
							for (int m=bounds.Rank-1; m >= 0; m--) {
								if (++indices [m] >= bounds.UpperBounds [m]) {
									if (m == 0)
										outOfRange = true;
									else
										indices [m] = bounds.LowerBounds [m];
								}
								else
									break;
							}
						}
						return values;
					}
					else {
						ObjectValue[] values = new ObjectValue [count];
						for (int n=0; n < values.Length; n++) {
							int index = n + firstItemIndex;
							string sidx = "[" + index + "]";
							if (index >= bounds.Length)
								values [n] = ObjectValue.CreateUnknown (sidx);
							else {
								TargetObject elem = arr.GetElement (thread, new int[] { index });
								values [n] = CreateObjectValue (thread, source, path.Append (sidx), elem, true);
							}
						}
						return values;
					}
					
				case TargetObjectKind.Class:
					TargetClassObject co = obj as TargetClassObject;
					if (co == null)
						return new ObjectValue [0];
					if (recurseCurrentObject) {
						TargetObject currob = co.GetCurrentObject (thread);
						if (currob != null)
							return GetObjectValueChildren (thread, source, currob, path, firstItemIndex, count, false);
					}
					
					List<ObjectValue> values = new List<ObjectValue> ();
					foreach (IValueReference val in GetMembers (thread, co)) {
						try {
							TargetObject ob = val.Value;
							if (ob == null)
								values.Add (ObjectValue.CreateNullObject (val.Name, val.Type.Name));
							else
								values.Add (CreateObjectValue (thread, source, path.Append (val.Name), ob, true));
						} catch (Exception ex) {
							values.Add (CreateExceptionValue (val.Name, val.Type, ex));
						}
					}
					return values.ToArray ();
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return new ObjectValue [0];
					if (recurseCurrentObject)
						return GetObjectValueChildren (thread, source, oob.GetDereferencedObject (thread), path, firstItemIndex, count, false);
					else
						return new ObjectValue [0];
					
				default:
					return new ObjectValue [0];
			}		
		}
		
		static ObjectValue CreateExceptionValue (string name, TargetType type, Exception ex)
		{
			return ObjectValue.CreateObject (null, new ObjectPath (name), type.Name, ex.Message, null);
		}
		
		static string GetIndicesString (int[] indices)
		{
			StringBuilder sb = new StringBuilder ("[");
			for (int n=0; n<indices.Length; n++) {
				if (n > 0) sb.Append (',');
				sb.Append (indices [n].ToString ());
			}
			sb.Append (']');
			return sb.ToString ();
		}
		
		public static IValueReference GetTargetObjectReference (MD.Thread thread, IValueReference rootObj, ObjectPath path, int pathIndex)
		{
			if (pathIndex >= path.Length)
				return rootObj;
			else
				return GetTargetObjectReference (thread, rootObj.Value, path, pathIndex, true);
		}
		
		static IValueReference GetTargetObjectReference (MD.Thread thread, TargetObject obj, ObjectPath path, int pathIndex, bool recurseCurrentObject)
		{
			if (obj == null)
				return null;
			
			string name = path [pathIndex];
			
			switch (obj.Kind)
			{
				case MD.Languages.TargetObjectKind.Array: {
					
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return null;
					
					// Parse the array indices
					string[] sinds = name.Substring (1, name.Length - 2).Split (',');
					int[] indices = new int [sinds.Length];
					for (int n=0; n<sinds.Length; n++)
						indices [n] = int.Parse (sinds [n]);
					
					IValueReference oref = new ArrayValueReference (thread, arr, indices);
					return GetTargetObjectReference (thread, oref, path, pathIndex + 1);
				}
					
				case TargetObjectKind.Class: {
					TargetClassObject co = obj as TargetClassObject;
					if (co == null)
						return null;
					if (recurseCurrentObject) {
						TargetObject currob = co.GetCurrentObject (thread);
						if (currob != null)
							return GetTargetObjectReference (thread, currob, path, pathIndex, false);
					}
					IValueReference ob = GetMemberValueReference (thread, (TargetStructType) co.Type, co, name);
					if (ob == null)
						return null;
					return GetTargetObjectReference (thread, ob, path, pathIndex + 1);
				}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return null;
					if (recurseCurrentObject)
						return GetTargetObjectReference (thread, oob.GetDereferencedObject (thread), path, pathIndex, false);
					else
						return null;
					
				default:
					return null;
			}
		}
		
		public static IValueReference GetMemberValueReference (MD.Thread thread, TargetStructType type, TargetStructObject thisobj, string name)
		{
			foreach (IValueReference val in GetMembers (thread, thisobj)) {
				if (val.Name == name)
					return val;
			}
			return null;
		}
		
		public static TargetObject GetRealObject (MD.Thread thread, TargetObject obj)
		{
			try {
				switch (obj.Kind) {
					case MD.Languages.TargetObjectKind.Array:
					case TargetObjectKind.Fundamental:
						return obj;
						
					case TargetObjectKind.Class:
						TargetClassObject co = obj as TargetClassObject;
						if (co == null)
							return null;
						TargetObject res = co.GetCurrentObject (thread);
						return res ?? obj;
						
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						return eob.GetValue (thread);
						
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null)
							return null;
						return oob.GetDereferencedObject (thread);
				}
			}
			catch {
				// Ignore
			}
			return obj;
		}

		public static string TargetObjectToString (MD.Thread thead, TargetObject obj)
		{
			return TargetObjectToString (thead, obj, true);
		}
		
		public static string TargetObjectToString (MD.Thread thread, TargetObject obj, bool recurseOb)
		{
			try {
				switch (obj.Kind) {
					case MD.Languages.TargetObjectKind.Array:
						TargetArrayObject arr = obj as TargetArrayObject;
						if (arr == null)
							return "null";
						StringBuilder tn = new StringBuilder (arr.Type.ElementType.Name);
						tn.Append ('[');
						TargetArrayBounds ab = arr.GetArrayBounds (thread);
						if (ab.IsMultiDimensional) {
							for (int n=0; n<ab.Rank; n++) {
								if (n>0)
									tn.Append (',');
								tn.Append (ab.UpperBounds [n] - ab.LowerBounds [n] + 1);
							}
						}
						else if (!ab.IsUnbound) {
							tn.Append (ab.Length.ToString ());
						}
						tn.Append (']');
						return tn.ToString ();
						
					case TargetObjectKind.Class:
						TargetClassObject co = obj as TargetClassObject;
						if (co == null)
							return "(null)";
						if (recurseOb) {
							TargetObject currob = co.GetCurrentObject (thread);
							if (currob != null)
								return TargetObjectToString (thread, currob, false);
						}
						if (co.TypeName == "System.Decimal")
							return CallToString (thread, co);
						return "{" + co.TypeName + "}";
						
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						return TargetObjectToString (thread, eob.GetValue (thread));
						
					case TargetObjectKind.Fundamental:
						TargetFundamentalObject fob = obj as TargetFundamentalObject;
						if (fob == null)
							return "null";
						return fob.Print (thread);
						
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null)
							return "null";
						if (recurseOb)
							return TargetObjectToString (thread, oob.GetDereferencedObject (thread), false);
						else
							return "{" + oob.TypeName + "}";
				}
			}
			catch (Exception ex) {
				return "? (" + ex.GetType () + ": " + ex.Message + ")";
			}
			return "?";
		}
		
		public static string ObjectToString (object value)
		{
			return value.ToString ();
		}
		
		public static object StringToObject (TargetType type, string value)
		{
			TargetFundamentalType ftype = type as TargetFundamentalType;
			if (ftype != null) {
				switch (ftype.FundamentalKind) {
					case FundamentalKind.Boolean: return bool.Parse (value);
					case FundamentalKind.Byte: return byte.Parse (value);
					case FundamentalKind.Char: return char.Parse (value);
					case FundamentalKind.Double: return double.Parse (value);
					case FundamentalKind.Int16: return short.Parse (value);
					case FundamentalKind.Int32: return int.Parse (value);
					case FundamentalKind.Int64: return long.Parse (value);
					case FundamentalKind.IntPtr: return new IntPtr (long.Parse (value));
					case FundamentalKind.SByte: return sbyte.Parse (value);
					case FundamentalKind.Single: return float.Parse (value);
					case FundamentalKind.String: return value;
					case FundamentalKind.UInt16: return ushort.Parse (value);
					case FundamentalKind.UInt32: return uint.Parse (value);
					case FundamentalKind.UInt64: return ulong.Parse (value);
					case FundamentalKind.UIntPtr: return new UIntPtr (ulong.Parse (value));
				}
			}
			throw new InvalidOperationException ("Value '" + value + "' can't be converted to type '" + type.Name + "'");
		}
		
		static string CallToString (MD.Thread thread, TargetStructObject obj)
		{
			TargetStructObject cobj = obj;

			do {
				TargetStructType ctype = cobj.Type;
				if ((ctype.Name == "System.Object") || (ctype.Name == "System.ValueType"))
					return null;
	
				TargetClass klass = ctype.GetClass (thread);
				TargetMethodInfo[] methods = klass.GetMethods (thread);
				if (methods == null)
					return null;
	
				foreach (TargetMethodInfo minfo in methods) {
					if (minfo.Name != "ToString")
						continue;
	
					TargetFunctionType ftype = minfo.Type;
					if (ftype.ParameterTypes.Length != 0)
						continue;
					if (ftype.ReturnType != ftype.Language.StringType)
						continue;
	
					MD.RuntimeInvokeResult result = thread.RuntimeInvoke (
						ftype, obj, new TargetObject [0], true, false);
	
					result.CompletedEvent.WaitOne ();
					if ((result.ExceptionMessage != null) || (result.ReturnObject == null))
						return null;
	
					TargetObject retval = (TargetObject) result.ReturnObject;
					object s = ((TargetFundamentalObject) retval).GetObject (thread);
					return s != null ? s.ToString () : "";
				}
				cobj = cobj.GetParentObject (thread) as TargetStructObject;
			}
			while (cobj != null);

			return null;
		}
		
		public static IEnumerable<IValueReference> GetMembers (MD.Thread thread, TargetStructObject co)
		{
			TargetStructType type = (TargetStructType) co.Type;
			
			while (type != null) {
				
				foreach (TargetFieldInfo field in type.ClassType.Fields)
					yield return new FieldReference (thread, co, type, field);
				
				foreach (TargetPropertyInfo prop in type.ClassType.Properties)
					yield return new PropertyReference (thread, prop, co);
				
				type = type.GetParentType (thread);
			}
		}
		
		public static void PrintObject (MD.StackFrame frame, TargetObject obj)
		{
			try {
				Console.WriteLine ("object");
				Console.WriteLine ("  kind: " + obj.Kind);
				Console.WriteLine ("  obj-type: " + obj.GetType ());
				Console.WriteLine ("  type-name: " + obj.TypeName);
				Console.WriteLine ("  has-addr: " + obj.HasAddress);
				switch (obj.Kind) {
					case MD.Languages.TargetObjectKind.Array:
						TargetArrayObject arr = obj as TargetArrayObject;
						if (arr == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
						Console.WriteLine ("  prn: " + arr.Type.ElementType.Name);
						TargetArrayBounds ab = arr.GetArrayBounds (frame.Thread);
						Console.WriteLine ("  bounds");
						Console.WriteLine ("     multidim: " + ab.IsMultiDimensional);
						Console.WriteLine ("     unbound: " + ab.IsUnbound);
						Console.WriteLine ("     length: " + (!ab.IsMultiDimensional ? ab.Length.ToString () : "(miltidim)"));
						Console.WriteLine ("     rank: " + ab.Rank);
						Console.Write ("     lower bounds: ");
						if (ab.LowerBounds != null)
							foreach (int b in ab.LowerBounds) Console.Write (b + " ");
						else
							Console.WriteLine ("?");
						Console.WriteLine ();
						Console.Write ("     upper bounds: ");
						if (ab.UpperBounds != null)
							foreach (int b in ab.UpperBounds) Console.Write (b + " ");
						else
							Console.WriteLine ("?");
						Console.WriteLine ();
						break;
					case TargetObjectKind.Class:
						TargetClassObject co = obj as TargetClassObject;
						if (co == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
						TargetObject currob = co.GetCurrentObject (frame.Thread);
						if (currob != null && currob != co) {
							Console.WriteLine ("  >> current object");
							Console.WriteLine ("  " + currob);
							Console.WriteLine ("  << current object");
						}
											
						currob = co.GetParentObject (frame.Thread);
						if (currob != null && currob != co) {
							Console.WriteLine ("  >> parent object");
							PrintObject (frame, currob);
							Console.WriteLine ("  << parent object");
						}
						break;
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						Console.WriteLine ("  print: " + eob.Print (frame.Thread));
						TargetObject val = eob.GetValue (frame.Thread);
						Console.WriteLine ("  >> value object");
						PrintObject (frame, val);
						Console.WriteLine ("  << value object");
						break;
					case TargetObjectKind.Fundamental:
						TargetFundamentalObject fob = obj as TargetFundamentalObject;
						if (fob == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
//						object ob = fob.GetObject (frame.Thread);
	//					Console.WriteLine ("  value: " + (ob != null ? ob.ToString () : "(null)"));
	//					Console.WriteLine ("  print: " + fob.Print (frame.Thread));
						break;
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null) {
							Console.WriteLine ("  (NULL)");
							return;
						}
						Console.WriteLine ("  >> dereferenced object");
						PrintObject (frame, oob.GetDereferencedObject (frame.Thread));
						Console.WriteLine ("  << dereferenced object");
						break;
				}
			}
			catch (Exception ex) {
				Console.WriteLine ("pp: " + ex);
			}
		}
		
		public static TargetObject RuntimeInvoke (MD.Thread thread, TargetFunctionType function,
							  TargetStructObject object_argument,
							  TargetObject[] param_objects)
		{
			MD.RuntimeInvokeResult res = thread.RuntimeInvoke (function, object_argument, param_objects, true, false);
			res.Wait ();
			if (res.ExceptionMessage != null)
				throw new Exception (res.ExceptionMessage);
			return res.ReturnObject;
		}
	}
}
