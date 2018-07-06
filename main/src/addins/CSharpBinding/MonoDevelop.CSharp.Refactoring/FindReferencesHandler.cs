// 
// FindReferencesHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.CSharp.Highlighting;
using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Refactoring;
using Roslyn.Utilities;
using System.Threading;

namespace MonoDevelop.CSharp.Refactoring
{
	class FindReferencesHandler
	{
		class StreamingFindReferencesProgress : IStreamingFindReferencesProgress
		{
			ConcurrentSet<SearchResult> antiDuplicatesSet = new ConcurrentSet<SearchResult> (new SearchResultComparer ());
			private SearchProgressMonitor monitor;
			private MonoDevelopWorkspace workspace;

			object reportingLock = new object ();

			public StreamingFindReferencesProgress (SearchProgressMonitor monitor, MonoDevelopWorkspace workspace)
			{
				this.monitor = monitor;
				this.workspace = workspace;
			}

			public Task OnCompletedAsync ()
			{
				if (!monitor.CancellationToken.IsCancellationRequested)
					monitor.ReportResults (antiDuplicatesSet);
				return Task.CompletedTask;
			}

			public Task OnDefinitionFoundAsync (SymbolAndProjectId symbolAndProjectId)
			{
				foreach (var loc in symbolAndProjectId.Symbol.Locations) {
					if (!loc.IsInSource)
						continue;
					var fileName = loc.SourceTree.FilePath;
					var offset = loc.SourceSpan.Start;
					string projectedName;
					int projectedOffset;
					if (workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
						fileName = projectedName;
						offset = projectedOffset;
					}
					var sr = new MemberReference (symbolAndProjectId.Symbol, fileName, offset, loc.SourceSpan.Length);
					sr.ReferenceUsageType = ReferenceUsageType.Declaration;
					antiDuplicatesSet.Add (sr);
				}
				return Task.CompletedTask;
			}

			public Task OnFindInDocumentCompletedAsync (Document document)
			{
				return Task.CompletedTask;
			}

			public Task OnFindInDocumentStartedAsync (Document document)
			{
				return Task.CompletedTask;
			}

			public Task OnReferenceFoundAsync (SymbolAndProjectId symbolAndProjectId, ReferenceLocation loc)
			{
				var fileName = loc.Document.FilePath;
				var offset = loc.Location.SourceSpan.Start;
				string projectedName;
				int projectedOffset;
				if (workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
					fileName = projectedName;
					offset = projectedOffset;
				}
				var sr = new MemberReference (symbolAndProjectId.Symbol, fileName, offset, loc.Location.SourceSpan.Length);
				if (antiDuplicatesSet.Add (sr)) {
					var root = loc.Location.SourceTree.GetRoot ();
					var node = root.FindNode (loc.Location.SourceSpan);
					var trivia = root.FindTrivia (loc.Location.SourceSpan.Start);
					sr.ReferenceUsageType = HighlightUsagesExtension.GetUsage (node);
				}
				return Task.CompletedTask;
			}

			public Task OnStartedAsync ()
			{
				return Task.CompletedTask;
			}

			internal double Progress;
			internal Action ProgressUpdated;

			public Task ReportProgressAsync (int current, int maximum)
			{
				Progress = (double)current / maximum;
				ProgressUpdated?.Invoke ();
				return Task.CompletedTask;
			}
		}

