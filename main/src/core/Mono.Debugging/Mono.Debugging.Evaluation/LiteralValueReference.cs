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
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace Mono.Debugging.Evaluation
{
	public class LiteralValueReference: ValueReference
	{
		string name;
		object value;
		object type;
		object objValue;
		bool objLiteral;
		bool objCreated;

		LiteralValueReference (EvaluationContext ctx): base (ctx)
		{
		}

		public static LiteralValueReference CreateTargetObjectLiteral (EvaluationContext ctx, string name, object value)
		{
			LiteralValueReference val = new LiteralValueReference (ctx);
			val.name = name;
			val.value = value;
			val.type = ctx.Adapter.GetValueType (ctx, value);
			val.objCreated = true;
			return val;
		}
		
		public static LiteralValueReference CreateObjectLiteral (EvaluationContext ctx, string name, object value)
		{
			LiteralValueReference val = new LiteralValueReference (ctx);
			val.name = name;
			val.objValue = value;
			val.objLiteral = true;
			return val;
		}
		
		public override object ObjectValue {
			get {
				if (objLiteral)
					return objValue;
				else
					return base.ObjectValue;
			}
		}

		public override object Value {
			get {
				if (!objCreated && objLiteral) {
					objCreated = true;
					value = Context.Adapter.CreateValue (Context, objValue);
					type = Context.Adapter.GetValueType (Context, value);
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
		
		public override object Type {
			get {
				if (!objCreated && objLiteral)
					type = Context.Adapter.GetValueType (Context, Value);
				return type;
			}
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.Field | ObjectValueFlags.ReadOnly;
			}
		}

		protected override ObjectValue OnCreateObjectValue ( )
		{
			if (ObjectValue is EvaluationResult) {
				EvaluationResult exp = (EvaluationResult) ObjectValue;
				return Mono.Debugging.Client.ObjectValue.CreateObject (this, new ObjectPath (Name), "", exp, Flags, null);
			}
			else
				return base.OnCreateObjectValue ();
		}
	}
}
