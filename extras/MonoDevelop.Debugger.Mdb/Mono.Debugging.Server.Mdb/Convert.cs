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

		static TargetObject ImplicitFundamentalConversion (Thread thread,
								   TargetFundamentalObject obj,
								   TargetFundamentalType type)
		{
			FundamentalKind skind = obj.Type.FundamentalKind;
			FundamentalKind tkind = type.FundamentalKind;

			if (!ImplicitFundamentalConversionExists (skind, tkind))
				return null;

			object value = obj.GetObject (thread);

			object new_value = ImplicitFundamentalConversion (value, tkind);
			if (new_value == null)
				return null;

			return type.Language.CreateInstance (thread, new_value);
		}

		public static TargetObject ExplicitFundamentalConversion (StackFrame context,
									  TargetFundamentalObject obj,
									  TargetFundamentalType type)
		{
			TargetObject retval = ImplicitFundamentalConversion (context.Thread, obj, type);
			if (retval != null)
				return retval;

			FundamentalKind tkind = type.FundamentalKind;

			try {
				object value = obj.GetObject (context.Thread);
				object new_value = ImplicitFundamentalConversion (value, tkind);
				if (new_value == null)
					return null;

				return type.Language.CreateInstance (context.Thread, new_value);
			} catch {
				return null;
			}
		}

		static bool ImplicitReferenceConversionExists (Thread thread,
							       TargetStructType source,
							       TargetStructType target)
		{
			if (source == target)
				return true;

			if (!source.HasParent)
				return false;

			TargetStructType parent_type = source.GetParentType (thread);
			return ImplicitReferenceConversionExists (thread, parent_type, target);
		}

		static TargetObject ImplicitReferenceConversion (Thread thread,
								 TargetClassObject obj,
								 TargetClassType type)
		{
			if (obj.Type == type)
				return obj;

			if (!obj.Type.HasParent)
				return null;

			TargetObject pobj = obj.GetParentObject (thread);
			if (pobj != null)
				return ImplicitConversion (thread, pobj, type);
			else
				return null;
		}

		public static bool ImplicitConversionExists (Thread thread,
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
				if (ImplicitConversionExists (thread, e.Value.Type, target))
					return true;
			}

			if (target is TargetEnumType) {
				TargetEnumType e = (TargetEnumType) target;
				if (ImplicitConversionExists (thread, source, e.Value.Type))
					return true;
			}

			if ((source is TargetFundamentalType) && (target is TargetFundamentalType))
				return ImplicitFundamentalConversionExists (
					(TargetFundamentalType) source,
					(TargetFundamentalType) target);

			if ((source is TargetClassType) && (target is TargetClassType))
				return ImplicitReferenceConversionExists (
					thread, (TargetClassType) source,
					(TargetClassType) target);

			return false;
		}

		public static TargetObject ImplicitConversion (Thread thread,
							       TargetObject obj, TargetType type)
		{
			if (obj.Type.Equals (type))
				return obj;
			
			if (type is TargetObjectType || ObjectUtil.FixTypeName (type.Name) == "System.Object") {
				if (obj.Type.IsByRef)
					return obj;
				return BoxValue (thread.CurrentFrame, obj);
			}

			if (obj is TargetEnumObject && type is TargetFundamentalType) {
				TargetEnumObject e = (TargetEnumObject) obj;
				return ImplicitConversion (thread, e.GetValue (thread), type);
			}

			if (type is TargetEnumType) {
				TargetEnumType e = (TargetEnumType) type;
				return ImplicitConversion (thread, obj, e.Value.Type);
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
					thread, (TargetFundamentalObject) obj,
					(TargetFundamentalType) type);

			if ((obj is TargetClassObject) && (type is TargetClassType)) {
				return ImplicitReferenceConversion (
					thread, (TargetClassObject) obj,
					(TargetClassType) type);
			}

			return null;
		}

		public static TargetObject ImplicitConversionRequired (Thread thread,
								       TargetObject obj, TargetType type)
		{
			TargetObject new_obj = ImplicitConversion (thread, obj, type);
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

		public static TargetClassObject ToClassObject (Thread target, TargetObject obj)
		{
			TargetClassObject cobj = obj as TargetClassObject;
			if (cobj != null)
				return cobj;

			TargetObjectObject oobj = obj as TargetObjectObject;
			if (oobj != null)
				return oobj.GetClassObject (target);

			TargetArrayObject aobj = obj as TargetArrayObject;
			if ((aobj != null) && aobj.HasClassObject)
				return aobj.GetClassObject (target);

			return null;
		}

		public static TargetStructObject ToStructObject (Thread target, TargetObject obj)
		{
			TargetStructObject sobj = obj as TargetStructObject;
			if (sobj != null)
				return sobj;

			TargetObjectObject oobj = obj as TargetObjectObject;
			if (oobj != null)
				return oobj.GetClassObject (target);

			TargetArrayObject aobj = obj as TargetArrayObject;
			if ((aobj != null) && aobj.HasClassObject)
				return aobj.GetClassObject (target);

			return null;
		}
		
		public static TargetObject Cast (StackFrame frame, TargetObject obj, TargetType targetType)
		{
			obj = ObjectUtil.GetRealObject (frame.Thread, obj);
			
			if (obj.Type == targetType)
				return obj;
			
			if (targetType is TargetObjectType || ObjectUtil.FixTypeName (targetType.Name) == "System.Object") {
				if (obj.Type.IsByRef)
					return obj;
				return BoxValue (frame, obj);
			}
			
			if (targetType is TargetPointerType)
				throw new NotSupportedException ();

			if (targetType is TargetFundamentalType) {
				TargetFundamentalObject fobj = obj as TargetFundamentalObject;
				if (fobj == null)
					throw new NotSupportedException ();

				TargetFundamentalType ftype = targetType as TargetFundamentalType;
				TargetObject ob = ExplicitFundamentalConversion (frame, fobj, ftype);
				if (ob == null)
					throw new NotSupportedException ();
				return ob;
			}

			TargetClassType ctype = ToClassType (targetType);
			TargetClassObject source = ToClassObject (frame.Thread, obj);

			if (source == null)
				throw new Exception (string.Format ("Variable is not a class type."));

			return TryCast (frame, source, ctype);
		}
		
		static TargetObject BoxValue (StackFrame frame, TargetObject fobj)
		{
			return frame.Language.CreateBoxedObject (frame.Thread, fobj);
		}
		
		static TargetStructObject TryParentCast (StackFrame frame, TargetStructObject source, TargetStructType source_type, TargetStructType target_type)
		{
			if (source_type == target_type)
				return source;

			if (!source_type.HasParent)
				return null;

			TargetStructType parent_type = source_type.GetParentType (frame.Thread);
			source = TryParentCast (frame, source, parent_type, target_type);
			if (source == null)
				return null;

			return source.GetParentObject (frame.Thread) as TargetClassObject;
		}

		static TargetStructObject TryCurrentCast (StackFrame frame, TargetClassObject source, TargetClassType target_type)
		{
			TargetStructObject current = source.GetCurrentObject (frame.Thread);
			if (current == null)
				return null;

			return TryParentCast (frame, current, current.Type, target_type);
		}

		public static TargetObject TryCast (StackFrame frame, TargetObject source, TargetClassType target_type)
		{
			if (source.Type == target_type)
				return source;

			TargetClassObject sobj = ToClassObject (frame.Thread, source);
			if (sobj == null)
				return null;

			TargetStructObject result = TryParentCast (frame, sobj, sobj.Type, target_type);
			if (result != null)
				return result;

			return TryCurrentCast (frame, sobj, target_type);
		}

		static bool TryParentCast (StackFrame frame, TargetStructType source_type, TargetStructType target_type)
		{
			if (source_type == target_type)
				return true;

			if (!source_type.HasParent)
				return false;

			TargetStructType parent_type = source_type.GetParentType (frame.Thread);
			return TryParentCast (frame, parent_type, target_type);
		}

		public static bool TryCast (StackFrame frame, TargetType source, TargetClassType target_type)
		{
			if (source == target_type)
				return true;

			TargetClassType stype = ToClassType (source);
			if (stype == null)
				return false;

			return TryParentCast (frame, stype, target_type);
		}
	}
}
