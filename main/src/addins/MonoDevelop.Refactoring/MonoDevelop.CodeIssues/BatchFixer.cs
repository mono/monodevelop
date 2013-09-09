//
// BatchFixer.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.Refactoring;
using System.Threading;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor.Utils;
using System.Text;
using Mono.TextEditor;

namespace MonoDevelop.CodeIssues
{
	public class BatchFixer
	{
		readonly IActionMatcher matcher;

		readonly IProgressMonitor monitor;

		public BatchFixer (IActionMatcher matcher, IProgressMonitor monitor)
		{
			this.matcher = matcher;
			this.monitor = monitor;
		}

		/// <summary>
		/// Tries to apply the actions passed in <paramref name="actions"/>.
		/// </summary>
		/// <param name="actions">The actions to apply.</param>
		/// <returns>The fixed code actions.</returns>
		public IEnumerable<ActionSummary> TryFixIssues (IEnumerable<ActionSummary> actions)
		{
			if (actions == null)
				throw new ArgumentNullException ("actions");
				
			// enumerate once
			var actionSummaries = actions as IList<ActionSummary> ?? actions.ToList ();
			var issueSummaries = actionSummaries.Select (action => action.IssueSummary).ToList ();
			var files = issueSummaries.Select (issue => issue.File).Distinct ().ToList ();
			monitor.BeginTask ("Applying fixes", files.Count);
			
			var appliedActions = new List<ActionSummary> (issueSummaries.Count);
			Parallel.ForEach (files, file => {
				monitor.Step (1);
				
				var fileSummaries = issueSummaries.Where (summary => summary.File == file);
				var inspectorIds = new HashSet<string> (fileSummaries.Select (summary => summary.InspectorIdString));
				
				bool hadBom;
				Encoding encoding;
				bool isOpen;
				var data = TextFileProvider.Instance.GetTextEditorData (file.FilePath, out hadBom, out encoding, out isOpen);
				IRefactoringContext refactoringContext;
				var realActions = GetIssues (data, file, inspectorIds, out refactoringContext).SelectMany (issue => issue.Actions).ToList ();
				if (realActions.Count == 0 || refactoringContext == null)
					return;
				
				var fileActionSummaries = actionSummaries.Where (summary => summary.IssueSummary.File == file).ToList ();
				var matches = matcher.Match (fileActionSummaries, realActions).ToList ();
				
				var appliedFixes = RefactoringService.ApplyFixes (matches.Select (match => match.Action), refactoringContext);
				appliedActions.AddRange (matches.Where (match => appliedFixes.Contains (match.Action)).Select (match => match.Summary));
				
				if (!isOpen) {
					// If the file is open we leave it to the user to explicitly save the file
					TextFileUtility.WriteText (file.Name, data.Text, encoding, hadBom);
				}
			});
			return appliedActions;
		}

		static IList<CodeIssue> GetIssues (TextEditorData data, ProjectFile file, ISet<string> inspectorIds, out IRefactoringContext refactoringContext)
		{
			var issues = new List<CodeIssue> ();
			
			var document = TypeSystemService.ParseFile (file.Project, data);
			if (document == null) {
				refactoringContext = null;
				return issues;
			}

			var content = TypeSystemService.GetProjectContext (file.Project);
			var compilation  = content.AddOrUpdateFiles (document.ParsedFile).CreateCompilation ();
			var resolver = new CSharpAstResolver (compilation, document.GetAst<SyntaxTree> (), document.ParsedFile as ICSharpCode.NRefactory.CSharp.TypeSystem.CSharpUnresolvedFile);
			
			refactoringContext = document.CreateRefactoringContextWithEditor (data, resolver, CancellationToken.None);
			var context = refactoringContext;
			foreach (var provider in GetInspectors (data, inspectorIds)) {
				var severity = provider.GetSeverity ();
				if (severity == Severity.None || !provider.GetIsEnabled ())
					continue;
				try {
					lock (issues) {
						issues.AddRange (provider.GetIssues (context, CancellationToken.None));
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Error while running code issue on: " + data.FileName, ex);
				}
			}
			return issues;
		}

		static IList<CodeIssueProvider> GetInspectors (TextEditorData editor, ICollection<string> inspectorIds)
		{
			var inspectors = RefactoringService.GetInspectors (editor.MimeType).ToList ();
			return inspectors
				.Where (inspector => inspectorIds.Contains (inspector.IdString))
				.ToList ();
		}
	}
}

