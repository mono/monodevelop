// FieldVariable.cs
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

using System.Reflection;
using Microsoft.Samples.Debugging.CorDebug;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

namespace Mono.Debugging.Win32
{
	public class FieldReference: ValueReference
	{
		readonly CorType type;
		readonly FieldInfo field;
		readonly CorValRef thisobj;
		readonly CorValRef.ValueLoader loader;
		readonly ObjectValueFlags flags;
		readonly string vname;

		public FieldReference (EvaluationContext ctx, CorValRef thisobj, CorType type, FieldInfo field, string vname, ObjectValueFlags vflags) : base (ctx)
		{
			this.thisobj = thisobj;
			this.type = type;
			this.field = field;
			this.vname = vname;
			if (field.IsStatic)
				this.thisobj = null;

			flags = vflags | GetFlags (field);

			loader = delegate {
				return ((CorValRef)Value).Val;
			};
		}

		public FieldReference (EvaluationContext ctx, CorValRef thisobj, CorType type, FieldInfo field)
			: this (ctx, thisobj, type, field, null, ObjectValueFlags.Field)
		{
		}
		
		public override object Type {
			get {
				return ((CorValRef)Value).Val.ExactType;
			}
		}
		
		public override object DeclaringType {
			get {
				return type;
			}
		}

		public override object ObjectValue {
			get {
				if (field.IsLiteral && field.IsStatic)
					return field.GetValue (null);
				return base.ObjectValue;
			}
		}

		public override object Value {
			get {
				var ctx = (CorEvaluationContext) Context;
				CorValue val;
				if (thisobj != null && !field.IsStatic) {
					CorObjectValue cval;
					val = CorObjectAdaptor.GetRealObject (ctx, thisobj);
					if (val is CorObjectValue) {
						cval = (CorObjectValue)val;
						val = cval.GetFieldValue (type.Class, field.MetadataToken);
						return new CorValRef (val, loader);
					}
					if (val is CorReferenceValue) {
						CorReferenceValue rval = (CorReferenceValue)val;
						return new CorValRef (rval, loader);
					}
				}

				if (field.IsLiteral && field.IsStatic) {
					object oval = field.GetValue (null);
					CorObjectAdaptor ad = ctx.Adapter;
					// When getting enum members, convert the integer value to an enum value
					if (ad.IsEnum (ctx, type))
						return ad.CreateEnum (ctx, type, Context.Adapter.CreateValue (ctx, oval));

					return Context.Adapter.CreateValue (ctx, oval);
				}
				val = type.GetStaticFieldValue (field.MetadataToken, ctx.Frame);
				return new CorValRef (val, loader);
			}
			set {
				((CorValRef)Value).SetValue (Context, (CorValRef) value);
				if (thisobj != null) {
					CorObjectValue cob = CorObjectAdaptor.GetRealObject (Context, thisobj) as CorObjectValue;
					if (cob != null && cob.IsValueClass)
						thisobj.Invalidate (); // Required to make sure that thisobj returns an up-to-date value object
				}
			}
		}

		public override string Name {
			get {
				return vname ?? field.Name;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				return flags;
			}
		}

		internal static ObjectValueFlags GetFlags (FieldInfo field)
		{
			ObjectValueFlags flags = ObjectValueFlags.Field;

			if (field.IsStatic)
				flags |= ObjectValueFlags.Global;

			if (field.IsFamilyOrAssembly)
				flags |= ObjectValueFlags.InternalProtected;
			else if (field.IsFamilyAndAssembly)
				flags |= ObjectValueFlags.Internal;
			else if (field.IsFamily)
				flags |= ObjectValueFlags.Protected;
			else if (field.IsPublic)
				flags |= ObjectValueFlags.Public;
			else
				flags |= ObjectValueFlags.Private;

			if (field.IsLiteral)
				flags |= ObjectValueFlags.ReadOnly;

			return flags;
		}
	}
}
