//
// CSharpFindReferencesProvider.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.CSharp.Highlighting;

namespace MonoDevelop.CSharp.Refactoring
{
	class CSharpFindReferencesProvider : FindReferencesProvider
	{
		internal class LookupResult
		{
			public static LookupResult Failure = new LookupResult ();

			public bool Success { get; private set; }
			public ISymbol Symbol { get; private set; }
			public Solution Solution { get; private set; }
			public MonoDevelop.Projects.Project MonoDevelopProject { get; internal set; }
			public Compilation Compilation { get; private set; }

			public LookupResult ()
			{
			}

			public LookupResult (ISymbol symbol, Solution solution, Compilation compilation)
			{
				this.Success = true;
				this.Symbol = symbol;
				this.Solution = solution;
				this.Compilation = compilation;
			}
		}

		static async Task<LookupResult> TryLookupSymbolInProject (Microsoft.CodeAnalysis.Project prj, string documentationCommentId, CancellationToken token)
		{
			if (string.IsNullOrEmpty (documentationCommentId))
				return LookupResult.Failure;
			bool searchNs = documentationCommentId [0] == 'N';
			bool searchType = documentationCommentId [0] == 'T';
			int reminderIndex = 2;
			var comp = await prj.GetCompilationAsync (token).ConfigureAwait (false);
			return await Task.Run (() => {
				var current = LookupNamespace (documentationCommentId, ref reminderIndex, comp.GlobalNamespace, token);
				if (current == null)
					return LookupResult.Failure;
				if (searchNs) {
					if (current.GetDocumentationCommentId () == documentationCommentId)
						return new LookupResult (current, prj.Solution, comp);
					return LookupResult.Failure;
				}
				INamedTypeSymbol type = null;
				foreach (var t in current.GetTypeMembers ()) {
					if (token.IsCancellationRequested)
						return LookupResult.Failure;
					type = LookupType (documentationCommentId, reminderIndex, t, token);
					if (type != null) {
						if (searchType) {
							return new LookupResult (type, prj.Solution, comp);
						}
						break;
					}
				}
				if (type == null)
					return LookupResult.Failure;
				foreach (var member in type.GetMembers ()) {
					if (token.IsCancellationRequested)
						return LookupResult.Failure;

					if (member.GetDocumentationCommentId () == documentationCommentId) {
						return new LookupResult (member, prj.Solution, comp);
					}
				}
				return LookupResult.Failure;
			}, token);
		}

		internal static async Task<LookupResult> TryLookupSymbol (string documentationCommentId, MonoDevelop.Projects.Project hintProject, CancellationToken token)
		{
			Microsoft.CodeAnalysis.Project codeAnalysisHintProject = null;
			LookupResult result = LookupResult.Failure;

			if (hintProject != null) {
				codeAnalysisHintProject = TypeSystemService.GetCodeAnalysisProject (hintProject);
				if (codeAnalysisHintProject != null) {
					var curResult = await TryLookupSymbolInProject (codeAnalysisHintProject, documentationCommentId, token);
					if (curResult.Success) {
						curResult.MonoDevelopProject = hintProject;
						result = curResult;
					}
				}
			}
			if (result.Success && result.Symbol.IsDefinedInSource ())
				return result;
			foreach (var ws in TypeSystemService.AllWorkspaces) {
				foreach (var prj in ws.CurrentSolution.Projects) {
					if (prj == codeAnalysisHintProject)
						continue;
					var curResult = await TryLookupSymbolInProject (prj, documentationCommentId, token);
					if (curResult.Success) {
						curResult.MonoDevelopProject = TypeSystemService.GetMonoProject (prj);
						if (curResult.Symbol.IsDefinedInSource ())
							return curResult;
						result = curResult;
					}
				}
			}

			return result;
		}

		static INamedTypeSymbol LookupType (string documentationCommentId, int reminder, INamedTypeSymbol current, CancellationToken token)
		{
			var idx = documentationCommentId.IndexOf ('.', reminder);
			var exact = idx < 0;
			var typeId = current.GetDocumentationCommentId ();
			if (exact) {
				if (typeId == documentationCommentId)
					return current;
				return null;
			}

			if (typeId.Length < reminder)
				return null;
			if (string.CompareOrdinal (documentationCommentId, reminder, typeId, reminder, idx - reminder - 1) == 0) {
				if (typeId.Length > idx)
					return null;
				foreach (var subType in current.GetTypeMembers ()) {
					if (token.IsCancellationRequested)
						return null;

					var child = LookupType (documentationCommentId, idx + 1, subType, token);
					if (child != null) {
						return child;
					}
				}
				return current;

			}
			return null;
		}

		static INamespaceSymbol LookupNamespace (string documentationCommentId, ref int reminder, INamespaceSymbol current, CancellationToken token)
		{
			var exact = documentationCommentId.IndexOf ('.', reminder) < 0;

			foreach (var subNamespace in current.GetNamespaceMembers ()) {
				if (token.IsCancellationRequested)
					return null;

				if (exact) {
					if (subNamespace.Name.Length == documentationCommentId.Length - reminder &&
						string.CompareOrdinal (documentationCommentId, reminder, subNamespace.Name, 0, subNamespace.Name.Length) == 0)
						return subNamespace;
				} else {
					if (subNamespace.Name.Length < documentationCommentId.Length - reminder - 1 &&
						string.CompareOrdinal (documentationCommentId, reminder, subNamespace.Name, 0, subNamespace.Name.Length) == 0 &&
						documentationCommentId [reminder + subNamespace.Name.Length] == '.') {
						reminder += subNamespace.Name.Length + 1;
						return LookupNamespace (documentationCommentId, ref reminder, subNamespace, token);
					}
				}
			}

			return current;
		}

