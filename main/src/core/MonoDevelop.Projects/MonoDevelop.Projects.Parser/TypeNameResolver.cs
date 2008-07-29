// TypeNameResolver.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Projects.Parser
{
	public class TypeNameResolver: ITypeNameResolver
	{
		protected Dictionary<string,string> names = new Dictionary<string,string> ();
		string enclosingNamespace;
		
		public TypeNameResolver ()
		{
		}
		
		public TypeNameResolver (ICompilationUnit unit, IClass cls)
		{
			AddNames (unit, cls.Region.BeginLine, cls.Region.BeginColumn);
		}
		
		public TypeNameResolver (ICompilationUnit unit, int caretLine, int caretColumn)
		{
			AddNames (unit, caretLine, caretColumn);
		}
		
		void AddNames (ICompilationUnit unit, int caretLine, int caretColumn)
		{
			foreach (IClass cls in unit.Classes) {
				if ((cls.Region != null && cls.Region.IsInside (caretLine, caretColumn)) || (cls.BodyRegion != null && cls.BodyRegion.IsInside (caretLine, caretColumn))) {
					// Enclosing namespace:
					AddName (cls.Namespace, "");
					enclosingNamespace = cls.Namespace;
					// For inner classes:
					AddName (cls.FullyQualifiedName, "");
				}
			}
			
			foreach (IUsing u in unit.Usings) {
				if (u != null) {
					foreach (string us in u.Usings)
						AddName (us, "");
				}
			}
			
			// Namespace aliases
			foreach (IUsing u in unit.Usings) {
				if (u != null) {
					foreach (string e in u.Aliases)
						AddName (u.GetAlias (e).FullyQualifiedName, e);
				}
			}
		}
		
		public void AddName (string name, string resolved)
		{
			names [name] = resolved;
		}
		
		public virtual string ResolveName (string typeName)
		{
			// Resolve complete class names (aliases replace complete class names)
			string res;
			if (names.TryGetValue (typeName, out res) && !string.IsNullOrEmpty (res))
				return res;
			
			// Try replacing the namespace
			
			int i = typeName.LastIndexOf ('.');
			if (i == -1)
				return typeName;
			
			string ns = typeName.Substring (0, i);
			
			if (names.TryGetValue (ns, out res)) {
				if (res.Length > 0)
					return res + "." + typeName.Substring (i+1);
				else
					return typeName.Substring (i+1);
			}
			else {
				// We don't resolve partial namespaces, but the enclosing namespace is an exception.
				if (enclosingNamespace != null && typeName.StartsWith (enclosingNamespace + "."))
					return typeName.Substring (enclosingNamespace.Length + 1);
				
				return typeName;
			}
		}
	}
}
