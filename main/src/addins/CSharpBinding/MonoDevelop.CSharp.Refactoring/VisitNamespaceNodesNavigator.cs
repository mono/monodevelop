// 
// VisitNamespaceNodesNavigator.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharp.Refactoring
{
	public class VisitNamespaceNodesNavigator : IResolveVisitorNavigator
	{
		HashSet<string> additionalNamespaces = new HashSet<string> ();
		
		public bool GetsUsed (ITypeResolveContext ctx, CSharpResolver cSharpResolver, TextLocation loc, string ns)
		{
			return cSharpResolver.usedScopes
				.OfType<ITypeOrNamespaceReference> ()
				.Any (u => u.ResolveNamespace (ctx).NamespaceName == ns) || additionalNamespaces.Contains (ns);
		}
		
		#region IResolveVisitorNavigator implementation
		ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
		{
			if (node is SimpleType || node is MemberType
			    || node is IdentifierExpression || node is MemberReferenceExpression
			    || node is Attribute
			    || node is InvocationExpression)
			{
				return ResolveVisitorNavigationMode.Resolve;
			} else {
				return ResolveVisitorNavigationMode.Scan;
			}
		}
		
		void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
		{
			if (node is Attribute)
				additionalNamespaces.Add (result.Type.Namespace);
			if (node is InvocationExpression) {
				var mr = result as InvocationResolveResult;
				if (mr == null)
					return;
				additionalNamespaces.Add (mr.Member.DeclaringTypeDefinition.Namespace);
			}
		}
		
		void IResolveVisitorNavigator.ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
		{
		}
		#endregion
	}
	
}
