// 
// NamespaceEntry.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom.MemoryDatabase
{
	public class NamespaceEntry
	{
		public string Name {
			get;
			set;
		}
		
		Dictionary<string, NamespaceEntry> subEntries = new Dictionary<string, NamespaceEntry> ();
		public Dictionary<string, NamespaceEntry> SubEntries {
			get {
				return subEntries;
			}
		}
		
		Dictionary<string, IType> containingTypes = new Dictionary<string, IType> ();
		public Dictionary<string, IType> ContainingTypes {
			get {
				return containingTypes;
			}
		}
		
		public NamespaceEntry () : this ("<root>")
		{
		}
		
		public NamespaceEntry (string name)
		{
			this.Name = name;
		}
		
		public override string ToString ()
		{
			return string.Format("[NamespaceEntry: Name={0}, #SubEntries={1}, #ContainingTypes={2}]", Name, SubEntries.Count, ContainingTypes.Count);
		}
 		
		NamespaceEntry GetNamespace (string name, bool createIfNotExtist)
		{
			NamespaceEntry result;
			if (!subEntries.TryGetValue (name, out result)) {
				if (!createIfNotExtist)
					return null;
				result = new NamespaceEntry (name);
				subEntries.Add (name, result);
			}
			return result;
		}
		
		public NamespaceEntry FindNamespace (string ns, bool createIfNotExists)
		{
			return FindNamespace (ns, createIfNotExists, false);
		}
		
		public NamespaceEntry FindNamespace (string ns, bool createIfNotExists, bool exactMatch)
		{
		//	if (string.IsNullOrEmpty (ns))
		//		return this;
			string[] path = ns.Split ('.');
			int      i  = 0;
			NamespaceEntry cur = this;
			while (i < path.Length) {
				string nsName = path[i];
				if (!string.IsNullOrEmpty (nsName)) {
					NamespaceEntry next = cur.GetNamespace (nsName, createIfNotExists);
					if (next == null)
						return exactMatch ? null : cur;
					cur = next;
				}
				i++;
			}
			return cur;
		}
		
		public void Add (IType type)
		{
			string name = GetDecoratedName (type);
			if (containingTypes.ContainsKey (name)) {
				containingTypes[name] = CompoundType.Merge (containingTypes[name], type);
			} else {
				containingTypes[name] = type;
			}
		}
		
		internal static string GetDecoratedName (string name, int genericArgumentCount)
		{
			if (genericArgumentCount <= 0)
				return name;
			return name + "`" + genericArgumentCount;
		}
		
		internal static string GetDecoratedName (IType type)
		{
			return GetDecoratedName (type.FullName, type.TypeParameters.Count);
		}

		internal static string GetDecoratedName (IReturnType type)
		{
			return ((DomReturnType)type).DecoratedFullName;
		}
		internal static string ConcatNamespaces (string n1, string n2)
		{
			if (string.IsNullOrEmpty (n1))
				return n2;
			if (string.IsNullOrEmpty (n2))
				return n1;
			return n1 + "." + n2;
		}
	}
}