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
using System.IO;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Refactoring;
using System.Threading;
using MonoDevelop.Projects;

namespace MonoDevelop.CodeIssues
{
	public class BatchFixer
	{
		object _lock = new object ();
		IIssueMatcher matcher;

		static string lastMime;

		static TypeSystemParser parser;

		public BatchFixer (IIssueMatcher matcher)
		{
			this.matcher = matcher;
		}

		/// <summary>
		/// Tries the fix the issues passed in <paramref name="issues"/>.
		/// </summary>
		/// <param name="issues">The issues to fix.</param>
		/// <returns>The fix issues.</returns>
		public IEnumerable<IssueSummary> TryFixIssues (IEnumerable<ActionSummary> actions)
		{
			// enumerate once
			var actionSummaries = actions as IList<ActionSummary> ?? actions.ToArray ();
			var summaries = actionSummaries.Select (action => action.IssueSummary).ToArray ();
			var files = summaries.Select (issue => issue.File);
			
			var fixedIssues = new List<IssueSummary> (summaries.Length);
			foreach (var file in files) {
				var fileSummaries = summaries.Where (summary => summary.File == file);
				var inspectorTypes = new HashSet<Type> (fileSummaries.Select (summary => summary.InspectorType));
				var realIssues = GetIssues (file, inspectorTypes);
				var matches = matcher.Match (summaries, realIssues);
				
				foreach (var match in matches) {
					var codeAction = match.Issue;
				}
			}
		}

		static IList<CodeIssue> GetIssues (ProjectFile file, ISet<Type> inspectorTypes)
		{
			lastMime = null;
			parser = null;
			CodeIssueProvider[] codeIssueProvider = null;
			var editor = TextFileProvider.Instance.GetReadOnlyTextEditorData (file.FilePath);
			var project = file.Project;
			var compilation = TypeSystemService.GetCompilation (project);
			
			if (lastMime != editor.MimeType || parser == null)
				parser = TypeSystemService.GetParser (editor.MimeType);
			if (parser == null)
				continue;
			ParsedDocument document;
			using (var stream = editor.OpenStream ())
			using (var reader = new StreamReader (stream)) {
				document = parser.Parse (true, editor.FileName, reader, project);
			}
			if (document == null)
				continue;
			var resolver = new CSharpAstResolver (compilation, document.GetAst<SyntaxTree> (), document.ParsedFile as ICSharpCode.NRefactory.CSharp.TypeSystem.CSharpUnresolvedFile);
			var context = document.CreateRefactoringContextWithEditor (editor, resolver, CancellationToken.None);
			if (lastMime != editor.MimeType || codeIssueProvider == null)
				codeIssueProvider = GetInspectors (editor, inspectorTypes);
			Parallel.ForEach (codeIssueProvider, provider => {
				var severity = provider.GetSeverity ();
				if (severity == Severity.None)
					return;
				try {
					var realIssues = provider;
				} catch (Exception ex) {
					LoggingService.LogError ("Error while running code issue on: " + editor.FileName, ex);
				}
			});
		}

		CodeIssueProvider[] GetInspectors (Mono.TextEditor.TextEditorData editor, ISet<Type> inspectorTypes)
		{
			return RefactoringService.GetInspectors (editor.MimeType)
				.Where (inspector => inspectorTypes.Contains (inspector.GetType ()))
				.ToArray ();
		}
	}
}

