// 
// UserVariableReference.cs
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
using Mono.Debugging.Client;
using Mono.Debugger.Languages;

namespace DebuggerServer
{
	public class UserVariableReference: ValueReference
	{
		string name;
		TargetObject currentValue;
		
		public UserVariableReference (EvaluationContext ctx, string name): base (ctx)
		{
			this.name = name;
		}
		
		public override string Name {
			get {
				return name;
			}
		}
		
		public override TargetObject Value {
			get {
				if (currentValue != null)
					return currentValue;
				else
					throw new EvaluatorException ("Value undefined.");
			}
			set {
				currentValue = value;
			}
		}

		public override TargetType Type {
			get {
				return Value.Type;
			}
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.Variable;
			}
		}

	}
}
