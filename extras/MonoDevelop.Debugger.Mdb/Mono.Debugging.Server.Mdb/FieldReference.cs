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

namespace DebuggerServer
{
	public class FieldReference: IValueReference
	{
		TargetStructType type;
		TargetFieldInfo field;
		TargetStructObject thisobj;
		Thread thread;
		
		public FieldReference (Thread thread, TargetStructObject thisobj, TargetStructType type, TargetFieldInfo field)
		{
			this.thread = thread;
			this.type = type;
			this.field = field;
			if (!field.IsStatic)
				this.thisobj = thisobj;
		}
		
		public TargetType Type {
			get {
				return field.Type;
			}
		}
		
		public TargetObject Value {
			get {
				if (field.HasConstValue)
					return thread.CurrentFrame.Language.CreateInstance (thread, field.ConstValue);
				TargetClass cls = type.GetClass (thread);
				return cls.GetField (thread, thisobj, field);
			}
			set {
				TargetClass cls = type.GetClass (thread);
				cls.SetField (thread, thisobj, field, value);
			}
		}
		
		public string Name {
			get {
				return field.Name;
			}
		}
		
		public bool CanWrite {
			get {
				return !field.HasConstValue;
			}
		}
	}
}
