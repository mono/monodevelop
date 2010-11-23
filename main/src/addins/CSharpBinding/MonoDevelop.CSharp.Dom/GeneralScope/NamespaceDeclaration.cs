using System.Collections.Generic;
// 
// NamespaceDeclaration.cs
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

namespace MonoDevelop.CSharp.Dom
{
	public class NamespaceDeclaration : DomNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public string Name {
			get {
				return NameIdentifier.QualifiedName;
			}
		}
		
		public string QualifiedName {
			get {
				NamespaceDeclaration parentNamespace = Parent as NamespaceDeclaration;
				if (parentNamespace != null)
					return BuildQualifiedName (parentNamespace.QualifiedName, Name);
				return Name;
			}
		}
		
		public CSharpTokenNode LBrace {
			get {
				return (CSharpTokenNode)GetChildByRole (Roles.LBrace) ?? CSharpTokenNode.Null;
			}
		}
		
		public CSharpTokenNode RBrace {
			get {
				return (CSharpTokenNode)GetChildByRole (Roles.RBrace) ?? CSharpTokenNode.Null;
			}
		}
		
		public QualifiedIdentifier NameIdentifier {
			get {
				return (QualifiedIdentifier)GetChildByRole (Roles.Identifier);
			}
		}
		
		public IEnumerable<DomNode> Members {
			get { return GetChildrenByRole(Roles.Member); }
		}
		
		public static string BuildQualifiedName (string name1, string name2)
		{
			if (string.IsNullOrEmpty (name1))
				return name2;
			if (string.IsNullOrEmpty (name2))
				return name1;
			return name1 + "." + name2;
		}
		
		
		public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitNamespaceDeclaration (this, data);
		}
	}
};
