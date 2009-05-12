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

using System;
using System.Reflection;
using Mono.Debugging.Client;
using MonoDevelop.Debugger.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	public class FieldReference: ValueReference<CorValue, CorType>
	{
		CorType type;
		FieldInfo field;
		CorObjectValue thisobj;

		public FieldReference (EvaluationContext<CorValue, CorType> ctx, CorObjectValue thisobj, CorType type, FieldInfo field)
			: base (ctx)
		{
			this.type = type;
			this.field = field;
			if (!field.IsStatic)
				this.thisobj = thisobj;
		}
		
		public override CorType Type {
			get {
				return GetValue ().ExactType;
			}
		}

		public override CorValue Value {
			get {
				CorValue val = GetValue ();
				return Context.Adapter.GetRealObject (Context, val);
			}
			set {
				CorValue val = GetValue ();
				CorGenericValue gval = val.CastToGenericValue ();
				if (gval != null)
					gval.SetValue (Context.Adapter.TargetObjectToObject (Context, value));
			}
		}

		CorValue GetValue ( )
		{
			CorEvaluationContext ctx = (CorEvaluationContext) Context;
			if (thisobj != null)
				return thisobj.GetFieldValue (type.Class, field.MetadataToken);
			else
				return type.Class.GetStaticFieldValue (field.MetadataToken, ctx.Frame);
		}
		
		public override string Name {
			get {
				return field.Name;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				ObjectValueFlags flags = ObjectValueFlags.Field;
				if (field.IsFamilyOrAssembly || field.IsFamilyAndAssembly)
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
}
