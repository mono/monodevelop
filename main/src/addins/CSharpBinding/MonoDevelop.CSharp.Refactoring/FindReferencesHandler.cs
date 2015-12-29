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
using System.Linq;
using System.Threading;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Tasks;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Refactoring
{
	class FindReferencesHandler
	{
		public static void FindRefs (ISymbol symbol)
		{
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			var workspace = TypeSystemService.Workspace as MonoDevelopWorkspace;
			if (workspace == null)
				return;
			var solution = workspace.CurrentSolution;
			Task.Run (async delegate {
				try {
					var antiDuplicatesSet = new HashSet<SearchResult> (new SearchResultComparer ());
					foreach (var loc in symbol.Locations) {
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
						var sr = new SearchResult (new FileProvider (fileName), offset, loc.SourceSpan.Length);
						antiDuplicatesSet.Add (sr);
						monitor.ReportResult (sr);
					}

					foreach (var mref in await SymbolFinder.FindReferencesAsync (symbol, solution).ConfigureAwait (false)) {
						foreach (var loc in mref.Locations) {
							var fileName = loc.Document.FilePath;
							var offset = loc.Location.SourceSpan.Start;
							string projectedName;
							int projectedOffset;
							if (workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
								fileName = projectedName;
								offset = projectedOffset;
							}
							var sr = new SearchResult (new FileProvider (fileName), offset, loc.Location.SourceSpan.Length);
							if (antiDuplicatesSet.Add (sr)) {
								monitor.ReportResult (sr);
							}
						}
					}
				} catch (Exception ex) {
					if (monitor != null)
						monitor.ReportError ("Error finding references", ex);
					else
						LoggingService.LogError ("Error finding references", ex);
				} finally {
					if (monitor != null)
						monitor.Dispose ();
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

			var info = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor.CaretOffset).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (sym != null)
				FindRefs (sym);
		}
	}

	class FindAllReferencesHandler
	{
		public static void FindRefs (ISymbol obj, Compilation compilation)
		{
			var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true);
			var workspace = TypeSystemService.Workspace as MonoDevelopWorkspace;
			if (workspace == null)
				return;
			var solution = workspace.CurrentSolution;
			Task.Run (async delegate {
				try {
					var antiDuplicatesSet = new HashSet<SearchResult> (new SearchResultComparer ());
					foreach (var simSym in SymbolFinder.FindSimilarSymbols (obj, compilation)) {
						foreach (var loc in simSym.Locations) {
							var sr = new SearchResult (new FileProvider (loc.SourceTree.FilePath), loc.SourceSpan.Start, loc.SourceSpan.Length);
							if (antiDuplicatesSet.Add (sr)) {
								monitor.ReportResult (sr);
							}
						}

						foreach (var mref in await SymbolFinder.FindReferencesAsync (simSym, solution).ConfigureAwait (false)) {
							foreach (var loc in mref.Locations) {
								var fileName = loc.Document.FilePath;
								var offset = loc.Location.SourceSpan.Start;
								string projectedName;
								int projectedOffset;
								if (workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
									fileName = projectedName;
									offset = projectedOffset;
								}

								var sr = new SearchResult (new FileProvider (fileName), offset, loc.Location.SourceSpan.Length);
								if (antiDuplicatesSet.Add (sr)) {
									monitor.ReportResult (sr);
								}
							}
						}
					}
				} catch (Exception ex) {
					if (monitor != null)
						monitor.ReportError ("Error finding references", ex);
					else
						LoggingService.LogError ("Error finding references", ex);
				} finally {
					if (monitor != null)
						monitor.Dispose ();
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
			
			var info = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor.CaretOffset).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			var semanticModel = doc.ParsedDocument.GetAst<SemanticModel> ();
			if (sym != null)
				FindRefs (sym, semanticModel.Compilation);
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
