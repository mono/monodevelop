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

// #define REFLECTION_INVOKE

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
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
		static Dictionary<string,TypeDisplayData> typeDisplayData = new Dictionary<string,TypeDisplayData> ();
		static Dictionary<TargetType,TargetType> proxyTypes = new Dictionary<TargetType,TargetType> ();
		
		public static TargetObject GetRealObject (EvaluationContext ctx, TargetObject obj)
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
						TargetObject res = co.GetCurrentObject (ctx.Thread);
						return res ?? obj;
						
					case TargetObjectKind.Enum:
						TargetEnumObject eob = (TargetEnumObject) obj;
						return eob.GetValue (ctx.Thread);
						
					case TargetObjectKind.Object:
						TargetObjectObject oob = obj as TargetObjectObject;
						if (oob == null)
							return null;
						if (oob.Type.CanDereference)
							return oob.GetDereferencedObject (ctx.Thread);
						else
							return oob;
				}
			}
			catch {
				// Ignore
			}
			return obj;
		}	
		
		public static TargetObject GetProxyObject (EvaluationContext ctx, TargetObject obj)
		{
			TypeDisplayData data = GetTypeDisplayData (ctx, obj.Type);
			if (data.ProxyType == null)
				return obj;

			TargetType ttype = ctx.Frame.Language.LookupType (data.ProxyType);
			if (ttype == null) {
				int i = data.ProxyType.IndexOf (',');
				if (i != -1)
					ttype = ctx.Frame.Language.LookupType (data.ProxyType.Substring (0, i).Trim ());
			}
			if (ttype == null)
				throw new EvaluatorException ("Unknown type '{0}'", data.ProxyType);

			TargetObject proxy = CreateObject (ctx, ttype, obj);
			return GetRealObject (ctx, proxy);
		}
		
		public static TypeDisplayData GetTypeDisplayData (EvaluationContext ctx, TargetType type)
		{
			if (type.Name == null)
				return TypeDisplayData.Default;
			
			TypeDisplayData td = null;
			try {
				td = GetTypeDisplayDataInternal (ctx, type);
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
			}
			if (td == null)
				typeDisplayData [type.Name] = td = TypeDisplayData.Default;
			return td;
		}
		
		static TypeDisplayData GetTypeDisplayDataInternal (EvaluationContext ctx, TargetType type)
		{
			TypeDisplayData data;
			if (typeDisplayData.TryGetValue (type.Name, out data))
				return data;

			data = new TypeDisplayData ();

			// Attribute inspection disabled until MDB provided a proper api for it
/*			TargetObject tt = GetTypeOf (ctx, type);
			if (tt == null) {
				typeDisplayData [type.Name] = data;
				throw new Exception ("Could not get type of " + type.Name);
			}
			TargetObject inherit = ctx.Frame.Language.CreateInstance (ctx.Thread, true);
			
			data.IsProxyType = proxyTypes.ContainsKey (type);
			
			// Look for DebuggerTypeProxyAttribute
			TargetStructObject attType = GetTypeOf (ctx, "System.Diagnostics.DebuggerTypeProxyAttribute");
			if (attType == null)
				return null;
			
			TargetObject at = CallStaticMethod (ctx, "GetCustomAttribute", "System.Attribute", tt, attType, inherit);
			// HACK: The first call doesn't work the first time for generic types. So we do it again.
			at = CallStaticMethod (ctx, "GetCustomAttribute", "System.Attribute", tt, attType, inherit);
			at = GetRealObject (ctx, at);
			
			if (at != null && !(at.HasAddress && at.GetAddress (ctx.Thread).IsNull)) {
				TargetFundamentalObject pname = GetPropertyValue (ctx, "ProxyTypeName", at) as TargetFundamentalObject;
				string ptname = (string) pname.GetObject (ctx.Thread);
				TargetType ptype = LookupType (ctx, ptname);
				if (ptype != null) {
					data.ProxyType = ptname;
					proxyTypes [ptype] = ptype;
				}
			}
			
			// Look for DebuggerDisplayAttribute
			attType = GetTypeOf (ctx, "System.Diagnostics.DebuggerDisplayAttribute");
			at = CallStaticMethod (ctx, "GetCustomAttribute", "System.Attribute", tt, attType, inherit);
			at = GetRealObject (ctx, at);
			
			if (at != null && !(at.HasAddress && at.GetAddress (ctx.Thread).IsNull)) {
				TargetFundamentalObject pname = GetPropertyValue (ctx, "Value", at) as TargetFundamentalObject;
				data.ValueDisplayString = (string) pname.GetObject (ctx.Thread);
				pname = GetPropertyValue (ctx, "Type", at) as TargetFundamentalObject;
				data.TypeDisplayString = (string) pname.GetObject (ctx.Thread);
				pname = GetPropertyValue (ctx, "Name", at) as TargetFundamentalObject;
				data.NameDisplayString = (string) pname.GetObject (ctx.Thread);
			}
			
			// Now check fields and properties

			Dictionary<string,DebuggerBrowsableState> members = new Dictionary<string,DebuggerBrowsableState> ();
			attType = GetTypeOf (ctx, "System.Diagnostics.DebuggerBrowsableAttribute");
			TargetObject flags = ctx.Frame.Language.CreateInstance (ctx.Thread, (int) (BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance));
			TargetObject fields = CallMethod (ctx, "GetFields", tt, flags);
			TargetObject properties = CallMethod (ctx, "GetProperties", tt, flags);
			foreach (TargetArrayObject array in new TargetObject [] { fields, properties }) {
				int len = array.GetArrayBounds (ctx.Thread).Length;
				int[] idx = new int [1];
				for (int n=0; n<len; n++) {
					idx [0] = n;
					TargetObject member = array.GetElement (ctx.Thread, idx);
					at = CallStaticMethod (ctx, "GetCustomAttribute", "System.Attribute", member, attType, inherit);
					if (at != null && !(at.HasAddress && at.GetAddress (ctx.Thread).IsNull)) {
						TargetFundamentalObject mname = (TargetFundamentalObject) GetPropertyValue (ctx, "Name", member);
						TargetEnumObject ob = (TargetEnumObject) GetPropertyValue (ctx, "State", at);
						TargetFundamentalObject fob = (TargetFundamentalObject) ob.GetValue (ctx.Thread);
						int val = (int) fob.GetObject (ctx.Thread);
						members [mname.GetObject (ctx.Thread).ToString ()] = (DebuggerBrowsableState) val;
					}
				}
			}
			if (members.Count > 0)
				data.MemberData = members;
			*/
			typeDisplayData [type.Name] = data;
			return data;
		}
		
		public static TargetType LookupType (EvaluationContext ctx, string name)
		{
			TargetType ttype = ctx.Frame.Language.LookupType (name);
			if (ttype == null) {
				int i = name.IndexOf (',');
				if (i != -1)
					ttype = ctx.Frame.Language.LookupType (name.Substring (0, i).Trim ());
			}
			return ttype;
		}
		
		public static string EvaluateDisplayString (EvaluationContext ctx, TargetStructObject obj, string exp)
		{
			StringBuilder sb = new StringBuilder ();
			int last = 0;
			int i = exp.IndexOf ("{");
			while (i != -1 && i < exp.Length) {
				sb.Append (exp.Substring (last, i - last));
				i++;
				int j = exp.IndexOf ("}", i);
				if (j == -1)
					return exp;
				string mem = exp.Substring (i, j-i).Trim ();
				if (mem.Length == 0)
					return exp;
				
				MemberReference mi = ObjectUtil.FindMember (ctx, obj.Type, mem, false, true, true, true, ReqMemberAccess.All);
				TargetObject val = mi.GetValue (ctx, obj);
				sb.Append (Server.Instance.Evaluator.TargetObjectToString (ctx, val));
				last = j + 1;
				i = exp.IndexOf ("{", last);
			}
			sb.Append (exp.Substring (last));
			return sb.ToString ();
		}
		
		public static string CallToString (EvaluationContext ctx, TargetStructObject obj)
		{
			try {
				TargetObject retval = CallMethod (ctx, "ToString", obj);
				object s = ((TargetFundamentalObject) retval).GetObject (ctx.Thread);
				return s != null ? s.ToString () : "";
			} catch {
				// Ignore
			}
			
			return null;
		}
		

		public static MemberReference OverloadResolve (EvaluationContext ctx, string methodName, TargetStructType type, TargetType[] argtypes, bool allowInstance, bool allowStatic)
		{
			List<MemberReference> candidates = new List<MemberReference> ();

			if (methodName == ".ctor") {
				TargetClassType ct = type as TargetClassType;
				if (ct == null && type.HasClassType)
					ct = type.ClassType;
				
				foreach (TargetMethodInfo met in ct.Constructors) {
					if (met.Type.ParameterTypes.Length == argtypes.Length)
						candidates.Add (new MemberReference (met, type));
				}
			}
			else {
				foreach (MemberReference mem in ObjectUtil.GetTypeMembers (ctx, type, false, false, false, true, ReqMemberAccess.All)) {
					TargetMethodInfo met = (TargetMethodInfo) mem.Member;
					if (met.Name == methodName && met.Type.ParameterTypes.Length == argtypes.Length && (met.IsStatic && allowStatic || !met.IsStatic && allowInstance))
						candidates.Add (mem);
				}
			}
			
			if (candidates.Count == 1) {
				TargetFunctionType func = (TargetFunctionType) ((TargetMethodInfo) candidates [0].Member).Type;
				string error;
				int matchCount;
				if (IsApplicable (ctx, func, argtypes, out error, out matchCount))
					return candidates [0];

				throw new EvaluatorException ("Invalid arguments for method `{0}': {1}", methodName, error);
			}

			if (candidates.Count == 0)
				throw new EvaluatorException ("Method `{0}' not found in type `{1}'.", methodName, type.Name);

			return OverloadResolve (ctx, methodName, argtypes, candidates);
		}

		static bool IsApplicable (EvaluationContext ctx, TargetFunctionType method, TargetType[] types, out string error, out int matchCount)
		{
			TargetMethodSignature sig = method.GetSignature (ctx.Thread);
			matchCount = 0;

			for (int i = 0; i < types.Length; i++) {
				TargetType param_type = sig.ParameterTypes [i];

				if (param_type == types [i]) {
					matchCount++;
					continue;
				}

				if (TargetObjectConvert.ImplicitConversionExists (ctx, types [i], param_type))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, types [i].Name, param_type.Name);
				return false;
			}

			error = null;
			return true;
		}

		static MemberReference OverloadResolve (EvaluationContext ctx, string methodName, TargetType[] argtypes, List<MemberReference> candidates)
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
				
				if (!IsApplicable (ctx, func, argtypes, out error, out matchCount))
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
		
		static TargetPropertyInfo ResolveProperty (EvaluationContext ctx, TargetType type, string name, ref TargetObject[] indexerArgs)
		{
			if (indexerArgs.Length == 0) {
				MemberReference mr = FindMember (ctx, type, name, false, false, true, false, ReqMemberAccess.All);
				return mr != null ? mr.Member as TargetPropertyInfo : null;
			}

			// It is an indexer. Find the best overload.
			
			TargetType[] types = new TargetType [indexerArgs.Length];
			for (int n=0; n<indexerArgs.Length; n++)
				types [n] = indexerArgs [n].Type;
			
			List<MemberReference> candidates = new List<MemberReference> ();
			foreach (MemberReference mem in GetTypeMembers (ctx, type, false, false, true, false, ReqMemberAccess.All)) {
				TargetPropertyInfo prop = mem.Member as TargetPropertyInfo;
				if (prop != null && prop.Getter.ParameterTypes.Length == indexerArgs.Length)
					candidates.Add (mem);
			}
			MemberReference memr = OverloadResolve (ctx, null, types, candidates);
			TargetPropertyInfo tprop = (TargetPropertyInfo) memr.Member;
			TargetMethodSignature sig = tprop.Getter.GetSignature (ctx.Thread);
			
			TargetObject[] objs = new TargetObject [indexerArgs.Length];
			for (int i = 0; i < indexerArgs.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (ctx, indexerArgs[i], sig.ParameterTypes [i]);
			}
			
			indexerArgs = objs;
			return tprop;
		}
		
		
		public static TargetObject CallMethod (EvaluationContext ctx, string name,
							  TargetObject target,
							  params TargetObject[] args)
		{
#if REFLECTION_INVOKE
			if (target is TargetGenericInstanceObject || !(target is TargetStructObject)) {
				// Calling methods on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				
				if (name != "ToString" || args.Length != 0) {
					GetTypeOf (ctx, "System.Convert");
					TargetType cc = ctx.Frame.Language.LookupType ("System.Convert");
					SR.BindingFlags sf = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Static | SR.BindingFlags.FlattenHierarchy | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
					CallMethodWithReflection (ctx, sf, "ToString", cc, null, target);
				}

				SR.BindingFlags f = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (ctx, f, name, target.Type, target, args);
			}
#endif
			TargetType[] types = new TargetType [args.Length];
			for (int n=0; n<types.Length; n++)
				types [n] = args [n].Type;
			
			TargetStructObject starget = (TargetStructObject) target;
			MemberReference mem = OverloadResolve (ctx, name, starget.Type, types, true, false);
			TargetFunctionType function = (TargetFunctionType) ((TargetMethodInfo) mem.Member).Type;
			
			while (target.Type != mem.DeclaringType) {
				TargetStructObject par = starget.GetParentObject (ctx.Thread);
				if (par != null)
					target = par;
				else
					break;
			}
			
			TargetMethodSignature sig = function.GetSignature (ctx.Thread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (
					ctx, args [i], sig.ParameterTypes [i]);
			}

			return Server.Instance.RuntimeInvoke (ctx, function, starget, objs);
		}
		
		public static TargetObject CallStaticMethod (EvaluationContext ctx, string name,
							  string typeName,
							  params TargetObject[] args)
		{
			return CallStaticMethod (ctx, name, ctx.Frame.Language.LookupType (typeName), args);
		}
		
		public static TargetObject CallStaticMethod (EvaluationContext ctx, string name,
							  TargetType type,
							  params TargetObject[] args)
		{
#if REFLECTION_INVOKE
			if (type is TargetGenericInstanceType || !(type is TargetStructType)) {
				// Calling methods on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				
				SR.BindingFlags f = SR.BindingFlags.InvokeMethod | SR.BindingFlags.Static | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (ctx, f, name, type, null, args);
			}
#endif			
			TargetType[] types = new TargetType [args.Length];
			for (int n=0; n<types.Length; n++)
				types [n] = args [n].Type;
			
			MemberReference mem = OverloadResolve (ctx, name, (TargetStructType) type, types, false, true);
			TargetFunctionType function = (TargetFunctionType) ((TargetMethodInfo) mem.Member).Type;
			
			TargetMethodSignature sig = function.GetSignature (ctx.Thread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (ctx, args [i], sig.ParameterTypes [i]);
			}

			return Server.Instance.RuntimeInvoke (ctx, function, null, objs);
		}
		
		public static void SetPropertyValue (EvaluationContext ctx, string name, TargetObject target, TargetObject value, params TargetObject[] indexerArgs)
		{
#if REFLECTION_INVOKE
			if (target is TargetGenericInstanceObject || !(target is TargetStructObject)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				TargetObject[] args = new TargetObject [indexerArgs.Length + 1];
				Array.Copy (indexerArgs, args, indexerArgs.Length);
				args [args.Length - 1] = value;
				SR.BindingFlags f = SR.BindingFlags.SetProperty | SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				CallMethodWithReflection (ctx, f, name, target.Type, target, args);
				return;
			}
#endif
			TargetStructObject starget = (TargetStructObject) target;
			TargetPropertyInfo prop = ResolveProperty (ctx, starget.Type, name, ref indexerArgs);
			TargetObject[] sargs = new TargetObject [indexerArgs.Length + 1];
			Array.Copy (indexerArgs, sargs, indexerArgs.Length);
			sargs [sargs.Length - 1] = value;
			Server.Instance.RuntimeInvoke (ctx, prop.Setter, starget, sargs);
		}
		
		public static TargetObject GetPropertyValue (EvaluationContext ctx, string name, TargetObject target, params TargetObject[] indexerArgs)
		{
#if REFLECTION_INVOKE
			if (target is TargetGenericInstanceObject || !(target is TargetStructObject)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				SR.BindingFlags f = SR.BindingFlags.GetProperty | SR.BindingFlags.Instance | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (ctx, f, name, target.Type, target, indexerArgs);
			}
#endif
			TargetStructObject starget = (TargetStructObject) target;
			TargetPropertyInfo prop = ResolveProperty (ctx, starget.Type, name, ref indexerArgs);
			if (prop.IsStatic)
				throw new EvaluatorException ("Property is static and can't be accessed using an object instance.");
			return Server.Instance.RuntimeInvoke (ctx, prop.Getter, starget, indexerArgs);
		}
		
		public static void SetStaticPropertyValue (EvaluationContext ctx, string name, TargetType type, TargetObject value, params TargetObject[] indexerArgs)
		{
#if REFLECTION_INVOKE
			if (type is TargetGenericInstanceType || !(type is TargetStructType)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				TargetObject[] args = new TargetObject [indexerArgs.Length + 1];
				Array.Copy (indexerArgs, args, indexerArgs.Length);
				args [args.Length - 1] = value;
				SR.BindingFlags f = SR.BindingFlags.SetProperty | SR.BindingFlags.Static | SR.BindingFlags.FlattenHierarchy | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				CallMethodWithReflection (ctx, f, name, type, null, args);
				return;
			}
#endif
			TargetPropertyInfo prop = ResolveProperty (ctx, type, name, ref indexerArgs);
			TargetObject[] sargs = new TargetObject [indexerArgs.Length + 1];
			Array.Copy (indexerArgs, sargs, indexerArgs.Length);
			sargs [sargs.Length - 1] = value;
			Server.Instance.RuntimeInvoke (ctx, prop.Setter, null, sargs);
		}
		
		public static TargetObject GetStaticPropertyValue (EvaluationContext ctx, string name, TargetType type, params TargetObject[] indexerArgs)
		{
#if REFLECTION_INVOKE
			if (type is TargetGenericInstanceType || !(type is TargetStructType)) {
				// Accessing properties on generic objects is suprisingly not supported
				// by the debugger. As a workaround we do the call using reflection.
				SR.BindingFlags f = SR.BindingFlags.GetProperty | SR.BindingFlags.Static | SR.BindingFlags.FlattenHierarchy | SR.BindingFlags.Public | SR.BindingFlags.NonPublic;
				return CallMethodWithReflection (ctx, f, name, type, null, indexerArgs);
			}
#endif
			TargetPropertyInfo prop = ResolveProperty (ctx, type, name, ref indexerArgs);
			if (!prop.IsStatic)
				throw new EvaluatorException ("Property is not static.");
			return Server.Instance.RuntimeInvoke (ctx, prop.Getter, null, indexerArgs);
		}

		internal static TargetObject CallMethodWithReflection (EvaluationContext ctx, SR.BindingFlags flags, string name,
							  TargetType type, TargetObject target,
							  params TargetObject[] args)
		{
			MD.StackFrame frame = ctx.Frame;
			string typeName = type.Name;
			
			TargetStructObject tt = GetTypeOf (ctx, type);
			if (tt == null)
				throw new InvalidOperationException ("Type not found: " + typeName);
			TargetObject objName = frame.Language.CreateInstance (ctx.Thread, name);
			TargetObject objFlags = frame.Language.CreateInstance (ctx.Thread, (int)flags);
			TargetObject objBinder = frame.Language.CreateNullObject (ctx.Thread, frame.Language.LookupType ("System.Reflection.Binder"));
			TargetObject objTarget = target ?? frame.Language.CreateNullObject (ctx.Thread, frame.Language.ObjectType);

			TargetType at = frame.Language.LookupType ("System.Array");
			TargetObject len = frame.Language.CreateInstance (ctx.Thread, args.Length);
			TargetObject ot = ObjectUtil.GetTypeOf (ctx, "System.Object");
			TargetObject arrayob = ObjectUtil.CallStaticMethod (ctx, "CreateInstance", at, ot, len);
			TargetArrayObject array = ObjectUtil.GetRealObject (ctx, arrayob) as TargetArrayObject;

			if (target != null)
				objTarget = TargetObjectConvert.Cast (ctx, objTarget, frame.Language.ObjectType);
			
			int[] idx = new int [1];
			for (int n=0; n<args.Length; n++) {
				idx [0] = n;
				TargetObject objObj = TargetObjectConvert.Cast (ctx, args[n], frame.Language.ObjectType);
				array.SetElement (ctx.Thread, idx, objObj);
			}
			TargetObject res = CallMethod (ctx, "InvokeMember", tt, objName, objFlags, objBinder, objTarget, array);
			return res;
		}
		
		public static TargetObject CreateObject (EvaluationContext ctx, TargetType type, params TargetObject[] args)
		{
			SR.BindingFlags flags = SR.BindingFlags.CreateInstance | SR.BindingFlags.Public | SR.BindingFlags.Instance | SR.BindingFlags.Static;
			TargetObject res = CallMethodWithReflection (ctx, flags, "", type, null, args);
			return GetRealObject (ctx, res);
		}
		
		public static MemberReference FindMethod (EvaluationContext ctx, TargetType type, string name, bool findStatic, ReqMemberAccess access, params string[] argTypes)
		{
			foreach (MemberReference mem in GetTypeMembers (ctx, type, findStatic, false, false, true, access)) {
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
		
		public static MemberReference FindMember (EvaluationContext ctx, TargetType type, string name, bool staticOnly, bool includeFields, bool includeProps, bool includeMethods, ReqMemberAccess access)
		{
			foreach (MemberReference mem in GetTypeMembers (ctx, type, staticOnly, includeFields, includeProps, includeMethods, access)) {
				if (mem.Member.Name == name)
					return mem;
			}
			return null;
		}
		
		public static IEnumerable<MemberReference> GetTypeMembers (EvaluationContext ctx, TargetType t, bool staticOnly, bool includeFields, bool includeProps, bool includeMethods, ReqMemberAccess access)
		{
			// Don't use yield in this method because the whole list of members
			// must be retrieved before we can do anything with them.
			List<MemberReference> members = new List<MemberReference> ();
			
			TargetStructType type = t as TargetStructType;
			Dictionary<string,string> foundMethods = new Dictionary<string,string> ();

			while (type != null) {
				
				TargetFieldInfo[] fields = null;
				TargetPropertyInfo[] properties = null;
				TargetMethodInfo[] methods = null;

				TargetClass cls = type.GetClass (ctx.Thread);
				if (cls != null) {
					if (includeFields)
						fields = cls.GetFields (ctx.Thread);
					if (includeProps)
						properties = cls.GetProperties (ctx.Thread);
					if (includeMethods)
						methods = cls.GetMethods (ctx.Thread);
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
					foreach (TargetFieldInfo field in fields) {
						if (access == ReqMemberAccess.Public && field.Accessibility != TargetMemberAccessibility.Public)
							continue;
						if (field.IsStatic || !staticOnly)
							members.Add (new MemberReference (field, type));
					}
				}
				
				if (properties != null) {
					foreach (TargetPropertyInfo prop in properties) {
						if (access == ReqMemberAccess.Public && prop.Accessibility != TargetMemberAccessibility.Public)
							continue;
						if (access == ReqMemberAccess.Auto && prop.Accessibility != TargetMemberAccessibility.Public && prop.Accessibility != TargetMemberAccessibility.Protected)
							continue;
						if (prop.IsStatic || !staticOnly)
							members.Add (new MemberReference (prop, type));
					}
				}
				
				if (methods != null) {
					foreach (TargetMethodInfo met in methods) {
						if (access == ReqMemberAccess.Public && met.Accessibility != TargetMemberAccessibility.Public)
							continue;
						if (access == ReqMemberAccess.Auto && met.Accessibility != TargetMemberAccessibility.Public && met.Accessibility != TargetMemberAccessibility.Protected)
							continue;
						if (met.IsStatic || !staticOnly) {
							string sig = GetSignature (met);
							if (!foundMethods.ContainsKey (sig)) {
								foundMethods [sig] = sig;
								members.Add (new MemberReference (met, type));
							}
						}
					}
				}
				
				if (type.HasParent)
					type = type.GetParentType (ctx.Thread);
				else
					break;
			}
			return members;
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
		
		public static TargetStructObject GetTypeOf (EvaluationContext ctx, TargetType ttype)
		{
			if (ttype.HasClassType && ttype.ClassType.Module != null)
				return GetTypeOf (ctx, FixTypeName (ttype.Name) + ", " + ttype.ClassType.Module.FullName);
			else
				return GetTypeOf (ctx, FixTypeName (ttype.Name));
		}
		
		public static TargetStructObject GetTypeOf (EvaluationContext ctx, string typeName)
		{
			TargetType tt = ctx.Frame.Language.LookupType ("System.Type");
			if (tt == null)
				return null;

			TargetObject tn = ctx.Frame.Language.CreateInstance (ctx.Thread, FixTypeName (typeName));
			TargetObject res = CallStaticMethod (ctx, "GetType", tt, tn);
			return (TargetStructObject) GetRealObject (ctx, res);
		}
		
		public static bool IsInstanceOfType (EvaluationContext ctx, string typeName, TargetObject obj)
		{
			TargetStructObject tt = GetTypeOf (ctx, typeName);
			if (tt == null)
				return false;
			TargetObject to = TargetObjectConvert.Cast (ctx, obj, ctx.Frame.Language.ObjectType);
			TargetFundamentalObject res = CallMethod (ctx, "IsInstanceOfType", tt, to) as TargetFundamentalObject;
			return (bool) res.GetObject (ctx.Thread);
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
		
		public TargetObject GetValue (EvaluationContext ctx, TargetStructObject thisObj)
		{
			if (Member is TargetPropertyInfo) {
				TargetPropertyInfo prop = (TargetPropertyInfo) Member;
				return ObjectUtil.GetRealObject (ctx, Server.Instance.RuntimeInvoke (ctx, prop.Getter, thisObj));
			}
			else if (Member is TargetFieldInfo) {
				TargetFieldInfo field = (TargetFieldInfo) Member;
				if (field.HasConstValue)
					return ctx.Frame.Language.CreateInstance (ctx.Thread, field.ConstValue);
				TargetClass cls = DeclaringType.GetClass (ctx.Thread);
				return ObjectUtil.GetRealObject (ctx, cls.GetField (ctx.Thread, thisObj, field));
			}
			else {
				TargetMethodInfo met = (TargetMethodInfo) Member;
				return ObjectUtil.GetRealObject (ctx, Server.Instance.RuntimeInvoke (ctx, met.Type, thisObj));
			}
		}
	}
	
	public class TypeDisplayData
	{
		public string ProxyType;
		public string ValueDisplayString;
		public string TypeDisplayString;
		public string NameDisplayString;
		public bool IsProxyType;

		public static readonly TypeDisplayData Default = new TypeDisplayData ();
		
		public Dictionary<string,DebuggerBrowsableState> MemberData;
		
		public DebuggerBrowsableState GetMemberBrowsableState (string name)
		{
			if (MemberData == null)
				return DebuggerBrowsableState.Collapsed;
			
			DebuggerBrowsableState state;
			if (MemberData.TryGetValue (name, out state))
				return state;
			else
				return DebuggerBrowsableState.Collapsed;
		}
	}
}
