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
using MonoDevelop.Debugger.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	class PropertyReference: ValueReference<CorValue, CorType>
	{
		PropertyInfo prop;
		CorObjectValue thisobj;
		CorModule module;

		public PropertyReference (EvaluationContext<CorValue, CorType> ctx, PropertyInfo prop, CorObjectValue thisobj, CorModule module)
			: base (ctx)
		{
			this.prop = prop;
			this.thisobj = thisobj;
			this.module = module;
		}
		
		public override CorType Type {
			get {
				if (!prop.CanRead)
					return null;
				return Value.ExactType;
			}
		}
		
		public override CorValue Value {
			get {
				if (!prop.CanRead)
					return null;
				CorEvaluationContext ctx = (CorEvaluationContext) Context;
				CorFunction func = thisobj.ExactType.Class.Module.GetFunctionFromToken (prop.GetGetMethod ().MetadataToken);
				CorValue val = ctx.RuntimeInvoke (func, thisobj, new CorValue[0]);
				return Context.Adapter.GetRealObject (Context, val);
			}
			set {
				CorEvaluationContext ctx = (CorEvaluationContext) Context;
				CorFunction func = thisobj.ExactType.Class.Module.GetFunctionFromToken (prop.GetSetMethod ().MetadataToken);
				ctx.RuntimeInvoke (func, thisobj, new CorValue[] { value });
			}
		}
		
		public override string Name {
			get {
				return prop.Name;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				ObjectValueFlags flags = ObjectValueFlags.Field;
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
