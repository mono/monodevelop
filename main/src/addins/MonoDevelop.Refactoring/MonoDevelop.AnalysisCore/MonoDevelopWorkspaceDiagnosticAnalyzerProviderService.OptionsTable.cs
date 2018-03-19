//
// MonoDevelopWorkspaceDiagnosticAnalyzerProviderService.OptionsTable.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.CodeActions;
using MonoDevelop.CodeIssues;
using MonoDevelop.Core;

namespace MonoDevelop.AnalysisCore
{
	partial class MonoDevelopWorkspaceDiagnosticAnalyzerProviderService
	{
		internal class OptionsTable
		{
			Dictionary<string, CodeDiagnosticDescriptor> diagnosticTable = new Dictionary<string, CodeDiagnosticDescriptor> ();
			Dictionary<Type, CodeRefactoringDescriptor> refactoringTable = new Dictionary<Type, CodeRefactoringDescriptor> ();
			List<CodeDiagnosticDescriptor> diagnostics = new List<CodeDiagnosticDescriptor> ();

			public IEnumerable<CodeDiagnosticDescriptor> AllDiagnostics => diagnostics;
			public IEnumerable<CodeRefactoringDescriptor> AllRefactorings => refactoringTable.Values;

			public bool TryGetDiagnosticDescriptor (string id, out CodeDiagnosticDescriptor desc)
			{
				return diagnosticTable.TryGetValue (id, out desc);
			}

			public bool TryGetRefactoringDescriptor (Type t, out CodeRefactoringDescriptor desc)
			{
				return refactoringTable.TryGetValue (t, out desc);
			}

			// Needed to support configuration of diagnostics and code fixes/refactorings.
			public void ProcessAssembly (Assembly asm)
			{
				try {
					foreach (var type in asm.GetTypes ()) {
						if (type.IsAbstract)
							continue;

						var analyzerAttr = (DiagnosticAnalyzerAttribute)type.GetCustomAttributes (typeof (DiagnosticAnalyzerAttribute), false).FirstOrDefault ();
						if (analyzerAttr != null) {
							try {
								var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance (type);
								var descriptor = new CodeDiagnosticDescriptor (analyzerAttr.Languages, type);

								foreach (var diag in analyzer.SupportedDiagnostics) {
									if (!IsDiagnosticSupported (diag))
										continue;
									
									diagnosticTable[diag.Id] = descriptor;
								}
								diagnostics.Add (descriptor);
							} catch (Exception e) {
								LoggingService.LogError ($"error while adding diagnostic analyzer {type}  from assembly {asm.FullName}", e);
							}
						}

						var exportAttr = type.GetCustomAttributes (typeof (ExportCodeRefactoringProviderAttribute), false).FirstOrDefault () as ExportCodeRefactoringProviderAttribute;
						if (exportAttr != null) {
							refactoringTable[type] = new CodeRefactoringDescriptor (type, exportAttr);
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
				return !diag.CustomTags.Contains (WellKnownDiagnosticTags.EditAndContinue);
			}
		}
	}
}
