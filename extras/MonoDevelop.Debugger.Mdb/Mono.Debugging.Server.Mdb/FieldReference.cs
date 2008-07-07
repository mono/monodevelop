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
using Mono.Debugger.Languages;
using Mono.Debugger;
using Mono.Debugging.Client;

namespace DebuggerServer
{
	public class FieldReference: ValueReference
	{
		TargetStructType type;
		TargetFieldInfo field;
		TargetStructObject thisobj;
		
		public FieldReference (Thread thread, TargetStructObject thisobj, TargetStructType type, TargetFieldInfo field): base (thread)
		{
			this.type = type;
			this.field = field;
			if (!field.IsStatic)
				this.thisobj = thisobj;
		}
		
		public override TargetType Type {
			get {
				return field.Type;
			}
		}
		
		public override TargetObject Value {
			get {
				if (field.HasConstValue)
					return Thread.CurrentFrame.Language.CreateInstance (Thread, field.ConstValue);
				TargetClass cls = type.GetClass (Thread);
				return cls.GetField (Thread, thisobj, field);
			}
			set {
				TargetClass cls = type.GetClass (Thread);
				cls.SetField (Thread, thisobj, field, value);
			}
		}
		
		public override string Name {
			get {
				return field.Name;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				ObjectValueFlags flags = ObjectValueFlags.Field | Util.GetAccessibility (field.Accessibility);
				if (field.HasConstValue) flags |= ObjectValueFlags.ReadOnly;
				return flags;
			}
		}
	}
}
