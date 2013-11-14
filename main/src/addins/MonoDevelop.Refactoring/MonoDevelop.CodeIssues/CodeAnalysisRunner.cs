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
using Mono.TextEditor;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.CodeActions;
using System.Diagnostics;

namespace MonoDevelop.CodeIssues
{
	public static class CodeAnalysisRunner
	{
		static IEnumerable<BaseCodeIssueProvider> EnumerateProvider (CodeIssueProvider p)
		{
			if (p.HasSubIssues)
				return p.SubIssues;
			return new BaseCodeIssueProvider[] { p };
		}

		public static IEnumerable<Result> Check (Document input, CancellationToken cancellationToken)
		{
			if (!QuickTaskStrip.EnableFancyFeatures || input.Project == null || !input.IsCompileableInProject)
				return Enumerable.Empty<Result> ();

			#if PROFILE
			var runList = new List<Tuple<long, string>> ();
			#endif
			var editor = input.Editor;
			if (editor == null)
				return Enumerable.Empty<Result> ();
			var loc = editor.Caret.Location;
			var result = new BlockingCollection<Result> ();
		
			var codeIssueProvider = RefactoringService.GetInspectors (editor.Document.MimeType).ToArray ();
			var context = input.ParsedDocument.CreateRefactoringContext != null ?
				input.ParsedDocument.CreateRefactoringContext (input, cancellationToken) : null;
			Parallel.ForEach (codeIssueProvider, (parentProvider) => {
				try {
					#if PROFILE
					var clock = new Stopwatch();
					clock.Start ();
					#endif
					foreach (var provider in EnumerateProvider (parentProvider)) {
						var severity = provider.GetSeverity ();
						if (severity == Severity.None || !provider.GetIsEnabled ())
							continue;
						foreach (var r in provider.GetIssues (context, cancellationToken)) {
							var fixes = r.Actions == null ? new List<GenericFix> () : new List<GenericFix> (r.Actions.Where (a => a != null).Select (a => {
								Action batchAction = null;
								if (a.SupportsBatchRunning)
									batchAction = () => a.BatchRun (input, loc);
								return new GenericFix (
									a.Title,
									() => {
										using (var script = context.CreateScript ()) {
											a.Run (context, script);
										}
									},
									batchAction) {
									DocumentRegion = new DocumentRegion (r.Region.Begin, r.Region.End),
									IdString = a.IdString
								};
							}));
							result.Add (new InspectorResults (
								provider, 
								r.Region, 
								r.Description,
								severity, 
								r.IssueMarker,
								fixes.ToArray ()
							));
						}
					}
					#if PROFILE
					clock.Stop ();
					lock (runList) {
						runList.Add (Tuple.Create (clock.ElapsedMilliseconds, parentProvider.Title)); 
					}
					#endif
				} catch (OperationCanceledException) {
					//ignore
				} catch (Exception e) {
					LoggingService.LogError ("CodeAnalysis: Got exception in inspector '" + parentProvider + "'", e);
				}
			});
#if PROFILE
			runList.Sort ();
			foreach (var item in runList) {
				Console.WriteLine (item.Item1 +"ms\t: " + item.Item2);
			}
#endif
			return result;
		}
	}
}

