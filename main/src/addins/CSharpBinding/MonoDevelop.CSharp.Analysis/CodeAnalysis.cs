// 
// NamingConventions.cs
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
using System.Linq;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.QuickFix;

namespace MonoDevelop.CSharp.Analysis
{
	public static class CodeAnalysis
	{
		static CodeAnalysis ()
		{
		}
		
		public static IEnumerable<Result> Check (Document input)
		{
			var unit = input != null ? input.ParsedDocument.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit : null;
			if (unit == null)
				yield break;
			
			var cg = new CallGraph ();
			cg.Inpect (input, CSharpQuickFix.GetResolver (input), unit);
			
			
			var visitor = new ObservableAstVisitor ();
			
			List<CSharpInspector> inspectors = new List<CSharpInspector> ();
			inspectors.Add (new NamingInspector (input.CompilationUnit));
			inspectors.Add (new StringIsNullOrEmptyInspector ());
			inspectors.Add (new ConditionalToNullCoalescingInspector ());
			inspectors.Add (new NotImplementedExceptionInspector (input));
			inspectors.Add (new UnusedUsingInpector (input, cg));
			inspectors.Add (new UseVarKeywordInspector ());
	
			foreach (var inspector in inspectors) {
				inspector.Attach (visitor);
			}
			
			unit.AcceptVisitor (visitor, null);
			foreach (var inspector in inspectors) {
				foreach (var fix in inspector.results)
					yield return fix;
			}
			
		}
	}
}
