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
using System.Diagnostics;
using MD = Mono.Debugger;
using SR = System.Reflection;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace DebuggerServer
{
	public static class Util
	{
		public static ObjectValue CreateObjectValue (EvaluationContext ctx, IObjectValueSource source, ObjectPath path, TargetObject obj, ObjectValueFlags flags)
		{
			try {
				return CreateObjectValueImpl (ctx, source, path, obj, flags);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return ObjectValue.CreateError (path.LastName, ex.Message, flags);
			}
		}
		
		static ObjectValue CreateObjectValueImpl (EvaluationContext ctx, IObjectValueSource source, ObjectPath path, TargetObject obj, ObjectValueFlags flags)
		{
			obj = ObjectUtil.GetRealObject (ctx, obj);
			
			if (obj == null)
				return ObjectValue.CreateObject (null, path, "", null, flags | ObjectValueFlags.ReadOnly, null);

			if (obj.HasAddress && obj.GetAddress (ctx.Thread).IsNull)
				return ObjectValue.CreateObject (null, path, obj.TypeName, "(null)", flags, null);
			
			switch (obj.Kind) {
				
				case TargetObjectKind.Struct:
				case TargetObjectKind.GenericInstance:
				case TargetObjectKind.Class:
					TypeDisplayData tdata = ObjectUtil.GetTypeDisplayData (ctx, obj.Type);
					
					TargetStructObject co = obj as TargetStructObject;
					if (co == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else {
						string tvalue;
						if (!string.IsNullOrEmpty (tdata.ValueDisplayString))
							tvalue = ObjectUtil.EvaluateDisplayString (ctx, co, tdata.ValueDisplayString);
						else
							tvalue = Server.Instance.Evaluator.TargetObjectToExpression (ctx, obj);
						
						string tname;
						if (!string.IsNullOrEmpty (tdata.TypeDisplayString))
							tname = ObjectUtil.EvaluateDisplayString (ctx, co, tdata.TypeDisplayString);
						else
							tname = obj.TypeName;
						
						ObjectValue val = ObjectValue.CreateObject (source, path, tname, tvalue, flags, null);
						if (!string.IsNullOrEmpty (tdata.NameDisplayString))
							val.Name = ObjectUtil.EvaluateDisplayString (ctx, co, tdata.NameDisplayString);
						return val;
					}
					
				case TargetObjectKind.Object:
					TargetObjectObject oob = obj as TargetObjectObject;
					if (oob == null)
						return ObjectValue.CreateUnknown (path.LastName);
					else
						return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);
					
				case TargetObjectKind.Array:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);
					
				case TargetObjectKind.Fundamental:
					TargetFundamentalObject fob = (TargetFundamentalObject) obj;
					return ObjectValue.CreatePrimitive (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, fob), flags);
					
				case TargetObjectKind.Enum:
					Console.WriteLine ("pp ??? ENUM: " + obj.GetType ());
					TargetEnumObject enumobj = (TargetEnumObject) obj;
					return CreateObjectValue (ctx, source, path, enumobj.GetValue (ctx.Thread), flags);
					
				case TargetObjectKind.Pointer:
					return ObjectValue.CreateObject (source, path, obj.TypeName, Server.Instance.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);
					
				default:
					return ObjectValue.CreateError (path.LastName, "Unknown value type: " + obj.Kind, flags);
			}
		}
		
		public static ObjectValue[] GetObjectValueChildren (EvaluationContext ctx, TargetObject obj, int firstItemIndex, int count)
		{
			return GetObjectValueChildren (ctx, obj, firstItemIndex, count, true);
		}
		
		public static ObjectValue[] GetObjectValueChildren (EvaluationContext ctx, TargetObject obj, int firstItemIndex, int count, bool dereferenceProxy)
		{
			obj = ObjectUtil.GetRealObject (ctx, obj);
			
			switch (obj.Kind)
			{
				case MD.Languages.TargetObjectKind.Array: {
					
					TargetArrayObject arr = obj as TargetArrayObject;
					if (arr == null)
						return new ObjectValue [0];
					
					ArrayElementGroup agroup = new ArrayElementGroup (ctx, new ArrayAdaptor (ctx, arr));
					return agroup.GetChildren ();
				}
				case TargetObjectKind.GenericInstance:
				case TargetObjectKind.Struct:
				case TargetObjectKind.Class: {
					// If there is a proxy, it has to show the members of the proxy
					TargetObject proxy = dereferenceProxy ? ObjectUtil.GetProxyObject (ctx, obj) : obj;
					TargetStructObject co = (TargetStructObject) proxy;
					TypeDisplayData tdata = ObjectUtil.GetTypeDisplayData (ctx, proxy.Type);
					List<ObjectValue> values = new List<ObjectValue> ();
					ReqMemberAccess access = tdata.IsProxyType ? ReqMemberAccess.Public : ReqMemberAccess.Auto;
					foreach (ValueReference val in GetMembers (ctx, co.Type, co, access)) {
						try {
							DebuggerBrowsableState state = tdata.GetMemberBrowsableState (val.Name);
							if (state == DebuggerBrowsableState.Never)
								continue;

							if (state == DebuggerBrowsableState.RootHidden) {
								TargetObject ob = val.Value;
								if (ob != null)
									values.AddRange (Util.GetObjectValueChildren (ctx, ob, -1, -1));
							}
							else {
								values.Add (val.CreateObjectValue (true));
							}
							
						} catch (Exception ex) {
							Server.Instance.WriteDebuggerError (ex);
							values.Add (ObjectValue.CreateError (null, new ObjectPath (val.Name), val.Type.Name, ex.Message, val.Flags));
						}
					}
					if (tdata.IsProxyType) {
						values.Add (RawViewSource.CreateRawView (ctx, obj));
					} else {
						CollectionAdaptor col = CollectionAdaptor.CreateAdaptor (ctx, co);
						if (col != null) {
							ArrayElementGroup agroup = new ArrayElementGroup (ctx, col);
							ObjectValue val = ObjectValue.CreateObject (null, new ObjectPath ("Raw View"), "", "", ObjectValueFlags.ReadOnly, values.ToArray ());
							values = new List<ObjectValue> ();
							values.Add (val);
							values.AddRange (agroup.GetChildren ());
						}
					}
					return values.ToArray ();
				}
				case TargetObjectKind.Object:
					return new ObjectValue [0];
				
				case TargetObjectKind.Pointer:
					TargetPointerObject pobj = (TargetPointerObject) obj;
					TargetObject defob = pobj.GetDereferencedObject (ctx.Thread);
					LiteralValueReference pval = new LiteralValueReference (ctx, "*", defob);
					return new ObjectValue [] {pval.CreateObjectValue () };
				default:
					return new ObjectValue [0];
			}		
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
		
		public static IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, TargetType t, TargetStructObject co)
		{
			return GetMembers (ctx, t, co, ReqMemberAccess.Auto);
		}
		
		public static IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, TargetType t, TargetStructObject co, ReqMemberAccess access)
		{
			foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, t, co==null, true, true, false, access)) {
				if (mem.Member is TargetFieldInfo) {
					TargetFieldInfo field = (TargetFieldInfo) mem.Member;
					yield return new FieldReference (ctx, co, mem.DeclaringType, field);
				}
				if (mem.Member is TargetPropertyInfo) {
					TargetPropertyInfo prop = (TargetPropertyInfo) mem.Member;
					if (prop.CanRead && (prop.Getter.ParameterTypes == null || prop.Getter.ParameterTypes.Length == 0))
						yield return new PropertyReference (ctx, prop, co);
				}
			}
		}
		
		public static IEnumerable<VariableReference> GetLocalVariables (EvaluationContext ctx)
		{
			foreach (TargetVariable var in ctx.Frame.Method.GetLocalVariables (ctx.Thread)) {
				yield return new VariableReference (ctx, var, ObjectValueFlags.Variable);
			}
		}
		
		public static IEnumerable<VariableReference> GetParameters (EvaluationContext ctx)
		{
			if (ctx.Frame.Method != null) {
				foreach (TargetVariable var in ctx.Frame.Method.GetParameters (ctx.Thread))
					yield return new VariableReference (ctx, var, ObjectValueFlags.Parameter);
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
	
	public enum ReqMemberAccess
	{
		All,
		Auto,
		Public
	}
}
