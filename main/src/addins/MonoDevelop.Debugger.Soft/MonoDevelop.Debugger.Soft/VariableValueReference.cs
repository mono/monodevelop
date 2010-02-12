// 
// VariableValueReference.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Client;
using Mono.Debugger.Soft;

namespace MonoDevelop.Debugger.Soft
{
	public class VariableValueReference : ValueReference
	{
		string name;
		LocalVariable variable;
		
		public VariableValueReference (EvaluationContext ctx, string name, LocalVariable variable): base (ctx)
		{
			this.name = name;
			this.variable = variable;
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.Variable;
			}
		}

		public override string Name {
			get {
				return name;
			}
		}

		public override object Type {
			get {
				return variable.Type;
			}
		}

		public override object Value {
			get {
				SoftEvaluationContext ctx = (SoftEvaluationContext) Context;
				return ctx.Frame.GetValue (variable);
			}
			set {
				SoftEvaluationContext ctx = (SoftEvaluationContext) Context;
				ctx.Frame.SetValue (variable, (Value) value);
			}
		}
	}
}
