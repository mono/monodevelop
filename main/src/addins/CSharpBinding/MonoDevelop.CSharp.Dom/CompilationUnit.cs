// 
// CompilationUnit.cs
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

namespace MonoDevelop.CSharp.Dom
{
	public class CompilationUnit : AbstractCSharpNode 
	{
		public CompilationUnit ()
		{
		}
		
		public ICSharpNode GetNodeAt (int line, int column)
		{
			return GetNodeAt (new DomLocation (line, column));
		}
		
		public ICSharpNode GetNodeAt (DomLocation location)
		{
			ICSharpNode node = this;
			while (node.FirstChild != null) {
				ICSharpNode child = node.FirstChild as ICSharpNode;
				while (child != null) {
					if (child.StartLocation <= location && location < child.EndLocation) {
						node = child;
						break;
					}
					child = child.NextSibling as ICSharpNode;
				}
				// found no better child node - therefore the parent is the right one.
				if (child == null)
					break;
			}
			return node;
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCompilationUnit (this, data);
		}
		
	}
}

