//
// Namespace.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.AssemblyBrowser
{
	class NamespaceData : IDisposable
	{
		public string Name { get; }
		INamespace decompilerNs;
		Microsoft.CodeAnalysis.INamespaceSymbol roslynNamespace;

		public static bool IsPublic (object typeObject)
		{
			return (typeObject is ITypeDefinition typeDefinition && typeDefinition.IsPublic ())
				|| (typeObject is Microsoft.CodeAnalysis.INamedTypeSymbol symbol && symbol.IsPublic ());
		}

		object [] types;
		public object[] Types {
			get {
				if (types == null) {
					types = decompilerNs?.Types.ToArray ()
						?? roslynNamespace?.GetTypeMembers ().ToArray ()
						?? Array.Empty<object> ();
				}
				return types;
			}
		}

		public NamespaceData(INamespace ns)
		{
			Name = ns.FullName;
			decompilerNs = ns;

			// Remove <Module> from root namespace.
			if (ns.ParentNamespace == null) {
				types = decompilerNs.Types.Where (x => x.Name != "<Module>").ToArray ();
			}
		}

		public NamespaceData(Microsoft.CodeAnalysis.INamespaceSymbol namespaceSymbol)
		{
			Name = namespaceSymbol.GetFullName ();
			roslynNamespace = namespaceSymbol;
		}
		
		public void Dispose ()
		{
			types = null;
		}
		
		public override string ToString ()
		{
			return string.Format ("[Namespace: Name={0}, #Types={1}]", Name, Types.Length);
		}
	}
}
