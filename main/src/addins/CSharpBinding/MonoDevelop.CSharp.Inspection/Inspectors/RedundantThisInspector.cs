// 
// RedundantThisInspector.cs
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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.CSharp.Refactoring.RefactorImports;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.CSharp.Refactoring;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.CSharp.Inspection
{
	public class RedundantThisInspector: CSharpInspector
	{
		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitor)
		{
			visitor.ThisReferenceExpressionVisited += HandleThisReferenceExpressionVisited;
		}
			
		void HandleThisReferenceExpressionVisited (ThisReferenceExpression expr, InspectionData data)
		{
			var memberReference = expr.Parent as MemberReferenceExpression;
			if (memberReference == null)
				return;
			var state = data.Graph.Resolver.GetResolverStateBefore (expr);
			var wholeResult = data.Graph.Resolver.Resolve (memberReference) as MemberResolveResult;
			var result = state.LookupSimpleNameOrTypeName (memberReference.MemberName, new List<IType> (), SimpleNameLookupMode.Expression) as MemberResolveResult;
			if (result == null || wholeResult == null)
				return;
			if (result.Member.Region.Equals (wholeResult.Member.Region)) {
				AddResult (data,
					new DomRegion (expr.StartLocation, memberReference.MemberNameToken.StartLocation),
					GettextCatalog.GetString ("Remove redundant 'this.'"),
					delegate {
						int offset = data.Document.Editor.LocationToOffset (expr.StartLocation);
						int end = data.Document.Editor.LocationToOffset (memberReference.MemberNameToken.StartLocation);
						data.Document.Editor.Remove (offset, end - offset);
					}
				);
			}
		}
	}
}

