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
using Mono.Debugger;
using Mono.Debugger.Languages;
using Mono.Debugging.Client;

namespace DebuggerServer
{
	public class TypeValueReference: ValueReference
	{
		TargetType type;
		string name;
		
		public TypeValueReference (Thread thread, TargetType type): base (thread)
		{
			this.type = type;
			int i = type.Name.LastIndexOf ('.');
			if (i != -1)
				this.name = type.Name.Substring (i+1);
			else
				this.name = type.Name;
		}
		
		public override TargetObject Value {
			get {
				throw new NotSupportedException ();
			}
			set {
				throw new NotSupportedException();
			}
		}

		
		public override TargetType Type {
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

		public override ObjectValue CreateObjectValue ()
		{
			Connect ();
			return Mono.Debugging.Client.ObjectValue.CreateObject (this, new ObjectPath (Name), "<type>", Name, Flags, null);
		}
		
		public override ValueReference GetChild (string name)
		{
			foreach (ValueReference val in Util.GetMembers (Thread, type, null)) {
				if (val.Name == name)
					return val;
			}
			return null;
		}

		public override ObjectValue[] GetChildren (Mono.Debugging.Client.ObjectPath path, int index, int count)
		{
			try {
				List<ObjectValue> list = new List<ObjectValue> ();
				foreach (ValueReference val in Util.GetMembers (Thread, type, null))
					list.Add (val.CreateObjectValue ());
				return list.ToArray ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
				Server.Instance.WriteDebuggerOutput (ex.Message);
				return new ObjectValue [0];
			}
		}
		
		public override IEnumerable<ValueReference> GetChildReferences ()
		{
			foreach (ValueReference val in Util.GetMembers (Thread, type, null))
				yield return val;
		}
	}
}
