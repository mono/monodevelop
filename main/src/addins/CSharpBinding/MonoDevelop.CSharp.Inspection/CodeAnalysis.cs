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
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharp.ContextAction;
using MonoDevelop.Refactoring;
using MonoDevelop.Inspection;
using Mono.Addins;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using System.Threading;
using MonoDevelop.SourceEditor;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CSharp.Inspection
{
	public class InspectionData	
	{
		readonly CancellationToken cancellationToken;
		readonly List<Result> results = new List<Result> ();
		
		public IEnumerable<Result> Results {
			get { return results; }
		}

		public CancellationToken CancellationToken {
			get {
				return cancellationToken;
			}
		}
		
		public CallGraph Graph { get; set; }
		public Document Document { get; set; }
		
		public InspectionData (CancellationToken cancellationToken)
		{
			this.cancellationToken = cancellationToken;
		}
		
		public void Add (Result result)
		{
			results.Add (result);
		}

		public CSharpResolver GetResolverStateBefore (AstNode node)
		{
			return Graph.Resolver.GetResolverStateBefore (node);
		}
		
		public ResolveResult GetResolveResult (AstNode node)
		{
			return Graph.Resolver.Resolve (node);
		}
	}
	
	class NRefactoryIssueWrapper : CodeIssueProvider
	{
		ICSharpCode.NRefactory.CSharp.Refactoring.ICodeIssueProvider issueProvider;
		IssueDescriptionAttribute attr;
		
		public override string IdString {
			get {
				return "refactoring.inspectors." + MimeType + "." + issueProvider.GetType ().FullName;
			}
		}

		public NRefactoryIssueWrapper (ICSharpCode.NRefactory.CSharp.Refactoring.ICodeIssueProvider issue, IssueDescriptionAttribute attr)
		{
			this.issueProvider = issue;
			this.attr = attr;
			this.MimeType = "text/x-csharp";
			this.Category = attr.Category;
			this.Title = attr.Title;
			this.Description = attr.Description;
			this.Severity = attr.Severity;
			this.IssueMarker = attr.IssueMarker;
		}

		public override IEnumerable<CodeIssue> GetIssues (Document document, ICSharpCode.NRefactory.TextLocation loc, CancellationToken cancellationToken)
		{
			var context = new MDRefactoringContext (document, loc);
			foreach (var action in issueProvider.GetIssues (context)) {
				var issue = new CodeIssue (action.Desription, action.Start, action.End, new [] {action.Action}.Select (
					act => new MDRefactoringContextAction (act.Description, ctx => {
						using (var script = ctx.StartScript ())
							act.Run (script);
					})));
				yield return issue;
			}
		}
		
	}

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