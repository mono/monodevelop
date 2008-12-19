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
using Mono.Debugger.Languages;
using Mono.Debugger;
using DC = Mono.Debugging.Client;

namespace DebuggerServer
{
	public class VariableReference: ValueReference
	{
		TargetVariable var;
		DC.ObjectValueFlags flags;
		
		public VariableReference (EvaluationContext ctx, TargetVariable var, DC.ObjectValueFlags flags): base (ctx)
		{
			this.flags = flags;
			this.var = var;
			if (!var.CanWrite)
				flags |= DC.ObjectValueFlags.ReadOnly;
		}
		
		public override TargetObject Value {
			get {
				TargetObject val = var.GetObject (Context.Frame);
				return ObjectUtil.GetRealObject (Context, val);
			}
			set {
				var.SetObject (Context.Frame, value);
			}
		}
		
		public override string Name {
			get {
				return var.Name;
			}
		}
		
		public override TargetType Type {
			get {
				return var.Type;
			}
		}
		
		public override DC.ObjectValueFlags Flags {
			get {
				return flags;
			}
		}
	}
}
