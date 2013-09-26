// 
// NRefactoryIssueWrapper.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide.Gui;
using System.Threading;
using MonoDevelop.CodeIssues;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	class NRefactoryIssueProvider : CodeIssueProvider
	{
		readonly ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssueProvider issueProvider;
		readonly IssueDescriptionAttribute attr;
		readonly string providerIdString;
		readonly TimerCounter counter;

		public override string IdString {
			get {
				return "refactoring.codeissues." + MimeType + "." + issueProvider.GetType ().FullName;
			}
		}

		public override bool HasSubIssues {
			get {
				return issueProvider.HasSubIssues;
			}
		}

		readonly List<BaseCodeIssueProvider> subIssues;
		public override IEnumerable<BaseCodeIssueProvider> SubIssues {
			get {
				return subIssues;
			}
		}

		public ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssueProvider IssueProvider {
			get {
				return issueProvider;
			}
		}

		public string ProviderIdString {
			get {
				return providerIdString;
			}
		}

		public NRefactoryIssueProvider (ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssueProvider issue, IssueDescriptionAttribute attr)
		{
			issueProvider = issue;
			this.attr = attr;
			providerIdString = issueProvider.GetType ().FullName;
			Category = GettextCatalog.GetString (attr.Category ?? "");
			Title = GettextCatalog.GetString (attr.Title ?? "");
			Description = GettextCatalog.GetString (attr.Description ?? "");
			DefaultSeverity = attr.Severity;
			IsEnabledByDefault = attr.IsEnabledByDefault;
			SetMimeType ("text/x-csharp");
			subIssues = issueProvider.SubIssues.Select (subIssue => (BaseCodeIssueProvider)new BaseNRefactoryIssueProvider (this, subIssue)).ToList ();

			counter = InstrumentationService.CreateTimerCounter (IdString, "CodeIssueProvider run times");
		}

		public override IEnumerable<CodeIssue> GetIssues (object ctx, CancellationToken cancellationToken)
		{
			var context = ctx as MDRefactoringContext;
			if (context == null || context.IsInvalid || context.RootNode == null || context.ParsedDocument.HasErrors)
				return new CodeIssue[0];
				
			// Holds all the actions in a particular sibling group.
			IList<ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue> issues;
			using (var timer = counter.BeginTiming ()) {
				// We need to enumerate here in order to time it. 
				// This shouldn't be a problem since there are current very few (if any) lazy providers.
				var _issues = issueProvider.GetIssues (context);
				issues = _issues as IList<ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue> ?? _issues.ToList ();
			}
			return ToMonoDevelopRepresentation (cancellationToken, context, issues);
		}

		IEnumerable<NRefactoryCodeAction> GetActions (ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue issue, MDRefactoringContext context)
		{
			foreach (var action in issue.Actions)
				yield return new NRefactoryCodeAction (IdString, action.Description, action, action.SiblingKey);

			if (issue.ActionProvider != null) {
				foreach (var provider in issue.ActionProvider) {
					var boundActionProvider = (ICSharpCode.NRefactory.CSharp.Refactoring.CodeActionProvider)Activator.CreateInstance (provider);
					context.SetLocation (issue.Start);
					foreach (var action in boundActionProvider.GetActions (context)) {
						yield return new NRefactoryCodeAction (provider.FullName, action.Description, action, action.SiblingKey);
					}
				}
			}
		}

		internal IEnumerable<CodeIssue> ToMonoDevelopRepresentation (CancellationToken cancellationToken, MDRefactoringContext context, IEnumerable<ICSharpCode.NRefactory.CSharp.Refactoring.CodeIssue> issues)
		{
			var actionGroups = new Dictionary<object, IList<MonoDevelop.CodeActions.CodeAction>> ();
			foreach (var issue in issues) {
				if (cancellationToken.IsCancellationRequested)
					yield break;
				if (issue.Actions == null) {
					LoggingService.LogError ("NRefactory actions == null in :" + Title);
					continue;
				}
				var actions = new List<NRefactoryCodeAction> ();
				foreach (var nrefactoryCodeAction in GetActions(issue, context)) {
					if (cancellationToken.IsCancellationRequested)
						yield break;
					if (nrefactoryCodeAction == null) {
						LoggingService.LogError ("NRefactory issue action was null in :" + Title);
						continue;
					}
					if (nrefactoryCodeAction.SiblingKey != null) {
						// make sure the action has a list of its siblings
						IList<MonoDevelop.CodeActions.CodeAction> siblingGroup;
						if (!actionGroups.TryGetValue (nrefactoryCodeAction.SiblingKey, out siblingGroup)) {
							siblingGroup = new List<MonoDevelop.CodeActions.CodeAction> ();
							actionGroups.Add (nrefactoryCodeAction.SiblingKey, siblingGroup);
						}
						siblingGroup.Add (nrefactoryCodeAction);
						nrefactoryCodeAction.SiblingActions = siblingGroup;
					}
					actions.Add (nrefactoryCodeAction);
				}
				yield return new CodeIssue (issue.IssueMarker, GettextCatalog.GetString (issue.Description ?? ""), context.TextEditor.FileName, issue.Start, issue.End, IdString, actions) {
					ActionProvider = issue.ActionProvider
				};
			}
		}	

		public override bool CanDisableOnce { get { return !string.IsNullOrEmpty (attr.AnalysisDisableKeyword); } }

		public override bool CanDisableAndRestore { get { return !string.IsNullOrEmpty (attr.AnalysisDisableKeyword); } }

		public override bool CanDisableWithPragma { get { return attr.PragmaWarning > 0; } }

		public override bool CanSuppressWithAttribute { get { return !string.IsNullOrEmpty (attr.SuppressMessageCheckId); } }

		const string analysisDisableTag = "Analysis ";

		public override void DisableOnce (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			document.Editor.Insert (
				document.Editor.LocationToOffset (loc.BeginLine, 1), 
				document.Editor.IndentationTracker.GetIndentationString (loc.Begin) + "// " + analysisDisableTag + "disable once " + attr.AnalysisDisableKeyword + document.Editor.EolMarker
			); 
		}

		public override void DisableAndRestore (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			using (document.Editor.OpenUndoGroup ()) {
				document.Editor.Insert (
					document.Editor.LocationToOffset (loc.EndLine + 1, 1),
					document.Editor.IndentationTracker.GetIndentationString (loc.End) + "// " + analysisDisableTag + "restore " + attr.AnalysisDisableKeyword + document.Editor.EolMarker
				); 
				document.Editor.Insert (
					document.Editor.LocationToOffset (loc.BeginLine, 1),
					document.Editor.IndentationTracker.GetIndentationString (loc.Begin) + "// " + analysisDisableTag + "disable " + attr.AnalysisDisableKeyword + document.Editor.EolMarker
				); 
			}
		}

		public override void DisableWithPragma (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			using (document.Editor.OpenUndoGroup ()) {
				document.Editor.Insert (
					document.Editor.LocationToOffset (loc.EndLine + 1, 1),
					document.Editor.IndentationTracker.GetIndentationString (loc.End) + "#pragma warning restore " + attr.PragmaWarning + document.Editor.EolMarker
				); 
				document.Editor.Insert (
					document.Editor.LocationToOffset (loc.BeginLine, 1),
					document.Editor.IndentationTracker.GetIndentationString (loc.Begin) + "#pragma warning disable " + attr.PragmaWarning + document.Editor.EolMarker
				); 
			}
		}

		public override void SuppressWithAttribute (MonoDevelop.Ide.Gui.Document document, DocumentRegion loc)
		{
			var member = document.ParsedDocument.GetMember (loc.End);
			document.Editor.Insert (
				document.Editor.LocationToOffset (member.Region.BeginLine, 1),
				document.Editor.IndentationTracker.GetIndentationString (loc.Begin) + string.Format ("[SuppressMessage(\"{0}\", \"{1}\")]" + document.Editor.EolMarker, attr.SuppressMessageCategory, attr.SuppressMessageCheckId)
			); 
		}
	}
}
