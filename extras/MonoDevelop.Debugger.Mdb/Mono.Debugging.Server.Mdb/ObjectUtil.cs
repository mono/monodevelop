// ObjectUtil.cs
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
using SR = System.Reflection;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace DebuggerServer
{
	public static class ObjectUtil
	{
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
		
		public static string CallToString (MD.Thread thread, TargetStructObject obj)
		{
			try {
				TargetObject retval = CallMethod (thread, "ToString", obj);
				object s = ((TargetFundamentalObject) retval).GetObject (thread);
				return s != null ? s.ToString () : "";
			} catch (Exception ex) {
				// Ignore
				Console.WriteLine ("pp: "  + ex);
			}
			
			return null;
		}
		

		public static MemberReference OverloadResolve (MD.Thread thread, string methodName, TargetStructType type, TargetType[] argtypes, bool allowInstance, bool allowStatic)
		{
			List<MemberReference> candidates = new List<MemberReference> ();

			foreach (MemberReference mem in ObjectUtil.GetTypeMembers (thread, type, false, false, false, true, false)) {
				TargetMethodInfo met = (TargetMethodInfo) mem.Member;
				if (met.Name == methodName && met.Type.ParameterTypes.Length == argtypes.Length && (met.IsStatic && allowStatic || !met.IsStatic && allowInstance))
					candidates.Add (mem);
			}
			
			if (candidates.Count == 1) {
				TargetFunctionType func = (TargetFunctionType) ((TargetMethodInfo) candidates [0].Member).Type;
				string error;
				int matchCount;
				if (IsApplicable (thread, func, argtypes, out error, out matchCount))
					return candidates [0];

				throw new EvaluatorException ("Invalid arguments for method `{0}': {1}", methodName, error);
			}

			if (candidates.Count == 0)
				throw new EvaluatorException ("Method `{0}' not found in type `{1}'.", methodName, type.Name);

			return OverloadResolve (thread, methodName, argtypes, candidates);
		}

		static bool IsApplicable (MD.Thread thread, TargetFunctionType method, TargetType[] types, out string error, out int matchCount)
		{
			TargetMethodSignature sig = method.GetSignature (thread);
			matchCount = 0;

			for (int i = 0; i < types.Length; i++) {
				TargetType param_type = sig.ParameterTypes [i];

				if (param_type == types [i]) {
					matchCount++;
					continue;
				}

				if (TargetObjectConvert.ImplicitConversionExists (thread, types [i], param_type))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, types [i].Name, param_type.Name);
				return false;
			}

			error = null;
			return true;
		}

		static MemberReference OverloadResolve (MD.Thread thread, string methodName, TargetType[] argtypes, List<MemberReference> candidates)
		{
			// Ok, no we need to find an exact match.
			MemberReference match = null;
			int bestCount = -1;
			bool repeatedBestCount = false;
			
			foreach (MemberReference method in candidates) {
				string error;
				int matchCount;
				TargetFunctionType func;
				if (method.Member is TargetMethodInfo)
					func = (TargetFunctionType) ((TargetMethodInfo) method.Member).Type;
				else
					func = (TargetFunctionType) ((TargetPropertyInfo) method.Member).Getter;
				
				if (!IsApplicable (thread, func, argtypes, out error, out matchCount))
					continue;

				if (matchCount == bestCount) {
					repeatedBestCount = true;
				} else if (matchCount > bestCount) {
					match = method;
					bestCount = matchCount;
					repeatedBestCount = false;
				}
			}
			
			if (match == null) {
				if (methodName != null)
					throw new EvaluatorException ("Invalid arguments for method `{0}'.", methodName);
				else
					throw new EvaluatorException ("Invalid arguments for indexer.");
			}
			
			if (repeatedBestCount) {
				if (methodName != null)
					throw new EvaluatorException ("Ambiguous method `{0}'; need to use full name", methodName);
				else
					throw new EvaluatorException ("Ambiguous arguments for indexer.", methodName);
			}

			return match;
		}		
		
		static TargetPropertyInfo ResolveProperty (MD.Thread thread, TargetType type, string name, ref TargetObject[] indexerArgs)
		{
			if (indexerArgs.Length == 0)
				return FindMember (thread, type, name, false, false, true, false, false) as TargetPropertyInfo;

			// It is an indexer. Find the best overload.
			
			TargetType[] types = new TargetType [indexerArgs.Length];
			for (int n=0; n<indexerArgs.Length; n++)
				types [n] = indexerArgs [n].Type;
			
			List<MemberReference> candidates = new List<MemberReference> ();
			foreach (MemberReference mem in GetTypeMembers (thread, type, false, false, true, false, false)) {
				TargetPropertyInfo prop = mem.Member as TargetPropertyInfo;
				if (prop != null && prop.Getter.ParameterTypes.Length == indexerArgs.Length)
					candidates.Add (mem);
			}
			MemberReference memr = OverloadResolve (thread, null, types, candidates);
			TargetPropertyInfo tprop = (TargetPropertyInfo) memr.Member;
			TargetMethodSignature sig = tprop.Getter.GetSignature (thread);
			
			TargetObject[] objs = new TargetObject [indexerArgs.Length];
			for (int i = 0; i < indexerArgs.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (thread, indexerArgs[i], sig.ParameterTypes [i]);
			}
			
			indexerArgs = objs;
			return tprop;
		}
		
		
		public static TargetObject CallMethod (MD.Thread thread, string name,
							  TargetObject target,
							  params TargetObject[] args)
		{
/*			if (target is TargetGenericInstanceObject || !(target is TargetStructObject)) {
				// Calling methods on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				
				if (name != "ToString" || args.Length != 0) {
					GetTypeOf (thread.CurrentFrame, "System.Convert");
					TargetType cc = thread.CurrentFrame.Language.LookupType ("System.Convert");
					SR.BindingFlags sf = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Static | SR.BindingFlags.FlattenHierarchy | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
					CallMethodWithReflection (thread, sf, "ToString", cc, null, target);
				}

				SR.BindingFlags f = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (thread, f, name, target.Type, target, args);
			}
*/
			TargetType[] types = new TargetType [args.Length];
			for (int n=0; n<types.Length; n++)
				types [n] = args [n].Type;
			
			TargetStructObject starget = (TargetStructObject) target;
			MemberReference mem = OverloadResolve (thread, name, starget.Type, types, true, false);
			TargetFunctionType function = (TargetFunctionType) ((TargetMethodInfo) mem.Member).Type;
			
			while (target.Type != mem.DeclaringType) {
				TargetStructObject par = starget.GetParentObject (thread);
				if (par != null)
					target = par;
				else
					break;
			}
			
			TargetMethodSignature sig = function.GetSignature (thread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (
					thread, args [i], sig.ParameterTypes [i]);
			}
			
			MD.RuntimeInvokeResult res = thread.RuntimeInvoke (function, starget, objs, true, false);
			res.Wait ();
			if (res.ExceptionMessage != null)
				throw new Exception (res.ExceptionMessage);
			return res.ReturnObject;
		}
		
		public static TargetObject CallStaticMethod (MD.Thread thread, string name,
							  TargetType type,
							  params TargetObject[] args)
		{
/*			if (type is TargetGenericInstanceType || !(type is TargetStructType)) {
				// Calling methods on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				
				SR.BindingFlags f = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Static | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (thread, f, name, type, null, args);
			}
*/			
			TargetType[] types = new TargetType [args.Length];
			for (int n=0; n<types.Length; n++)
				types [n] = args [n].Type;
			
			MemberReference mem = OverloadResolve (thread, name, (TargetStructType) type, types, false, true);
			TargetFunctionType function = (TargetFunctionType) ((TargetMethodInfo) mem.Member).Type;
			
			TargetMethodSignature sig = function.GetSignature (thread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (thread, args [i], sig.ParameterTypes [i]);
			}

			MD.RuntimeInvokeResult res = thread.RuntimeInvoke (function, null, objs, true, false);
			res.Wait ();
			if (res.ExceptionMessage != null)
				throw new Exception (res.ExceptionMessage);
			return res.ReturnObject;
		}
		
		public static void SetPropertyValue (MD.Thread thread, string name, TargetObject target, TargetObject value, params TargetObject[] indexerArgs)
		{
/*			if (target is TargetGenericInstanceObject || !(target is TargetStructObject)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				TargetObject[] args = new TargetObject [indexerArgs.Length + 1];
				Array.Copy (indexerArgs, args, indexerArgs.Length);
				args [args.Length - 1] = value;
				SR.BindingFlags f = SR.BindingFlags.SetProperty | SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				CallMethodWithReflection (thread, f, name, target.Type, target, args);
				return;
			}
*/
			TargetStructObject starget = (TargetStructObject) target;
			TargetPropertyInfo prop = ResolveProperty (thread, starget.Type, name, ref indexerArgs);
			TargetObject[] args = new TargetObject [indexerArgs.Length + 1];
			Array.Copy (indexerArgs, args, indexerArgs.Length);
			args [args.Length - 1] = value;
			Server.Instance.RuntimeInvoke (thread, prop.Setter, starget, args);
		}
		
		public static TargetObject GetPropertyValue (MD.Thread thread, string name, TargetObject target, params TargetObject[] indexerArgs)
		{
/*			if (target is TargetGenericInstanceObject || !(target is TargetStructObject)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				SR.BindingFlags f = SR.BindingFlags.GetProperty | SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (thread, f, name, target.Type, target, indexerArgs);
			}
*/
			TargetStructObject starget = (TargetStructObject) target;
			TargetPropertyInfo prop = ResolveProperty (thread, starget.Type, name, ref indexerArgs);
			if (prop.IsStatic)
				throw new EvaluatorException ("Property is static and can't be accessed using an object instance.");
			return Server.Instance.RuntimeInvoke (thread, prop.Getter, starget, indexerArgs);
		}
		
		public static void SetStaticPropertyValue (MD.Thread thread, string name, TargetType type, TargetObject value, params TargetObject[] indexerArgs)
		{
/*			if (type is TargetGenericInstanceType || !(type is TargetStructType)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				TargetObject[] args = new TargetObject [indexerArgs.Length + 1];
				Array.Copy (indexerArgs, args, indexerArgs.Length);
				args [args.Length - 1] = value;
				SR.BindingFlags f = SR.BindingFlags.SetProperty | SR.BindingFlags.Static | SR.BindingFlags.FlattenHierarchy | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				CallMethodWithReflection (thread, f, name, type, null, args);
				return;
			}
			*/
			TargetPropertyInfo prop = ResolveProperty (thread, type, name, ref indexerArgs);
			TargetObject[] args = new TargetObject [indexerArgs.Length + 1];
			Array.Copy (indexerArgs, args, indexerArgs.Length);
			args [args.Length - 1] = value;
			Server.Instance.RuntimeInvoke (thread, prop.Setter, null, args);
		}
		
		public static TargetObject GetStaticPropertyValue (MD.Thread thread, string name, TargetType type, params TargetObject[] indexerArgs)
		{
/*			if (type is TargetGenericInstanceType || !(type is TargetStructType)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				SR.BindingFlags f = SR.BindingFlags.GetProperty | SR.BindingFlags.Static | SR.BindingFlags.FlattenHierarchy | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (thread, f, name, type, null, indexerArgs);
			}
*/
			TargetPropertyInfo prop = ResolveProperty (thread, type, name, ref indexerArgs);
			if (!prop.IsStatic)
				throw new EvaluatorException ("Property is not static.");
			return Server.Instance.RuntimeInvoke (thread, prop.Getter, null, indexerArgs);
		}
		
/*		static TargetObject CallMethodWithReflection (MD.Thread thread, SR.BindingFlags flags, string name,
							  TargetType type, TargetObject target,
							  params TargetObject[] args)
		{
			MD.StackFrame frame = thread.CurrentFrame;
			string typeName = type.Name;
			
			TargetStructObject tt = GetTypeOf (frame, typeName);
			if (tt == null)
				throw new InvalidOperationException ("Type not found: " + typeName);
			TargetObject objName = frame.Language.CreateInstance (thread, name);
			TargetObject objFlags = frame.Language.CreateInstance (thread, (int)flags);
			TargetObject objBinder = frame.Language.CreateNullObject (thread, frame.Language.LookupType ("System.Reflection.Binder"));
			TargetObject objTarget = target ?? frame.Language.CreateNullObject (thread, frame.Language.ObjectType);

			TargetType at = frame.Language.LookupType ("System.Array");
			TargetObject len = frame.Language.CreateInstance (thread, args.Length);
			TargetObject ot = ObjectUtil.GetTypeOf (frame, "System.Object");
			TargetObject arrayob = ObjectUtil.CallStaticMethod (thread, "CreateInstance", at, ot, len);
			TargetArrayObject array = ObjectUtil.GetRealObject (thread, arrayob) as TargetArrayObject;

			if (target != null)
				objTarget = TargetObjectConvert.Cast (frame, objTarget, frame.Language.ObjectType);
			
			int[] idx = new int [1];
			for (int n=0; n<args.Length; n++) {
				idx [0] = n;
				TargetObject objObj = TargetObjectConvert.Cast (frame, args[n], frame.Language.ObjectType);
				array.SetElement (thread, idx, objObj);
			}
			TargetObject res = CallMethod (thread, "InvokeMember", tt, objName, objFlags, objBinder, objTarget, array);
			return res;
		}
*/
		
		public static MemberReference FindMethod (MD.Thread thread, TargetType type, string name, bool findStatic, bool publicApiOnly, params string[] argTypes)
		{
			foreach (MemberReference mem in GetTypeMembers (thread, type, findStatic, false, false, true, publicApiOnly)) {
				if (mem.Member.Name == name && mem.Member.IsStatic == findStatic) {
					TargetMethodInfo met = (TargetMethodInfo) mem.Member;
					if (met.Type.ParameterTypes.Length == argTypes.Length) {
						bool allMatch = true;
						for (int n=0; n<argTypes.Length && allMatch; n++)
							allMatch = argTypes [n] == FixTypeName (met.Type.ParameterTypes [n].Name);
						if (allMatch)
							return mem;
					}
				}
			}
			return null;
		}
		
		public static TargetMemberInfo FindMember (MD.Thread thread, TargetType type, string name, bool staticOnly, bool includeFields, bool includeProps, bool includeMethods, bool publicApiOnly)
		{
			foreach (MemberReference mem in GetTypeMembers (thread, type, staticOnly, includeFields, includeProps, includeMethods, publicApiOnly)) {
				if (mem.Member.Name == name)
					return mem.Member;
			}
			return null;
		}
		
		public static IEnumerable<MemberReference> GetTypeMembers (MD.Thread thread, TargetType t, bool staticOnly, bool includeFields, bool includeProps, bool includeMethods, bool publicApiOnly)
		{
			TargetStructType type = t as TargetStructType;
			Dictionary<string,string> foundMethods = new Dictionary<string,string> ();

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
							yield return new MemberReference (field, type);
				}
				
				if (properties != null) {
					foreach (TargetPropertyInfo prop in properties) {
						if (publicApiOnly && prop.Accessibility != TargetMemberAccessibility.Public && prop.Accessibility != TargetMemberAccessibility.Protected)
							continue;
						if (prop.IsStatic || !staticOnly)
							yield return new MemberReference (prop, type);
					}
				}
				
				if (methods != null) {
					foreach (TargetMethodInfo met in methods) {
						if (publicApiOnly && met.Accessibility != TargetMemberAccessibility.Public && met.Accessibility != TargetMemberAccessibility.Protected)
							continue;
						if (met.IsStatic || !staticOnly) {
							string sig = GetSignature (met);
							if (!foundMethods.ContainsKey (sig)) {
								foundMethods [sig] = sig;
								yield return new MemberReference (met, type);
							}
						}
					}
				}
				
				if (type.HasParent)
					type = type.GetParentType (thread);
				else
					break;
			}
		}
		
		static string GetSignature (TargetMethodInfo met)
		{
			StringBuilder sb = new StringBuilder (met.Name);
			foreach (TargetType t in met.Type.ParameterTypes)
				sb.Append (' ').Append (t.Name);
			return sb.ToString ();
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
		
		public static TargetStructObject GetTypeOf (MD.StackFrame frame, string typeName)
		{
			TargetType tt = frame.Language.LookupType ("System.Type");
			if (tt == null)
				return null;

			TargetObject tn = frame.Language.CreateInstance (frame.Thread, FixTypeName (typeName));
			TargetObject res = CallStaticMethod (frame.Thread, "GetType", tt, tn);
			return (TargetStructObject) GetRealObject (frame.Thread, res);
		}
		
		public static bool IsInstanceOfType (MD.Thread thread, string typeName, TargetObject obj)
		{
			TargetStructObject tt = GetTypeOf (thread.CurrentFrame, typeName);
			TargetObject to = TargetObjectConvert.Cast (thread.CurrentFrame, obj, thread.CurrentFrame.Language.ObjectType);
			TargetFundamentalObject res = CallMethod (thread, "IsInstanceOfType", tt, to) as TargetFundamentalObject;
			return (bool) res.GetObject (thread);
		}
		
		public static string FixTypeName (string typeName)
		{
			// Required since the debugger uses C# type aliases for fundamental types, 
			// which is silly, but looks like it won't be fixed any time soon.
			
			typeName = typeName.Replace ('/','+');
			
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
			
			int i = typeName.IndexOf ('<');
			if (i != -1) {
				StringBuilder sb = new StringBuilder (typeName.Substring (0, i));
				sb.Append ('[');
				i++;
				int e;
				do {
					e = FindTypeEnd (typeName, i);
					sb.Append (FixTypeName (typeName.Substring (i, e-i)));
					sb.Append (',');
					i = e + 1;
				} while (typeName [e] == ',');
				
				sb.Remove (sb.Length - 1, 1);
				sb.Append (']');
				return sb.ToString ();
			}
			
			return typeName;
		}
		
		static int FindTypeEnd (string str, int index)
		{
			int level = 0;
			while (index < str.Length) {
				char c = str [index];
				if (c == '[' || c == '<')
					level++;
				if (c == ']' || c == '>') {
					if (level == 0)
						return index;
					else
						level--;
				}
				if (c == ',' && level == 0)
					return index;
				index++;
			}
			return index;
		}
	}
	
	public class MemberReference
	{
		public readonly TargetMemberInfo Member;
		public readonly TargetStructType DeclaringType;
		
		public MemberReference (TargetMemberInfo member, TargetStructType type)
		{
			Member = member;
			DeclaringType = type;
		}
	}
}
