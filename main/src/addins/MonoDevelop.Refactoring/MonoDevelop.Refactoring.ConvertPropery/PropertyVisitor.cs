// 
// PropertyVisitor.cs
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
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Refactoring.ConvertPropery
{
	public class PropertyVisitor : AbstractAstVisitor
	{
		IProperty Property {
			get;
			set;
		}
		public string BackingStoreName {
			get;
			set;
		}
		public PropertyVisitor (IProperty property)
		{
			this.Property = property;
			BackingStoreName = null;
		}
		
		PropertyDeclaration curPropertyDeclaration;
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			this.curPropertyDeclaration = propertyDeclaration;
				
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		public override object VisitPropertyGetRegion (PropertyGetRegion propertyGetRegion, object data)
		{
			if (curPropertyDeclaration != null && curPropertyDeclaration.Name == Property.Name && propertyGetRegion.Block.Children.Count == 1) {
				ReturnStatement returnStatement = propertyGetRegion.Block.Children[0] as ReturnStatement;
				if (returnStatement != null) {
					MemberReferenceExpression mrr = returnStatement.Expression as MemberReferenceExpression;
					if (mrr != null) {
						if (mrr.TargetObject is ThisReferenceExpression)
							BackingStoreName = mrr.MemberName;
					}
					IdentifierExpression idExpr = returnStatement.Expression as IdentifierExpression;
					if (idExpr != null) 
						BackingStoreName = idExpr.Identifier;
				}
			}
			return base.VisitPropertyGetRegion (propertyGetRegion, data);
		}
		
	}
}
