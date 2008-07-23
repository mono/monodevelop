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
			try {
				return CreateObjectValueImpl (thread, source, path, obj, flags);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return ObjectValue.CreateError (path.LastName, ex.Message, flags);
			}
		}
		
		static ObjectValue CreateObjectValueImpl (MD.Thread thread, IObjectValueSource source, ObjectPath path, TargetObject obj, ObjectValueFlags flags)
		{
			obj = GetRealObject (thread, obj);
			
			if (obj == null)
				return ObjectValue.CreateObject (null, path, "", null, flags | ObjectValueFlags.ReadOnly, null);

			if (obj.HasAddress && obj.GetAddress (thread).IsNull)
				return ObjectValue.CreateObject (null, path, obj.TypeName, "(null)", flags, null);
			
			switch (obj.Kind) {
				
				case TargetObjectKind.Struct:
				case TargetObjectKind.GenericInstance:
				case TargetObjectKind.Class:
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else {
						ObjectValue val = ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj), flags, null);
						return val;
					}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else
						return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj), flags, null);
					
				case TargetObjectKind.Array:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj), flags, null);
					
				case TargetObjectKind.Fundamental:
					TargetFundamentalObject fob = (TargetFundamentalObject) obj;
					return ObjectValue.CreatePrimitive (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, fob), flags);
					
				case TargetObjectKind.Enum:
					TargetEnumObject enumobj = (TargetEnumObject) obj;
					return CreateObjectValue (thread, source, path, enumobj.GetValue (thread), flags);
					
				case TargetObjectKind.Pointer:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToString (thread, obj), flags, null);
					
				default:
					return ObjectValue.CreateError (path.LastName, "Unknown value type: " + obj.Kind, flags);
			}
		}
		
		public static ObjectValue[] GetObjectValueChildren (MD.Thread thread, TargetObject obj, int firstItemIndex, int count)
		{
			obj = GetRealObject (thread, obj);
			
			switch (obj.Kind)
			{
				case MD.Languages.TargetObjectKind.Array: {
					
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return new ObjectValue [0];
					
					ArrayElementGroup agroup = new ArrayElementGroup (thread, new ArrayAdaptor (thread, arr));
					return agroup.GetChildren ();
				}
				case TargetObjectKind.GenericInstance:
				case TargetObjectKind.Struct:
				case TargetObjectKind.Class: {
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return new ObjectValue [0];
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
					CollectionAdaptor col = CollectionAdaptor.CreateAdaptor (thread, co);
					if (col != null) {
						ArrayElementGroup agroup = new ArrayElementGroup (thread, col);
						ObjectValue val = ObjectValue.CreateObject (null, new ObjectPath ("Raw View"), "", "", ObjectValueFlags.ReadOnly, values.ToArray ());
						values = new List<ObjectValue> ();
						values.Add (val);
						values.AddRange (agroup.GetChildren ());
					}
					return values.ToArray ();
				}
				case TargetObjectKind.Object:
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
			if (obj == null)
				return null;

			try {
				switch (obj.Kind) {
					case MD.Languages.TargetObjectKind.Array:
					case TargetObjectKind.Fundamental:
						return obj;
						
					case TargetObjectKind.Struct:
					case TargetObjectKind.GenericInstance:
					case TargetObjectKind.Class:
						TargetStructObject co = obj as TargetStructObject;
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
						if (oob.Type.CanDereference)
							return oob.GetDereferencedObject (thread);
						else
							return oob;
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
			foreach (KeyValuePair<TargetMemberInfo,TargetStructType> mem in GetTypeMembers (thread, t, co==null, true, true, false, true)) {
				if (mem.Key is TargetFieldInfo) {
					TargetFieldInfo field = (TargetFieldInfo) mem.Key;
					yield return new FieldReference (thread, co, mem.Value, field);
				}
				if (mem.Key is TargetPropertyInfo) {
					TargetPropertyInfo prop = (TargetPropertyInfo) mem.Key;
					if (prop.Getter.ParameterTypes == null || prop.Getter.ParameterTypes.Length == 0)
						yield return new PropertyReference (thread, prop, co);
				}
			}
		}
		
		public static TargetMethodInfo FindMethod (MD.Thread thread, TargetType type, string name, bool staticOnly, bool publicApiOnly, params string[] argTypes)
		{
			foreach (KeyValuePair<TargetMemberInfo,TargetStructType> mem in GetTypeMembers (thread, type, staticOnly, false, false, true, publicApiOnly)) {
				if (mem.Key.Name == name) {
					TargetMethodInfo met = (TargetMethodInfo) mem.Key;
					if (met.Type.ParameterTypes.Length == argTypes.Length) {
						bool allMatch = true;
						for (int n=0; n<argTypes.Length && allMatch; n++)
							allMatch = argTypes [n] == FixTypeName (met.Type.ParameterTypes [n].Name);
						if (allMatch)
							return met;
					}
				}
			}
			return null;
		}
		
		public static TargetMemberInfo FindMember (MD.Thread thread, TargetType type, string name, bool staticOnly, bool includeFields, bool includeProps, bool includeMethods, bool publicApiOnly)
		{
			foreach (KeyValuePair<TargetMemberInfo,TargetStructType> mem in GetTypeMembers (thread, type, staticOnly, includeFields, includeProps, includeMethods, publicApiOnly)) {
				if (mem.Key.Name == name)
					return mem.Key;
			}
			return null;
		}
		
		public static IEnumerable<KeyValuePair<TargetMemberInfo,TargetStructType>> GetTypeMembers (MD.Thread thread, TargetType t, bool staticOnly, bool includeFields, bool includeProps, bool includeMethods, bool publicApiOnly)
		{
			TargetStructType type = t as TargetStructType;

			while (type != null) {
				
				TargetFieldInfo[] fields = null;
				TargetPropertyInfo[] properties = null;
				TargetMethodInfo[] methods = null;
				
				TargetClass cls = type.GetClass (thread);
				if (cls != null) {
					if (includeFields)
						fields = cls.GetFields (thread);
					if (includeProps)
						properties = cls.GetProperties (thread);
					if (includeMethods)
						methods = cls.GetMethods (thread);
				}
				else {
					TargetClassType ct = type as TargetClassType;
					if (ct == null && type.HasClassType)
						ct = type.ClassType;
					if (ct != null) {
						if (includeFields)
							fields = ct.Fields;
						if (includeProps)
							properties = ct.Properties;
						if (includeMethods)
							methods = ct.Methods;
					}
				}
				
				if (fields != null) {
					foreach (TargetFieldInfo field in fields)
						if (field.IsStatic || !staticOnly)
							yield return new KeyValuePair<TargetMemberInfo,TargetStructType> (field, type);
				}
				
				if (properties != null) {
					foreach (TargetPropertyInfo prop in properties) {
						if (publicApiOnly && prop.Accessibility != TargetMemberAccessibility.Public && prop.Accessibility != TargetMemberAccessibility.Protected)
							continue;
						if (prop.IsStatic || !staticOnly)
							yield return new KeyValuePair<TargetMemberInfo,TargetStructType> (prop, type);
					}
				}
				
				if (methods != null) {
					foreach (TargetMethodInfo met in methods) {
						if (publicApiOnly && met.Accessibility != TargetMemberAccessibility.Public && met.Accessibility != TargetMemberAccessibility.Protected)
							continue;
						if (met.IsStatic || !staticOnly)
							yield return new KeyValuePair<TargetMemberInfo,TargetStructType> (met, type);
					}
				}
				
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
		
		public static string FixTypeName (string typeName)
		{
			// Required since the debugger uses C# type aliases for fundamental types, 
			// which is silly, but looks like it won't be fixed any time soon.
			
			switch (typeName) {
				case "short":   return "System.Int16";
				case "ushort":  return "System.UInt16";
				case "int":     return "System.Int32";
				case "uint":    return "System.UInt32";
				case "long":    return "System.Int64";
				case "ulong":   return "System.UInt64";
				case "float":   return "System.Single";
				case "double":  return "System.Double";
				case "char":    return "System.Char";
				case "byte":    return "System.Byte";
				case "sbyte":   return "System.SByte";
				case "object":  return "System.Object";
				case "string":  return "System.String";
				case "bool":    return "System.Boolean";
				case "void":    return "System.Void";
				case "decimal": return "System.Decimal";
			}
			return typeName;
		}
		
		public static TargetObject GetTypeOf (MD.StackFrame frame, string typeName)
		{
			TargetType tt = frame.Language.LookupType ("System.Type");
			if (tt == null)
				return null;

			TargetMethodInfo gt = FindMethod (frame.Thread, tt, "GetType", true, false, "System.String");
			if (gt == null)
				return null;
			
			TargetObject tn = frame.Language.CreateInstance (frame.Thread, FixTypeName (typeName));
			return Server.Instance.RuntimeInvoke (frame.Thread, gt.Type, null, new TargetObject [] { tn });
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
