// NullValueReference.cs
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

namespace MonoDevelop.Debugger.Evaluation
{
	public class NullValueReference<TValue, TType>: ValueReference<TValue, TType>
		where TValue: class
		where TType: class
	{
		TType type;
		TValue obj;
		bool valueCreated;

		public NullValueReference (EvaluationContext<TValue, TType> ctx, TType type)
			: base (ctx)
		{
			this.type = type;
		}
	
		public override TValue Value {
			get {
				if (!valueCreated) {
					valueCreated = true;
					obj = Context.Adapter.CreateNullValue (Context, type);
				}
				return obj;
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		public override TType Type {
			get {
				return type;
			}
		}
		
		public override object ObjectValue {
			get {
				return null;
			}
		}

		public override string Name {
			get {
				return "null";
			}
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.Literal;
			}
		}

		protected override ObjectValue OnCreateObjectValue ()
		{
			string tn = Context.Adapter.GetTypeName (Context, Type);
			return Mono.Debugging.Client.ObjectValue.CreateObject (null, new ObjectPath (Name), tn, "null", Flags, null);
		}

		public override ValueReference<TValue, TType> GetChild (string name)
		{
			return null;
		}

		public override ObjectValue[] GetChildren (Mono.Debugging.Client.ObjectPath path, int index, int count)
		{
			return new ObjectValue [0];
		}
	}
}
