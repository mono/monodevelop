// LiteralValueReference.cs
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
using Mono.Debugging.Client;

namespace DebuggerServer
{
	public class LiteralValueReference: ValueReference
	{
		string name;
		TargetObject value;
		TargetType type;
		object objValue;
		bool objLiteral;
		
		public LiteralValueReference (Thread thread, string name, TargetObject value): base (thread)
		{
			this.name = name;
			this.value = value;
			this.type = value.Type;
		}

		public LiteralValueReference (Thread thread, string name, object value): base (thread)
		{
			this.name = name;
			this.objValue = value;
			objLiteral = true;
		}
		
		public override object ObjectValue {
			get {
				if (objLiteral)
					return objValue;
				else
					return value;
			}
		}

		public override TargetObject Value {
			get {
				if (value == null && objLiteral) {
					if (objValue == null)
						value = Thread.CurrentFrame.Language.CreateNullObject (Thread, null);
					else
						value = Thread.CurrentFrame.Language.CreateInstance (Thread, objValue);
				}
				return value;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		public override string Name {
			get {
				return name;
			}
		}
		
		public override Mono.Debugger.Languages.TargetType Type {
			get {
				if (type == null && objLiteral)
					type = Value.Type;
				return type;
			}
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.Field | ObjectValueFlags.ReadOnly;
			}
		}
	}
}
