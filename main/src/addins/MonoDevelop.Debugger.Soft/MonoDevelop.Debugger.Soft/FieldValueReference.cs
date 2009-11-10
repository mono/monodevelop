// 
// FieldValueReference.cs
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
using Mono.Debugging.Evaluation;
using Mono.Debugger;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.Soft
{
	public class FieldValueReference: ValueReference
	{
		FieldInfoMirror field;
		object obj;
		TypeMirror declaringType;
		
		public FieldValueReference (EvaluationContext ctx, FieldInfoMirror field, object obj, TypeMirror declaringType): base (ctx)
		{
			this.field = field;
			this.obj = obj;
			this.declaringType = declaringType;
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.Field;
			}
		}

		public override string Name {
			get {
				return field.Name;
			}
		}

		public override object Type {
			get {
				return field.FieldType;
			}
		}

		public override object Value {
			get {
				if (obj == null)
					return declaringType.GetValue (field);
				else if (obj is ObjectMirror)
					return ((ObjectMirror)obj).GetValue (field);
				else
					return ((StructMirror)obj) [field.Name];
			}
			set {
				if (obj == null)
					declaringType.SetValue (field, (Value)value);
				else if (obj is ObjectMirror)
					((ObjectMirror)obj).SetValue (field, (Value)value);
				else
					throw new NotSupportedException ();
			}
		}
	}
}
