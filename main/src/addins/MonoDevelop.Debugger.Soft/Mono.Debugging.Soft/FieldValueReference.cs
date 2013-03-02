// 
// FieldValueReference.cs
//  
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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
using System.Reflection;
using Mono.Debugging.Evaluation;
using Mono.Debugger.Soft;
using Mono.Debugging.Client;

namespace Mono.Debugging.Soft
{
	public class FieldValueReference: ValueReference
	{
		FieldInfoMirror field;
		object obj;
		TypeMirror declaringType;
		ObjectValueFlags flags;
		string vname;
		
		public FieldValueReference (EvaluationContext ctx, FieldInfoMirror field, object obj, TypeMirror declaringType)
			: this (ctx, field, obj, declaringType, null, ObjectValueFlags.Field)
		{
		}
		
		public FieldValueReference (EvaluationContext ctx, FieldInfoMirror field, object obj, TypeMirror declaringType, string vname, ObjectValueFlags vflags): base (ctx)
		{
			this.field = field;
			this.obj = obj;
			this.declaringType = declaringType;
			this.vname = vname;
			flags = vflags;
			if (field.IsStatic) {
				flags |= ObjectValueFlags.Global;
				this.obj = null;
			}
			if (field.IsPublic)
				flags |= ObjectValueFlags.Public;
			else if (field.IsPrivate)
				flags |= ObjectValueFlags.Private;
			else if (field.IsFamily)
				flags |= ObjectValueFlags.Protected;
			else if (field.IsFamilyAndAssembly)
				flags |= ObjectValueFlags.Internal;
			else if (field.IsFamilyOrAssembly)
				flags |= ObjectValueFlags.InternalProtected;
			if (obj is PrimitiveValue)
				flags |= ObjectValueFlags.ReadOnly;
		}
		
		public override ObjectValueFlags Flags {
			get {
				return flags;
			}
		}

		public override string Name {
			get {
				return vname ?? field.Name;
			}
		}

		public override object Type {
			get {
				return field.FieldType;
			}
		}
		
		public override object DeclaringType {
			get {
				return field.DeclaringType;
			}
		}

		public override object Value {
			get {
				if (obj == null) {
					// If the type hasn't already been loaded, invoke the .cctor() for types w/ the BeforeFieldInit attribute.
					Context.Adapter.ForceLoadType (Context, declaringType);

					return declaringType.GetValue (field);
				} else if (obj is ObjectMirror) {
					return ((ObjectMirror)obj).GetValue (field);
				} else if (obj is StructMirror) {
					StructMirror sm = (StructMirror)obj;
					int idx = 0;
					foreach (FieldInfoMirror f in sm.Type.GetFields ()) {
						if (f.IsStatic) continue;
						if (f == field)
							break;
						idx++;
					}
					return sm.Fields [idx];
				} else if (obj is StringMirror) {
					SoftEvaluationContext cx = (SoftEvaluationContext) Context;
					StringMirror val = (StringMirror) obj;
					FieldInfo rfield = typeof(string).GetField (field.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					return cx.Session.VirtualMachine.CreateValue (rfield.GetValue (val.Value));
				} else {
					SoftEvaluationContext cx = (SoftEvaluationContext) Context;
					PrimitiveValue val = (PrimitiveValue) obj;
					if (val.Value == null)
						return null;
					FieldInfo rfield = val.Value.GetType ().GetField (field.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					return cx.Session.VirtualMachine.CreateValue (rfield.GetValue (val.Value));
				}
			}
			set {
				if (obj == null)
					declaringType.SetValue (field, (Value)value);
				else if (obj is ObjectMirror)
					((ObjectMirror)obj).SetValue (field, (Value)value);
				else if (obj is StructMirror) {
					StructMirror sm = (StructMirror)obj;
					int idx = 0;
					foreach (FieldInfoMirror f in sm.Type.GetFields ()) {
						if (f.IsStatic) continue;
						if (f == field)
							break;
						idx++;
					}
					if (idx != -1) {
						sm.Fields [idx] = (Value)value;
						// Structs are handled by-value in the debugger, so the source of the object has to be updated
						if (ParentSource != null && obj != null)
							ParentSource.Value = obj;
					}
				}
				else
					throw new NotSupportedException ();
			}
		}
	}
}
