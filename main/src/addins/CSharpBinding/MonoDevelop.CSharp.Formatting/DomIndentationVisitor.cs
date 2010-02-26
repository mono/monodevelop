// 
// DomIndentationVisitor.cs
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
using MonoDevelop.CSharp.Dom;
using System.Text;
using MonoDevelop.Projects.Dom;
using Mono.TextEditor;
using MonoDevelop.Refactoring;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Formatting
{
	public class DomIndentationVisitor : AbtractCSharpDomVisitor<object, object>
	{
		CSharpFormattingPolicy policy;
		TextEditorData data;
		List<Change> changes = new List<Change> ();
		
		public int IndentLevel {
			get;
			set;
		}
		
		public int CurrentSpaceIndents {
			get;
			set;
		}
		
		public DomIndentationVisitor (CSharpFormattingPolicy policy, TextEditorData data)
		{
			this.policy = policy;
			this.data = data;
		}
		
		public override object VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, object data)
		{
			IndentLevel++;
//			FixIndentation (namespaceDeclaration.StartLocation);
			object result = base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			
			IndentLevel--;
			return result;
		}
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			IndentLevel++;
			object result = base.VisitTypeDeclaration (typeDeclaration, data);
			IndentLevel--;
			return result;
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			IndentLevel++;
			object result = base.VisitPropertyDeclaration (propertyDeclaration, data);
			IndentLevel--;
			return result;
		}
		
		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			IndentLevel++;
			object result = base.VisitMethodDeclaration (methodDeclaration, data);
			IndentLevel--;
			return result;
		}
		
		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			IndentLevel++;
			object result = base.VisitBlockStatement (blockStatement, data);
			IndentLevel--;
			return result;
		}
	}
}

