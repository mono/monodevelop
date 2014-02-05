// PropertyVariable.cs
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
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	class PropertyReference: ValueReference
	{
		readonly PropertyInfo prop;
		readonly CorValRef thisobj;
		readonly CorValRef[] index;
		readonly CorModule module;
		readonly CorType declaringType;
		readonly CorValRef.ValueLoader loader;
		readonly ObjectValueFlags flags;
		CorValRef cachedValue;

		public PropertyReference (EvaluationContext ctx, PropertyInfo prop, CorValRef thisobj, CorType declaringType)
			: this (ctx, prop, thisobj, declaringType, null)
		{
		}

		public PropertyReference (EvaluationContext ctx, PropertyInfo prop, CorValRef thisobj, CorType declaringType, CorValRef[] index)
			: base (ctx)
		{
			this.prop = prop;
			this.declaringType = declaringType;
			this.module = declaringType.Class.Module;
			this.index = index;
			if (!prop.GetGetMethod (true).IsStatic)
				this.thisobj = thisobj;

			flags = GetFlags (prop);

			loader = delegate {
				return ((CorValRef)Value).Val;
			};
		}
		
		public override object Type {
			get {
				if (!prop.CanRead)
					return null;
				return ((CorValRef)Value).Val.ExactType;
			}
		}
		
		public override object DeclaringType {
			get {
				return declaringType;
			}
		}
		
		public override object Value {
			get {
				if (cachedValue != null && cachedValue.IsValid)
					return cachedValue;
				if (!prop.CanRead)
					return null;
				CorEvaluationContext ctx = (CorEvaluationContext) Context;
				CorValue[] args;
				if (index != null) {
					args = new CorValue[index.Length];
					ParameterInfo[] metArgs = prop.GetGetMethod ().GetParameters ();
					for (int n = 0; n < index.Length; n++)
						args[n] = ctx.Adapter.GetBoxedArg (ctx, index[n], metArgs[n].ParameterType).Val;
				}
				else
					args = new CorValue[0];

				MethodInfo mi = prop.GetGetMethod ();
				CorFunction func = module.GetFunctionFromToken (mi.MetadataToken);
				CorValue val = ctx.RuntimeInvoke (func, declaringType.TypeParameters, thisobj != null ? thisobj.Val : null, args);
				return cachedValue = new CorValRef (val, loader);
			}
			set {
				CorEvaluationContext ctx = (CorEvaluationContext)Context;
				CorFunction func = module.GetFunctionFromToken (prop.GetSetMethod ().MetadataToken);
				CorValRef val = (CorValRef) value;
				CorValue[] args;
				ParameterInfo[] metArgs = prop.GetSetMethod ().GetParameters ();

				if (index == null)
					args = new CorValue[1];
				else {
					args = new CorValue [index.Length + 1];
					for (int n = 0; n < index.Length; n++) {
						args[n] = ctx.Adapter.GetBoxedArg (ctx, index[n], metArgs[n].ParameterType).Val;
					}
				}
				args[args.Length - 1] = ctx.Adapter.GetBoxedArg (ctx, val, metArgs[metArgs.Length - 1].ParameterType).Val;
				ctx.RuntimeInvoke (func, declaringType.TypeParameters, thisobj != null ? thisobj.Val : null, args);
			}
		}
		
		public override string Name {
			get {
				if (index != null) {
					System.Text.StringBuilder sb = new System.Text.StringBuilder ("[");
					foreach (CorValRef vr in index) {
						if (sb.Length > 1)
							sb.Append (",");
						sb.Append (Context.Evaluator.TargetObjectToExpression (Context, vr));
					}
					sb.Append ("]");
					return sb.ToString ();
				}
				return prop.Name;
			}
		}

		internal static ObjectValueFlags GetFlags (PropertyInfo prop)
		{
			ObjectValueFlags flags = ObjectValueFlags.Property;
			MethodInfo mi = prop.GetGetMethod () ?? prop.GetSetMethod ();

			if (prop.GetSetMethod (true) == null)
				flags |= ObjectValueFlags.ReadOnly;

			if (mi.IsStatic)
				flags |= ObjectValueFlags.Global;

			if (mi.IsFamilyAndAssembly)
				flags |= ObjectValueFlags.Internal;
			else if (mi.IsFamilyOrAssembly)
				flags |= ObjectValueFlags.InternalProtected;
			else if (mi.IsFamily)
				flags |= ObjectValueFlags.Protected;
			else if (mi.IsPublic)
				flags |= ObjectValueFlags.Public;
			else
				flags |= ObjectValueFlags.Private;

			if (!prop.CanWrite)
				flags |= ObjectValueFlags.ReadOnly;

			return flags;
		}

		public override ObjectValueFlags Flags {
			get {
				return flags;
			}
		}
	}
}
