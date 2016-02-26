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

namespace MonoDevelop.CSharp.Refactoring
{
	class CSharpFindReferencesProvider : FindReferencesProvider
	{
		internal class LookupResult
		{
			public static LookupResult Failure = new LookupResult ();

			public bool Success  { get; private set; }
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
			bool searchNs = documentationCommentId[0] == 'N';
			bool searchType = documentationCommentId[0] == 'T';
			int reminderIndex = 2;
			var comp = await prj.GetCompilationAsync (token).ConfigureAwait (false);
			var current = LookupNamespace (documentationCommentId, ref reminderIndex, comp.GlobalNamespace);
			if (current == null)
				return LookupResult.Failure;
			if (searchNs) {
				if (current.GetDocumentationCommentId () == documentationCommentId)
					return new LookupResult (current, prj.Solution, comp);
				return LookupResult.Failure;
			}
			
			INamedTypeSymbol type = null;
			foreach (var t in current.GetAllTypes ()) {
				type = LookupType (documentationCommentId, reminderIndex, t);
				if (type != null) {
					if (searchType) {
						return new LookupResult(type, prj.Solution, comp);
					}
					break;
				}
			}
			if (type == null)
				return LookupResult.Failure;
			foreach (var member in type.GetMembers ()) {
				if (member.GetDocumentationCommentId () == documentationCommentId) {
					return new LookupResult(member, prj.Solution, comp);
				}
			}
			return LookupResult.Failure;
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

		static INamedTypeSymbol LookupType (string documentationCommentId, int reminder, INamedTypeSymbol current)
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
					var child = LookupType (documentationCommentId, idx  + 1, subType);
					if (child != null) {
						return child;
					}
				}
				return current;
				
			}
			return null;
		}

		static INamespaceSymbol LookupNamespace (string documentationCommentId, ref int reminder, INamespaceSymbol current)
		{
			var exact = documentationCommentId.IndexOf ('.', reminder) < 0;

			foreach (var subNamespace in current.GetNamespaceMembers ()) {
				if (exact) {
					if (subNamespace.Name.Length == documentationCommentId.Length - reminder &&
					    string.CompareOrdinal (documentationCommentId, reminder, subNamespace.Name, 0, subNamespace.Name.Length) == 0)
						return subNamespace;
				} else {
					if (subNamespace.Name.Length < documentationCommentId.Length - reminder - 1 &&
						string.CompareOrdinal (documentationCommentId, reminder, subNamespace.Name, 0, subNamespace.Name.Length) == 0 &&
						documentationCommentId [reminder + subNamespace.Name.Length] == '.') {
						reminder += subNamespace.Name.Length + 1;
						return LookupNamespace (documentationCommentId, ref reminder, subNamespace);
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
				foreach (var workspace in TypeSystemService.AllWorkspaces.OfType<MonoDevelopWorkspace> ()) {
					LookupResult lookup = null;

					foreach (var project in workspace.CurrentSolution.Projects) {
						lookup = await TryLookupSymbolInProject (project, documentationCommentId, token);
						if (lookup.Success)
							break;
					}

					if (lookup == null || !lookup.Success) {
						continue;
					}

					foreach (var loc in lookup.Symbol.Locations) {
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
						result.Add (sr);
					}

					foreach (var mref in await SymbolFinder.FindReferencesAsync (lookup.Symbol, lookup.Solution).ConfigureAwait (false)) {
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
				return (IEnumerable<SearchResult>)result;
			});
		}

		public override Task<IEnumerable<SearchResult>> FindAllReferences (string documentationCommentId, MonoDevelop.Projects.Project hintProject, CancellationToken token)
		{
			var workspace = TypeSystemService.Workspace as MonoDevelopWorkspace;
			if (workspace == null)
				return Task.FromResult (Enumerable.Empty<SearchResult> ());
			return Task.Run (async delegate {
				var antiDuplicatesSet = new HashSet<SearchResult> (new SearchResultComparer ());
				var result = new List<SearchResult> ();
				var lookup = await TryLookupSymbol (documentationCommentId, hintProject, token);
				if (!lookup.Success)
					return result;

				foreach (var simSym in SymbolFinder.FindSimilarSymbols (lookup.Symbol, lookup.Compilation)) {
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
				return (IEnumerable<SearchResult>)result;
			});
		}
	}
	
}
