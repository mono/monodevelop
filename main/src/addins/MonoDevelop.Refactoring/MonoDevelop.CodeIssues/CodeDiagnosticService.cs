// 
// CodeAnalysisRunner.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
//#define PROFILE
using System;
using System.Linq;
using MonoDevelop.AnalysisCore;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using System.Threading;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using System.Diagnostics;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.CodeIssues
{
	static class CodeDiagnosticService
	{
		readonly static Task<BuiltInDiagnostics> builtInDiagnostics;

		class BuiltInDiagnostics
		{
			public List<CodeDiagnosticDescriptor> Analyzers;
			public List<CodeFixDescriptor> Fixes;
		}

		static readonly object loadingLock = new object();

		static CodeDiagnosticService ()
		{
			
			builtInDiagnostics = Task.Run (() => {
				lock (loadingLock) {
					var result = new BuiltInDiagnostics ();
					result.Analyzers = new List<CodeDiagnosticDescriptor> ();
					result.Fixes = new List<CodeFixDescriptor> ();
					foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
						try {
							CheckAddins (result.Analyzers, result.Fixes, asm);
						} catch (Exception e) {
							LoggingService.LogError ("error while loading diagnostics in " + asm.FullName, e);
						}
					}
					return result;
				}
			});
		}

		readonly static string diagnosticAnalyzerAssembly = typeof (DiagnosticAnalyzerAttribute).Assembly.GetName ().Name;

		static void CheckAddins (List<CodeDiagnosticDescriptor> analyzers, List<CodeFixDescriptor> codeFixes, System.Reflection.Assembly asm)
		{
			var assemblyName = asm.GetName ().Name;
			if (assemblyName == "MonoDevelop.AspNet" ||
				assemblyName != "ICSharpCode.NRefactory6.CSharp.Refactoring" &&
				!(asm.GetReferencedAssemblies ().Any (a => a.Name == diagnosticAnalyzerAssembly) && asm.GetReferencedAssemblies ().Any (a => a.Name == "MonoDevelop.Ide")))
				return;
 			foreach (var type in asm.GetTypes ()) {
				var analyzerAttr = (DiagnosticAnalyzerAttribute)type.GetCustomAttributes (typeof(DiagnosticAnalyzerAttribute), false).FirstOrDefault ();
				var nrefactoryAnalyzerAttribute = (NRefactoryCodeDiagnosticAnalyzerAttribute)type.GetCustomAttributes (typeof(NRefactoryCodeDiagnosticAnalyzerAttribute), false).FirstOrDefault ();
				if (analyzerAttr != null) {
					var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance (type);
					foreach (var diag in analyzer.SupportedDiagnostics) {
						analyzers.Add (new CodeDiagnosticDescriptor (diag.Title.ToString (), new[] {
							"C#"
						}, type, nrefactoryAnalyzerAttribute));
					}
				}
				var codeFixAttr = (ExportCodeFixProviderAttribute)type.GetCustomAttributes (typeof(ExportCodeFixProviderAttribute), false).FirstOrDefault ();
				if (codeFixAttr != null) {
					codeFixes.Add (new CodeFixDescriptor (type, codeFixAttr));
				}
			}
		}

		public async static Task<IEnumerable<CodeDiagnosticDescriptor>> GetBuiltInCodeIssuesAsync (string language, bool includeDisabledNodes = false, CancellationToken cancellationToken = default (CancellationToken))
		{
			var diags = await builtInDiagnostics.ConfigureAwait (false);
			var builtInCodeDiagnostics = diags.Analyzers;
			if (string.IsNullOrEmpty (language))
				return includeDisabledNodes ? builtInCodeDiagnostics : builtInCodeDiagnostics.Where (act => act.IsEnabled);
			return includeDisabledNodes ? builtInCodeDiagnostics.Where (ca => ca.Languages.Contains (language)) : builtInCodeDiagnostics.Where (ca => ca.Languages.Contains (language) && ca.IsEnabled);
		}

		public async static Task<IEnumerable<CodeFixDescriptor>> GetBuiltInCodeFixDescriptorAsync (string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var diags = await builtInDiagnostics.ConfigureAwait (false);
			var builtInCodeFixes = diags.Fixes;
			if (string.IsNullOrEmpty (language))
				return builtInCodeFixes;
			return builtInCodeFixes.Where (cfp => cfp.Languages.Contains (language));
		}
	}
}