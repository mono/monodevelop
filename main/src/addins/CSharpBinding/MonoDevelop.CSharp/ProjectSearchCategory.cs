// 
// ProjectSearchCategory.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core.Text;
using Gtk;
using System.Linq;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Collections.Concurrent;
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.CSharp
{
	class ProjectSearchCategory : SearchCategory
	{
		static Components.PopoverWindow widget;

		internal static void Init ()
		{
			MonoDevelopWorkspace.LoadingFinished += delegate {
				UpdateSymbolInfos ();
			};
		}

		public ProjectSearchCategory () : base (GettextCatalog.GetString ("Solution"))
		{
			sortOrder = FirstCategory;
		}

		public override void Initialize (Components.PopoverWindow popupWindow)
		{
			widget = popupWindow;
			lastResult = new WorkerResult (popupWindow);
		}

		internal static Task<SymbolCache> SymbolInfoTask;

		static TimerCounter getMembersTimer = InstrumentationService.CreateTimerCounter ("Time to get all members", "NavigateToDialog");
		static TimerCounter getTypesTimer = InstrumentationService.CreateTimerCounter ("Time to get all types", "NavigateToDialog");

		static CancellationTokenSource symbolInfoTokenSrc = new CancellationTokenSource();
		public static void UpdateSymbolInfos ()
		{
			symbolInfoTokenSrc.Cancel ();
			if (SymbolInfoTask != null) {
				var old = SymbolInfoTask.Result;
				if (old != null)
					old.Dispose ();
            }
			symbolInfoTokenSrc = new CancellationTokenSource();
			lastResult = new WorkerResult (widget);
			SymbolInfoTask = null;
			CancellationToken token = symbolInfoTokenSrc.Token;
			SymbolInfoTask = Task.Run (delegate {
				return GetSymbolInfos (token);
			}, token);
		}

		internal class SymbolCache : IDisposable
		{
			public static readonly SymbolCache Empty = new SymbolCache ();

			List<Microsoft.CodeAnalysis.Workspace> workspaces = new List<Microsoft.CodeAnalysis.Workspace> ();
			ConcurrentDictionary<Microsoft.CodeAnalysis.DocumentId, List<DeclaredSymbolInfo>> documentInfos = new ConcurrentDictionary<Microsoft.CodeAnalysis.DocumentId, List<DeclaredSymbolInfo>> ();

			public void AddWorkspace (Microsoft.CodeAnalysis.Workspace ws, CancellationToken token)
			{
				workspaces.Add (ws);
				ws.WorkspaceChanged += Ws_WorkspaceChanged;

				foreach (var p in ws.CurrentSolution.Projects) {
					SearchAsync (documentInfos, p, token);
				}
			}

			public List<DeclaredSymbolInfo> AllTypes {
				get {
					var result = new List<DeclaredSymbolInfo> ();
					foreach (var infos in documentInfos.Values)
						result.AddRange (infos);
					return result;
				}
			}

			static void SearchAsync (ConcurrentDictionary<Microsoft.CodeAnalysis.DocumentId, List<DeclaredSymbolInfo>> result, Microsoft.CodeAnalysis.Project project, CancellationToken cancellationToken)
			{
				Parallel.ForEach (project.Documents, async delegate (Microsoft.CodeAnalysis.Document document) {
					try {
						cancellationToken.ThrowIfCancellationRequested ();
						await UpdateDocument (result, document, cancellationToken).ConfigureAwait (false);
					} catch (OperationCanceledException) {
					}
				});
			}

			static async Task UpdateDocument (ConcurrentDictionary<DocumentId, List<DeclaredSymbolInfo>> result, Microsoft.CodeAnalysis.Document document, CancellationToken cancellationToken)
			{
				var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
				var infos = new List<DeclaredSymbolInfo> ();
				foreach (var current in root.DescendantNodesAndSelf (CSharpSyntaxFactsService.DescentIntoSymbolForDeclarationSearch)) {
					DeclaredSymbolInfo declaredSymbolInfo;
					if (current.TryGetDeclaredSymbolInfo (out declaredSymbolInfo)) {
						infos.Add (declaredSymbolInfo);
					}
				}
				RemoveDocument (result, document.Id);
				result.TryAdd (document.Id, infos);
			}

			static void RemoveDocument (ConcurrentDictionary<DocumentId, List<DeclaredSymbolInfo>> result, Microsoft.CodeAnalysis.DocumentId documentId)
			{
				if (result.ContainsKey (documentId)) {
					List<DeclaredSymbolInfo> val;
					result.TryRemove (documentId, out val);
				}
			}

			public void Dispose ()
			{
				if (workspaces == null)
					return;
				foreach (var ws in workspaces)
					ws.WorkspaceChanged -= Ws_WorkspaceChanged;
				workspaces = null;
				documentInfos = null;
			}

			async void Ws_WorkspaceChanged (object sender, WorkspaceChangeEventArgs e)
			{
				var ws = (Microsoft.CodeAnalysis.Workspace)sender;
				switch (e.Kind) {
				case WorkspaceChangeKind.ProjectAdded:
					SearchAsync (documentInfos, ws.CurrentSolution.GetProject (e.ProjectId), default (CancellationToken));
					break;
				case WorkspaceChangeKind.ProjectRemoved:
					foreach (var docId in ws.CurrentSolution.GetProject (e.ProjectId).DocumentIds)
						RemoveDocument (documentInfos, docId);
					break;
				case WorkspaceChangeKind.DocumentAdded:
					await UpdateDocument (documentInfos, ws.CurrentSolution.GetDocument (e.DocumentId), default (CancellationToken));
					break;
				case WorkspaceChangeKind.DocumentRemoved:
					RemoveDocument (documentInfos, e.DocumentId);
					break;
				case WorkspaceChangeKind.DocumentChanged:
					await UpdateDocument (documentInfos, ws.CurrentSolution.GetDocument (e.DocumentId), default (CancellationToken));
					break;
				}
			}
		}

		static SymbolCache GetSymbolInfos (CancellationToken token)
		{
			getTypesTimer.BeginTiming ();
			try {
				var result = new SymbolCache ();
				foreach (var workspace in TypeSystemService.AllWorkspaces) {
					result.AddWorkspace (workspace, token);
				}
				return result;
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (ex => ex is TaskCanceledException);
				return SymbolCache.Empty;
			} catch (TaskCanceledException) {
				return SymbolCache.Empty;
			} finally {
				getTypesTimer.EndTiming ();
			}
		}


		static WorkerResult lastResult;
		static readonly string[] typeTags = new [] { "type", "c", "s", "i", "e", "d" };
		static readonly string[] memberTags = new [] { "member", "m", "p", "f", "evt" };
		static readonly string[] tags = typeTags.Concat(memberTags).ToArray();

		public override string[] Tags {
			get {
				return tags;
			}
		}

		public override bool IsValidTag (string tag)
		{
			return typeTags.Any (t => t == tag) || memberTags.Any (t => t == tag);
		}

		public override Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern searchPattern, CancellationToken token)
		{
			return Task.Run (async delegate {
				if (searchPattern.Tag != null && !(typeTags.Contains (searchPattern.Tag) || memberTags.Contains (searchPattern.Tag)) || searchPattern.HasLineNumber)
					return null;
				try {
					var newResult = new WorkerResult (widget);
					newResult.pattern = searchPattern.Pattern;
					newResult.IncludeFiles = true;
					newResult.Tag = searchPattern.Tag;
					newResult.IncludeTypes = searchPattern.Tag == null || typeTags.Contains (searchPattern.Tag);
					newResult.IncludeMembers = searchPattern.Tag == null || memberTags.Contains (searchPattern.Tag);
					List<DeclaredSymbolInfo> allTypes;
					if (SymbolInfoTask == null)
						SymbolInfoTask = Task.FromResult (GetSymbolInfos (token));
					var cache = await SymbolInfoTask.ConfigureAwait (false);
					allTypes = cache.AllTypes;
					string toMatch = searchPattern.Pattern;
					newResult.matcher = StringMatcher.GetMatcher (toMatch, false);
					newResult.FullSearch = toMatch.IndexOf ('.') > 0;
					var oldLastResult = lastResult;
					if (newResult.FullSearch && oldLastResult != null && !oldLastResult.FullSearch)
						oldLastResult = new WorkerResult (widget);
//					var now = DateTime.Now;

					AllResults (searchResultCallback, oldLastResult, newResult, allTypes, token);
					//newResult.results.SortUpToN (new DataItemComparer (token), resultsCount);
					lastResult = newResult;
//					Console.WriteLine ((now - DateTime.Now).TotalMilliseconds);
					return (ISearchDataSource)newResult.results;
				} catch {
					token.ThrowIfCancellationRequested ();
					throw;
				}
			}, token);
		}

		void AllResults (ISearchResultCallback searchResultCallback, WorkerResult lastResult, WorkerResult newResult, IReadOnlyList<DeclaredSymbolInfo> completeTypeList, CancellationToken token)
		{
			if (newResult.isGotoFilePattern)
				return;
			uint x = 0;
			// Search Types
			if (newResult.IncludeTypes && (newResult.Tag == null || typeTags.Any (t => t == newResult.Tag))) {
				newResult.filteredSymbols = new List<DeclaredSymbolInfo> ();
				bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern, StringComparison.Ordinal) && lastResult.filteredSymbols != null;
				var allTypes = startsWithLastFilter ? lastResult.filteredSymbols : completeTypeList;
				foreach (var type in allTypes) {
					if (unchecked(x++) % 100 == 0 && token.IsCancellationRequested) {
						newResult.filteredSymbols = null;
						return;
					}

					if (type.Kind == DeclaredSymbolInfoKind.Constructor ||
					    type.Kind == DeclaredSymbolInfoKind.Module ||
					    type.Kind == DeclaredSymbolInfoKind.Indexer)
						continue;
					
					if (newResult.Tag != null) {
						if (newResult.Tag == "c" && type.Kind != DeclaredSymbolInfoKind.Class)
							continue;
						if (newResult.Tag == "s" && type.Kind != DeclaredSymbolInfoKind.Struct)
							continue;
						if (newResult.Tag == "i" && type.Kind != DeclaredSymbolInfoKind.Interface)
							continue;
						if (newResult.Tag == "e" && type.Kind != DeclaredSymbolInfoKind.Enum)
							continue;
						if (newResult.Tag == "d" && type.Kind != DeclaredSymbolInfoKind.Delegate)
							continue;

						if (newResult.Tag == "m" && type.Kind != DeclaredSymbolInfoKind.Method)
							continue;
						if (newResult.Tag == "p" && type.Kind != DeclaredSymbolInfoKind.Property)
							continue;
						if (newResult.Tag == "f" && type.Kind != DeclaredSymbolInfoKind.Field)
							continue;
						if (newResult.Tag == "evt" && type.Kind != DeclaredSymbolInfoKind.Event)
							continue;
						
					}
					SearchResult curResult = newResult.CheckType (type);
					if (curResult != null) {
						newResult.filteredSymbols.Add (type);
						newResult.results.AddResult (curResult);
						searchResultCallback.ReportResult (curResult);
					}
				}
			}
		}

		class WorkerResult
		{
			public string Tag {
				get;
				set;
			}

			public List<DeclaredSymbolInfo> filteredSymbols;

			string pattern2;
			char firstChar;
			char[] firstChars;

			public string pattern {
				get {
					return pattern2;
				}
				set {
					pattern2 = value;
					if (pattern2.Length == 1) {
						firstChar = pattern2 [0];
						firstChars = new [] { char.ToUpper (firstChar), char.ToLower (firstChar) };
					} else {
						firstChars = null;
					}
				}
			}

			public bool isGotoFilePattern;
			public ResultsDataSource results;
			public bool FullSearch;
			public bool IncludeFiles, IncludeTypes, IncludeMembers;
			public StringMatcher matcher;

			public WorkerResult (Widget widget)
			{
				results = new ResultsDataSource (widget);
			}

			internal SearchResult CheckType (DeclaredSymbolInfo symbol)
			{
				int rank;
				var name = symbol.Name;
				if (MatchName(name, out rank)) {
//					if (type.ContainerDisplayName != null)
//						rank--;
					return new DeclaredSymbolInfoResult (pattern, symbol.Name, rank, symbol, false);
				}
				if (!FullSearch)
					return null;
				name = symbol.FullyQualifiedContainerName;
				if (MatchName(name, out rank)) {
//					if (type.ContainingType != null)
//						rank--;
					return new DeclaredSymbolInfoResult (pattern, name, rank, symbol, true);
				}
				return null;
			}

			Dictionary<string, MatchResult> savedMatches = new Dictionary<string, MatchResult> (StringComparer.Ordinal);

			bool MatchName (string name, out int matchRank)
			{
				if (name == null) {
					matchRank = -1;
					return false;
				}

				MatchResult savedMatch;
				if (!savedMatches.TryGetValue (name, out savedMatch)) {
					bool doesMatch;
					if (firstChars != null) {
						int idx = name.IndexOfAny (firstChars);
						doesMatch = idx >= 0;
						if (doesMatch) {
							matchRank = int.MaxValue - (name.Length - 1) * 10 - idx;
							if (name [idx] != firstChar)
								matchRank /= 2;
							savedMatches [name] = savedMatch = new MatchResult (true, matchRank);
							return true;
						}
						matchRank = -1;
						savedMatches [name] = savedMatch = new MatchResult (false, -1);
						return false;
					}
					doesMatch = matcher.CalcMatchRank (name, out matchRank);
					savedMatches [name] = savedMatch = new MatchResult (doesMatch, matchRank);
				}
				
				matchRank = savedMatch.Rank;
				return savedMatch.Match;
			}
		}
	}
}
