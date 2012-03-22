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
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Refactoring;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.CodeIssues;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	public static class CodeAnalysis
	{
		static CodeAnalysis ()
		{
			foreach (var t in typeof (ICSharpCode.NRefactory.CSharp.Refactoring.ICodeIssueProvider).Assembly.GetTypes ()) {
				var attr = t.GetCustomAttributes (typeof(IssueDescriptionAttribute), false);
				if (attr == null || attr.Length != 1)
					continue;
				NRefactoryIssueWrapper provider = new NRefactoryIssueWrapper ((ICSharpCode.NRefactory.CSharp.Refactoring.ICodeIssueProvider)Activator.CreateInstance (t), (IssueDescriptionAttribute)attr [0]);
				RefactoringService.AddProvider (provider);
			}

		}
		
		public static IEnumerable<Result> Check (Document input, CancellationToken cancellationToken)
		{
			if (!QuickTaskStrip.EnableFancyFeatures)
				return Enumerable.Empty<Result> ();
			var loc = input.Editor.Caret.Location;
			var result = new BlockingCollection<Result> ();
			var codeIssueProvider = RefactoringService.GetInspectors ("text/x-csharp");
			Parallel.ForEach (codeIssueProvider, (provider) => {
				try {
					var severity = provider.GetSeverity ();
					if (severity == Severity.None)
						return;
					foreach (var r in provider.GetIssues (input, loc, cancellationToken)) {
						foreach (var a in r.Actions) {
							result.Add (new InspectorResults (
								provider, 
								new DomRegion (r.Start, r.End), 
								r.Description,
								severity, 
								provider.IssueMarker,
								new GenericFix (a.Title, new System.Action (() => a.Run (input, loc)))));
						}
					}
				} catch (Exception e) {
					LoggingService.LogError ("CodeAnalysis: Got exception in inspector '" + provider + "'", e);
				}
			});

			return result;
		}
			
	}
}