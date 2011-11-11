// 
// RemoveUnusedUsingsRefactoring.cs
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.CSharp.Refactoring.RefactorImports
{
	public class RemoveUnusedImportsRefactoring : RefactoringOperation
	{
		public RemoveUnusedImportsRefactoring ()
		{
			Name = "Remove unused imports";
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object properties)
		{
			var doc = options.Document.UpdateParseDocument ();
			var unit = doc.Annotation<CompilationUnit> ();
			var resolver  = new CSharpResolver (options.Dom, System.Threading.CancellationToken.None);
			var nav = new VisitNamespaceNodesNavigator ();
			new ResolveVisitor (resolver, doc.Annotation<CSharpParsedFile> (), nav).Scan (unit);
			var result = new List<Change> ();
			var visitor = new ObservableAstVisitor<object, object> ();
			visitor.UsingDeclarationVisited += delegate(UsingDeclaration u, object arg) {
				if (!nav.GetsUsed (options.Dom, resolver, u.StartLocation, u.Namespace))
					result.Add (options.Document.Editor.GetRemoveNodeChange (u));
			};
			unit.AcceptVisitor (visitor);
			return result;
		}
	}
}
