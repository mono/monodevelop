// 
// UnusedUsingInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using Mono.CSharp;
using MonoDevelop.Refactoring.RefactorImports;
using MonoDevelop.Refactoring;
using MonoDevelop.AnalysisCore.Fixes;
using System.Collections.Generic;


namespace MonoDevelop.CSharp.Inspection
{
	public class UnusedUsingInspector : CSharpInspector
	{
		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitor)
		{
			visitor.UsingDeclarationVisited += HandleVisitorUsingDeclarationVisited;
		}
		
		void HandleVisitorUsingDeclarationVisited (UsingDeclaration node, InspectionData data)
		{
			if (!data.Graph.UsedUsings.Contains (node.Namespace)) {
				AddResult (data,
					new DomRegion (node.StartLocation.Line, node.StartLocation.Column, node.EndLocation.Line, node.EndLocation.Column),
					GettextCatalog.GetString ("Remove unused usings"),
					delegate {
						RefactoringOptions options = new RefactoringOptions () { Document = data.Document, Dom = data.Document.Dom};
						new RemoveUnusedImportsRefactoring ().Run (options);
					}
				);
			}
		}
	}
}