		internal static Task FindRefs (SymbolAndProjectId[] symbolAndProjectIds, Solution solution, SearchProgressMonitor monitor = null)
		{
			bool owningMonitor = false;
			if (monitor == null) {
				monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
				owningMonitor = true;
			}
			var workspace = TypeSystemService.Workspace as MonoDevelopWorkspace;
			if (workspace == null)
				return Task.CompletedTask;
			return Task.Run (async delegate {
				ITimeTracker timer = null;
				var metadata = MonoDevelop.Refactoring.Counters.CreateFindReferencesMetadata ();

				try {
					timer = MonoDevelop.Refactoring.Counters.FindReferences.BeginTiming (metadata);
					monitor.BeginTask (GettextCatalog.GetString ("Searching..."), 100);

					var streamingProgresses = new StreamingFindReferencesProgress [symbolAndProjectIds.Length];
					var tasks = new Task [symbolAndProjectIds.Length];
					int reportedProgress = 0;
					for (int i = 0; i < symbolAndProjectIds.Length; i++) {
						var reportingProgress = new StreamingFindReferencesProgress (monitor, workspace);
						reportingProgress.ProgressUpdated = delegate {
							double sumOfProgress = 0;
							for (int j = 0; j < streamingProgresses.Length; j++) {
								sumOfProgress += streamingProgresses [j].Progress;
							}
							int newProgress = (int)((sumOfProgress / streamingProgresses.Length) * 100);
							if (newProgress > reportedProgress) {
								lock (streamingProgresses) {
									if (newProgress > reportedProgress) {
										monitor.Step (newProgress - reportedProgress);
										reportedProgress = newProgress;
									}
								}
							}
						};
						streamingProgresses [i] = reportingProgress;
					}
					for (int i = 0; i < tasks.Length; i++) {
						tasks [i] = SymbolFinder.FindReferencesAsync (symbolAndProjectIds [i], solution, streamingProgresses [i], null, monitor.CancellationToken);
					}
					await Task.WhenAll (tasks);
				} catch (OperationCanceledException) {
				} catch (Exception ex) {
					metadata.SetFailure ();
					if (monitor != null)
						monitor.ReportError ("Error finding references", ex);
					else
						LoggingService.LogError ("Error finding references", ex);
				} finally {
					if (owningMonitor)
						monitor.Dispose ();
					if (monitor.CancellationToken.IsCancellationRequested)
						metadata.SetUserCancel ();
					if (timer != null)
						timer.Dispose ();
				}
			});
		}

		public void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null) {
				info.Enabled = false;
				return;
			}
			info.Enabled = doc.AnalysisDocument != null;
		}

		public void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;

			var info = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (sym != null) {
				if (sym.Kind == SymbolKind.Local || sym.Kind == SymbolKind.Parameter || sym.Kind == SymbolKind.TypeParameter) {
					FindRefs (new [] { SymbolAndProjectId.Create (sym, doc.AnalysisDocument.Project.Id) }, doc.AnalysisDocument.Project.Solution).Ignore ();
				} else {
					RefactoringService.FindReferencesAsync (FilterSymbolForFindReferences (sym).GetDocumentationCommentId ()).Ignore ();
				}
			}
		}

		internal static ISymbol FilterSymbolForFindReferences (ISymbol sym)
		{
			var meth = sym as IMethodSymbol;
			if (meth != null && meth.IsReducedExtension ()) {
				return meth.ReducedFrom;
			}
			return sym;
		}
	}

	class FindAllReferencesHandler
	{
		public void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null) {
				info.Enabled = false;
				return;
			}
			info.Enabled = doc.AnalysisDocument != null;
		}

		public void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			
			var info = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (sym != null) {
				if (sym.Kind == SymbolKind.Local || sym.Kind == SymbolKind.Parameter || sym.Kind == SymbolKind.TypeParameter) {
					FindReferencesHandler.FindRefs (new [] { SymbolAndProjectId.Create (sym, doc.AnalysisDocument.Project.Id) }, doc.AnalysisDocument.Project.Solution).Ignore ();
				} else {
					RefactoringService.FindAllReferencesAsync (FindReferencesHandler.FilterSymbolForFindReferences (sym).GetDocumentationCommentId ()).Ignore ();
				}
			}
		}
	}

	class SearchResultComparer : IEqualityComparer<SearchResult>
	{
		public bool Equals (SearchResult x, SearchResult y)
		{
			return x.FileName == y.FileName &&
				        x.Offset == y.Offset &&
				        x.Length == y.Length;
		}

		public int GetHashCode (SearchResult obj)
		{
			int hash = 17;
			hash = hash * 23 + obj.Offset.GetHashCode ();
			hash = hash * 23 + obj.Length.GetHashCode ();
			hash = hash * 23 + (obj.FileName ?? "").GetHashCode ();
			return hash;
		}
	}
}
