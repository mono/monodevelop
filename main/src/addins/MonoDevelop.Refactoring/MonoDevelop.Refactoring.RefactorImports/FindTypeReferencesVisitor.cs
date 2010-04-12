// 
// FindTypeReferencesVisitor.cs
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
using Mono.TextEditor;

namespace MonoDevelop.Refactoring.RefactorImports
{
	public class FindTypeReferencesVisitor : AbstractAstVisitor
	{
		TextEditorData data;
		IResolver resolver;
		List<TypeReference> possibleTypeReferences = new List<TypeReference> ();
		
		public List<TypeReference> PossibleTypeReferences {
			get { return this.possibleTypeReferences; }
		}
		
		public FindTypeReferencesVisitor (TextEditorData data, IResolver resolver)
		{
			this.data = data;
			this.resolver = resolver;
		}
		
		public override object VisitIdentifierExpression (ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
		{
			possibleTypeReferences.Add (new TypeReference (identifierExpression.Identifier));
			
			return base.VisitIdentifierExpression (identifierExpression, data);
		}
		
		public override object VisitTypeReference (TypeReference typeReference, object data)
		{
			if (!typeReference.IsGlobal)
				possibleTypeReferences.Add (typeReference);
			return base.VisitTypeReference (typeReference, data);
		}
		
		public override object VisitAttribute (ICSharpCode.NRefactory.Ast.Attribute attribute, object data)
		{
			possibleTypeReferences.Add (new TypeReference (attribute.Name));
			possibleTypeReferences.Add (new TypeReference (attribute.Name + "Attribute"));
			return base.VisitAttribute(attribute, data);
		}
		
		public override object VisitInvocationExpression (InvocationExpression invocationExpression, object data)
		{
			string invocation = "";
			if (!invocationExpression.StartLocation.IsEmpty) {
				invocation = this.data.Document.GetTextBetween (this.data.Document.LocationToOffset (invocationExpression.StartLocation.Line - 1, invocationExpression.StartLocation.Column - 1),
				                                                this.data.Document.LocationToOffset (invocationExpression.EndLocation.Line - 1, invocationExpression.EndLocation.Column - 1));
			}
			base.VisitInvocationExpression (invocationExpression, data);
			
			MethodResolveResult mrr = resolver.Resolve (new ExpressionResult (invocation), new DomLocation (invocationExpression.StartLocation.Line - 1, invocationExpression.StartLocation.Column - 1)) as MethodResolveResult;
			if (mrr != null && mrr.MostLikelyMethod != null && mrr.MostLikelyMethod is ExtensionMethod) {
				IMethod originalMethod = ((ExtensionMethod)mrr.MostLikelyMethod).OriginalMethod;
				possibleTypeReferences.Add (new TypeReference (originalMethod.DeclaringType.Name));
			}
			return null;
		}
	}
}
