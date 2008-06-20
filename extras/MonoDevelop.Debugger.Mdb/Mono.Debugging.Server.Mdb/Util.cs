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
		public static ObjectValue CreateObjectValue (MD.Thread thread, IObjectValueSource source, ObjectPath path, TargetObject obj)
		{
			return CreateObjectValue (thread, source, path, obj, true);
		}
		
		static ObjectValue CreateObjectValue (MD.Thread thread, IObjectValueSource source, ObjectPath path, TargetObject obj, bool recurseCurrentObject)
		{
			if (obj == null) {
				ObjectValue.CreateObject (null, path, "", null, null);
			}
			switch (obj.Kind) {
				
				case TargetObjectKind.Class:
					TargetClassObject co = obj as TargetClassObject;
					if (co == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else {
						if (recurseCurrentObject) {
							TargetObject ob = co.GetCurrentObject (thread);
							if (ob != null)
								return CreateObjectValue (thread, source, path, ob, false);
						}
						return ObjectValue.CreateObject (source, path, obj.TypeName, null, null);
					}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else
						return CreateObjectValue (thread, source, path, oob.GetDereferencedObject (thread), false);
					
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
					return ObjectValue.CreatePrimitive (source, path, obj.TypeName, val);
					
				case TargetObjectKind.Enum:
					TargetEnumObject enumobj = (TargetEnumObject) obj;
					return CreateObjectValue (thread, source, path, enumobj.GetValue (thread));
					
				default:
					return ObjectValue.CreateUnknown (source, path, obj.TypeName);
			}
		}
		
		static TargetObject FindChildObject (IObjectValueSource source, TargetObject rootObj, string[] path, int pathIndex)
		{
			return null;
		}
		
		public static ObjectValue GetObjectValue (MD.Thread thread, IObjectValueSource source, TargetObject rootObj, ObjectPath path, int pathIndex, int rootPathIndex)
		{
			TargetObject obj = GetTargetObject (thread, rootObj, path, pathIndex);
			if (obj != null)
				return CreateObjectValue (thread, source, path.GetSubpath (rootPathIndex), obj);
			else
				return ObjectValue.CreateUnknown (path.LastName);
		}

		public static ObjectValue[] GetObjectValueChildren (MD.Thread thread, IObjectValueSource source, TargetObject rootObj, ObjectPath path, int pathIndex, int rootPathIndex, int firstItemIndex, int count)
		{
			return GetObjectValueChildren (thread, source, rootObj, path, pathIndex, rootPathIndex, firstItemIndex, count, false);
		}
		
		static ObjectValue[] GetObjectValueChildren (MD.Thread thread, IObjectValueSource source, TargetObject rootObj, ObjectPath path, int pathIndex, int rootPathIndex, int firstItemIndex, int count, bool recurseCurrentObject)
		{
			TargetObject obj = GetTargetObject (thread, rootObj, path, pathIndex);
			if (obj == null)
				return new ObjectValue [0];
			
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
								values [n] = CreateObjectValue (thread, source, path.Append (idx), elem);
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
								values [n] = CreateObjectValue (thread, source, path.Append (sidx), elem);
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
						return GetObjectValueChildren (thread, source, currob, path, pathIndex, rootPathIndex, firstItemIndex, count, false);
					}
					
					TargetStructType type = (TargetStructType) co.Type;
					TargetClass cls = type.GetClass (thread);

					// Get field values
					List<ObjectValue> values = new List<ObjectValue> ();
					foreach (TargetFieldInfo field in type.ClassType.Fields) {
						TargetObject ob = cls.GetField (thread, co, field);
						if (ob == null)
							values.Add (ObjectValue.CreateUnknown (field.Name));
						else
							values.Add (CreateObjectValue (thread, source, path.Append (field.Name), ob));
					}
					
					// Get property values
					foreach (TargetPropertyInfo prop in type.ClassType.Properties) {
						if (prop.CanRead && !prop.IsStatic) {
							MD.RuntimeInvokeResult result = thread.RuntimeInvoke (prop.Getter, co, new TargetObject[0], true, false);
							if (result.ReturnObject == null)
								values.Add (ObjectValue.CreateUnknown (prop.Name));
							else
								values.Add (CreateObjectValue (thread, source, path.Append (prop.Name), result.ReturnObject));
						}
					}
					return values.ToArray ();
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return new ObjectValue [0];
					return GetObjectValueChildren (thread, source, oob.GetDereferencedObject (thread), path, pathIndex, rootPathIndex, firstItemIndex, count, false);
					
				default:
					return new ObjectValue [0];
			}		
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
		
		static TargetObject GetTargetObject (MD.Thread thread, TargetObject rootObj, ObjectPath path, int pathIndex)
		{
			return GetTargetObject (thread, rootObj, path, pathIndex, true);
		}
		
		static TargetObject GetTargetObject (MD.Thread thread, TargetObject obj, ObjectPath path, int pathIndex, bool recurseCurrentObject)
		{
			if (pathIndex >= path.Length)
				return obj;
			
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
					
					TargetObject ob = arr.GetElement (thread, indices);
					return GetTargetObject (thread, ob, path, pathIndex + 1);
				}
					
				case TargetObjectKind.Class: {
					TargetClassObject co = obj as TargetClassObject;
					if (co == null)
						return null;
					if (recurseCurrentObject) {
						TargetObject currob = co.GetCurrentObject (thread);
						return GetTargetObject (thread, currob, path, pathIndex, false);
					}
					TargetObject ob = GetMemberValue (thread, (TargetStructType) co.Type, co, name);
					if (ob == null)
						return null;
					return GetTargetObject (thread, ob, path, pathIndex + 1);
				}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return null;
					return GetTargetObject (thread, oob.GetDereferencedObject (thread), path, pathIndex);
					
				default:
					return null;
			}
		}
		
		public static TargetObject GetMemberValue (MD.Thread thread, TargetStructType type, TargetStructObject thisobj, string name)
		{
			while (type != null)
			{
				TargetClass cls = type.GetClass (thread);
				
				foreach (TargetPropertyInfo prop in type.ClassType.Properties) {
					if (prop.Name == name && prop.CanRead && (prop.IsStatic || thisobj != null)) {
						MD.RuntimeInvokeResult result = thread.RuntimeInvoke (prop.Getter, thisobj, new TargetObject[0], true, false);
						return result.ReturnObject;
					}
				}
				foreach (TargetFieldInfo field in type.ClassType.Fields) {
					if (field.Name == name && (field.IsStatic || thisobj != null))
						return cls.GetField (thread, thisobj, field);
				}
			}
			return null;
		}
		
		public static string ObjectToString (MD.StackFrame frame, TargetObject obj)
		{
			return ObjectToString (frame, obj, true);
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
						return co.GetCurrentObject (thread);
						
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
			catch (Exception ex) {
				// Ignore
			}
			return obj;
		}

		public static string ObjectToString (MD.StackFrame frame, TargetObject obj, bool recurseOb)
		{
			try {
				switch (obj.Kind) {
					case MD.Languages.TargetObjectKind.Array:
						TargetArrayObject arr = obj as TargetArrayObject;
						if (arr == null)
							return "null";
						StringBuilder tn = new StringBuilder (arr.Type.ElementType.Name);
						tn.Append ('[');
						TargetArrayBounds ab = arr.GetArrayBounds (frame.Thread);
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
							return "null";
						TargetObject currob = co.GetCurrentObject (frame.Thread);
						if (currob != null && recurseOb)
							return ObjectToString (frame, currob, false);
						else if (currob != null)
							return "{" + currob.TypeName + "}";
						else
							return "{" + co.TypeName + "}";
						
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						return ObjectToString (frame, eob.GetValue (frame.Thread));
						
					case TargetObjectKind.Fundamental:
						TargetFundamentalObject fob = obj as TargetFundamentalObject;
						if (fob == null)
							return "null";
						object ob = fob.GetObject (frame.Thread);
						return fob.Print (frame.Thread);
						
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null)
							return "null";
						return ObjectToString (frame, oob.GetDereferencedObject (frame.Thread));
				}
			}
			catch (Exception ex) {
				return "?";
			}
			return "?";
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
						object ob = fob.GetObject (frame.Thread);
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
	}
}
