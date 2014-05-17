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
using MonoDevelop.SourceEditor.QuickTasks;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;

namespace MonoDevelop.CodeIssues
{
	static class CodeIssueService
	{
		static readonly CodeIssueDescriptor[] codeIssues;
		static readonly ICodeFixProvider[] codeFixProvider;

		public static IReadOnlyList<ICodeFixProvider> CodeFixProvider {
			get {
				return codeFixProvider;
			}
		}

		static CodeIssueService ()
		{
			var analyzers = new List<CodeIssueDescriptor> ();
			var codeFixes = new List<ICodeFixProvider> ();
			
			foreach (var type in typeof (ICSharpCode.NRefactory6.CSharp.IssueCategories).Assembly.GetTypes ()) {
				var analyzerAttr = (ExportDiagnosticAnalyzerAttribute)type.GetCustomAttributes(typeof(ExportDiagnosticAnalyzerAttribute), false).FirstOrDefault ();
				if (analyzerAttr != null) {
					analyzers.Add (new CodeIssueDescriptor (analyzerAttr.Name, analyzerAttr.Language, type));
				}
				
				var codeFixAttr = (ExportCodeFixProviderAttribute)type.GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false).FirstOrDefault ();
				if (codeFixAttr != null) {
					codeFixes.Add ((ICodeFixProvider)Activator.CreateInstance(type)); 
				}
			}
			
			codeIssues = analyzers.ToArray ();
			codeFixProvider = codeFixes.ToArray ();
		}

		public static IEnumerable<CodeIssueDescriptor> GetCodeIssues (string language, bool includeDisabledNodes)
		{
			if (string.IsNullOrEmpty (language))
				return includeDisabledNodes ? codeIssues : codeIssues.Where (act => act.IsEnabled);
			return includeDisabledNodes ? codeIssues.Where (ca => ca.Language == language) : codeIssues.Where (ca => ca.Language == language && ca.IsEnabled);
		}
	}
}