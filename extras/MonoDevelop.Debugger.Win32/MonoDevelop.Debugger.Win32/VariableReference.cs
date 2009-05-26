// VariableReference.cs
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
using DC = Mono.Debugging.Client;
using MonoDevelop.Debugger.Evaluation;
using Microsoft.Samples.Debugging.CorDebug;

namespace MonoDevelop.Debugger.Win32
{
	public class VariableReference: ValueReference<CorValRef,CorType>
	{
		CorValRef var;
		DC.ObjectValueFlags flags;
		string name;

		public VariableReference (EvaluationContext<CorValRef, CorType> ctx, CorValRef var, string name, DC.ObjectValueFlags flags)
			: base (ctx)
		{
			this.flags = flags;
			this.var = var;
			this.name = name;
		}
		
		public override CorValRef Value {
			get {
				return var;
			}
			set {
				var.SetValue (Context, value);
			}
		}
		
		public override string Name {
			get {
				return name;
			}
		}
		
		public override CorType Type {
			get {
				return var.Val.ExactType;
			}
		}
		
		public override DC.ObjectValueFlags Flags {
			get {
				return flags;
			}
		}
	}
}
