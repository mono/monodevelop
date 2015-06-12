//
// AnalyzersFromAssembly.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CodeIssues
{
	
	class AnalyzersFromAssembly
	{
		public List<CodeDiagnosticDescriptor> Analyzers;
		public List<CodeDiagnosticFixDescriptor> Fixes;
		public List<CodeRefactoringDescriptor> Refactorings;

		public readonly static AnalyzersFromAssembly Empty = new AnalyzersFromAssembly ();

		public AnalyzersFromAssembly ()
		{
			Analyzers = new List<CodeDiagnosticDescriptor> ();
			Fixes = new List<CodeDiagnosticFixDescriptor> ();
			Refactorings = new List<CodeRefactoringDescriptor> ();
		}

		internal static AnalyzersFromAssembly CreateFrom (System.Reflection.Assembly asm, bool force = false)
		{
			var result = new AnalyzersFromAssembly ();
			result.AddAssembly (asm, force);
			return result;
		}

		internal void AddAssembly (System.Reflection.Assembly asm, bool force = false)
		{
			if (!force) {
				var assemblyName = asm.GetName ().Name;
				if (assemblyName == "MonoDevelop.AspNet" ||
					assemblyName == "Microsoft.CodeAnalysis.CSharp" ||
					assemblyName != "NR6Pack" &&
					!(asm.GetReferencedAssemblies ().Any (a => a.Name == diagnosticAnalyzerAssembly) && asm.GetReferencedAssemblies ().Any (a => a.Name == "MonoDevelop.Ide")))
					return;
			}
			foreach (var type in asm.GetTypes ()) {
				var notPortedYetAttribute = (NotPortedYetAttribute)type.GetCustomAttributes (typeof(NotPortedYetAttribute), false).FirstOrDefault ();
				if (notPortedYetAttribute!= null) {
					continue;
				}
				var analyzerAttr = (DiagnosticAnalyzerAttribute)type.GetCustomAttributes (typeof(DiagnosticAnalyzerAttribute), false).FirstOrDefault ();
				if (analyzerAttr != null) {
					var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance (type);
					foreach (var diag in analyzer.SupportedDiagnostics) {
						try {
							Analyzers.Add (new CodeDiagnosticDescriptor (diag, analyzerAttr.Languages, type));
						} catch (Exception e) {
							LoggingService.LogError ("error while adding diagnostic analyzer: " + diag.Id + " from assembly " + asm.FullName, e);
						}
					}
				}

				var codeFixAttr = (ExportCodeFixProviderAttribute)type.GetCustomAttributes (typeof(ExportCodeFixProviderAttribute), false).FirstOrDefault ();
				if (codeFixAttr != null) {
					Fixes.Add (new CodeDiagnosticFixDescriptor (type, codeFixAttr));
				}

				var exportAttr = type.GetCustomAttributes (typeof(ExportCodeRefactoringProviderAttribute), false).FirstOrDefault () as ExportCodeRefactoringProviderAttribute;
				if (exportAttr != null) {
					Refactorings.Add (new CodeRefactoringDescriptor (type, exportAttr)); 
				}
			}
		}

		readonly static string diagnosticAnalyzerAssembly = typeof (DiagnosticAnalyzerAttribute).Assembly.GetName ().Name;

	}

}