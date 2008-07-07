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
		public static ObjectValue CreateObjectValue (MD.Thread thread, IObjectValueSource source, ObjectPath path, TargetObject obj, ObjectValueFlags flags)
		{
			return CreateObjectValue (thread, source, path, obj, flags, true);
		}
		
		static ObjectValue CreateObjectValue (MD.Thread thread, IObjectValueSource source, ObjectPath path, TargetObject obj, ObjectValueFlags flags, bool recurseCurrentObject)
		{
			if (obj == null)
				return ObjectValue.CreateObject (null, path, "", null, flags | ObjectValueFlags.ReadOnly, null);

			if (obj.HasAddress && obj.GetAddress (thread).IsNull)
				return ObjectValue.CreateObject (null, path, obj.TypeName, "(null)", flags, null);
			
			switch (obj.Kind) {
				
				case TargetObjectKind.Struct:
				case TargetObjectKind.Class:
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else {
						if (recurseCurrentObject) {
							TargetObject ob = co.GetCurrentObject (thread);
							if (ob != null)
								return CreateObjectValue (thread, source, path, ob, flags, false);
						}
						return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj, false), flags, null);
					}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else if (recurseCurrentObject)
						return CreateObjectValue (thread, source, path, oob.GetDereferencedObject (thread), flags, false);
					else
						return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj, false), flags, null);
					
				case TargetObjectKind.Array:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj), flags, null);
					
				case TargetObjectKind.Fundamental:
					TargetFundamentalObject fob = (TargetFundamentalObject) obj;
					return ObjectValue.CreatePrimitive (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, fob), flags);
					
				case TargetObjectKind.Enum:
					TargetEnumObject enumobj = (TargetEnumObject) obj;
					return CreateObjectValue (thread, source, path, enumobj.GetValue (thread), flags);
					
				case TargetObjectKind.Pointer:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj, false), flags, null);
					
				default:
					return ObjectValue.CreateError ("?", "Unknown value type: " + obj.Kind, flags);
			}
		}
		
		public static ObjectValue[] GetObjectValueChildren (MD.Thread thread, TargetObject obj, int firstItemIndex, int count)
		{
			return GetObjectValueChildren (thread, obj, firstItemIndex, count, false);
		}
		
		static ObjectValue[] GetObjectValueChildren (MD.Thread thread, TargetObject obj, int firstItemIndex, int count, bool recurseCurrentObject)
		{
			switch (obj.Kind)
			{
				case MD.Languages.TargetObjectKind.Array:
					
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return new ObjectValue [0];
					
					ArrayElementGroup agroup = new ArrayElementGroup (thread, arr);
					return agroup.GetChildren ();
					
				case TargetObjectKind.Struct:
				case TargetObjectKind.Class:
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return new ObjectValue [0];
					if (recurseCurrentObject) {
						TargetObject currob = co.GetCurrentObject (thread);
						if (currob != null)
							return GetObjectValueChildren (thread, currob, firstItemIndex, count, false);
					}
					
					List<ObjectValue> values = new List<ObjectValue> ();
					foreach (ValueReference val in GetMembers (thread, co.Type, co)) {
						try {
							TargetObject ob = val.Value;
							if (ob == null)
								values.Add (ObjectValue.CreateNullObject (val.Name, val.Type.Name, val.Flags));
							else
								values.Add (val.CreateObjectValue ());
						} catch (Exception ex) {
							Console.WriteLine ("pp: " + ex);
							values.Add (ObjectValue.CreateError (null, new ObjectPath (val.Name), val.Type.Name, ex.Message, val.Flags));
						}
					}
					return values.ToArray ();
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return new ObjectValue [0];
					if (recurseCurrentObject)
						return GetObjectValueChildren (thread, oob.GetDereferencedObject (thread), firstItemIndex, count, false);
					else
						return new ObjectValue [0];
				
				case TargetObjectKind.Pointer:
					TargetPointerObject pobj = (TargetPointerObject) obj;
					TargetObject defob = pobj.GetDereferencedObject (thread);
					LiteralValueReference pval = new LiteralValueReference (thread, "*", defob);
					return new ObjectValue [] {pval.CreateObjectValue () };
				default:
					return new ObjectValue [0];
			}		
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
		
		public static string CallToString (MD.Thread thread, TargetStructObject obj)
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
				if (cobj.Kind != TargetObjectKind.Struct)
					cobj = cobj.GetParentObject (thread) as TargetStructObject;
				else
					break;
			}
			while (cobj != null);

			return null;
		}
		
		public static IEnumerable<ValueReference> GetMembers (MD.Thread thread, TargetType t, TargetStructObject co)
		{
			TargetStructType type = t as TargetStructType;
			
			while (type != null) {
				
				foreach (TargetFieldInfo field in type.ClassType.Fields)
					yield return new FieldReference (thread, co, type, field);
				
				foreach (TargetPropertyInfo prop in type.ClassType.Properties)
					yield return new PropertyReference (thread, prop, co);
				
				if (type.HasParent)
					type = type.GetParentType (thread);
				else
					break;
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
		
		public static ObjectValueFlags GetAccessibility (TargetMemberAccessibility ma)
		{
			switch (ma) {
				case TargetMemberAccessibility.Internal: return ObjectValueFlags.Internal;
				case TargetMemberAccessibility.Protected: return ObjectValueFlags.Protected;
				case TargetMemberAccessibility.Public: return ObjectValueFlags.Public;
				default: return ObjectValueFlags.Private;
			}
		}
		
		public static IEnumerable<VariableReference> GetLocalVariables (MD.StackFrame frame)
		{
			foreach (TargetVariable var in frame.Method.GetLocalVariables (frame.Thread)) {
				yield return new VariableReference (frame, var, ObjectValueFlags.Variable);
			}
		}
		
		public static IEnumerable<VariableReference> GetParameters (MD.StackFrame frame)
		{
			if (frame.Method != null) {
				foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread))
					yield return new VariableReference (frame, var, ObjectValueFlags.Parameter);
			}
		}
		public static string UnscapeString (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				if (c != '\\') {
					sb.Append (c);
					continue;
				}
				i++;
				if (i >= text.Length)
					return null;
				
				switch (text[i]) {
					case '\\': c = '\\'; break;
					case 'a': c = '\a'; break;
					case 'b': c = '\b'; break;
					case 'f': c = '\f'; break;
					case 'v': c = '\v'; break;
					case 'n': c = '\n'; break;
					case 'r': c = '\r'; break;
					case 't': c = '\t'; break;
					default: return null;
				}
				sb.Append (c);
			}
			return sb.ToString ();
		}
		
		public static string EscapeString (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				string txt;
				switch (c) {
					case '\\': txt = @"\\"; break;
					case '\a': txt = @"\a"; break;
					case '\b': txt = @"\b"; break;
					case '\f': txt = @"\f"; break;
					case '\v': txt = @"\v"; break;
					case '\n': txt = @"\n"; break;
					case '\r': txt = @"\r"; break;
					case '\t': txt = @"\t"; break;
					default: 
						sb.Append (c);
						continue;
				}
				sb.Append (txt);
			}
			return sb.ToString ();
		}
	}
}
