// 
// TypeFilterTransformer.cs
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
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Refactoring.MoveTypeToFile
{
	public class TypeFilterTransformer : DepthFirstAstVisitor<object, object>
	{
		string fullName;
		public TypeDeclaration TypeDeclaration {
			get;
			set;
		}
		public TypeFilterTransformer (string fullName)
		{
			this.fullName = fullName;
		}
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			if (BuildName (typeDeclaration) != fullName) {
				typeDeclaration.Remove ();
				return null;
			}
			TypeDeclaration = typeDeclaration;
			object result = base.VisitTypeDeclaration (typeDeclaration, data);
			return result;
		}
		
		Stack<NamespaceDeclaration> namespaces = new Stack<NamespaceDeclaration> ();
		public override object VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
		{
			namespaces.Push (namespaceDeclaration);
			object result = base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			namespaces.Pop ();
			return result;
		}

		string BuildName (TypeDeclaration typeDeclaration)
		{
			// note: inner types can't be moved therefore they're missing here.
			StringBuilder result = new StringBuilder ();
			foreach (var ns in namespaces) {
				result.Append (ns.Name);
				result.Append (".");
			}
			result.Append (typeDeclaration.Name);
			if (typeDeclaration.TypeParameters.Count > 0) {
				result.Append ("`");
				result.Append (typeDeclaration.TypeParameters.Count);
			}
			
			return result.ToString ();
		}

	}
	
	
}
