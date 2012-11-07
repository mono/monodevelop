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

namespace MonoDevelop.CodeIssues
{
	public static class CodeAnalysisRunner
	{
		public static IEnumerable<Result> Check (Document input, CancellationToken cancellationToken)
		{
			if (!QuickTaskStrip.EnableFancyFeatures)
				return Enumerable.Empty<Result> ();

//			var now = DateTime.Now;

			var editor = input.Editor;
			if (editor == null)
				return Enumerable.Empty<Result> ();
			var loc = editor.Caret.Location;
			var result = new BlockingCollection<Result> ();
		
			var codeIssueProvider = RefactoringService.GetInspectors (editor.Document.MimeType).ToArray ();
			var context = input.ParsedDocument.CreateRefactoringContext != null ?
				input.ParsedDocument.CreateRefactoringContext (input, cancellationToken) : null;
//			Console.WriteLine ("start check:"+ (DateTime.Now - now).TotalMilliseconds);
			Parallel.ForEach (codeIssueProvider, (provider) => {
				try {
					var severity = provider.GetSeverity ();
					if (severity == Severity.None)
						return;
//					var now2 = DateTime.Now;
					foreach (var r in provider.GetIssues (input, context, cancellationToken)) {
						var fixes = new List<GenericFix> (r.Actions.Where (a => a != null).Select (a => new GenericFix (a.Title, new System.Action (() => a.Run (input, loc)))));
						result.Add (new InspectorResults (
							provider, 
							r.Region, 
							r.Description,
							severity, 
							provider.IssueMarker,
							fixes.ToArray ()
						));
					}
/*					var ms = (DateTime.Now - now2).TotalMilliseconds;
					if (ms > 1000)
						Console.WriteLine (ms +"\t\t"+ provider.Title);*/
				} catch (OperationCanceledException) {
					//ignore
				} catch (Exception e) {
					LoggingService.LogError ("CodeAnalysis: Got exception in inspector '" + provider + "'", e);
				}
			});
//			Console.WriteLine ("END check:"+ (DateTime.Now - now).TotalMilliseconds);
			return result;
		}
	}
}

