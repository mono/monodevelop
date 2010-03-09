// 
// SoftDebuggerAdaptor.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Diagnostics;
using Mono.Debugger.Soft;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Client;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using ST = System.Threading;
using Mono.Debugging.Backend;

namespace Mono.Debugging.Soft
{
	public class SoftDebuggerAdaptor : ObjectValueAdaptor
	{
		public SoftDebuggerAdaptor ()
		{
		}
		
		public override string CallToString (EvaluationContext ctx, object obj)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			
			if (obj is StringMirror)
				return ((StringMirror)obj).Value;
			else if (obj is EnumMirror) {
				EnumMirror eob = (EnumMirror) obj;
				return eob.StringValue;
			}
			else if (obj is PrimitiveValue)
				return ((PrimitiveValue)obj).Value.ToString ();
			else if ((obj is StructMirror) && ((StructMirror)obj).Type.IsPrimitive) {
				// Boxed primitive
				StructMirror sm = (StructMirror) obj;
				if (sm.Fields.Length > 0 && (sm.Fields[0] is PrimitiveValue))
					return ((PrimitiveValue)sm.Fields[0]).Value.ToString ();
			}
			else if (obj == null)
				return string.Empty;
			else if ((obj is ObjectMirror) && cx.Options.AllowTargetInvoke) {
				ObjectMirror ob = (ObjectMirror) obj;
				MethodMirror method = OverloadResolve (cx, "ToString", ob.Type, new TypeMirror[0], true, false, false);
				if (method != null && method.DeclaringType.FullName != "System.Object") {
					StringMirror res = (StringMirror) cx.RuntimeInvoke (method, obj, new Value[0]);
					return res.Value;
				}
			}
			else if ((obj is StructMirror) && cx.Options.AllowTargetInvoke) {
				StructMirror ob = (StructMirror) obj;
				MethodMirror method = OverloadResolve (cx, "ToString", ob.Type, new TypeMirror[0], true, false, false);
				if (method != null && method.DeclaringType.FullName != "System.ValueType") {
					StringMirror res = (StringMirror) cx.RuntimeInvoke (method, obj, new Value[0]);
					return res.Value;
				}
			}
			return GetDisplayTypeName (GetValueTypeName (ctx, obj));
		}

		public override object TryConvert (EvaluationContext ctx, object obj, object targetType)
		{
			object res = TryCast (ctx, obj, targetType);
			if (res != null)
				return res;
			if (obj == null)
				return null;
			object otype = GetValueType (ctx, obj);
			if (otype is Type) {
				if (targetType is TypeMirror)
					targetType = Type.GetType (((TypeMirror)targetType).FullName, false);
				Type tt = targetType as Type;
				if (tt != null) {
					try {
						if (obj is PrimitiveValue)
							obj = ((PrimitiveValue)obj).Value;
						res = System.Convert.ChangeType (obj, tt);
						return CreateValue (ctx, res);
					} catch {
					}
				}
			}
			return null;
		}

