// Convert.cs
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
using Mono.Debugger;
using Mono.Debugger.Languages;

namespace DebuggerServer
{
	public static class TargetObjectConvert
	{
		static bool ImplicitFundamentalConversionExists (FundamentalKind skind,
								 FundamentalKind tkind)
		{
			//
			// See Convert.ImplicitStandardConversionExists in MCS.
			//
			switch (skind) {
			case FundamentalKind.SByte:
				if ((tkind == FundamentalKind.Int16) ||
				    (tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Byte:
				if ((tkind == FundamentalKind.Int16) ||
				    (tkind == FundamentalKind.UInt16) ||
				    (tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.UInt32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Int16:
				if ((tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.UInt16:
				if ((tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.UInt32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Int32:
				if ((tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.UInt32:
				if ((tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Int64:
			case FundamentalKind.UInt64:
				if ((tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Char:
				if ((tkind == FundamentalKind.UInt16) ||
				    (tkind == FundamentalKind.Int32) ||
				    (tkind == FundamentalKind.UInt32) ||
				    (tkind == FundamentalKind.Int64) ||
				    (tkind == FundamentalKind.UInt64) ||
				    (tkind == FundamentalKind.Single) ||
				    (tkind == FundamentalKind.Double))
					return true;
				break;

			case FundamentalKind.Single:
				if (tkind == FundamentalKind.Double)
					return true;
				break;

			default:
				break;
			}

			return false;
		}

		static bool ImplicitFundamentalConversionExists (TargetFundamentalType source, TargetFundamentalType target)
		{
			return ImplicitFundamentalConversionExists (
				source.FundamentalKind, target.FundamentalKind);
		}

		static object ImplicitFundamentalConversion (object value, FundamentalKind tkind)
		{
			switch (tkind) {
			case FundamentalKind.Char:
				return System.Convert.ToChar (value);
			case FundamentalKind.SByte:
				return System.Convert.ToSByte (value);
			case FundamentalKind.Byte:
				return System.Convert.ToByte (value);
			case FundamentalKind.Int16:
				return System.Convert.ToInt16 (value);
			case FundamentalKind.UInt16:
				return System.Convert.ToUInt16 (value);
			case FundamentalKind.Int32:
				return System.Convert.ToInt32 (value);
			case FundamentalKind.UInt32:
				return System.Convert.ToUInt32 (value);
			case FundamentalKind.Int64:
				return System.Convert.ToInt64 (value);
			case FundamentalKind.UInt64:
				return System.Convert.ToUInt64 (value);
			case FundamentalKind.Single:
				return System.Convert.ToSingle (value);
			case FundamentalKind.Double:
				return System.Convert.ToDouble (value);
			case FundamentalKind.String:
				return System.Convert.ToString (value);
			default:
				return null;
			}
		}

		static TargetObject ImplicitFundamentalConversion (EvaluationContext ctx,
								   TargetFundamentalObject obj,
								   TargetFundamentalType type)
		{
			FundamentalKind skind = obj.Type.FundamentalKind;
			FundamentalKind tkind = type.FundamentalKind;

			if (!ImplicitFundamentalConversionExists (skind, tkind))
				return null;

			object value = obj.GetObject (ctx.Thread);

			object new_value = ImplicitFundamentalConversion (value, tkind);
			if (new_value == null)
				return null;

			return type.Language.CreateInstance (ctx.Thread, new_value);
		}

		public static TargetObject ExplicitFundamentalConversion (EvaluationContext ctx,
									  TargetFundamentalObject obj,
									  TargetFundamentalType type)
		{
			TargetObject retval = ImplicitFundamentalConversion (ctx, obj, type);
			if (retval != null)
				return retval;

			FundamentalKind tkind = type.FundamentalKind;

			try {
				object value = obj.GetObject (ctx.Thread);
				object new_value = ImplicitFundamentalConversion (value, tkind);
				if (new_value == null)
					return null;

				return type.Language.CreateInstance (ctx.Thread, new_value);
			} catch {
				return null;
			}
		}

		static bool ImplicitReferenceConversionExists (EvaluationContext ctx,
							       TargetStructType source,
							       TargetStructType target)
		{
			if (source == target)
				return true;

			if (!source.HasParent)
				return false;

			TargetStructType parent_type = source.GetParentType (ctx.Thread);
			return ImplicitReferenceConversionExists (ctx, parent_type, target);
		}

		static TargetObject ImplicitReferenceConversion (EvaluationContext ctx,
								 TargetClassObject obj,
								 TargetClassType type)
		{
			if (obj.Type == type)
				return obj;

			if (!obj.Type.HasParent)
				return null;

			TargetObject pobj = obj.GetParentObject (ctx.Thread);
			if (pobj != null)
				return ImplicitConversion (ctx, pobj, type);
			else
				return null;
		}

		public static bool ImplicitConversionExists (EvaluationContext ctx,
							     TargetType source, TargetType target)
		{
			if (source.Equals (target))
				return true;
			
			if (source is TargetArrayType && target.Name == "System.Array")
				return true;

			if (ObjectUtil.FixTypeName (target.Name) == "System.Object")
				return true;
			
			if (source is TargetArrayType && target is TargetArrayType) {
				TargetArrayType sa = (TargetArrayType) source;
				TargetArrayType ta = (TargetArrayType) target;
				return sa.ElementType.Equals (ta.ElementType);
			}
			
			if (source is TargetEnumType) {
				TargetEnumType e = (TargetEnumType) source;
				if (ImplicitConversionExists (ctx, e.Value.Type, target))
					return true;
			}

			if (target is TargetEnumType) {
				TargetEnumType e = (TargetEnumType) target;
				if (ImplicitConversionExists (ctx, source, e.Value.Type))
					return true;
			}

			if ((source is TargetFundamentalType) && (target is TargetFundamentalType))
				return ImplicitFundamentalConversionExists (
					(TargetFundamentalType) source,
					(TargetFundamentalType) target);

			if ((source is TargetClassType) && (target is TargetClassType))
				return ImplicitReferenceConversionExists (
					ctx, (TargetClassType) source,
					(TargetClassType) target);

			return false;
		}

		public static TargetObject ImplicitConversion (EvaluationContext ctx,
							       TargetObject obj, TargetType type)
		{
			if (obj.Type.Equals (type))
				return obj;
			
			if (type is TargetObjectType || ObjectUtil.FixTypeName (type.Name) == "System.Object") {
				if (obj.Type.IsByRef)
					return obj;
				return BoxValue (ctx, obj);
			}

			if (obj is TargetEnumObject && type is TargetFundamentalType) {
				TargetEnumObject e = (TargetEnumObject) obj;
				return ImplicitConversion (ctx, e.GetValue (ctx.Thread), type);
			}

			if (type is TargetEnumType) {
				TargetEnumType e = (TargetEnumType) type;
				return ImplicitConversion (ctx, obj, e.Value.Type);
			}
			
			if (obj is TargetArrayObject && type.Name == "System.Array") {
				return obj;
			}
			
			if (obj is TargetArrayObject && type is TargetArrayType) {
				TargetArrayObject sa = (TargetArrayObject) obj;
				TargetArrayType ta = (TargetArrayType) type;
				if (sa.Type.ElementType.Equals (ta.ElementType))
					return obj;
			}
			
			if ((obj is TargetFundamentalObject) && (type is TargetFundamentalType))
				return ImplicitFundamentalConversion (
					ctx, (TargetFundamentalObject) obj,
					(TargetFundamentalType) type);

			if ((obj is TargetClassObject) && (type is TargetClassType)) {
				return ImplicitReferenceConversion (
					ctx, (TargetClassObject) obj,
					(TargetClassType) type);
			}

			return null;
		}

		public static TargetObject ImplicitConversionRequired (EvaluationContext ctx,
								       TargetObject obj, TargetType type)
		{
			TargetObject new_obj = ImplicitConversion (ctx, obj, type);
			if (new_obj != null)
				return new_obj;

			throw new Exception (string.Format ("Cannot implicitly convert `{0}' to `{1}'", obj.Type.Name, type.Name));
		}

		public static TargetClassType ToClassType (TargetType type)
		{
			TargetClassType ctype = type as TargetClassType;
			if (ctype != null)
				return ctype;

			TargetObjectType otype = type as TargetObjectType;
			if (otype != null && otype.HasClassType) {
				ctype = otype.ClassType;
				if (ctype != null)
					return ctype;
			}

			TargetArrayType atype = type as TargetArrayType;
			if (atype != null) {
				if (atype.Language.ArrayType != null)
					return atype.Language.ArrayType;
			}

			throw new Exception (string.Format ("Type `{0}' is not a struct or class.", type.Name));
		}

		public static TargetClassObject ToClassObject (EvaluationContext ctx, TargetObject obj)
		{
			TargetClassObject cobj = obj as TargetClassObject;
			if (cobj != null)
				return cobj;

			TargetObjectObject oobj = obj as TargetObjectObject;
			if (oobj != null)
				return oobj.GetClassObject (ctx.Thread);

			TargetArrayObject aobj = obj as TargetArrayObject;
			if ((aobj != null) && aobj.HasClassObject)
				return aobj.GetClassObject (ctx.Thread);

			return null;
		}

		public static TargetStructObject ToStructObject (EvaluationContext ctx, TargetObject obj)
		{
			TargetStructObject sobj = obj as TargetStructObject;
			if (sobj != null)
				return sobj;

			TargetObjectObject oobj = obj as TargetObjectObject;
			if (oobj != null)
				return oobj.GetClassObject (ctx.Thread);

			TargetArrayObject aobj = obj as TargetArrayObject;
			if ((aobj != null) && aobj.HasClassObject)
				return aobj.GetClassObject (ctx.Thread);

			return null;
		}
		
		public static TargetObject Cast (EvaluationContext ctx, TargetObject obj, TargetType targetType)
		{
			obj = ObjectUtil.GetRealObject (ctx, obj);
			
			if (obj.Type == targetType)
				return obj;
			
			if (targetType is TargetObjectType || ObjectUtil.FixTypeName (targetType.Name) == "System.Object") {
				if (obj.Type.IsByRef)
					return obj;
				return BoxValue (ctx, obj);
			}
			
			if (targetType is TargetPointerType)
				throw new NotSupportedException ();

			if (targetType is TargetFundamentalType) {
				TargetFundamentalObject fobj = obj as TargetFundamentalObject;
				if (fobj == null)
					throw new NotSupportedException ();

				TargetFundamentalType ftype = targetType as TargetFundamentalType;
				TargetObject ob = ExplicitFundamentalConversion (ctx, fobj, ftype);
				if (ob == null)
					throw new NotSupportedException ();
				return ob;
			}

			TargetClassType ctype = ToClassType (targetType);
			TargetClassObject source = ToClassObject (ctx, obj);

			if (source == null)
				throw new Exception (string.Format ("Variable is not a class type."));

			return TryCast (ctx, source, ctype);
		}
		
		static TargetObject BoxValue (EvaluationContext ctx, TargetObject fobj)
		{
			return ctx.Frame.Language.CreateBoxedObject (ctx.Thread, fobj);
		}
		
		static TargetStructObject TryParentCast (EvaluationContext ctx, TargetStructObject source, TargetStructType source_type, TargetStructType target_type)
		{
			if (source_type == target_type)
				return source;

			if (!source_type.HasParent)
				return null;

			TargetStructType parent_type = source_type.GetParentType (ctx.Thread);
			source = TryParentCast (ctx, source, parent_type, target_type);
			if (source == null)
				return null;

			return source.GetParentObject (ctx.Thread) as TargetClassObject;
		}

		static TargetStructObject TryCurrentCast (EvaluationContext ctx, TargetClassObject source, TargetClassType target_type)
		{
			TargetStructObject current = source.GetCurrentObject (ctx.Thread);
			if (current == null)
				return null;

			return TryParentCast (ctx, current, current.Type, target_type);
		}

		public static TargetObject TryCast (EvaluationContext ctx, TargetObject source, TargetClassType target_type)
		{
			if (source.Type == target_type)
				return source;

			TargetClassObject sobj = ToClassObject (ctx, source);
			if (sobj == null)
				return null;

			TargetStructObject result = TryParentCast (ctx, sobj, sobj.Type, target_type);
			if (result != null)
				return result;

			return TryCurrentCast (ctx, sobj, target_type);
		}

		static bool TryParentCast (EvaluationContext ctx, TargetStructType source_type, TargetStructType target_type)
		{
			if (source_type == target_type)
				return true;

			if (!source_type.HasParent)
				return false;

			TargetStructType parent_type = source_type.GetParentType (ctx.Thread);
			return TryParentCast (ctx, parent_type, target_type);
		}

		public static bool TryCast (EvaluationContext ctx, TargetType source, TargetClassType target_type)
		{
			if (source == target_type)
				return true;

			TargetClassType stype = ToClassType (source);
			if (stype == null)
				return false;

			return TryParentCast (ctx, stype, target_type);
		}
	}
}
