// 
// SoftDebuggerAdaptor.cs
//  
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2011,2012 Xamain Inc. (http://www.xamarin.com)
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
using System.Linq;
using System.Diagnostics;
using Mono.Debugger.Soft;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Client;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using ST = System.Threading;
using Mono.Debugging.Backend;

namespace Mono.Debugging.Soft
{
	public class SoftDebuggerAdaptor : ObjectValueAdaptor
	{
		static Dictionary<Type, OpCode> convertOps = new Dictionary<Type, OpCode> ();
		delegate object TypeCastDelegate (object value);

		static SoftDebuggerAdaptor ()
		{
			convertOps.Add (typeof (double), OpCodes.Conv_R8);
			convertOps.Add (typeof (float), OpCodes.Conv_R4);
			convertOps.Add (typeof (ulong), OpCodes.Conv_U8);
			convertOps.Add (typeof (uint), OpCodes.Conv_U4);
			convertOps.Add (typeof (ushort), OpCodes.Conv_U2);
			convertOps.Add (typeof (char), OpCodes.Conv_U2);
			convertOps.Add (typeof (byte), OpCodes.Conv_U1);
			convertOps.Add (typeof (long), OpCodes.Conv_I8);
			convertOps.Add (typeof (int), OpCodes.Conv_I4);
			convertOps.Add (typeof (short), OpCodes.Conv_I2);
			convertOps.Add (typeof (sbyte), OpCodes.Conv_I1);
		}

		public SoftDebuggerAdaptor ()
		{
		}
		
		public override string CallToString (EvaluationContext ctx, object obj)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			
			if (obj == null)
				return null;
			
			if (obj is StringMirror)
				return ((StringMirror)obj).Value;
			else if (obj is EnumMirror) {
				EnumMirror eob = (EnumMirror) obj;
				return eob.StringValue;
			}
			else if (obj is PrimitiveValue)
				return ((PrimitiveValue)obj).Value.ToString ();
			else if (obj is PointerValue)
				return string.Format ("0x{0:x}", ((PointerValue)obj).Address);
			else if ((obj is StructMirror) && ((StructMirror)obj).Type.IsPrimitive) {
				// Boxed primitive
				StructMirror sm = (StructMirror) obj;
				if (sm.Fields.Length > 0 && (sm.Fields[0] is PrimitiveValue))
					return ((PrimitiveValue)sm.Fields[0]).Value.ToString ();
			}
			else if ((obj is ObjectMirror) && cx.Options.AllowTargetInvoke) {
				ObjectMirror ob = (ObjectMirror) obj;
				MethodMirror method = OverloadResolve (cx, "ToString", ob.Type, new TypeMirror[0], true, false, false);
				if (method != null && method.DeclaringType.FullName != "System.Object") {
					StringMirror res = cx.RuntimeInvoke (method, obj, new Value[0]) as StringMirror;
					return res != null ? res.Value : null;
				}
			}
			else if ((obj is StructMirror) && cx.Options.AllowTargetInvoke) {
				StructMirror ob = (StructMirror) obj;
				MethodMirror method = OverloadResolve (cx, "ToString", ob.Type, new TypeMirror[0], true, false, false);
				if (method != null && method.DeclaringType.FullName != "System.ValueType") {
					StringMirror res = cx.RuntimeInvoke (method, obj, new Value[0]) as StringMirror;
					return res != null ? res.Value : null;
				}
			}
			
			return GetDisplayTypeName (GetValueTypeName (ctx, obj));
		}

		public override object TryConvert (EvaluationContext ctx, object obj, object targetType)
		{
			object res = TryCast (ctx, obj, targetType);
			
			if (res != null || obj == null)
				return res;
			
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

		static TypeCastDelegate GenerateTypeCastDelegate (string methodName, Type fromType, Type toType)
		{
			var argTypes = new Type[] {
				typeof (object)
			};
			var method = new DynamicMethod (methodName, typeof (object), argTypes, true);
			ILGenerator il = method.GetILGenerator ();
			ConstructorInfo ctorInfo;
			MethodInfo methodInfo;
			OpCode conv;

			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Unbox_Any, fromType);

			if (fromType.IsSubclassOf (typeof (System.Nullable))) {
				PropertyInfo propInfo = fromType.GetProperty ("Value");
				methodInfo = propInfo.GetGetMethod ();

				il.Emit (OpCodes.Stloc_0);
				il.Emit (OpCodes.Ldloca_S);
				il.Emit (OpCodes.Call, methodInfo);

				fromType = methodInfo.ReturnType;
			}

			if (!convertOps.TryGetValue (toType, out conv)) {
				argTypes = new Type[] {
					fromType
				};

				if (toType == typeof (string)) {
					methodInfo = fromType.GetMethod ("ToString", new Type[0]);
					il.Emit (OpCodes.Call, methodInfo);
				} else if ((methodInfo = toType.GetMethod ("op_Explicit", argTypes)) != null) {
					il.Emit (OpCodes.Call, methodInfo);
				} else if ((methodInfo = toType.GetMethod ("op_Implicit", argTypes)) != null) {
					il.Emit (OpCodes.Call, methodInfo);
				} else if ((ctorInfo = toType.GetConstructor (argTypes)) != null) {
					il.Emit (OpCodes.Call, ctorInfo);
				} else {
					// No idea what else to try...
					throw new InvalidCastException ();
				}
			} else {
				il.Emit (conv);
			}

			il.Emit (OpCodes.Box, toType);
			il.Emit (OpCodes.Ret);

			return (TypeCastDelegate) method.CreateDelegate (typeof (TypeCastDelegate));
		}

		static object DynamicCast (object value, Type target)
		{
			string methodName = string.Format ("CastFrom{0}To{1}", value.GetType ().Name, target.Name);
			TypeCastDelegate method = GenerateTypeCastDelegate (methodName, value.GetType (), target);

			return method.Invoke (value);
		}

		object TryForceCast (EvaluationContext ctx, Value value, TypeMirror fromType, TypeMirror toType)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			MethodMirror method;

			method = OverloadResolve (cx, "op_Explicit", toType, new TypeMirror[] { fromType }, false, true, false);
			if (method != null)
				return cx.RuntimeInvoke (method, toType, new Value[] { value });

			method = OverloadResolve (cx, "op_Implicit", toType, new TypeMirror[] { fromType }, false, true, false);
			if (method != null)
				return cx.RuntimeInvoke (method, toType, new Value[] { value });

			// Finally, try a ctor...
			try {
				return CreateValue (ctx, toType, value);
			} catch {
				return null;
			}
		}

		public override object TryCast (EvaluationContext ctx, object obj, object targetType)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			TypeMirror toType = targetType as TypeMirror;
			TypeMirror fromType;
			
			if (obj == null)
				return null;
			
			object valueType = GetValueType (ctx, obj);
			if (valueType is TypeMirror) {
				fromType = (TypeMirror) valueType;

				if (toType != null && toType.IsAssignableFrom (fromType))
					return obj;
				
				// Try casting the primitive type of the enum
				EnumMirror em = obj as EnumMirror;
				if (em != null)
					return TryCast (ctx, CreateValue (ctx, em.Value), targetType);

				if (toType == null)
					return null;

				MethodMirror method;

				if (toType.CSharpName == "string") {
					method = OverloadResolve (cx, "ToString", fromType, new TypeMirror[0], true, false, false);
					if (method != null)
						return cx.RuntimeInvoke (method, obj, new Value[0]);
				}

				if (fromType.IsGenericType && fromType.FullName.StartsWith ("System.Nullable`1", StringComparison.InvariantCulture)) {
					method = OverloadResolve (cx, "get_Value", fromType, new TypeMirror[0], true, false, false);
					if (method != null) {
						obj = cx.RuntimeInvoke (method, obj, new Value[0]);
						return TryCast (ctx, obj, targetType);
					}
				}

				return TryForceCast (ctx, (Value) obj, fromType, toType);
			} else if (valueType is Type) {
				if (toType != null) {
					if (toType.IsEnum) {
						PrimitiveValue casted = TryCast (ctx, obj, toType.EnumUnderlyingType) as PrimitiveValue;
						if (casted == null)
							return null;
						return cx.Session.VirtualMachine.CreateEnumMirror (toType, casted);
					}

					targetType = Type.GetType (toType.FullName, false);
				}
				
				Type tt = targetType as Type;
				if (tt != null) {
					if (tt.IsAssignableFrom ((Type) valueType))
						return obj;

					try {
						if (tt.IsPrimitive || tt == typeof (string)) {
							if (obj is PrimitiveValue)
								obj = ((PrimitiveValue) obj).Value;

							if (obj == null)
								return null;

							object res;

							try {
								res = System.Convert.ChangeType (obj, tt);
							} catch {
								res = DynamicCast (obj, tt);
							}

							return CreateValue (ctx, res);
						} else {
							fromType = (TypeMirror) ForceLoadType (ctx, ((Type) valueType).FullName);
							if (toType == null)
								toType = (TypeMirror) ForceLoadType (ctx, tt.FullName);

							return TryForceCast (ctx, (Value) obj, fromType, toType);
						}
					} catch {
					}
				}
			}

			return null;
		}
		
		public override IStringAdaptor CreateStringAdaptor (EvaluationContext ctx, object str)
		{
			return new StringAdaptor ((StringMirror) str);
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
			Value[] values = new Value[args.Length];
			for (int n = 0; n < args.Length; n++) {
				types[n] = ToTypeMirror (ctx, GetValueType (ctx, args[n]));
				values[n] = (Value) args[n];
			}
			
			MethodMirror ctor = OverloadResolve (cx, ".ctor", t, types, true, true, true);
			if (ctor == null)
				return null;

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

		public override bool NullableHasValue (EvaluationContext ctx, object type, object obj)
		{
			ValueReference hasValue = GetMember (ctx, type, obj, "has_value");

			return (bool) hasValue.ObjectValue;
		}

		public override ValueReference NullableGetValue (EvaluationContext ctx, object type, object obj)
		{
			return GetMember (ctx, type, obj, "value");
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
			object valueType = GetValueType (ctx, target);
			TypeMirror targetType = null;

			if (valueType is Type)
				targetType = (TypeMirror) ForceLoadType (ctx, ((Type) valueType).FullName);
			else if (valueType is TypeMirror)
				targetType = (TypeMirror) valueType;
			else
				return null;
			
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
			
			MethodMirror getter = props[i].GetGetMethod (true);
			if (getter == null)
				return null;
			
			return new PropertyValueReference (ctx, props[i], target, null, getter, values);
		}
		
		static bool InGeneratedClosureOrIteratorType (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			if (cx.Frame.Method.IsStatic)
				return false;
			TypeMirror tm = cx.Frame.Method.DeclaringType;
			return IsGeneratedType (tm);
		}
		
		internal static bool IsGeneratedType (TypeMirror tm)
		{
			//
			// This should cover all C# generated special containers
			// - anonymous methods
			// - lambdas
			// - iterators
			// - async methods
			//
			// which allow stepping into
			//
			return tm.Name[0] == '<' &&
				// mcs is of the form <${NAME}>.c__{KIND}${NUMBER}
				(tm.Name.IndexOf (">c__") > 0 ||
				// csc is of form <${NAME}>d__${NUMBER}
				 tm.Name.IndexOf (">d__") > 0);
		}

		internal static string GetNameFromGeneratedType (TypeMirror tm)
		{
			return tm.Name.Substring (1, tm.Name.IndexOf ('>') - 1);
		}
		
		static bool IsHoistedThisReference (FieldInfoMirror field)
		{
			// mcs is "<>f__this" or "$this" (if in an async compiler generated type)
			// csc is "<>4__this"
			return (field.Name.StartsWith ("<>") && field.Name.EndsWith ("__this")) || field.Name == "$this";
		}
		
		static bool IsClosureReferenceField (FieldInfoMirror field)
		{
			// mcs is "<>f__ref"
			// csc is "CS$<>"
			return field.Name.StartsWith ("CS$<>") || field.Name.StartsWith ("<>f__ref");
		}
		
		static bool IsClosureReferenceLocal (LocalVariable local)
		{
			if (local.Name == null)
				return false;
			
			return
				// mcs
				local.Name.Length == 0 || local.Name[0] == '<' || local.Name.StartsWith ("$locvar")
				// csc
				|| local.Name.StartsWith ("CS$<>");
		}
		
		static bool IsGeneratedTemporaryLocal (LocalVariable local)
		{
			return local.Name != null && local.Name.StartsWith ("CS$");
		}
		
		static string GetHoistedIteratorLocalName (FieldInfoMirror field)
		{
			//mcs captured args, of form <$>name
			if (field.Name.StartsWith ("<$>")) {
				return field.Name.Substring (3);
			}
			
			// csc, mcs locals of form <name>__0
			if (field.Name.StartsWith ("<")) {
				int i = field.Name.IndexOf ('>');
				if (i > 1) {
					return field.Name.Substring (1, i - 1);
				}
			}
			return null;
		}

		IEnumerable<ValueReference> GetHoistedLocalVariables (SoftEvaluationContext cx, ValueReference vthis)
		{
			if (vthis == null)
				return new ValueReference [0];
			
			object val = vthis.Value;
			if (IsNull (cx, val))
				return new ValueReference [0];
			
			TypeMirror tm = (TypeMirror) vthis.Type;
			bool isIterator = IsGeneratedType (tm);
			
			var list = new List<ValueReference> ();
			TypeMirror type = (TypeMirror) vthis.Type;
			foreach (FieldInfoMirror field in type.GetFields ()) {
				if (IsHoistedThisReference (field))
					continue;
				if (IsClosureReferenceField (field)) {
					list.AddRange (GetHoistedLocalVariables (cx, new FieldValueReference (cx, field, val, type)));
					continue;
				}
				if (field.Name.StartsWith ("<")) {
					if (isIterator) {
						var name = GetHoistedIteratorLocalName (field);
						if (!string.IsNullOrEmpty (name)) {
							list.Add (new FieldValueReference (cx, field, val, type, name, ObjectValueFlags.Variable));
						}
					}
				} else if (!field.Name.Contains ("$")) {
					list.Add (new FieldValueReference (cx, field, val, type, field.Name, ObjectValueFlags.Variable));
				}
			}
			return list;
		}
		
		ValueReference GetHoistedThisReference (SoftEvaluationContext cx)
		{
			try {
				Value val = cx.Frame.GetThis ();
				TypeMirror type = (TypeMirror) GetValueType (cx, val);
				return GetHoistedThisReference (cx, type, val);
			} catch (AbsentInformationException) {
			}
			return null;
		}
		
		ValueReference GetHoistedThisReference (SoftEvaluationContext cx, TypeMirror type, object val)
		{
			foreach (FieldInfoMirror field in type.GetFields ()) {
				if (IsHoistedThisReference (field)) {
					return new FieldValueReference (cx, field, val, type, "this", ObjectValueFlags.Literal);
				} else if (IsClosureReferenceField (field)) {
					var fieldRef = new FieldValueReference (cx, field, val, type);
					var thisRef = GetHoistedThisReference (cx, field.FieldType, fieldRef.Value);
					if (thisRef != null)
						return thisRef;
				}
			}
			return null;
		}
		
		// if the local does not have a name, constructs one from the index
		static string GetLocalName (SoftEvaluationContext cx, LocalVariable local)
		{
			if (!string.IsNullOrEmpty (local.Name) || cx.SourceCodeAvailable)
				return local.Name;
			return "loc" + local.Index;
		}
		
		protected override ValueReference OnGetLocalVariable (EvaluationContext ctx, string name)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			if (InGeneratedClosureOrIteratorType (cx))
				return FindByName (OnGetLocalVariables (cx), v => v.Name, name, ctx.CaseSensitive);
			
			try {
				LocalVariable local = null;
				if (!cx.SourceCodeAvailable) {
					if (name.StartsWith ("loc")) {
						int idx;
						if (int.TryParse (name.Substring (3), out idx))
							local = cx.Frame.Method.GetLocals ().FirstOrDefault (loc => loc.Index == idx);
					}
				} else {
					local = ctx.CaseSensitive
						? cx.Frame.GetVisibleVariableByName (name)
						: FindByName (cx.Frame.GetVisibleVariables(), v => v.Name, name, false);
				}
				if (local != null) {
					return new VariableValueReference (ctx, GetLocalName (cx, local), local);
				}
				return FindByName (OnGetLocalVariables (ctx), v => v.Name, name, ctx.CaseSensitive);
			} catch (AbsentInformationException) {
				return null;
			}
		}

		protected override IEnumerable<ValueReference> OnGetLocalVariables (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			if (InGeneratedClosureOrIteratorType (cx)) {
				ValueReference vthis = GetThisReference (cx);
				return GetHoistedLocalVariables (cx, vthis).Union (GetLocalVariables (cx));
			}
			else
				return GetLocalVariables (cx);
		}
		
		IEnumerable<ValueReference> GetLocalVariables (SoftEvaluationContext cx)
		{
			IList<LocalVariable> locals;
			try {
				locals = cx.Frame.GetVisibleVariables ();
			} catch (AbsentInformationException) {
				yield break;
			}
			foreach (LocalVariable local in locals) {
				if (local.IsArg)
					continue;
				if (IsClosureReferenceLocal (local) && IsGeneratedType (local.Type)) {
					foreach (var gv in GetHoistedLocalVariables (cx, new VariableValueReference (cx, local.Name, local))) {
						yield return gv;
					}
				} else if (!IsGeneratedTemporaryLocal (local)) {
					yield return new VariableValueReference (cx, GetLocalName (cx, local), local);
				}
			}
		}

		public override bool HasMember (EvaluationContext ctx, object type, string memberName, BindingFlags bindingFlags)
		{
			TypeMirror tm = (TypeMirror) type;

			while (tm != null) {
				FieldInfoMirror field = FindByName (tm.GetFields (), f => f.Name, memberName, ctx.CaseSensitive);
				if (field != null)
					return true;

				PropertyInfoMirror prop = FindByName (tm.GetProperties (), p => p.Name, memberName, ctx.CaseSensitive);
				if (prop != null) {
					MethodMirror getter = prop.GetGetMethod (bindingFlags.HasFlag (BindingFlags.NonPublic));
					if (getter != null)
						return true;
				}

				if (bindingFlags.HasFlag (BindingFlags.DeclaredOnly))
					break;

				tm = tm.BaseType;
			}

			return false;
		}

		static bool IsAnonymousType (TypeMirror type)
		{
			return type.Name.StartsWith ("<>__AnonType", StringComparison.InvariantCulture);
		}

		protected override ValueReference GetMember (EvaluationContext ctx, object t, object co, string name)
		{
			TypeMirror type = t as TypeMirror;

			while (type != null) {
				FieldInfoMirror field = FindByName (type.GetFields(), f => f.Name, name, ctx.CaseSensitive);
				if (field != null && (field.IsStatic || co != null))
					return new FieldValueReference (ctx, field, co, type);

				PropertyInfoMirror prop = FindByName (type.GetProperties(), p => p.Name, name, ctx.CaseSensitive);
				if (prop != null && (IsStatic (prop) || co != null)) {
					// Optimization: if the property has a CompilerGenerated backing field, use that instead.
					// This way we avoid overhead of invoking methods on the debugee when the value is requested.
					string cgFieldName = string.Format ("<{0}>{1}", prop.Name, IsAnonymousType (type) ? "" : "k__BackingField");
					if ((field = FindByName (type.GetFields (), f => f.Name, cgFieldName, true)) != null && IsCompilerGenerated (field))
						return new FieldValueReference (ctx, field, co, type, prop.Name, ObjectValueFlags.Property);

					// Backing field not available, so do things the old fashioned way.
					MethodMirror getter = prop.GetGetMethod (true);
					if (getter == null)
						return null;
					
					return new PropertyValueReference (ctx, prop, co, type, getter, null);
				}

				type = type.BaseType;
			}

			return null;
		}

		static bool IsCompilerGenerated (FieldInfoMirror field)
		{
			CustomAttributeDataMirror[] attrs = field.GetCustomAttributes (true);
			var cga = GetAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute> (attrs);
			return cga != null;
		}
		
		static bool IsStatic (PropertyInfoMirror prop)
		{
			MethodMirror met = prop.GetGetMethod (true) ?? prop.GetSetMethod (true);
			return met.IsStatic;
		}
		
		static T FindByName<T> (IEnumerable<T> elems, Func<T,string> getName, string name, bool caseSensitive)
		{
			T best = default(T);
			foreach (T t in elems) {
				string n = getName (t);
				if (n == name) 
					return t;
				else if (!caseSensitive && n.Equals (name, StringComparison.CurrentCultureIgnoreCase))
					best = t;
			}
			return best;
		}
		
		protected override IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, object t, object co, BindingFlags bindingFlags)
		{
			Dictionary<string, PropertyInfoMirror> subProps = new Dictionary<string, PropertyInfoMirror> ();
			TypeMirror type = t as TypeMirror;
			TypeMirror realType = null;
			if (co != null && (bindingFlags & BindingFlags.Instance) != 0)
				realType = GetValueType (ctx, co) as TypeMirror;

			// First of all, get a list of properties overriden in sub-types
			while (realType != null && realType != type) {
				foreach (PropertyInfoMirror prop in realType.GetProperties (bindingFlags | BindingFlags.DeclaredOnly)) {
					MethodMirror met = prop.GetGetMethod (true);
					if (met == null || met.GetParameters ().Length != 0 || met.IsAbstract || !met.IsVirtual || met.IsStatic)
						continue;
					if (met.IsPublic && ((bindingFlags & BindingFlags.Public) == 0))
						continue;
					if (!met.IsPublic && ((bindingFlags & BindingFlags.NonPublic) == 0))
						continue;
					subProps [prop.Name] = prop;
				}
				realType = realType.BaseType;
			}
			
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
					MethodMirror getter = prop.GetGetMethod (true);
					if (getter == null || getter.GetParameters ().Length != 0 || getter.IsAbstract)
						continue;
					if (getter.IsStatic && ((bindingFlags & BindingFlags.Static) == 0))
						continue;
					if (!getter.IsStatic && ((bindingFlags & BindingFlags.Instance) == 0))
						continue;
					if (getter.IsPublic && ((bindingFlags & BindingFlags.Public) == 0))
						continue;
					if (!getter.IsPublic && ((bindingFlags & BindingFlags.NonPublic) == 0))
						continue;
					
					// If a property is overriden, return the override instead of the base property
					PropertyInfoMirror overridden;
					if (getter.IsVirtual && subProps.TryGetValue (prop.Name, out overridden)) {
						getter = overridden.GetGetMethod (true);
						if (getter == null)
							continue;
						
						yield return new PropertyValueReference (ctx, overridden, co, overridden.DeclaringType, getter, null);
					} else {
						yield return new PropertyValueReference (ctx, prop, co, type, getter, null);
					}
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
				if (type.Namespace == namspace || type.Namespace.StartsWith (namspacePrefix, StringComparison.InvariantCulture)) {
					namespaces.Add (type.Namespace);
					types.Add (type.FullName);
				}
			}
			childNamespaces = new string [namespaces.Count];
			namespaces.CopyTo (childNamespaces);
			
			childTypes = new string [types.Count];
			types.CopyTo (childTypes);
		}

		protected override IEnumerable<ValueReference> OnGetParameters (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			LocalVariable[] locals;
			try {
				locals = cx.Frame.Method.GetLocals ();
			} catch (AbsentInformationException) {
				yield break;
			}
				
			foreach (LocalVariable var in locals) {
				if (var.IsArg) {
					string name = !string.IsNullOrEmpty (var.Name) ? var.Name : "arg" + var.Index;
					yield return new VariableValueReference (ctx, name, var);
				}
			}
		}

		protected override ValueReference OnGetThisReference (EvaluationContext ctx)
		{
			SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
			if (InGeneratedClosureOrIteratorType (cx))
				return GetHoistedThisReference (cx);
			else
				return GetThisReference (cx);
		}
		
		ValueReference GetThisReference (SoftEvaluationContext cx)
		{
			try {
				if (cx.Frame.Method.IsStatic)
					return null;
				Value val = cx.Frame.GetThis ();
				return LiteralValueReference.CreateTargetObjectLiteral (cx, "this", val);
			} catch (AbsentInformationException) {
				return null;
			}
		}
		
		public override ValueReference GetCurrentException (EvaluationContext ctx)
		{
			try {
				SoftEvaluationContext cx = (SoftEvaluationContext) ctx;
				ObjectMirror exc = cx.Session.GetExceptionObject (cx.Thread);
				if (exc != null)
					return LiteralValueReference.CreateTargetObjectLiteral (ctx, ctx.Options.CurrentExceptionTag, exc);
				else
					return null;
			} catch (AbsentInformationException) {
				return null;
			}
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

		public override object GetParentType (EvaluationContext ctx, object type)
		{
			TypeMirror tm = type as TypeMirror;

			if (tm != null) {
				int plus = tm.FullName.LastIndexOf ('+');

				return plus != -1 ? GetType (ctx, tm.FullName.Substring (0, plus)) : null;
			}

			return ((Type) type).DeclaringType;
		}

		public override IEnumerable<object> GetNestedTypes (EvaluationContext ctx, object type)
		{
			TypeMirror t = (TypeMirror) type;
			foreach (TypeMirror nt in t.GetNestedTypes ())
				yield return nt;
		}
		
		public override string GetTypeName (EvaluationContext ctx, object type)
		{
			TypeMirror tm = type as TypeMirror;
			if (tm != null) {
				if (IsGeneratedType (tm)) {
					// Return the name of the container-type.
					return tm.FullName.Substring (0, tm.FullName.LastIndexOf ('+'));
				}
				
				return tm.FullName;
			} else
				return ((Type)type).FullName;
		}
		
		public override object GetValueType (EvaluationContext ctx, object val)
		{
			if (val is ArrayMirror)
				return ((ArrayMirror)val).Type;
			if (val is ObjectMirror)
				return ((ObjectMirror)val).Type;
			if (val is EnumMirror)
				return ((EnumMirror)val).Type;
			if (val is StructMirror)
				return ((StructMirror)val).Type;
			if (val is PointerValue)
				return ((PointerValue) val).Type;
			if (val is PrimitiveValue) {
				PrimitiveValue pv = (PrimitiveValue) val;
				if (pv.Value == null)
					return typeof(Object);
				else
					return pv.Value.GetType ();
			}

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
			TypeMirror[] types = null;

			if (argTypes != null) {
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

		public override bool IsString (EvaluationContext ctx, object val)
		{
			return val is StringMirror;
		}
		
		public override bool IsArray (EvaluationContext ctx, object val)
		{
			return val is ArrayMirror;
		}

		public override bool IsValueType (object type)
		{
			TypeMirror t = type as TypeMirror;
			return t != null && t.IsValueType;
		}

		public override bool IsClass (object type)
		{
			TypeMirror t = type as TypeMirror;
			return t != null && (t.IsClass || t.IsValueType) && !t.IsPrimitive;
		}

		public override bool IsNull (EvaluationContext ctx, object val)
		{
			return val == null || ((val is PrimitiveValue) && ((PrimitiveValue)val).Value == null) || ((val is PointerValue) && ((PointerValue)val).Address == 0);
		}

		public override bool IsPrimitive (EvaluationContext ctx, object val)
		{
			return val is PrimitiveValue || val is StringMirror || ((val is StructMirror) && ((StructMirror)val).Type.IsPrimitive) || val is PointerValue;
		}

		public override bool IsPointer (EvaluationContext ctx, object val)
		{
			return val is PointerValue;
		}

		public override bool IsEnum (EvaluationContext ctx, object val)
		{
			return val is EnumMirror;
		}
		
		protected override TypeDisplayData OnGetTypeDisplayData (EvaluationContext gctx, object type)
		{
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			
			bool isCompilerGenerated = false;
			string nameString = null;
			string typeString = null;
			string valueString = null;
			string proxyType = null;
			Dictionary<string, DebuggerBrowsableState> memberData = null;
			
			try {
				TypeMirror t = (TypeMirror) type;
				foreach (CustomAttributeDataMirror attr in t.GetCustomAttributes (true)) {
					string attName = attr.Constructor.DeclaringType.FullName;
					if (attName == "System.Diagnostics.DebuggerDisplayAttribute") {
						DebuggerDisplayAttribute at = BuildAttribute<DebuggerDisplayAttribute> (attr);
						nameString = at.Name;
						typeString = at.Type;
						valueString = at.Value;
					}
					else if (attName == "System.Diagnostics.DebuggerTypeProxyAttribute") {
						DebuggerTypeProxyAttribute at = BuildAttribute<DebuggerTypeProxyAttribute> (attr);
						proxyType = at.ProxyTypeName;
						if (!string.IsNullOrEmpty (proxyType))
							ForceLoadType (ctx, proxyType);
					}
					else if (attName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
						isCompilerGenerated = true;
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
						if (memberData == null)
							memberData = new Dictionary<string, DebuggerBrowsableState> ();
						memberData [fi.Name] = att.State;
					}
				}
				foreach (PropertyInfoMirror pi in t.GetProperties ()) {
					DebuggerBrowsableAttribute att = GetAttribute <DebuggerBrowsableAttribute> (pi.GetCustomAttributes (true));
					if (att != null) {
						if (memberData == null)
							memberData = new Dictionary<string, DebuggerBrowsableState> ();
						memberData [pi.Name] = att.State;
					}
				}
			} catch (Exception ex) {
				ctx.Session.WriteDebuggerOutput (true, ex.ToString ());
			}
			return new TypeDisplayData (proxyType, valueString, typeString, nameString, isCompilerGenerated, memberData);
		}
		
		static T GetAttribute<T> (CustomAttributeDataMirror[] attrs)
		{
			foreach (CustomAttributeDataMirror attr in attrs) {
				if (attr.Constructor.DeclaringType.FullName == typeof(T).FullName)
					return BuildAttribute<T> (attr);
			}
			return default(T);
		}

		public override bool IsTypeLoaded (EvaluationContext gctx, string typeName)
		{
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			
			return ctx.Session.GetType (typeName) != null;
		}
		
		public override bool IsTypeLoaded (EvaluationContext ctx, object type)
		{
			TypeMirror tm = (TypeMirror) type;

			if (tm.VirtualMachine.Version.AtLeast (2, 23))
				return tm.IsInitialized;

			return IsTypeLoaded (ctx, tm.FullName);
		}
		
		public override bool ForceLoadType (EvaluationContext gctx, object type)
		{
			SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
			TypeMirror tm = (TypeMirror) type;

			if (!tm.VirtualMachine.Version.AtLeast (2, 23))
				return IsTypeLoaded (gctx, tm.FullName);

			if (tm.IsInitialized)
				return true;

			if (!tm.Attributes.HasFlag (TypeAttributes.BeforeFieldInit))
				return false;

			MethodMirror cctor = OverloadResolve (ctx, ".cctor", tm, new TypeMirror[0], false, true, false);
			if (cctor == null)
				return true;

			try {
				tm.InvokeMethod (ctx.Thread, cctor, new Value[0], InvokeOptions.DisableBreakpoints | InvokeOptions.SingleThreaded);
			} catch {
				return false;
			} finally {
				ctx.Session.StackVersion++;
			}

			return true;
		}
		
		static T BuildAttribute<T> (CustomAttributeDataMirror attr)
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
			TypeMirror type = ToTypeMirror (ctx, targetType);
			
			ctx.AssertTargetInvokeAllowed ();
			
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
			var cache = ctx.Session.OverloadResolveCache;
			TypeMirror currentType = type;
			
			while (currentType != null) {
				MethodMirror[] methods = null;
				
				if (ctx.CaseSensitive) {
					lock (cache) {
						cache.TryGetValue (Tuple.Create (currentType, methodName), out methods);
					}
				}
				
				if (methods == null) {
					if (currentType.VirtualMachine.Version.AtLeast (2, 7))
						methods = currentType.GetMethodsByNameFlags (methodName, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static, !ctx.CaseSensitive);
					else
						methods = currentType.GetMethods ();
					
					if (ctx.CaseSensitive) {
						lock (cache) {
							cache [Tuple.Create (currentType, methodName)] = methods;
						}
					}
				}
				
				foreach (MethodMirror met in methods) {
					if (met.Name == methodName || (!ctx.CaseSensitive && met.Name.Equals (methodName, StringComparison.CurrentCultureIgnoreCase))) {
						ParameterInfoMirror[] pars = met.GetParameters ();
						if (argtypes == null || pars.Length == argtypes.Length && ((met.IsStatic && allowStatic) || (!met.IsStatic && allowInstance)))
							candidates.Add (met);
					}
				}

				if (argtypes == null && candidates.Count > 0)
					break; // when argtypes is null, we are just looking for *any* match (not a specific match)
				
				if (methodName == ".ctor")
					break; // Can't create objects using constructor from base classes
				
				// Make sure that we always pull in at least System.Object methods (this is mostly needed for cases where 'type' was an interface)
				if (currentType.BaseType == null && currentType.FullName != "System.Object")
					currentType = ctx.Session.GetType ("System.Object");
				else
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
			if (candidates.Count == 0) {
				if (throwIfNotFound) {
					if (methodName == null)
						throw new EvaluatorException ("Indexer not found in type `{0}'.", typeName);

					throw new EvaluatorException ("Method `{0}' not found in type `{1}'.", methodName, typeName);
				} else
					return null;
			}

			if (argtypes == null) {
				// This is just a probe to see if the type contains *any* methods of the given name
				return candidates[0];
			}

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
			if (obj is StringMirror) {
				StringMirror mirror = (StringMirror) obj;
				string str;
				
				if (gctx.Options.EllipsizeStrings) {
					if (mirror.VirtualMachine.Version.AtLeast (2, 10)) {
						int length = mirror.Length;
						
						if (length > gctx.Options.EllipsizedLength)
							str = new string (mirror.GetChars (0, gctx.Options.EllipsizedLength)) + EvaluationOptions.Ellipsis;
						else
							str = mirror.Value;
					} else {
						str = mirror.Value;
						if (str.Length > gctx.Options.EllipsizedLength)
							str = str.Substring (0, gctx.Options.EllipsizedLength) + EvaluationOptions.Ellipsis;
					}
				} else {
					str = mirror.Value;
				}
				
				return str;
			} else if (obj is PrimitiveValue) {
				return ((PrimitiveValue)obj).Value;
			} else if (obj is PointerValue) {
				return new IntPtr (((PointerValue)obj).Address);
			} else if (obj is StructMirror) {
				StructMirror sm = (StructMirror) obj;

				if (sm.Type.IsPrimitive) {
					// Boxed primitive
					if (sm.Type.FullName == "System.IntPtr")
						return new IntPtr ((long)((PrimitiveValue)sm.Fields[0]).Value);
					if (sm.Fields.Length > 0 && (sm.Fields[0] is PrimitiveValue))
						return ((PrimitiveValue)sm.Fields[0]).Value;
				} else if (sm.Type.FullName == "System.Decimal") {
					SoftEvaluationContext ctx = (SoftEvaluationContext) gctx;
					MethodMirror method = OverloadResolve (ctx, "GetBits", sm.Type, new TypeMirror[1] { sm.Type }, false, true, false);
					if (method != null) {
						ArrayMirror array;
						
						try {
							array = sm.Type.InvokeMethod (ctx.Thread, method, new Value[1] { sm }, InvokeOptions.DisableBreakpoints | InvokeOptions.SingleThreaded) as ArrayMirror;
						} catch {
							array = null;
						} finally {
							ctx.Session.StackVersion++;
						}
						
						if (array != null) {
							int[] bits = new int [4];
							for (int i = 0; i < 4; i++)
								bits[i] = (int) TargetObjectToObject (gctx, array[i]);
							
							return new decimal (bits);
						}
					}
				}
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
				else if (obj is PrimitiveValue)
					handle = ((PrimitiveValue)obj).BeginInvokeMethod (ctx.Thread, function, args, options, null, null);
				else
					throw new ArgumentException ("Soft debugger method calls cannot be invoked on objects of type " + obj.GetType ().Name);
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
				// Don't wait for the abort to finish. The engine will do it.
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
				else if (obj is StructMirror)
					result = ((StructMirror)obj).EndInvokeMethod (handle);
				else
					result = ((PrimitiveValue)obj).EndInvokeMethod (handle);
			} catch (InvocationException ex) {
				if (!Aborting && ex.Exception != null) {
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
