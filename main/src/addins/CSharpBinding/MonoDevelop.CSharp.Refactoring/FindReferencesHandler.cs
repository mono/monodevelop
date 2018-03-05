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

namespace MonoDevelop.CSharp.Refactoring
{
	class FindReferencesHandler
	{
		internal static void FindRefs (ISymbol symbol, Solution solution)
		{
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			var workspace = TypeSystemService.Workspace as MonoDevelopWorkspace;
			if (workspace == null)
				return;
			Task.Run (async delegate {
				ITimeTracker timer = null;
				var metadata = MonoDevelop.Refactoring.Counters.CreateFindReferencesMetadata ();

				try {
					timer = MonoDevelop.Refactoring.Counters.FindReferences.BeginTiming (metadata);

					var antiDuplicatesSet = new HashSet<SearchResult> (new SearchResultComparer ());
					foreach (var loc in symbol.Locations) {
						if (monitor.CancellationToken.IsCancellationRequested)
							return;

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
						var sr = new MemberReference (symbol, fileName, offset, loc.SourceSpan.Length);
						sr.ReferenceUsageType = ReferenceUsageType.Declaration;
						antiDuplicatesSet.Add (sr);
						monitor.ReportResult (sr);
					}

					foreach (var mref in await SymbolFinder.FindReferencesAsync (symbol, solution, monitor.CancellationToken).ConfigureAwait (false)) {
						foreach (var loc in mref.Locations) {
							if (monitor.CancellationToken.IsCancellationRequested)
								return;
							var fileName = loc.Document.FilePath;
							var offset = loc.Location.SourceSpan.Start;
							string projectedName;
							int projectedOffset;
							if (workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
								fileName = projectedName;
								offset = projectedOffset;
							}
							var sr = new MemberReference (symbol, fileName, offset, loc.Location.SourceSpan.Length);
							if (antiDuplicatesSet.Add (sr)) {

								var root = loc.Location.SourceTree.GetRoot ();
								var node = root.FindNode (loc.Location.SourceSpan);
								var trivia = root.FindTrivia (loc.Location.SourceSpan.Start);
								sr.ReferenceUsageType = HighlightUsagesExtension.GetUsage (node);

								monitor.ReportResult (sr);
							}
						}
					}
				} catch (OperationCanceledException) {
				} catch (Exception ex) {
					MonoDevelop.Refactoring.Counters.SetFailure (metadata);
					if (monitor != null)
						monitor.ReportError ("Error finding references", ex);
					else
						LoggingService.LogError ("Error finding references", ex);
				} finally {
					if (monitor != null)
						monitor.Dispose ();
					if (monitor.CancellationToken.IsCancellationRequested)
						MonoDevelop.Refactoring.Counters.SetUserCancel (metadata);
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
			var pd = doc.ParsedDocument.GetAst<SemanticModel> ();
			info.Enabled = pd != null;
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
					FindRefs (sym, doc.AnalysisDocument.Project.Solution);
				} else {
					RefactoringService.FindReferencesAsync (FilterSymbolForFindReferences (sym).GetDocumentationCommentId ());
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
			var pd = doc.ParsedDocument.GetAst<SemanticModel> ();
			info.Enabled = pd != null;
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
					FindReferencesHandler.FindRefs (sym, doc.AnalysisDocument.Project.Solution);
				} else {
					RefactoringService.FindAllReferencesAsync (FindReferencesHandler.FilterSymbolForFindReferences (sym).GetDocumentationCommentId ());
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
