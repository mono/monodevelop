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

using System;
using System.Reflection;
using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	class PropertyReference: ValueReference<CorValRef, CorType>
	{
		PropertyInfo prop;
		CorValRef thisobj;
		CorValRef index;
		CorModule module;
		CorValRef.ValueLoader loader;
		CorValRef cachedValue;

		public PropertyReference (EvaluationContext<CorValRef, CorType> ctx, PropertyInfo prop, CorValRef thisobj, CorModule module)
			: this (ctx, prop, thisobj, module, null)
		{
		}

		public PropertyReference (EvaluationContext<CorValRef, CorType> ctx, PropertyInfo prop, CorValRef thisobj, CorModule module, CorValRef index)
			: base (ctx)
		{
			this.prop = prop;
			this.thisobj = thisobj;
			this.module = module;
			this.index = index;

			loader = delegate {
				return Value.Val;
			};
		}
		
		public override CorType Type {
			get {
				if (!prop.CanRead)
					return null;
				return Value.Val.ExactType;
			}
		}
		
		public override CorValRef Value {
			get {
				if (cachedValue != null && cachedValue.IsValid)
					return cachedValue;
				if (!prop.CanRead)
					return null;
				CorEvaluationContext ctx = (CorEvaluationContext) Context;
				MethodInfo mi = prop.GetGetMethod ();
				CorValue[] args = index == null ? new CorValue[0] : new CorValue[] { index.Val };
				CorFunction func = module.GetFunctionFromToken (mi.MetadataToken);
				CorValue val = ctx.RuntimeInvoke (func, thisobj.Val.ExactType.TypeParameters, thisobj.Val, args);
				return cachedValue = new CorValRef (val, loader);
			}
			set {
				CorEvaluationContext ctx = (CorEvaluationContext) Context;
				CorFunction func = module.GetFunctionFromToken (prop.GetSetMethod ().MetadataToken);
				CorValue[] args = index == null ? new CorValue[] { value.Val } : new CorValue[] { index.Val, value.Val };
				ctx.RuntimeInvoke (func, thisobj.Val.ExactType.TypeParameters, thisobj.Val, args);
			}
		}
		
		public override string Name {
			get {
				if (index != null)
					return "[" + Context.Evaluator.TargetObjectToExpression (Context, index) + "]";
				else
					return prop.Name;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				ObjectValueFlags flags = ObjectValueFlags.Property;
				MethodInfo mi = prop.GetGetMethod () ?? prop.GetSetMethod ();
				if (mi.IsFamilyOrAssembly || mi.IsFamilyAndAssembly)
					flags |= ObjectValueFlags.Internal;
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
		}
	}
}
