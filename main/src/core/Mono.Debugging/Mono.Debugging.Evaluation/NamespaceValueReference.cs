// NamespaceValueReference.cs
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	public class NamespaceValueReference<TValue, TType>: ValueReference<TValue, TType>
		where TValue: class
		where TType: class
	{
		string name;
		string namspace;
		string[] namespaces;

		public NamespaceValueReference (EvaluationContext<TValue, TType> ctx, string name)
			: base (ctx)
		{
			this.namspace = name;
			int i = namspace.LastIndexOf ('.');
			if (i != -1)
				this.name = namspace.Substring (i+1);
			else
				this.name = namspace;
		}

		public override TValue Value {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		
		public override TType Type {
			get {
				throw new NotSupportedException();
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
				return ObjectValueFlags.Namespace;
			}
		}

		public override ValueReference<TValue, TType> GetChild (string name)
		{
			string newNs = namspace + "." + name;
			
			TType t = Context.Adapter.GetType (Context, newNs);
			if (t != null)
				return new TypeValueReference<TValue, TType> (Context, t);

			if (namespaces == null)
				namespaces = Context.Adapter.GetImportedNamespaces (Context);
			
			foreach (string ns in namespaces) {
				if (ns == newNs || ns.StartsWith (newNs + "."))
					return new NamespaceValueReference<TValue, TType> (Context, newNs);
			}
			return null;
		}

		public override ObjectValue[] GetChildren (ObjectPath path, int index, int count)
		{
			List<ObjectValue> obs = new List<ObjectValue> ();
			foreach (ValueReference<TValue, TType> val in GetChildReferences ()) {
				obs.Add (val.CreateObjectValue ());
			}
			return obs.ToArray ();
		}

		public override IEnumerable<ValueReference<TValue, TType>> GetChildReferences ( )
		{
			// Child types

			string[] childNamespaces;
			string[] childTypes;

			Context.Adapter.GetNamespaceContents (Context, namspace, out childNamespaces, out childTypes);

			foreach (string typeName in childTypes) {
				TType tt = Context.Adapter.GetType (Context, typeName);
				if (tt != null)
					yield return new TypeValueReference<TValue, TType> (Context, tt);
			}
			
			// Child namespaces
			foreach (string ns in childNamespaces)
				yield return new NamespaceValueReference<TValue, TType> (Context, ns);
		}

		protected override Mono.Debugging.Client.ObjectValue OnCreateObjectValue ()
		{
			return Mono.Debugging.Client.ObjectValue.CreateObject (this, new ObjectPath (Name), "<namespace>", namspace, Flags, null);
		}

		public override string CallToString ()
		{
			return namspace;
		}
	}
}
