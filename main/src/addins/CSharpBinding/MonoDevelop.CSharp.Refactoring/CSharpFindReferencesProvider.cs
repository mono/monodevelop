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
			public Solution Solution { get => Project.Solution; }
			public SymbolAndProjectId SymbolAndProjectId { get => SymbolAndProjectId.Create (Symbol, Project.Id); }
			public MonoDevelop.Projects.Project MonoDevelopProject { get; internal set; }
			public Microsoft.CodeAnalysis.Project Project { get; private set; }
			public Compilation Compilation { get; private set; }

			public LookupResult ()
			{
			}

			public LookupResult (ISymbol symbol, Microsoft.CodeAnalysis.Project project, Compilation compilation)
			{
				this.Success = true;
				this.Symbol = symbol;
				this.Project = project;
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
						return new LookupResult (current, prj, comp);
					return LookupResult.Failure;
				}
				INamedTypeSymbol type = null;
				foreach (var t in current.GetTypeMembers ()) {
					if (token.IsCancellationRequested)
						return LookupResult.Failure;
					type = LookupType (documentationCommentId, reminderIndex, t, token);
					if (type != null) {
						if (searchType) {
							return new LookupResult (type, prj, comp);
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
						return new LookupResult (member, prj, comp);
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
			if (string.CompareOrdinal (documentationCommentId, reminder, typeId + ".", reminder, idx - reminder) == 0) {
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

		public override Task FindReferences (string documentationCommentId, Projects.Project hintProject, SearchProgressMonitor monitor)
		{
			return Task.Run (async delegate {
				var lookup = await TryLookupSymbol (documentationCommentId, hintProject, monitor.CancellationToken);
				if (lookup == null || !lookup.Success)
					return;

				var workspace = TypeSystemService.AllWorkspaces.FirstOrDefault (w => w.CurrentSolution == lookup.Solution) as MonoDevelopWorkspace;
				if (workspace == null)
					return;

				await FindReferencesHandler.FindRefs (new[] { lookup.SymbolAndProjectId }, lookup.Solution, monitor);
			});
		}

		public static async Task<SymbolAndProjectId []> GatherSymbols (SymbolAndProjectId symbol, Microsoft.CodeAnalysis.Solution solution, CancellationToken token)
		{
			var implementations = await SymbolFinder.FindImplementationsAsync (symbol, solution, null, token);
			var result = new SymbolAndProjectId [implementations.Length + 1];
			result [0] = symbol;
			for (int i = 0; i < implementations.Length; i++)
				result [i + 1] = implementations [i];
			return result;
		}

		public override Task FindAllReferences (string documentationCommentId, Projects.Project hintProject, SearchProgressMonitor monitor)
		{
			return Task.Run (async delegate {
				var lookup = await TryLookupSymbol (documentationCommentId, hintProject, monitor.CancellationToken);
				if (!lookup.Success)
					return;
				var workspace = TypeSystemService.AllWorkspaces.FirstOrDefault (w => w.CurrentSolution == lookup.Solution) as MonoDevelopWorkspace;
				if (workspace == null)
					return;
				if (lookup.Symbol.Kind == SymbolKind.Method) {
					var symbolsToLookup = new List<SymbolAndProjectId> ();
					foreach (var curSymbol in lookup.Symbol.ContainingType.GetMembers ().Where (m => m.Kind == lookup.Symbol.Kind && m.Name == lookup.Symbol.Name)) {
						foreach (var sym in SymbolFinder.FindSimilarSymbols (curSymbol, lookup.Compilation, monitor.CancellationToken)) {
							//assumption here is, that FindSimilarSymbols returns symbols inside same project
							var symbolsToAdd = await GatherSymbols (SymbolAndProjectId.Create (sym, lookup.Project.Id), lookup.Solution, monitor.CancellationToken);
							symbolsToLookup.AddRange (symbolsToAdd);
						}
					}
					await FindReferencesHandler.FindRefs (symbolsToLookup.ToArray (), lookup.Solution, monitor);
				} else {
					await FindReferencesHandler.FindRefs (await GatherSymbols (lookup.SymbolAndProjectId, lookup.Solution, monitor.CancellationToken), lookup.Solution, monitor);
				}
			});
		}
	}

}
