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
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SR = System.Reflection;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Evaluation;
using CorElementType = Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType;
using CorDebugMappingResult = Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult;
using CorDebugHandleType = Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugHandleType;
using System.Diagnostics.SymbolStore;
using Microsoft.Samples.Debugging.CorMetadata;
using MonoDevelop.Core.Collections;

namespace MonoDevelop.Debugger.Win32
{
	public class CorObjectAdaptor: ObjectValueAdaptor
	{
		public override bool IsPrimitive (EvaluationContext ctx, object val)
		{
			object v = GetRealObject (ctx, val);
			return (v is CorGenericValue) || (v is CorStringValue);
		}

		public override bool IsPointer (EvaluationContext ctx, object val)
		{
			// FIXME: implement this correctly.
			return false;
		}

		public override bool IsEnum (EvaluationContext ctx, object val)
		{
			CorType type = (CorType) GetValueType (ctx, val);
			return IsEnum (ctx, type);
		}

		public override bool IsArray (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val) is CorArrayValue;
		}
		
		public override bool IsString (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val) is CorStringValue;
		}

		public override bool IsClassInstance (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val) is CorObjectValue;
		}

		public override bool IsNull (EvaluationContext ctx, object gval)
		{
			CorValRef val = (CorValRef) gval;
			return val == null || ((val.Val is CorReferenceValue) && ((CorReferenceValue) val.Val).IsNull);
		}

		public override bool IsValueType (object type)
		{
			return ((CorType)type).Type == CorElementType.ELEMENT_TYPE_VALUETYPE;
		}

		public override bool IsClass (object type)
		{
			return ((CorType)type).Class != null;
		}

		public override string GetTypeName (EvaluationContext ctx, object gtype)
		{
			CorType type = (CorType) gtype;
			CorEvaluationContext cctx = (CorEvaluationContext) ctx;
			Type t;
			if (CorMetadataImport.CoreTypes.TryGetValue (type.Type, out t))
				return t.FullName;
			try {
				if (type.Type == CorElementType.ELEMENT_TYPE_ARRAY || type.Type == CorElementType.ELEMENT_TYPE_SZARRAY)
					return GetTypeName (ctx, type.FirstTypeParameter) + "[" + new string (',', type.Rank - 1) + "]";

				if (type.Type == CorElementType.ELEMENT_TYPE_BYREF)
					return GetTypeName (ctx, type.FirstTypeParameter) + "&";

				if (type.Type == CorElementType.ELEMENT_TYPE_PTR)
					return GetTypeName (ctx, type.FirstTypeParameter) + "*";
				
				return type.GetTypeInfo (cctx.Session).FullName;
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				throw;
			}
		}

		public override object GetValueType (EvaluationContext ctx, object val)
		{
			return GetRealObject (ctx, val).ExactType;
		}
		
		public override object GetBaseType (EvaluationContext ctx, object type)
		{
			return ((CorType) type).Base;
		}

		public override object[] GetTypeArgs (EvaluationContext ctx, object type)
		{
			CorType[] types = ((CorType)type).TypeParameters;
			return CastArray<object> (types);
		}

		IEnumerable<Type> GetAllTypes (EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			foreach (CorModule mod in ctx.Session.GetModules ()) {
				CorMetadataImport mi = ctx.Session.GetMetadataForModule (mod.Name);
				if (mi != null) {
					foreach (Type t in mi.DefinedTypes)
						yield return t;
				}
			}
		}

		public override object GetType (EvaluationContext gctx, string name, object[] gtypeArgs)
		{
			CorType[] typeArgs = CastArray<CorType> (gtypeArgs);

			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			foreach (CorModule mod in ctx.Session.GetModules ()) {
				CorMetadataImport mi = ctx.Session.GetMetadataForModule (mod.Name);
				if (mi != null) {
					foreach (Type t in mi.DefinedTypes)
						if (t.FullName == name) {
							CorClass cls = mod.GetClassFromToken (t.MetadataToken);
							return cls.GetParameterizedType (CorElementType.ELEMENT_TYPE_CLASS, typeArgs);
						}
				}
			}
			return null;
		}

		T[] CastArray<T> (object[] array)
		{
			if (array == null)
				return null;
			T[] ret = new T[array.Length];
			Array.Copy (array, ret, array.Length);
			return ret;
		}

        public override string CallToString(EvaluationContext ctx, object objr)
        {
            CorValue obj = GetRealObject(ctx, objr);

            if ((obj is CorReferenceValue) && ((CorReferenceValue)obj).IsNull)
                return string.Empty;

            CorStringValue stringVal = obj as CorStringValue;
            if (stringVal != null)
                return stringVal.String;

            CorArrayValue arr = obj as CorArrayValue;
            if (arr != null)
            {
                StringBuilder tn = new StringBuilder (GetDisplayTypeName (ctx, arr.ExactType.FirstTypeParameter));
                tn.Append("[");
                int[] dims = arr.GetDimensions();
                for (int n = 0; n < dims.Length; n++)
                {
                    if (n > 0)
                        tn.Append(',');
                    tn.Append(dims[n]);
                }
                tn.Append("]");
                return tn.ToString();
            }

            CorEvaluationContext cctx = (CorEvaluationContext)ctx;
            CorObjectValue co = obj as CorObjectValue;
            if (co != null)
            {
                if (IsEnum (ctx, co.ExactType))
                {
                    MetadataType rt = co.ExactType.GetTypeInfo(cctx.Session) as MetadataType;
                    bool isFlags = rt != null && rt.ReallyIsFlagsEnum;
                    string enumName = GetTypeName(ctx, co.ExactType);
                    ValueReference val = GetMember(ctx, null, objr, "value__");
                    ulong nval = (ulong)System.Convert.ChangeType(val.ObjectValue, typeof(ulong));
                    ulong remainingFlags = nval;
                    string flags = null;
                    foreach (ValueReference evals in GetMembers(ctx, co.ExactType, null, BindingFlags.Public | BindingFlags.Static))
                    {
                        ulong nev = (ulong)System.Convert.ChangeType(evals.ObjectValue, typeof(ulong));
                        if (nval == nev)
                            return evals.Name;
                        if (isFlags && nev != 0 && (nval & nev) == nev)
                        {
                            if (flags == null)
                                flags = enumName + "." + evals.Name;
                            else
                                flags += " | " + enumName + "." + evals.Name;
                            remainingFlags &= ~nev;
                        }
                    }
                    if (isFlags)
                    {
                        if (remainingFlags == nval)
                            return nval.ToString ();
                        if (remainingFlags != 0)
                            flags += " | " + remainingFlags;
                        return flags;
                    }
                    else
                        return nval.ToString ();
                }

				CorType targetType = (CorType)GetValueType (ctx, objr);

				MethodInfo met = OverloadResolve (cctx, "ToString", targetType, new CorType[0], BindingFlags.Public | BindingFlags.Instance, false);
				if (met != null && met.DeclaringType.FullName != "System.Object") {
					object[] args = new object[0];
					object ores = RuntimeInvoke (ctx, targetType, objr, "ToString", args, args);
					CorStringValue res = GetRealObject (ctx, ores) as CorStringValue;
                    if (res != null)
                        return res.String;
                }

				return GetDisplayTypeName (ctx, targetType);
            }

            CorGenericValue genVal = obj as CorGenericValue;
            if (genVal != null)
            {
                return genVal.GetValue().ToString ();
            }

            return base.CallToString(ctx, obj);
        }

		public override object CreateTypeObject (EvaluationContext ctx, object type)
		{
			CorType t = (CorType)type;
			string tname = GetTypeName (ctx, t) + ", " + System.IO.Path.GetFileNameWithoutExtension (t.Class.Module.Assembly.Name);
			CorType stype = (CorType) GetType (ctx, "System.Type");
			object[] argTypes = new object[] { GetType (ctx, "System.String") };
			object[] argVals = new object[] { CreateValue (ctx, tname) };
			return RuntimeInvoke (ctx, stype, null, "GetType", argTypes, argVals);
		}

		public CorValRef GetBoxedArg (CorEvaluationContext ctx, CorValRef val, Type argType)
		{
			// Boxes a value when required
			if (argType == typeof (object) && IsValueType (ctx, val))
				return Box (ctx, val);
			else
				return val;
		}

		bool IsValueType (CorEvaluationContext ctx, CorValRef val)
		{
			CorValue v = GetRealObject (ctx, val);
			if (v.Type == CorElementType.ELEMENT_TYPE_VALUETYPE)
				return true;
			return v is CorGenericValue;
		}

		CorValRef Box (CorEvaluationContext ctx, CorValRef val)
		{
			CorValRef arr = new CorValRef (delegate {
				return ctx.Session.NewArray (ctx, (CorType)GetValueType (ctx, val), 1);
			});
			CorArrayValue array = CorObjectAdaptor.GetRealObject (ctx, arr) as CorArrayValue;
			
			ArrayAdaptor realArr = new ArrayAdaptor (ctx, arr, array);
			realArr.SetElement (new int[] { 0 }, val);
			
			CorType at = (CorType) GetType (ctx, "System.Array");
			object[] argTypes = new object[] { GetType (ctx, "System.Int32") };
			return (CorValRef)RuntimeInvoke (ctx, at, arr, "GetValue", argTypes, new object[] { CreateValue (ctx, 0) });
		}

		public override bool HasMethod (EvaluationContext gctx, object gtargetType, string methodName, object[] gargTypes, BindingFlags flags)
		{
			CorType targetType = (CorType) gtargetType;
			CorType[] argTypes = gargTypes != null ? CastArray<CorType> (gargTypes) : null;
			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			flags = flags | BindingFlags.Public | BindingFlags.NonPublic;

			return OverloadResolve (ctx, methodName, targetType, argTypes, flags, false) != null;
		}

		public override object RuntimeInvoke (EvaluationContext gctx, object gtargetType, object gtarget, string methodName, object[] gargTypes, object[] gargValues)
		{
			CorType targetType = (CorType) gtargetType;
			CorValRef target = (CorValRef) gtarget;
			CorType[] argTypes = CastArray<CorType> (gargTypes);
			CorValRef[] argValues = CastArray<CorValRef> (gargValues);

			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic;
			if (target != null)
				flags |= BindingFlags.Instance;
			else
				flags |= BindingFlags.Static;

			CorEvaluationContext ctx = (CorEvaluationContext)gctx;
			MethodInfo method = OverloadResolve (ctx, methodName, targetType, argTypes, flags, true);
			ParameterInfo[] parameters = method.GetParameters ();
			for (int n = 0; n < parameters.Length; n++) {
				if (parameters[n].ParameterType == typeof(object) && (IsValueType (ctx, argValues[n])))
					argValues[n] = Box (ctx, argValues[n]);
			}

			if (method != null) {
				CorValRef v = new CorValRef (delegate {
					CorFunction func = targetType.Class.Module.GetFunctionFromToken (method.MetadataToken);
					CorValue[] args = new CorValue[argValues.Length];
					for (int n = 0; n < args.Length; n++)
						args[n] = argValues[n].Val;
					return ctx.RuntimeInvoke (func, new CorType[0], target != null ? target.Val : null, args);
				});
				if (v.Val == null)
					return null;
				else
					return v;
			}
			else
				throw new EvaluatorException ("Invalid method name or incompatible arguments.");
		}


		MethodInfo OverloadResolve (CorEvaluationContext ctx, string methodName, CorType type, CorType[] argtypes, BindingFlags flags, bool throwIfNotFound)
		{
			List<MethodInfo> candidates = new List<MethodInfo> ();
			CorType currentType = type;

			while (currentType != null) {
				Type rtype = currentType.GetTypeInfo (ctx.Session);
				foreach (MethodInfo met in rtype.GetMethods (flags)) {
					if (met.Name == methodName || (!ctx.CaseSensitive && met.Name.Equals (methodName, StringComparison.CurrentCultureIgnoreCase))) {
						if (argtypes == null)
							return met;
						ParameterInfo[] pars = met.GetParameters ();
						if (pars.Length == argtypes.Length)
							candidates.Add (met);
					}
				}
				if (methodName == ".ctor")
					break; // Can't create objects using constructor from base classes
				currentType = currentType.Base;
			}


			return OverloadResolve (ctx, GetTypeName (ctx, type), methodName, argtypes, candidates, throwIfNotFound);
		}

		bool IsApplicable (CorEvaluationContext ctx, MethodInfo method, CorType[] types, out string error, out int matchCount)
		{
			ParameterInfo[] mparams = method.GetParameters ();
			matchCount = 0;

			for (int i = 0; i < types.Length; i++) {

				Type param_type = mparams[i].ParameterType;

				if (param_type.FullName == GetTypeName (ctx, types[i])) {
					matchCount++;
					continue;
				}

				if (IsAssignableFrom (ctx, param_type, types[i]))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, GetTypeName (ctx, types[i]), param_type.FullName);
				return false;
			}

			error = null;
			return true;
		}

		MethodInfo OverloadResolve (CorEvaluationContext ctx, string typeName, string methodName, CorType[] argtypes, List<MethodInfo> candidates, bool throwIfNotFound)
		{
			if (candidates.Count == 1) {
				string error;
				int matchCount;
				if (IsApplicable (ctx, candidates[0], argtypes, out error, out matchCount))
					return candidates[0];

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
			MethodInfo match = null;
			int bestCount = -1;
			bool repeatedBestCount = false;

			foreach (MethodInfo method in candidates) {
				string error;
				int matchCount;

				if (!IsApplicable (ctx, method, argtypes, out error, out matchCount))
					continue;

				if (matchCount == bestCount) {
					repeatedBestCount = true;
				}
				else if (matchCount > bestCount) {
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
				*/
			}

			return match;
		}


		public override string[] GetImportedNamespaces (EvaluationContext ctx)
		{
			Set<string> list = new Set<string> ();
			foreach (Type t in GetAllTypes (ctx)) {
				list.Add (t.Namespace);
			}
			string[] arr = new string[list.Count];
			list.CopyTo (arr, 0);
			return arr;
		}

		public override void GetNamespaceContents (EvaluationContext ctx, string namspace, out string[] childNamespaces, out string[] childTypes)
		{
			Set<string> nss = new Set<string> ();
			List<string> types = new List<string> ();
			foreach (Type t in GetAllTypes (ctx)) {
				if (t.Namespace == namspace)
					types.Add (t.FullName);
				else if (t.Namespace.StartsWith (namspace + ".")) {
					if (t.Namespace.IndexOf ('.', namspace.Length + 1) == -1)
						nss.Add (t.Namespace);
				}
			}
			childNamespaces = new string[nss.Count];
			nss.CopyTo (childNamespaces, 0);
			childTypes = types.ToArray ();
		}

		bool IsAssignableFrom (CorEvaluationContext ctx, Type baseType, CorType ctype)
		{
			string tname = baseType.FullName;
			string ctypeName = GetTypeName (ctx, ctype);
			if (tname == "System.Object")
				return true;

			if (tname == ctypeName)
				return true;

			if (CorMetadataImport.CoreTypes.ContainsKey (ctype.Type))
				return false;

			switch (ctype.Type) {
				case CorElementType.ELEMENT_TYPE_ARRAY:
				case CorElementType.ELEMENT_TYPE_SZARRAY:
				case CorElementType.ELEMENT_TYPE_BYREF:
				case CorElementType.ELEMENT_TYPE_PTR:
					return false;
			}

			while (ctype != null) {
				if (GetTypeName (ctx, ctype) == tname)
					return true;
				ctype = ctype.Base;
			}
			return false;
		}

		public override object TryCast (EvaluationContext ctx, object val, object type)
		{
			CorType ctype = (CorType) GetValueType (ctx, val);
            CorValue obj = GetRealObject(ctx, val);
            string tname = GetTypeName(ctx, type);
            string ctypeName = GetValueTypeName (ctx, val);
            if (tname == "System.Object")
                return val;

            if (tname == ctypeName)
                return val;

            if (obj is CorStringValue)
                return ctypeName == tname ? val : null;

            if (obj is CorArrayValue)
                return (ctypeName == tname || ctypeName == "System.Array") ? val : null;

            if (obj is CorObjectValue)
            {
				CorObjectValue co = (CorObjectValue)obj;
				if (IsEnum (ctx, co.ExactType)) {
					ValueReference rval = GetMember (ctx, null, val, "value__");
					return TryCast (ctx, rval.Value, type);
				}

                while (ctype != null)
                {
                    if (GetTypeName(ctx, ctype) == tname)
                        return val;
                    ctype = ctype.Base;
                }
                return null;
            }

            CorGenericValue genVal = obj as CorGenericValue;
            if (genVal != null) {
                Type t = Type.GetType(tname);
				if (t != null && t.IsPrimitive && t != typeof (string)) {
					object pval = genVal.GetValue ();
					try {
						pval = System.Convert.ChangeType (pval, t);
					}
					catch {
						return null;
					}
					return CreateValue (ctx, pval);
				}
				else if (IsEnum (ctx, (CorType)type)) {
					return CreateEnum (ctx, (CorType)type, val);
				}
            }
            return null;
        }

		public object CreateEnum (EvaluationContext ctx, CorType type, object val)
		{
			object systemEnumType = GetType (ctx, "System.Enum");
			object enumType = CreateTypeObject (ctx, type);
			object[] argTypes = new object[] { GetValueType (ctx, enumType), GetValueType (ctx, val) };
			object[] args = new object[] { enumType, val };
			return RuntimeInvoke (ctx, systemEnumType, null, "ToObject", argTypes, args);
		}

		public bool IsEnum (EvaluationContext ctx, CorType targetType)
		{
			return (targetType.Type == CorElementType.ELEMENT_TYPE_VALUETYPE || targetType.Type == CorElementType.ELEMENT_TYPE_CLASS) && targetType.Base != null && GetTypeName (ctx, targetType.Base) == "System.Enum";
		}

		public override object CreateValue (EvaluationContext gctx, object value)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			if (value is string) {
				return new CorValRef (delegate {
					return ctx.Session.NewString (ctx, (string) value);
				});
			}

			foreach (KeyValuePair<CorElementType, Type> tt in CorMetadataImport.CoreTypes) {
				if (tt.Value == value.GetType ()) {
					CorValue val = ctx.Eval.CreateValue (tt.Key, null);
					CorGenericValue gv = val.CastToGenericValue ();
					gv.SetValue (value);
					return new CorValRef (val);
				}
			}
			throw new NotSupportedException ();
		}

		public override object CreateValue (EvaluationContext ctx, object type, params object[] gargs)
		{
			CorValRef[] args = CastArray<CorValRef> (gargs);
			return new CorValRef (delegate {
				return CreateCorValue (ctx, (CorType) type, args);
			});
		}

		public CorValue CreateCorValue (EvaluationContext ctx, CorType type, params CorValRef[] args)
		{
			CorEvaluationContext cctx = (CorEvaluationContext) ctx;
			CorValue[] vargs = new CorValue [args.Length];
			for (int n=0; n<args.Length; n++)
				vargs [n] = args [n].Val;

			Type t = type.GetTypeInfo (cctx.Session);
			MethodInfo ctor = null;
			foreach (MethodInfo met in t.GetMethods ()) {
				if (met.IsSpecialName && met.Name == ".ctor") {
					ParameterInfo[] pinfos = met.GetParameters ();
					if (pinfos.Length == 1) {
						ctor = met;
						break;
					}
				}
			}
			if (ctor == null)
				return null;

			CorFunction func = type.Class.Module.GetFunctionFromToken (ctor.MetadataToken);
			return cctx.RuntimeInvoke (func, type.TypeParameters, null, vargs);
		}

		public override object CreateNullValue (EvaluationContext gctx, object type)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			return new CorValRef (ctx.Eval.CreateValueForType ((CorType)type));
		}

		public override ICollectionAdaptor CreateArrayAdaptor (EvaluationContext ctx, object arr)
		{
			CorValue val = CorObjectAdaptor.GetRealObject (ctx, arr);
			
			if (val is CorArrayValue)
				return new ArrayAdaptor (ctx, (CorValRef) arr, (CorArrayValue) val);
			else
				return null;
		}
		
		public override IStringAdaptor CreateStringAdaptor (EvaluationContext ctx, object str)
		{
			CorValue val = CorObjectAdaptor.GetRealObject (ctx, str);
			
			if (val is CorStringValue)
				return new StringAdaptor (ctx, (CorValRef) str, (CorStringValue) val);
			else
				return null;
		}

		public static CorValue GetRealObject (EvaluationContext cctx, object objr)
		{
			if (objr == null || ((CorValRef)objr).Val == null)
				return null;

			return GetRealObject (cctx, ((CorValRef)objr).Val);
		}

		public static CorValue GetRealObject (EvaluationContext ctx, CorValue obj)
		{
			CorEvaluationContext cctx = (CorEvaluationContext) ctx;
			if (obj == null)
				return null;

			try {
				if (obj is CorStringValue)
					return obj;

				if (obj is CorGenericValue)
					return obj;

				if (obj is CorGenericValue)
					return obj;

				if (obj is CorArrayValue)
					return obj;

				CorArrayValue arrayVal = obj.CastToArrayValue ();
				if (arrayVal != null)
					return arrayVal;

				CorReferenceValue refVal = obj.CastToReferenceValue ();
				if (refVal != null) {
					if (refVal.IsNull)
						return refVal;
					else {
						cctx.Session.WaitUntilStopped ();
						return GetRealObject (cctx, refVal.Dereference ());
					}
				}

				cctx.Session.WaitUntilStopped ();
				CorBoxValue boxVal = obj.CastToBoxValue ();
				if (boxVal != null)
					return Unbox (ctx, boxVal);

				if (obj.ExactType.Type == CorElementType.ELEMENT_TYPE_STRING)
					return obj.CastToStringValue ();

				if (CorMetadataImport.CoreTypes.ContainsKey (obj.Type)) {
					CorGenericValue genVal = obj.CastToGenericValue ();
					if (genVal != null)
						return genVal;
				}

				if (!(obj is CorObjectValue))
					return obj.CastToObjectValue ();
			}
			catch {
				// Ignore
				throw;
			}
			return obj;
		}

		static CorValue Unbox (EvaluationContext ctx, CorBoxValue boxVal)
		{
			CorObjectValue bval = boxVal.GetObject ();
			Type ptype = Type.GetType (ctx.Adapter.GetTypeName (ctx, bval.ExactType));
			
			if (ptype != null && ptype.IsPrimitive) {
				ptype = bval.ExactType.GetTypeInfo (((CorEvaluationContext)ctx).Session);
				foreach (FieldInfo field in ptype.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
					if (field.Name == "m_value") {
						CorValue val = bval.GetFieldValue (bval.ExactType.Class, field.MetadataToken);
						val = GetRealObject (ctx, val);
						return val;
					}
				}
			}

			return GetRealObject (ctx, bval);
		}

		public override object GetEnclosingType (EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			if (ctx.Frame.FrameType != CorFrameType.ILFrame || ctx.Frame.Function == null)
				return null;

			CorClass cls = ctx.Frame.Function.Class;
			List<CorType> tpars = new List<CorType> ();
			foreach (CorType t in ctx.Frame.TypeParameters)
				tpars.Add (t);
			return cls.GetParameterizedType (CorElementType.ELEMENT_TYPE_CLASS, tpars.ToArray ());
		}

		public override IEnumerable<EnumMember> GetEnumMembers (EvaluationContext ctx, object tt)
		{
			CorType t = (CorType)tt;
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;

			Type type = t.GetTypeInfo (cctx.Session);

			foreach (FieldInfo field in type.GetFields (BindingFlags.Public | BindingFlags.Static)) {
				if (field.IsLiteral && field.IsStatic) {
					object val = field.GetValue (null);
					EnumMember em = new EnumMember ();
					em.Value = (long) System.Convert.ChangeType (val, typeof (long));
					em.Name = field.Name;
					yield return em;
				}
			}
		}

		public override ValueReference GetIndexerReference (EvaluationContext ctx, object target, object[] indices)
		{
			CorEvaluationContext cctx = (CorEvaluationContext)ctx;
			CorType targetType = GetValueType (ctx, target) as CorType;

			CorValRef[] values = new CorValRef[indices.Length];
			CorType[] types = new CorType[indices.Length];
			for (int n = 0; n < indices.Length; n++) {
				types[n] = (CorType) GetValueType (ctx, indices[n]);
				values[n] = (CorValRef) indices[n];
			}

			List<MethodInfo> candidates = new List<MethodInfo> ();
			List<PropertyInfo> props = new List<PropertyInfo> ();
			List<CorType> propTypes = new List<CorType> ();

			CorType t = targetType;
			while (t != null) {
				Type type = t.GetTypeInfo (cctx.Session);

				foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
					MethodInfo mi = null;
					try {
						mi = prop.CanRead ? prop.GetGetMethod () : null;
					}
					catch {
						// Ignore
					}
					if (mi != null && mi.GetParameters ().Length > 0) {
						candidates.Add (mi);
						props.Add (prop);
						propTypes.Add (t);
					}
				}
				t = t.Base;
			}

			MethodInfo idx = OverloadResolve (cctx, GetTypeName (ctx, targetType), null, types, candidates, true);
			int i = candidates.IndexOf (idx);
			return new PropertyReference (ctx, props[i], (CorValRef)target, propTypes[i], values);
		}

		public override bool HasMember (EvaluationContext ctx, object tt, string memberName, BindingFlags bindingFlags)
		{
			CorEvaluationContext cctx = (CorEvaluationContext) ctx;
			CorType ct = (CorType) tt;

			while (ct != null) {
				Type type = ct.GetTypeInfo (cctx.Session);

				FieldInfo field = type.GetField (memberName, bindingFlags);
				if (field != null)
					return true;

				PropertyInfo prop = type.GetProperty (memberName, bindingFlags);
				if (prop != null) {
					MethodInfo getter = prop.CanRead ? prop.GetGetMethod (bindingFlags.HasFlag (BindingFlags.NonPublic)) : null;
					if (getter != null)
						return true;
				}

				if (bindingFlags.HasFlag (BindingFlags.DeclaredOnly))
					break;

				ct = ct.Base;
			}

			return false;
		}

		protected override IEnumerable<ValueReference> GetMembers (EvaluationContext ctx, object tt, object gval, BindingFlags bindingFlags)
		{
			CorType t = (CorType) tt;
			CorValRef val = (CorValRef) gval;

			if (t.Class == null)
				yield break;

			CorEvaluationContext cctx = (CorEvaluationContext) ctx;

			while (t != null) {
				Type type = t.GetTypeInfo (cctx.Session);

				foreach (FieldInfo field in type.GetFields (bindingFlags))
					yield return new FieldReference (ctx, val, t, field);

				foreach (PropertyInfo prop in type.GetProperties (bindingFlags)) {
					MethodInfo mi = null;
					try {
						mi = prop.CanRead ? prop.GetGetMethod () : null;
					} catch {
						// Ignore
					}
					if (mi != null && mi.GetParameters ().Length == 0)
						yield return new PropertyReference (ctx, prop, val, t);
				}
				if ((bindingFlags & BindingFlags.DeclaredOnly) != 0)
					break;
				t = t.Base;
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

		public override object TargetObjectToObject (EvaluationContext ctx, object objr)
		{
			CorValue obj = GetRealObject (ctx, objr);

			if ((obj is CorReferenceValue) && ((CorReferenceValue) obj).IsNull)
				return new EvaluationResult ("(null)");

			CorStringValue stringVal = obj as CorStringValue;
			if (stringVal != null)
				return stringVal.String;

			CorArrayValue arr = obj as CorArrayValue;
			if (arr != null)
                return base.TargetObjectToObject(ctx, objr);

			CorEvaluationContext cctx = (CorEvaluationContext) ctx;
			CorObjectValue co = obj as CorObjectValue;
			if (co != null)
                return base.TargetObjectToObject(ctx, objr);

			CorGenericValue genVal = obj as CorGenericValue;
			if (genVal != null)
				return genVal.GetValue ();

			return base.TargetObjectToObject (ctx, objr);
		}

		protected override ValueReference OnGetThisReference (EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			if (ctx.Frame.FrameType != CorFrameType.ILFrame || ctx.Frame.Function == null)
				return null;

			MethodInfo mi = ctx.Frame.Function.GetMethodInfo (ctx.Session);
			if (mi == null || mi.IsStatic)
				return null;

			CorValRef vref = new CorValRef (delegate {
				return ctx.Frame.GetArgument (0);
			});

			return new VariableReference (ctx, vref, "this", ObjectValueFlags.Variable | ObjectValueFlags.ReadOnly);
		}

		protected override IEnumerable<ValueReference> OnGetParameters (EvaluationContext gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			if (ctx.Frame.FrameType == CorFrameType.ILFrame && ctx.Frame.Function != null) {
				MethodInfo met = ctx.Frame.Function.GetMethodInfo (ctx.Session);
				if (met != null) {
					foreach (ParameterInfo pi in met.GetParameters ()) {
						int pos = pi.Position;
						if (met.IsStatic)
							pos--;
						CorValRef vref = null;
						try {
							vref = new CorValRef (delegate {
								return ctx.Frame.GetArgument (pos);
							});
						}
						catch (Exception /*ex*/) {
						}
						if (vref != null)
							yield return new VariableReference (ctx, vref, pi.Name, ObjectValueFlags.Parameter);
					}
					yield break;
				}
			}

			int count = ctx.Frame.GetArgumentCount ();
			for (int n = 0; n < count; n++) {
				int locn = n;
				CorValRef vref = new CorValRef (delegate {
					return ctx.Frame.GetArgument (locn);
				});
				yield return new VariableReference (ctx, vref, "arg_" + (n + 1), ObjectValueFlags.Parameter);
			}
		}

		protected override IEnumerable<ValueReference> OnGetLocalVariables (EvaluationContext ctx)
		{
			CorEvaluationContext wctx = (CorEvaluationContext) ctx;
			uint offset;
			CorDebugMappingResult mr;
			wctx.Frame.GetIP (out offset, out mr);
			return GetLocals (wctx, null, (int) offset, false);
		}
		
		public override ValueReference GetCurrentException (EvaluationContext ctx)
		{
			CorEvaluationContext wctx = (CorEvaluationContext) ctx;
			CorValue exception = wctx.Thread.CurrentException;
			
			if (exception != null)
			{
				CorHandleValue exceptionHandle = wctx.Session.GetHandle (exception);
				
				CorValRef vref = new CorValRef (delegate {
					return exceptionHandle;
				});
				
				return new VariableReference (ctx, vref, "__EXCEPTION_OBJECT__", ObjectValueFlags.Variable);
			}
			else
				return base.GetCurrentException(ctx);
		}

		IEnumerable<ValueReference> GetLocals (CorEvaluationContext ctx, ISymbolScope scope, int offset, bool showHidden)
		{
            if (ctx.Frame.FrameType != CorFrameType.ILFrame)
                yield break;

			if (scope == null) {
				ISymbolMethod met = ctx.Frame.Function.GetSymbolMethod (ctx.Session);
				if (met != null)
					scope = met.RootScope;
				else {
					int count = ctx.Frame.GetLocalVariablesCount ();
					for (int n = 0; n < count; n++) {
						int locn = n;
						CorValRef vref = new CorValRef (delegate {
							return ctx.Frame.GetLocalVariable (locn);
						});
						yield return new VariableReference (ctx, vref, "local_" + (n + 1), ObjectValueFlags.Variable);
					}
					yield break;
				}
			}

			foreach (ISymbolVariable var in scope.GetLocals ()) {
				if (var.Name == "$site")
					continue;
				if (var.Name.IndexOfAny(new char[] {'$','<','>'}) == -1 || showHidden) {
					int addr = var.AddressField1;
					CorValRef vref = new CorValRef (delegate {
						return ctx.Frame.GetLocalVariable (addr);
					});
					yield return new VariableReference (ctx, vref, var.Name, ObjectValueFlags.Variable);
				}
			}

			foreach (ISymbolScope cs in scope.GetChildren ()) {
				if (cs.StartOffset <= offset && cs.EndOffset >= offset) {
					foreach (VariableReference var in GetLocals (ctx, cs, offset, showHidden))
						yield return var;
				}
			}
		}

		protected override TypeDisplayData OnGetTypeDisplayData (EvaluationContext ctx, object gtype)
		{
			CorType type = (CorType) gtype;

			CorEvaluationContext wctx = (CorEvaluationContext) ctx;
			Type t = type.GetTypeInfo (wctx.Session);
			if (t == null)
				return null;

			string proxyType = null;
			string nameDisplayString = null;
			string typeDisplayString = null;
			string valueDisplayString = null;
			Dictionary<string, DebuggerBrowsableState> memberData = null;
			bool hasTypeData = false;

			foreach (object att in t.GetCustomAttributes (false)) {
				DebuggerTypeProxyAttribute patt = att as DebuggerTypeProxyAttribute;
				if (patt != null) {
					proxyType = patt.ProxyTypeName;
					hasTypeData = true;
					continue;
				}
				DebuggerDisplayAttribute datt = att as DebuggerDisplayAttribute;
				if (datt != null) {
					hasTypeData = true;
					nameDisplayString = datt.Name;
					typeDisplayString = datt.Type;
					valueDisplayString = datt.Value;
					continue;
				}
			}

			ArrayList mems = new ArrayList ();
			mems.AddRange (t.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
			mems.AddRange (t.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));

			foreach (MemberInfo m in mems) {
				object[] atts = m.GetCustomAttributes (typeof (DebuggerBrowsableAttribute), false);
				if (atts.Length == 0) {
					atts = m.GetCustomAttributes (typeof (System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);
					if (atts.Length > 0)
						atts[0] = new DebuggerBrowsableAttribute (DebuggerBrowsableState.Never);
				}
				if (atts.Length > 0) {
					hasTypeData = true;
					if (memberData == null) memberData = new Dictionary<string, DebuggerBrowsableState> ();
					memberData[m.Name] = ((DebuggerBrowsableAttribute)atts[0]).State;
				}
			}
			if (hasTypeData)
				return new TypeDisplayData (proxyType, valueDisplayString, typeDisplayString, nameDisplayString, false, memberData);
			else
				return null;
		}
	}
}
