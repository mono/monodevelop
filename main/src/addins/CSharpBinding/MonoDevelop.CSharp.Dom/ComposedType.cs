// 
// ComposedType.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using System.Linq;


namespace MonoDevelop.CSharp.Dom
{
	public class ComposedType : DomNode
	{
		public const int NullableRole  = 100;
		public const int PointerRole   = 101;
		public const int ArraySpecRole = 102;
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public DomNode BaseType {
			get {
				return GetChildByRole (Roles.ReturnType);
			}
		}
		
		public IEnumerable<ArraySpecifier> Compositions {
			get { return GetChildrenByRole (ArraySpecRole).Cast<ArraySpecifier> () ?? new ArraySpecifier[0]; }
		}
		
		public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitComposedType (this, data);
		}
		
		public class ArraySpecifier : DomNode
		{
			public override NodeType NodeType {
				get {
					return NodeType.Unknown;
				}
			}
			
			public CSharpTokenNode LBracket {
				get { return (CSharpTokenNode)GetChildByRole (Roles.LBracket); }
			}
			
			public CSharpTokenNode RBracket {
				get { return (CSharpTokenNode)GetChildByRole (Roles.RBracket); }
			}
		
			
			public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
		}
	}
}

