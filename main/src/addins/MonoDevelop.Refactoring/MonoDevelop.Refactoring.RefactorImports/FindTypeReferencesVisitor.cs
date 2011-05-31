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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.TextEditor;

namespace MonoDevelop.Refactoring.RefactorImports
{
	public class FindTypeReferencesVisitor : DepthFirstAstVisitor<object, object>
	{
		TextEditorData data;
		IResolver resolver;
		List<AstType> possibleTypeReferences = new List<AstType> ();
		
		public List<AstType> PossibleTypeReferences {
			get { return this.possibleTypeReferences; }
		}
		
		public FindTypeReferencesVisitor (TextEditorData data, IResolver resolver)
		{
			this.data = data;
			this.resolver = resolver;
		}
		
		public override object VisitIdentifierExpression (IdentifierExpression identifierExpression, object data)
		{
			possibleTypeReferences.Add (new SimpleType (identifierExpression.Identifier));
			
			return base.VisitIdentifierExpression (identifierExpression, data);
		}
		
		public override object VisitComposedType (ComposedType composedType, object data)
		{
			possibleTypeReferences.Add (composedType);
			return null;
		}
		
		public override object VisitMemberType (ICSharpCode.NRefactory.CSharp.MemberType memberType, object data)
		{
			possibleTypeReferences.Add (memberType);
			return null;
		}
		
		public override object VisitSimpleType (SimpleType simpleType, object data)
		{
			possibleTypeReferences.Add (simpleType);
			return null;
		}
		
		public override object VisitAttribute (ICSharpCode.NRefactory.CSharp.Attribute attribute, object data)
		{
			possibleTypeReferences.Add (attribute.Type);
			var t = attribute.Type.Clone ();
			if (t is SimpleType) {
				((SimpleType)t).IdentifierToken.Name += "Attribute";
				possibleTypeReferences.Add (t);
			} else if (t is ICSharpCode.NRefactory.CSharp.MemberType) {
				((ICSharpCode.NRefactory.CSharp.MemberType)t).MemberNameToken.Name += "Attribute";
				possibleTypeReferences.Add (t);
			}
			return base.VisitAttribute (attribute, data);
		}
		
		public override object VisitInvocationExpression (InvocationExpression invocationExpression, object data)
		{
			string invocation = "";
			if (!invocationExpression.StartLocation.IsEmpty && !invocationExpression.EndLocation.IsEmpty) {
				invocation = this.data.Document.GetTextBetween (this.data.Document.LocationToOffset (invocationExpression.StartLocation.Line, invocationExpression.StartLocation.Column),
				                                                this.data.Document.LocationToOffset (invocationExpression.EndLocation.Line, invocationExpression.EndLocation.Column));
			}
			base.VisitInvocationExpression (invocationExpression, data);
			
			MethodResolveResult mrr = resolver.Resolve (new ExpressionResult (invocation), new DomLocation (invocationExpression.StartLocation.Line, invocationExpression.StartLocation.Column)) as MethodResolveResult;
			if (mrr != null && mrr.MostLikelyMethod != null && mrr.MostLikelyMethod is ExtensionMethod) {
				IMethod originalMethod = ((ExtensionMethod)mrr.MostLikelyMethod).OriginalMethod;
				possibleTypeReferences.Add (new SimpleType (originalMethod.DeclaringType.Name));
			}
			return null;
		}
	}
}