		public override Task<IEnumerable<SearchResult>> FindReferences (string documentationCommentId, MonoDevelop.Projects.Project hintProject, CancellationToken token)
		{
			return Task.Run (async delegate {
				var result = new List<SearchResult> ();
				var antiDuplicatesSet = new HashSet<SearchResult> (new SearchResultComparer ());
				var lookup = await TryLookupSymbol (documentationCommentId, hintProject, token);
				if (lookup == null || !lookup.Success)
					return Enumerable.Empty<SearchResult> ();

				var workspace = TypeSystemService.AllWorkspaces.FirstOrDefault (w => w.CurrentSolution == lookup.Solution) as MonoDevelopWorkspace;
				if (workspace == null)
					return Enumerable.Empty<SearchResult> ();
				foreach (var sym in await GatherSymbols (lookup.Symbol, lookup.Solution, token)) {
					foreach (var loc in sym.Locations) {
						if (token.IsCancellationRequested)
							break;

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
						var sr = new MemberReference (sym, fileName, offset, loc.SourceSpan.Length);
						sr.ReferenceUsageType = ReferenceUsageType.Declaration;
						antiDuplicatesSet.Add (sr);
						result.Add (sr);
					}

					foreach (var mref in await SymbolFinder.FindReferencesAsync (sym, lookup.Solution, token).ConfigureAwait (false)) {
						foreach (var loc in mref.Locations) {
							if (token.IsCancellationRequested)
								break;
							var fileName = loc.Document.FilePath;
							var offset = loc.Location.SourceSpan.Start;
							string projectedName;
							int projectedOffset;
							if (workspace.TryGetOriginalFileFromProjection (fileName, offset, out projectedName, out projectedOffset)) {
								fileName = projectedName;
								offset = projectedOffset;
							}
							var sr = new MemberReference (sym, fileName, offset, loc.Location.SourceSpan.Length);


							if (antiDuplicatesSet.Add (sr)) {
								var root = loc.Location.SourceTree.GetRoot ();
								var node = root.FindNode (loc.Location.SourceSpan);
								var trivia = root.FindTrivia (loc.Location.SourceSpan.Start);
								sr.ReferenceUsageType = HighlightUsagesExtension.GetUsage (node);
								result.Add (sr);
							}
						}
					}
				}
				return (IEnumerable<SearchResult>)result;
			});
		}

		public static async Task<IEnumerable<ISymbol>> GatherSymbols (ISymbol symbol, Solution solution, CancellationToken token)
		{
			var implementations = await SymbolFinder.FindImplementationsAsync (symbol, solution, null, token);
			var result = new List<ISymbol> ();
			result.Add (symbol);
			result.AddRange (implementations);
			return result;
		}

		public override Task<IEnumerable<SearchResult>> FindAllReferences (string documentationCommentId, MonoDevelop.Projects.Project hintProject, CancellationToken token)
		{
			return Task.Run (async delegate {
				var antiDuplicatesSet = new HashSet<SearchResult> (new SearchResultComparer ());
				var result = new List<SearchResult> ();
				var lookup = await TryLookupSymbol (documentationCommentId, hintProject, token);
				if (!lookup.Success)
					return result;
				var workspace = TypeSystemService.AllWorkspaces.FirstOrDefault (w => w.CurrentSolution == lookup.Solution) as MonoDevelopWorkspace;
				if (workspace == null)
					return Enumerable.Empty<SearchResult> ();
				if (lookup.Symbol.Kind == SymbolKind.Method) {
					foreach (var curSymbol in lookup.Symbol.ContainingType.GetMembers ().Where (m => m.Kind == lookup.Symbol.Kind && m.Name == lookup.Symbol.Name)) {
						foreach (var sym in SymbolFinder.FindSimilarSymbols (curSymbol, lookup.Compilation)) {
							foreach (var simSym in await GatherSymbols (sym, lookup.Solution, token)) {
								await FindSymbolReferencesAsync (antiDuplicatesSet, result, lookup, workspace, simSym);
							}
						}
					}
				} else {
					await FindSymbolReferencesAsync (antiDuplicatesSet, result, lookup, workspace, lookup.Symbol);
				}
				return (IEnumerable<SearchResult>)result;
			});
		}

		static async Task FindSymbolReferencesAsync (HashSet<SearchResult> antiDuplicatesSet, List<SearchResult> result, LookupResult lookup, MonoDevelopWorkspace workspace, ISymbol simSym)
		{
			foreach (var loc in simSym.Locations) {
				if (!loc.IsInSource)
					continue;
				var sr = new SearchResult (new FileProvider (loc.SourceTree.FilePath), loc.SourceSpan.Start, loc.SourceSpan.Length);
				if (antiDuplicatesSet.Add (sr)) {
					result.Add (sr);
				}
			}

			foreach (var mref in await SymbolFinder.FindReferencesAsync (simSym, lookup.Solution).ConfigureAwait (false)) {
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
						result.Add (sr);
					}
				}
			}
		}
	}

}
