//
// CodeIssueBatchRunner.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;
using System.IO;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.Core;
using System.Collections.Concurrent;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.TextEditor;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.CodeIssues
{
	public class CodeAnalysisBatchRunner
	{
		private readonly object _lock = new object ();

		private int workerCount;

		private readonly AnalysisJobQueue jobQueue = new AnalysisJobQueue ();

		public IJobContext QueueJob (IAnalysisJob job)
		{
			jobQueue.Add (job);
			EnsureRunning ();
			return new JobContext (job, jobQueue, this);
		}

		private void EnsureRunning ()
		{
			for (; Interlocked.Add (ref workerCount, 1) < Environment.ProcessorCount;) {
				new Thread (() => {
					try {
						ProcessQueue ();
					}
					finally {
						Interlocked.Add (ref workerCount, -1);
					}
				}).Start ();
			}
		}

		private void ProcessQueue ()
		{
			while (true) {
				try {
					using (var slice = GetSlice ()) {
						if (slice == null)
							// TODO: Do something smarter if the queue is empty
							return;
						AnalyzeFile (slice, slice.GetJobs ().SelectMany (job => job.GetIssueProviders (slice.File)));
					}
				} catch (Exception e) {
					LoggingService.LogError ("Unhandled exception", e);
					MessageService.ShowException (e);
				}
			}
		}

		private JobSlice GetSlice ()
		{
			lock (_lock) {
				return jobQueue.Dequeue (1).FirstOrDefault ();
			}
		}

		void AnalyzeFile (JobSlice item, IEnumerable<BaseCodeIssueProvider> codeIssueProviders)
		{
			var file = item.File;

			if (file.BuildAction != BuildAction.Compile)
				return;

			TextEditorData editor;
			try {
				editor = TextFileProvider.Instance.GetReadOnlyTextEditorData (file.FilePath);
			} catch (FileNotFoundException) {
				// Swallow exception and ignore this file
				return;
			}
			var document = TypeSystemService.ParseFile (file.Project, editor);
			if (document == null)
				return;

			var content = TypeSystemService.GetProjectContext (file.Project);
			var compilation = content.AddOrUpdateFiles (document.ParsedFile).CreateCompilation ();

			CSharpAstResolver resolver;
			using (var timer = ExtensionMethods.ResolveCounter.BeginTiming ()) {
				resolver = new CSharpAstResolver (compilation, document.GetAst<SyntaxTree> (), document.ParsedFile as ICSharpCode.NRefactory.CSharp.TypeSystem.CSharpUnresolvedFile);
				try {
					resolver.ApplyNavigator (new ExtensionMethods.ConstantModeResolveVisitorNavigator (ResolveVisitorNavigationMode.Resolve, null));
				} catch (Exception e) {
					LoggingService.LogError ("Error while applying navigator", e);
				}
			}
			var context = document.CreateRefactoringContextWithEditor (editor, resolver, CancellationToken.None);

			foreach (var provider in codeIssueProviders) {
				if (item.CancellationToken.IsCancellationRequested)
					break;
				IList<IAnalysisJob> jobs;
				lock (_lock)
					jobs = item.GetJobs ().ToList ();
				var jobsForProvider = jobs.Where (j => j.GetIssueProviders (file).Contains (provider)).ToList();
				try {
					var issues = provider.GetIssues (context, CancellationToken.None).ToList ();
					foreach (var job in jobsForProvider) {
						// Call AddResult even if issues.Count == 0, to enable a job implementation to keep
						// track of progress information.
						job.AddResult (file, provider, issues);
					}
				} catch (OperationCanceledException) {
					// The operation was cancelled, no-op as the user-visible parts are
					// handled elsewhere
				} catch (Exception e) {
					foreach (var job in jobsForProvider) {
						job.AddError (file, provider);
					}
				}
			}
		}
	}
}

