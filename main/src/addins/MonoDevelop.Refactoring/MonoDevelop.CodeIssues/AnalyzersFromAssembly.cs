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
using System.Reflection;
using System.Threading;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using RefactoringEssentials;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis;

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

		readonly static string diagnosticAnalyzerAssembly = typeof (DiagnosticAnalyzerAttribute).Assembly.GetName ().Name;

		const bool ClrHeapEnabled = false;
		internal void AddAssembly (System.Reflection.Assembly asm, bool force = false)
		{
			//FIXME; this is a really hacky arbitrary heuristic
			//we should be using proper MEF composition APIs as part of the addin scan
			if (!force) {
				var assemblyName = asm.GetName ().Name;
				switch (assemblyName) {
				//whitelist
				case "RefactoringEssentials":
				case "Refactoring Essentials":
				case "Microsoft.CodeAnalysis.Features":
				case "Microsoft.CodeAnalysis.VisualBasic.Features":
				case "Microsoft.CodeAnalysis.CSharp.Features":
					break;
				case "ClrHeapAllocationAnalyzer":
					if (!ClrHeapEnabled)
						return;
					break;
				//blacklist
				case "FSharpBinding":
					return;
				//addin assemblies that reference roslyn
				default:
					var refAsm = asm.GetReferencedAssemblies ();
					if (refAsm.Any (a => a.Name == diagnosticAnalyzerAssembly) && refAsm.Any (a => a.Name == "MonoDevelop.Ide"))
						break;
					return;
				}
			}

			try {
				foreach (var type in asm.GetTypes ()) {

					//HACK: Workaround for missing UI
					if (type == typeof (Microsoft.CodeAnalysis.GenerateOverrides.GenerateOverridesCodeRefactoringProvider))
						continue;
					if (type == typeof (Microsoft.CodeAnalysis.AddMissingReference.AbstractAddMissingReferenceCodeFixProvider))
						continue;



					var analyzerAttr = (DiagnosticAnalyzerAttribute)type.GetCustomAttributes (typeof (DiagnosticAnalyzerAttribute), false).FirstOrDefault ();
					if (analyzerAttr != null) {
						try {
							var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance (type);

							if (analyzer.SupportedDiagnostics.Any (IsDiagnosticSupported)) {
								Analyzers.Add (new CodeDiagnosticDescriptor (analyzerAttr.Languages, type));
							}
						} catch (Exception e) {
							LoggingService.LogError ($"error while adding diagnostic analyzer {type}  from assembly {asm.FullName}", e);
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
			} catch (ReflectionTypeLoadException ex) {
				foreach (var subException in ex.LoaderExceptions) {
					LoggingService.LogError ("Error while loading diagnostics in " + asm.FullName, subException);
				}
				throw;
			}
		}

		static bool IsDiagnosticSupported (DiagnosticDescriptor diag)
		{
			//filter out E&C analyzers as we don't support E&C
			if (diag.CustomTags.Contains (WellKnownDiagnosticTags.EditAndContinue)) {
				return false;
			}

			return true;
		}
	}
}
