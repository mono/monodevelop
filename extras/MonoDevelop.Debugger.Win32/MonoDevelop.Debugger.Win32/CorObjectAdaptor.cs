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
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SR = System.Reflection;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;
using Microsoft.Samples.Debugging.CorDebug;
using MonoDevelop.Debugger.Evaluation;
using CorElementType = Microsoft.Samples.Debugging.CorDebug.NativeApi.CorElementType;
using CorDebugMappingResult = Microsoft.Samples.Debugging.CorDebug.NativeApi.CorDebugMappingResult;
using System.Diagnostics.SymbolStore;

namespace MonoDevelop.Debugger.Win32
{
	public class CorObjectAdaptor: ObjectValueAdaptor<CorValue,CorType>
	{
		static Dictionary<CorElementType, Type> coreTypes = new Dictionary<CorElementType, Type> ();

		static CorObjectAdaptor ( )
		{
			coreTypes.Add (CorElementType.ELEMENT_TYPE_BOOLEAN, typeof (bool));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_CHAR, typeof (char));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_I1, typeof (sbyte));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_U1, typeof (byte));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_I2, typeof (short));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_U2, typeof (ushort));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_I4, typeof (int));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_U4, typeof (uint));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_I8, typeof (long));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_U8, typeof (ulong));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_R4, typeof (float));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_R8, typeof (double));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_STRING, typeof (string));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_I, typeof (IntPtr));
			coreTypes.Add (CorElementType.ELEMENT_TYPE_U, typeof (UIntPtr));
		}

		public override bool IsPrimitive (CorValue val)
		{
			return val is CorGenericValue;
		}

		public override bool IsArray (CorValue val)
		{
			return val is CorArrayValue;
		}

		public override bool IsClass (CorType val)
		{
			return val.Class != null;
		}

		public override bool IsClassInstance (CorValue val)
		{
			return val is CorObjectValue;
		}

		public override string GetTypeName (CorType type)
		{
			Type t;
			if (coreTypes.TryGetValue (type.Type, out t))
				return t.FullName;
			try {
				if (type.Type == CorElementType.ELEMENT_TYPE_ARRAY || type.Type == CorElementType.ELEMENT_TYPE_SZARRAY)
					return GetTypeName (type.FirstTypeParameter) + "[]";

				if (type.Type == CorElementType.ELEMENT_TYPE_BYREF)
					return GetTypeName (type.FirstTypeParameter) + "&";

				if (type.Type == CorElementType.ELEMENT_TYPE_PTR)
					return GetTypeName (type.FirstTypeParameter) + "*";

				return type.Class.GetTypeInfo ().Name;
			}
			catch (Exception ex) {
				Console.WriteLine (ex);
				throw;
			}
		}

		public override CorType GetValueType (CorValue val)
		{
			return val.ExactType;
		}

		public override ICollectionAdaptor<CorValue, CorType> CreateArrayAdaptor (EvaluationContext<CorValue, CorType> ctx, CorValue arr)
		{
			if (arr is CorArrayValue)
				return new ArrayAdaptor (ctx, (CorArrayValue) arr);
			else
				return null;
		}

		public override CorValue GetRealObject (EvaluationContext<CorValue, CorType> ctx, CorValue obj)
		{
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
					else
						return GetRealObject (ctx, refVal.Dereference ());
				}

				CorBoxValue boxVal = obj.CastToBoxValue ();
				if (boxVal != null)
					return GetRealObject (ctx, boxVal.GetObject ());

				if (obj.ExactType.Type == CorElementType.ELEMENT_TYPE_STRING)
					return obj.CastToStringValue ();

				if (coreTypes.ContainsKey (obj.Type)) {
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

		protected override ObjectValue CreateObjectValueImpl (EvaluationContext<CorValue, CorType> ctx, IObjectValueSource source, ObjectPath path, CorValue obj, ObjectValueFlags flags)
		{
			obj = GetRealObject (ctx, obj);
			
			if (obj == null)
				return ObjectValue.CreateObject (null, path, "", null, flags | ObjectValueFlags.ReadOnly, null);

			if ((obj is CorReferenceValue) && ((CorReferenceValue)obj).IsNull)
				return ObjectValue.CreateObject (null, path, GetTypeName (obj.ExactType), "(null)", flags, null);

			if ((obj is CorArrayValue) || (obj is CorObjectValue))
				return ObjectValue.CreateObject (source, path, GetTypeName (obj.ExactType), ctx.Evaluator.TargetObjectToExpression (ctx, obj), flags, null);

			CorGenericValue genVal = obj as CorGenericValue;
			if (genVal != null) {
				return ObjectValue.CreatePrimitive (source, path, GetTypeName (obj.ExactType), ctx.Evaluator.TargetObjectToExpression (ctx, obj), flags);
			}

			CorStringValue sVal = obj as CorStringValue;
			if (sVal != null)
				return ObjectValue.CreatePrimitive (source, path, "System.String", ctx.Evaluator.TargetObjectToExpression (ctx, obj), flags);

			return ObjectValue.CreateError (path.LastName, "Unknown value type: " + obj.Type, flags);
		}
		
		public override object StringToObject (CorType type, string value)
		{
			switch (type.Type) {
				case CorElementType.ELEMENT_TYPE_BOOLEAN: return bool.Parse (value);
				case CorElementType.ELEMENT_TYPE_U1: return byte.Parse (value);
				case CorElementType.ELEMENT_TYPE_CHAR: return char.Parse (value);
				case CorElementType.ELEMENT_TYPE_R8: return double.Parse (value);
				case CorElementType.ELEMENT_TYPE_I2: return short.Parse (value);
				case CorElementType.ELEMENT_TYPE_I4: return int.Parse (value);
				case CorElementType.ELEMENT_TYPE_I8: return long.Parse (value);
				case CorElementType.ELEMENT_TYPE_I: return new IntPtr (long.Parse (value));
				case CorElementType.ELEMENT_TYPE_I1: return sbyte.Parse (value);
				case CorElementType.ELEMENT_TYPE_R4: return float.Parse (value);
				case CorElementType.ELEMENT_TYPE_STRING: return value;
				case CorElementType.ELEMENT_TYPE_U2: return ushort.Parse (value);
				case CorElementType.ELEMENT_TYPE_U4: return uint.Parse (value);
				case CorElementType.ELEMENT_TYPE_U8: return ulong.Parse (value);
				case CorElementType.ELEMENT_TYPE_U: return new UIntPtr (ulong.Parse (value));
			}
			throw new InvalidOperationException ("Value '" + value + "' can't be converted to type '" + GetTypeName (type) + "'");
		}

		public override IEnumerable<ValueReference<CorValue, CorType>> GetMembers (EvaluationContext<CorValue, CorType> ctx, CorType t, CorValue val, ReqMemberAccess access)
		{
			if (t.Class == null)
				yield break;

			CorObjectValue co = val.CastToObjectValue ();
			CorModule module = co.ExactType.Class.Module;

			Type type = t.Class.GetTypeInfo ();

			foreach (FieldInfo field in type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				yield return new FieldReference (ctx, co, t, field);

			foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (prop.CanRead && (prop.GetGetMethod ().GetParameters ().Length == 0))
					yield return new PropertyReference (ctx, prop, co, module);
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

		public override object TargetObjectToObject (EvaluationContext<CorValue, CorType> ctx, CorValue obj)
		{
			obj = GetRealObject (ctx, obj);

			if ((obj is CorReferenceValue) && ((CorReferenceValue) obj).IsNull)
				return new LiteralExp ("(null)");

			CorStringValue stringVal = obj as CorStringValue;
			if (stringVal != null)
				return stringVal.String;

			CorArrayValue arr = obj as CorArrayValue;
			if (arr != null) {
				StringBuilder tn = new StringBuilder (GetTypeName (arr.ExactType.FirstTypeParameter));
				tn.Append ("[");
				int[] dims = arr.GetDimensions ();
				for (int n = 0; n < dims.Length; n++) {
					if (n > 0)
						tn.Append (',');
					tn.Append (dims [n]);
				}
				tn.Append ("]");
				return new LiteralExp (tn.ToString ());
			}

			CorObjectValue co = obj as CorObjectValue;
			if (co != null) {
				TypeDisplayData tdata = GetTypeDisplayData (ctx, co.ExactType);
				if (co == null)
					return null;
				if (co.Class.GetTypeInfo().Name == "System.Decimal")
					return new LiteralExp (CallToString (ctx, co));
				if (tdata.ValueDisplayString != null)
					return new LiteralExp (EvaluateDisplayString (ctx, co, tdata.ValueDisplayString));

				// Try using a collection adaptor
				ICollectionAdaptor<CorValue, CorType> col = CreateArrayAdaptor (ctx, co);
				if (col != null)
					return new LiteralExp (ArrayElementGroup<CorValue, CorType>.GetArrayDescription (col.GetDimensions ()));

				// Return the type name
				if (tdata.TypeDisplayString != null)
					return new LiteralExp ("{" + tdata.TypeDisplayString + "}");
				return new LiteralExp ("{" + GetTypeName (co.ExactType) + "}");
			}

			CorGenericValue genVal = obj as CorGenericValue;
			if (genVal != null) {
				return genVal.GetValue ();
			}

			return new LiteralExp ("?");
		}

		public override ValueReference<CorValue, CorType> GetThisReference (EvaluationContext<CorValue, CorType> gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			if (ctx.Frame.FrameType != CorFrameType.ILFrame || ctx.Frame.Function == null)
				return null;

			MethodInfo mi = ctx.Frame.Function.GetMethodInfo (ctx.Session);
			if (mi == null || mi.IsStatic)
				return null;

			return new VariableReference (ctx, ctx.Frame.GetArgument (0), "this", ObjectValueFlags.Variable | ObjectValueFlags.ReadOnly);
		}

		public override IEnumerable<ValueReference<CorValue, CorType>> GetParameters (EvaluationContext<CorValue, CorType> gctx)
		{
			CorEvaluationContext ctx = (CorEvaluationContext) gctx;
			if (ctx.Frame.Function != null) {
				MethodInfo met = ctx.Frame.Function.GetMethodInfo (ctx.Session);
				if (met != null) {
					foreach (ParameterInfo pi in met.GetParameters ()) {
						CorValue cv = ctx.Frame.GetArgument (pi.Position - 1);
						yield return new VariableReference (ctx, cv, pi.Name, ObjectValueFlags.Parameter);
					}
					yield break;
				}
			}

			int count = ctx.Frame.GetArgumentCount ();
			for (int n = 0; n < count; n++) {
				CorValue val = ctx.Frame.GetArgument (n);
				yield return new VariableReference (ctx, val, "arg_" + (n + 1), ObjectValueFlags.Parameter);
			}
		}

		public override IEnumerable<ValueReference<CorValue,CorType>> GetLocalVariables (EvaluationContext<CorValue, CorType> ctx)
		{
			CorEvaluationContext wctx = (CorEvaluationContext) ctx;
			uint offset;
			CorDebugMappingResult mr;
			wctx.Frame.GetIP (out offset, out mr);
			return GetLocals (wctx, null, (int) offset, false);
		}

		IEnumerable<ValueReference<CorValue, CorType>> GetLocals (CorEvaluationContext ctx, ISymbolScope scope, int offset, bool showHidden)
		{
			if (scope == null) {
				ISymbolMethod met = ctx.Frame.Function.GetSymbolMethod (ctx.Session);
				if (met != null)
					scope = met.RootScope;
				else {
					int count = ctx.Frame.GetLocalVariablesCount ();
					for (int n = 0; n < count; n++) {
						CorValue val = ctx.Frame.GetLocalVariable (n);
						yield return new VariableReference (ctx, val, "local_" + (n + 1), ObjectValueFlags.Variable);
					}
					yield break;
				}
			}

			foreach (ISymbolVariable var in scope.GetLocals ()) {
				if (var.Name == "$site")
					continue;
				if (!var.Name.StartsWith ("$") || showHidden) {
					CorValue cv = ctx.Frame.GetLocalVariable (var.AddressField1);
					yield return new VariableReference (ctx, cv, var.Name, ObjectValueFlags.Variable);
				}
			}

			foreach (ISymbolScope cs in scope.GetChildren ()) {
				if (cs.StartOffset <= offset && cs.EndOffset >= offset) {
					foreach (VariableReference var in GetLocals (ctx, cs, offset, showHidden))
						yield return var;
				}
			}
		}
	}
}