		public override object TryCast (EvaluationContext ctx, object obj, object targetType)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			if (obj == null)
				return null;
			object valueType = GetValueType (ctx, obj);
			if (valueType is TypeMirror) {
				if ((targetType is TypeMirror) && ((TypeMirror)targetType).IsAssignableFrom ((TypeMirror)valueType))
					return obj;
				// Try casting the primitive type of the enum
				EnumMirror em = obj as EnumMirror;
				if (em != null)
					return TryCast (ctx, CreateValue (ctx, em.Value), targetType);
			} else if (valueType is Type) {
				if (targetType is TypeMirror) {
					TypeMirror tm = (TypeMirror) targetType;
					if (tm.IsEnum) {
						PrimitiveValue casted = TryCast (ctx, obj, tm.EnumUnderlyingType) as PrimitiveValue;
						if (casted == null)
							return null;
						return cx.Session.VirtualMachine.CreateEnumMirror (tm, casted);
					}
					targetType = Type.GetType (((TypeMirror)targetType).FullName, false);
				}
				Type tt = targetType as Type;
				if (tt != null) {
					if (tt.IsAssignableFrom ((Type)valueType))
						return obj;
					if (obj is PrimitiveValue)
						obj = ((PrimitiveValue)obj).Value;
					if (tt != typeof(string) && !(obj is string)) {
						try {
							object res = System.Convert.ChangeType (obj, tt);
							return CreateValue (ctx, res);
						} catch {
						}
					}
				}
			}
			return null;
		}

		public override ICollectionAdaptor CreateArrayAdaptor (EvaluationContext ctx, object arr)
		{
			return new ArrayAdaptor ((ArrayMirror) arr);
		}

		public override object CreateNullValue (EvaluationContext ctx, object type)
		{
			return null;
		}

		public override object CreateTypeObject (EvaluationContext ctx, object type)
		{
			TypeMirror t = (TypeMirror) type;
			return t.GetTypeObject ();
		}

		public override object CreateValue (EvaluationContext ctx, object type, params object[] args)
		{
			ctx.AssertTargetInvokeAllowed ();
			
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			TypeMirror t = (TypeMirror) type;
			
			TypeMirror[] types = new TypeMirror [args.Length];
			for (int n=0; n<args.Length; n++)
				types [n] = ToTypeMirror (ctx, GetValueType (ctx, args [n]));
			
			Value[] values = new Value[args.Length];
			for (int n=0; n<args.Length; n++)
				values[n] = (Value) args [n];
			
			MethodMirror ctor = OverloadResolve (cx, ".ctor", t, types, true, true, true);
			return t.NewInstance (cx.Thread, ctor, values);
		}

		public override object CreateValue (EvaluationContext ctx, object value)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			if (value is string)
				return cx.Thread.Domain.CreateString ((string)value);
			else
				return cx.Session.VirtualMachine.CreateValue (value);
		}

		public override object GetBaseValue (EvaluationContext ctx, object val)
		{
			return val;
		}

		public override object GetEnclosingType (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			return cx.Frame.Method.DeclaringType;
		}

		public override string[] GetImportedNamespaces (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			HashSet<string> namespaces = new HashSet<string> ();
			foreach (TypeMirror type in cx.Session.GetAllTypes ())
				namespaces.Add (type.Namespace);
			
			string[] nss = new string [namespaces.Count];
			namespaces.CopyTo (nss);
			return nss;
		}

		public override ValueReference GetIndexerReference (EvaluationContext ctx, object target, object[] indices)
		{
			TypeMirror targetType = GetValueType (ctx, target) as TypeMirror;
			
			Value[] values = new Value [indices.Length];
			TypeMirror[] types = new TypeMirror [indices.Length];
			for (int n=0; n<indices.Length; n++) {
				types [n] = ToTypeMirror (ctx, GetValueType (ctx, indices [n]));
				values [n] = (Value) indices [n];
			}
			
			List<MethodMirror> candidates = new List<MethodMirror> ();
			List<PropertyInfoMirror> props = new List<PropertyInfoMirror> ();
			
			TypeMirror type = targetType;
			while (type != null) {
				foreach (PropertyInfoMirror prop in type.GetProperties ()) {
					MethodMirror met = prop.GetGetMethod (true);
					if (met != null && !met.IsStatic && met.GetParameters ().Length > 0) {
						candidates.Add (met);
						props.Add (prop);
					}
				}
				type = type.BaseType;
			}
			MethodMirror idx = OverloadResolve ((SoftEvaluationContext) ctx, targetType.Name, null, types, candidates, true);
			int i = candidates.IndexOf (idx);
			return new PropertyValueReference (ctx, props[i], target, null, values);
		}

		public override ValueReference GetLocalVariable (EvaluationContext ctx, string name)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			LocalVariable local = cx.Frame.Method.GetLocal (name);
			if (local != null)
				return new VariableValueReference (ctx, name, local);
			else
				return null;
		}

		public override IEnumerable<ValueReference> GetLocalVariables (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			LocalVariable[] locals = cx.Frame.Method.GetLocals ();
			foreach (LocalVariable var in locals) {
				if (!var.IsArg)
					yield return new VariableValueReference (ctx, var.Name, var);
			}
		}

		protected override ValueReference GetMember (EvaluationContext ctx, object t, object co, string name)
		{
			TypeMirror type = (TypeMirror) t;
			while (type != null) {
				FieldInfoMirror field = type.GetField (name);
				if (field != null)
					return new FieldValueReference (ctx, field, co, type);
				PropertyInfoMirror prop = type.GetProperty (name);
				if (prop != null)
					return new PropertyValueReference (ctx, prop, co, type, null);
				type = type.BaseType;
			}
			return null;
		}

		protected override IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, object t, object co, BindingFlags bindingFlags)
		{
			TypeMirror type = t as TypeMirror;
			while (type != null) {
				foreach (FieldInfoMirror field in type.GetFields ()) {
					if (field.IsStatic && ((bindingFlags & BindingFlags.Static) == 0))
						continue;
					if (!field.IsStatic && ((bindingFlags & BindingFlags.Instance) == 0))
						continue;
					if (field.IsPublic && ((bindingFlags & BindingFlags.Public) == 0))
						continue;
					if (!field.IsPublic && ((bindingFlags & BindingFlags.NonPublic) == 0))
						continue;
					yield return new FieldValueReference (ctx, field, co, type);
				}
				foreach (PropertyInfoMirror prop in type.GetProperties (bindingFlags)) {
					MethodMirror met = prop.GetGetMethod (true);
					if (met == null || met.GetParameters ().Length != 0 || met.IsAbstract)
						continue;
					if (met.IsStatic && ((bindingFlags & BindingFlags.Static) == 0))
						continue;
					if (!met.IsStatic && ((bindingFlags & BindingFlags.Instance) == 0))
						continue;
					if (met.IsPublic && ((bindingFlags & BindingFlags.Public) == 0))
						continue;
					if (!met.IsPublic && ((bindingFlags & BindingFlags.NonPublic) == 0))
						continue;
					yield return new PropertyValueReference (ctx, prop, co, type, null);
				}
				if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
					break;
				type = type.BaseType;
			}
		}

		public override void GetNamespaceContents (EvaluationContext ctx, string namspace, out string[] childNamespaces, out string[] childTypes)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			HashSet<string> types = new HashSet<string> ();
			HashSet<string> namespaces = new HashSet<string> ();
			string namspacePrefix = namspace.Length > 0 ? namspace + "." : "";
			foreach (TypeMirror type in cx.Session.GetAllTypes ()) {
				if (type.Namespace == namspace || type.Namespace.StartsWith (namspacePrefix)) {
					namespaces.Add (type.Namespace);
					types.Add (type.FullName);
				}
			}
			childNamespaces = new string [namespaces.Count];
			namespaces.CopyTo (childNamespaces);
			
			childTypes = new string [types.Count];
			types.CopyTo (childTypes);
		}

		public override IEnumerable<ValueReference> GetParameters (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			LocalVariable[] locals = cx.Frame.Method.GetLocals ();
			foreach (LocalVariable var in locals) {
				if (var.IsArg)
					yield return new VariableValueReference (ctx, var.Name, var);
			}
		}

		public override ValueReference GetThisReference (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			if (cx.Frame.Method.IsStatic)
				return null;
			Value val = cx.Frame.GetThis ();
			return LiteralValueReference.CreateTargetObjectLiteral (ctx, "this", val);
		}
		
		public override ValueReference GetCurrentException (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			ObjectMirror exc = cx.Session.GetExceptionObject (cx.Thread);
			if (exc != null)
				return LiteralValueReference.CreateTargetObjectLiteral (ctx, ctx.Options.CurrentExceptionTag, exc);
			else
				return null;
		}


		public override object[] GetTypeArgs (EvaluationContext ctx, object type)
		{
			string s = ((TypeMirror)type).FullName;
			int i = s.IndexOf ('`');
			List<string> names = new List<string> ();
			if (i != -1) {
				i = s.IndexOf ('[', i);
				if (i == -1)
					return new object [0];
				int si = ++i;
				int nt = 0;
				for (; i < s.Length && (nt > 0 || s[i] != ']'); i++) {
					if (s[i] == '[')
						nt++;
					else if (s[i] == ']')
						nt--;
					else if (s[i] == ',' && nt == 0) {
						names.Add (s.Substring (si, i - si));
						si = i + 1;
					}
				}
				names.Add (s.Substring (si, i - si));
				object[] types = new object [names.Count];
				for (int n=0; n<names.Count; n++) {
					string tn = names [n];
					if (tn.StartsWith ("["))
						tn = tn.Substring (1, tn.Length - 2);
					types [n] = GetType (ctx, tn);
					if (types [n] == null)
						return new object [0];
				}
				return types;
			}
			return new object [0];
		}

		public override object GetType (EvaluationContext ctx, string name, object[] typeArgs)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			int i = name.IndexOf (',');
			if (i != -1) {
				// Find first comma outside brackets
				int nest = 0;
				for (int n=0; n<name.Length; n++) {
					char c = name [n];
					if (c == '[')
						nest++;
					else if (c == ']')
						nest--;
					else if (c == ',' && nest == 0) {
						name = name.Substring (0, n).Trim ();
						break;
					}
				}
			}
			
			if (typeArgs != null && typeArgs.Length > 0){
				string args = "";
				foreach (object t in typeArgs) {
					if (args.Length > 0)
						args += ",";
					string tn;
					if (t is TypeMirror) {
						TypeMirror atm = (TypeMirror) t;
						tn = atm.FullName + "," + atm.Assembly.GetName ();
					} else {
						Type atm = (Type) t;
						tn = atm.FullName + "," + atm.Assembly.GetName ();
					}
					if (tn.IndexOf (',') != -1)
						tn = "[" + tn + "]";
					args += tn;
				}
				name += "[" +args + "]";
			}
			
			TypeMirror tm = cx.Session.GetType (name);
			if (tm != null)
				return tm;
			foreach (AssemblyMirror asm in cx.Thread.Domain.GetAssemblies ()) {
				tm = asm.GetType (name, false, false);
				if (tm != null)
					return tm;
			}
			return null;
		}

		public override IEnumerable<object> GetNestedTypes (EvaluationContext ctx, object type)
		{
			TypeMirror t = (TypeMirror) type;
			foreach (TypeMirror nt in t.GetNestedTypes ())
				yield return nt;
		}
		
		public override string GetTypeName (EvaluationContext ctx, object val)
		{
			TypeMirror tm = val as TypeMirror;
			if (tm != null)
				return tm.FullName;
			else
				return ((Type)val).FullName;
		}

		public override object GetValueType (EvaluationContext ctx, object val)
		{
			if (val is ObjectMirror)
				return ((ObjectMirror)val).Type;
			if (val is StructMirror)
				return ((StructMirror)val).Type;
			if (val is PrimitiveValue) {
				PrimitiveValue pv = (PrimitiveValue) val;
				if (pv.Value == null)
					return typeof(Object);
				else
					return ((PrimitiveValue)val).Value.GetType ();
			}
			if (val is ArrayMirror)
				return ((ArrayMirror)val).Type;
			if (val is EnumMirror)
				return ((EnumMirror)val).Type;
			throw new NotSupportedException ();
		}
		
		public override object GetBaseType (EvaluationContext ctx, object type)
		{
			if (type is TypeMirror)
				return ((TypeMirror)type).BaseType;
			else
				return null;
		}

		public override bool HasMethod (EvaluationContext gctx, object targetType, string methodName, object[] argTypes, BindingFlags flags)
		{
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			
			TypeMirror[] types;
			if (argTypes == null)
				types = new TypeMirror [0];
			else {
				types = new TypeMirror [argTypes.Length];
				for (int n=0; n<argTypes.Length; n++) {
					if (argTypes [n] is TypeMirror)
						types [n] = (TypeMirror) argTypes [n];
					else
						types [n] = (TypeMirror) GetType (ctx, ((Type)argTypes[n]).FullName);
				}
			}
			
			MethodMirror met = OverloadResolve (ctx, methodName, (TypeMirror) targetType, types, (flags & BindingFlags.Instance) != 0, (flags & BindingFlags.Static) != 0, false);
			return met != null;
		}
		
		public override bool IsExternalType (EvaluationContext gctx, object type)
		{
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			TypeMirror tm = type as TypeMirror;
			if (tm != null)
				return ctx.Session.IsExternalCode (tm);
			else
				return true;
		}


		public override bool IsArray (EvaluationContext ctx, object val)
		{
			return val is ArrayMirror;
		}

		public override bool IsClass (object type)
		{
			TypeMirror t = type as TypeMirror;
			return t != null && (t.IsClass || t.IsValueType) && !t.IsPrimitive;
		}

		public override bool IsNull (EvaluationContext ctx, object val)
		{
			return val == null;
		}

		public override bool IsPrimitive (EvaluationContext ctx, object val)
		{
			return val is PrimitiveValue || val is StringMirror || ((val is StructMirror) && ((StructMirror)val).Type.IsPrimitive);
		}

		public override bool IsEnum (EvaluationContext ctx, object val)
		{
			return val is EnumMirror;
		}
		
		protected override TypeDisplayData OnGetTypeDisplayData (EvaluationContext gctx, object type)
		{
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			TypeDisplayData td = new TypeDisplayData ();
			TypeMirror t = (TypeMirror) type;
			foreach (CustomAttributeDataMirror attr in t.GetCustomAttributes (true)) {
				string attName = attr.Constructor.DeclaringType.FullName;
				if (attName == "System.Diagnostics.DebuggerDisplayAttribute") {
					DebuggerDisplayAttribute at = BuildAttribute<DebuggerDisplayAttribute> (attr);
					td.NameDisplayString = at.Name;
					td.TypeDisplayString = at.Type;
					td.ValueDisplayString = at.Value;
				}
				else if (attName == "System.Diagnostics.DebuggerTypeProxyAttribute") {
					DebuggerTypeProxyAttribute at = BuildAttribute<DebuggerTypeProxyAttribute> (attr);
					td.ProxyType = at.ProxyTypeName;
					if (!string.IsNullOrEmpty (td.ProxyType))
						ForceLoadType (ctx, td.ProxyType);
				}
			}
			foreach (FieldInfoMirror fi in t.GetFields ()) {
				CustomAttributeDataMirror[] attrs = fi.GetCustomAttributes (true);
				DebuggerBrowsableAttribute att = GetAttribute <DebuggerBrowsableAttribute> (attrs);
				if (att == null) {
					var cga = GetAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute> (attrs);
					if (cga != null)
						att = new DebuggerBrowsableAttribute (DebuggerBrowsableState.Never);
				}
				if (att != null) {
					if (td.MemberData == null)
						td.MemberData = new Dictionary<string, DebuggerBrowsableState> ();
					td.MemberData [fi.Name] = att.State;
				}
			}
			foreach (PropertyInfoMirror pi in t.GetProperties ()) {
				DebuggerBrowsableAttribute att = GetAttribute <DebuggerBrowsableAttribute> (pi.GetCustomAttributes (true));
				if (att != null) {
					if (td.MemberData == null)
						td.MemberData = new Dictionary<string, DebuggerBrowsableState> ();
					td.MemberData [pi.Name] = att.State;
				}
			}
			return td;
		}
					    
		T GetAttribute<T> (CustomAttributeDataMirror[] attrs)
		{
			foreach (CustomAttributeDataMirror attr in attrs) {
				if (attr.Constructor.DeclaringType.FullName == typeof(T).FullName)
					return BuildAttribute<T> (attr);
			}
			return default(T);
		}
		
		public override object ForceLoadType (EvaluationContext gctx, string typeName)
		{
			// Shortcut to avoid a target invoke in case the type is already loaded
			object t = GetType (gctx, typeName);
			if (t != null)
				return t;
			
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			if (!ctx.Options.AllowTargetInvoke)
				return null;
			TypeMirror tm = (TypeMirror) ctx.Thread.Type.GetTypeObject ().Type;
			TypeMirror stype = ctx.Session.GetType ("System.String");
			if (stype == null) {
				// If the string type is not loaded, we need to get it in another way
				StringMirror ss = ctx.Thread.Domain.CreateString ("");
				stype = ss.Type;
			}
			TypeMirror[] ats = new TypeMirror[] { stype };
			MethodMirror met = OverloadResolve (ctx, "GetType", tm, ats, false, true, true);
			try {
				tm.InvokeMethod (ctx.Thread, met, new Value[] {(Value) CreateValue (ctx, typeName)});
			} catch {
				return null;
			} finally {
				ctx.Session.StackVersion++;
			}
			return GetType (ctx, typeName);
		}

		
		T BuildAttribute<T> (CustomAttributeDataMirror attr)
		{
			List<object> args = new List<object> ();
			foreach (CustomAttributeTypedArgumentMirror arg in attr.ConstructorArguments) {
				object val = arg.Value;
				if (val is TypeMirror) {
					// The debugger attributes that take a type as parameter of the constructor have
					// a corresponding constructor overload that takes a type name. We'll use that
					// constructor because we can't load target types in the debugger process.
					// So what we do here is convert the Type to a String.
					TypeMirror tm = (TypeMirror) val;
					val = tm.FullName + ", " + tm.Assembly.ManifestModule.Name;
				} else if (val is EnumMirror) {
					EnumMirror em = (EnumMirror) val;
					val = em.Value;
				}
				args.Add (val);
			}
			Type type = typeof(T);
			object at = Activator.CreateInstance (type, args.ToArray ());
			foreach (CustomAttributeNamedArgumentMirror arg in attr.NamedArguments) {
				object val = arg.TypedValue.Value;
				string postFix = "";
				if (arg.TypedValue.ArgumentType == typeof(Type))
					postFix = "TypeName";
				if (arg.Field != null)
					type.GetField (arg.Field.Name + postFix).SetValue (at, val);
				else if (arg.Property != null)
					type.GetProperty (arg.Property.Name + postFix).SetValue (at, val, null);
			}
			return (T) at;
		}
		
		TypeMirror ToTypeMirror (EvaluationContext ctx, object type)
		{
			TypeMirror t = type as TypeMirror;
			if (t != null)
				return t;
			return (TypeMirror) GetType (ctx, ((Type)type).FullName);
		}

		public override object RuntimeInvoke (EvaluationContext gctx, object targetType, object target, string methodName, object[] argTypes, object[] argValues)
		{
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			ctx.AssertTargetInvokeAllowed ();
			
			TypeMirror type = target != null ? ((ObjectMirror) target).Type : (TypeMirror) targetType;
			
			TypeMirror[] types = new TypeMirror [argTypes.Length];
			for (int n=0; n<argTypes.Length; n++)
				types [n] = ToTypeMirror (ctx, argTypes [n]);
			
			Value[] values = new Value[argValues.Length];
			for (int n=0; n<argValues.Length; n++)
				values[n] = (Value) argValues [n];

			MethodMirror method = OverloadResolve (ctx, methodName, type, types, target != null, target == null, true);
			return ctx.RuntimeInvoke (method, target ?? targetType, values);
		}
		
		public static MethodMirror OverloadResolve (SoftEvaluationContext ctx, string methodName, TypeMirror type, TypeMirror[] argtypes, bool allowInstance, bool allowStatic, bool throwIfNotFound)
		{
			List<MethodMirror> candidates = new List<MethodMirror> ();
			TypeMirror currentType = type;
			while (currentType != null) {
				foreach (MethodMirror met in currentType.GetMethods ()) {
					if (met.Name == methodName) {
						ParameterInfoMirror[] pars = met.GetParameters ();
						if (pars.Length == argtypes.Length && (met.IsStatic && allowStatic || !met.IsStatic && allowInstance))
							candidates.Add (met);
					}
				}
				if (methodName == ".ctor")
					break; // Can't create objects using constructor from base classes
				currentType = currentType.BaseType;
			}
			

			return OverloadResolve (ctx, type.Name, methodName, argtypes, candidates, throwIfNotFound);
		}

		static bool IsApplicable (SoftEvaluationContext ctx, MethodMirror method, TypeMirror[] types, out string error, out int matchCount)
		{
			ParameterInfoMirror[] mparams = method.GetParameters ();
			matchCount = 0;

			for (int i = 0; i < types.Length; i++) {
				
				TypeMirror param_type = mparams[i].ParameterType;

				if (param_type.FullName == types [i].FullName) {
					matchCount++;
					continue;
				}

				if (param_type.IsAssignableFrom (types [i]))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, types [i].FullName, param_type.FullName);
				return false;
			}

			error = null;
			return true;
		}

		static MethodMirror OverloadResolve (SoftEvaluationContext ctx, string typeName, string methodName, TypeMirror[] argtypes, List<MethodMirror> candidates, bool throwIfNotFound)
		{
			if (candidates.Count == 1) {
				string error;
				int matchCount;
				if (IsApplicable (ctx, candidates[0], argtypes, out error, out matchCount))
					return candidates [0];

				if (throwIfNotFound)
					throw new EvaluatorException ("Invalid arguments for method `{0}': {1}", methodName, error);
				else
					return null;
			}

			if (candidates.Count == 0) {
				if (throwIfNotFound)
					throw new EvaluatorException ("Method `{0}' not found in type `{1}'.", methodName, typeName);
				else
					return null;
			}
			
			// Ok, now we need to find an exact match.
			MethodMirror match = null;
			int bestCount = -1;
			bool repeatedBestCount = false;
			
			foreach (MethodMirror method in candidates) {
				string error;
				int matchCount;
				
				if (!IsApplicable (ctx, method, argtypes, out error, out matchCount))
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
				if (!throwIfNotFound)
					return null;
				if (methodName != null)
					throw new EvaluatorException ("Invalid arguments for method `{0}'.", methodName);
				else
					throw new EvaluatorException ("Invalid arguments for indexer.");
			}
			
			if (repeatedBestCount) {
				// If there is an ambiguous match, just pick the first match. If the user was expecting
				// something else, he can provide more specific arguments
				
/*				if (!throwIfNotFound)
					return null;
				if (methodName != null)
					throw new EvaluatorException ("Ambiguous method `{0}'; need to use full name", methodName);
				else
					throw new EvaluatorException ("Ambiguous arguments for indexer.", methodName);
*/			}
			 
			return match;
		}		

		public override object TargetObjectToObject (EvaluationContext gctx, object obj)
		{
			if (obj is StringMirror)
				return ((StringMirror)obj).Value;
			else if (obj is PrimitiveValue)
				return ((PrimitiveValue)obj).Value;
			else if ((obj is StructMirror) && ((StructMirror)obj).Type.IsPrimitive) {
				// Boxed primitive
				StructMirror sm = (StructMirror) obj;
				if (sm.Type.FullName == "System.IntPtr")
					return new IntPtr ((long)((PrimitiveValue)sm.Fields[0]).Value);
				if (sm.Fields.Length > 0 && (sm.Fields[0] is PrimitiveValue))
					return ((PrimitiveValue)sm.Fields[0]).Value;
			}
			return base.TargetObjectToObject (gctx, obj);
		}
	}

	class MethodCall: AsyncOperation
	{
		SoftEvaluationContext ctx;
		MethodMirror function;
		object obj;
		Value[] args;
		Value result;
		IAsyncResult handle;
		Exception exception;
		ST.ManualResetEvent shutdownEvent = new ST.ManualResetEvent (false);
		const InvokeOptions options = InvokeOptions.DisableBreakpoints | InvokeOptions.SingleThreaded;
		
		public MethodCall (SoftEvaluationContext ctx, MethodMirror function, object obj, Value[] args)
		{
			this.ctx = ctx;
			this.function = function;
			this.obj = obj;
			this.args = args;
		}
		
		public override string Description {
			get {
				return function.DeclaringType.FullName + "." + function.Name;
			}
		}

		public override void Invoke ()
		{
			try {
				if (obj is ObjectMirror)
					handle = ((ObjectMirror)obj).BeginInvokeMethod (ctx.Thread, function, args, options, null, null);
				else if (obj is TypeMirror)
					handle = ((TypeMirror)obj).BeginInvokeMethod (ctx.Thread, function, args, options, null, null);
				else if (obj is StructMirror)
					handle = ((StructMirror)obj).BeginInvokeMethod (ctx.Thread, function, args, options, null, null);
				else
					throw new ArgumentException (obj.GetType ().ToString ());
			} catch (InvocationException ex) {
				ctx.Session.StackVersion++;
				exception = ex;
			} catch (Exception ex) {
				ctx.Session.StackVersion++;
				LoggingService.LogError ("Error in soft debugger method call thread on " + GetInfo (), ex);
				exception = ex;
			}
		}

		public override void Abort ()
		{
			if (handle is IInvokeAsyncResult) {
				var info = GetInfo ();
				LoggingService.LogMessage ("Aborting invocation of " + info);
				((IInvokeAsyncResult) handle).Abort ();
				WaitForCompleted (-1);
				LoggingService.LogMessage ("Aborted invocation of " + info);
			} else {
				throw new NotSupportedException ();
			}
		}
		
		public override void Shutdown ()
		{
			shutdownEvent.Set ();
		}
		
		void EndInvoke ()
		{
			try {
				if (obj is ObjectMirror)
					result = ((ObjectMirror)obj).EndInvokeMethod (handle);
				else if (obj is TypeMirror)
					result = ((TypeMirror)obj).EndInvokeMethod (handle);
				else
					result = ((StructMirror)obj).EndInvokeMethod (handle);
			} catch (InvocationException ex) {
				if (ex.Exception != null) {
					string ename = ctx.Adapter.GetValueTypeName (ctx, ex.Exception);
					ValueReference vref = ctx.Adapter.GetMember (ctx, null, ex.Exception, "Message");
					if (vref != null) {
						exception = new Exception (ename + ": " + (string)vref.ObjectValue);
						return;
					} else {
						exception = new Exception (ename);
						return;
					}
				}
				exception = ex;
			} catch (Exception ex) {
				LoggingService.LogError ("Error in soft debugger method call thread on " + GetInfo (), ex);
				exception = ex;
			} finally {
				ctx.Session.StackVersion++;
			}
		}
		
		string GetInfo ()
		{
			try {
				TypeMirror type = null;
				if (obj is ObjectMirror)
					type = ((ObjectMirror)obj).Type;
				else if (obj is TypeMirror)
					type = (TypeMirror)obj;
				else if (obj is StructMirror)
					type = ((StructMirror)obj).Type;
				return string.Format ("method {0} on object {1}",
				                      function == null? "[null]" : function.FullName,
				                      type == null? "[null]" : type.FullName);
			} catch (Exception ex) {
				LoggingService.LogError ("Error getting info for SDB MethodCall", ex);
				return "";
			}
		}

		public override bool WaitForCompleted (int timeout)
		{
			if (handle == null)
				return true;
			int res = ST.WaitHandle.WaitAny (new ST.WaitHandle[] { handle.AsyncWaitHandle, shutdownEvent }, timeout); 
			if (res == 0) {
				EndInvoke ();
				return true;
			}
			// Return true if shut down.
			return res == 1;
		}

		public Value ReturnValue {
			get {
				if (exception != null)
					throw new EvaluatorException (exception.Message);
				return result;
			}
		}
	}
}
