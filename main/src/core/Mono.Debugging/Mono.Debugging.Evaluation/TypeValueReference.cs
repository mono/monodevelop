// TypeValueReference.cs
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
using System.Collections.Generic;
using System.Reflection;
using Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	public class TypeValueReference: ValueReference
	{
		object type;
		string name;

		public TypeValueReference (EvaluationContext ctx, object type)
			: base (ctx)
		{
			this.type = type;
			name = ctx.Adapter.GetTypeName (ctx, type);
			int i = name.LastIndexOf ('.');
			if (i != -1)
				this.name = name.Substring (i + 1);
		}
		
		public override object Value {
			get {
				throw new NotSupportedException ();
			}
			set {
				throw new NotSupportedException();
			}
		}

		
		public override object Type {
			get {
				return type;
			}
		}

		
		public override object ObjectValue {
			get {
				throw new NotSupportedException ();
			}
		}

		
		public override string Name {
			get {
				return name;
			}
		}
		
		public override ObjectValueFlags Flags {
			get {
				return ObjectValueFlags.Type;
			}
		}

		protected override ObjectValue OnCreateObjectValue ()
		{
			return Mono.Debugging.Client.ObjectValue.CreateObject (this, new ObjectPath (Name), "<type>", Name, Flags, null);
		}

		public override ValueReference GetChild (string name)
		{
			foreach (ValueReference val in Context.Adapter.GetMembers (Context, type, null)) {
				if (val.Name == name)
					return val;
			}
			return null;
		}

		public override ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			try {
				List<ObjectValue> list = new List<ObjectValue> ();
				foreach (ValueReference val in Context.Adapter.GetMembers (Context, type, null, BindingFlags.Public | BindingFlags.Static))
					list.Add (val.CreateObjectValue ());
				list.Add (FilteredMembersSource.CreateNode (Context, type, null, BindingFlags.NonPublic | BindingFlags.Static));
				return list.ToArray ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
				Context.WriteDebuggerOutput (ex.Message);
				return new ObjectValue [0];
			}
		}

		public override IEnumerable<ValueReference> GetChildReferences ( )
		{
			try {
				return Context.Adapter.GetMembers (Context, type, null, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				Context.WriteDebuggerOutput (ex.Message);
				return new ValueReference[0];
			}
		}
	}
}
