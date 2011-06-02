//// 
//// NotImplementedExceptionInspector.cs
////  
//// Author:
////       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Core;
using MonoDevelop.AnalysisCore;
using MonoDevelop.CSharp.ContextAction;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.CSharp.Inspection
{
	public class NotImplementedExceptionInspector : CSharpInspector
	{
		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitior)
		{
			visitior.ThrowStatementVisited += delegate(ThrowStatement node, InspectionData data) {
				if (node.Expression.IsNull)
					return;
				if (node.Expression is IdentifierExpression && ((IdentifierExpression)node.Expression).Identifier != "NotImplementedException")
					return;
				if (node.Expression is MemberReferenceExpression && ((MemberReferenceExpression)node.Expression).MemberName != "NotImplementedException")
					return;
				// may be a not implemented exception, to get 100% sure we need to make a resolve.
				var resolver = data.Resolver;
				var result = resolver.Resolve (node.Expression.ToString (), new DomLocation (node.StartLocation.Line, node.EndLocation.Column));
				if (result != null && result.ResolvedType.FullName != null && result.ResolvedType.FullName == "System.NotImplementedException") {
					data.Add (new Result (
						new DomRegion (node.StartLocation.Line, node.StartLocation.Column, node.EndLocation.Line, node.EndLocation.Column),
						GettextCatalog.GetString ("NotImplemented exception thrown"),
						MonoDevelop.SourceEditor.QuickTaskSeverity.Suggestion,
						ResultCertainty.High,
						ResultImportance.Low,
						false)
					);
				}
			};
		}
	}
}
