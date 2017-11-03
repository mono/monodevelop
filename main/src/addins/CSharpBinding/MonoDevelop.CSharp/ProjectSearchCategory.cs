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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp
{
	class ProjectSearchCategory : SearchCategory
	{
		internal static void Init ()
		{
			MonoDevelopWorkspace.LoadingFinished += async delegate {
				await UpdateSymbolInfos ();
			};
			if (IdeApp.IsInitialized) {
				IdeApp.Workspace.LastWorkspaceItemClosed += async delegate {
					await DisposeSymbolInfoTask ();
				};
			}
		}

		public ProjectSearchCategory () : base (GettextCatalog.GetString ("Solution"))
		{
			sortOrder = FirstCategory;
		}

		public override void Initialize (Components.XwtPopup popupWindow)
		{
			lastResult = new WorkerResult ();
		}

		internal static Task<SymbolCache> SymbolInfoTask;

		static TimerCounter getTypesTimer = InstrumentationService.CreateTimerCounter ("Time to get all types", "NavigateToDialog");

		static CancellationTokenSource symbolInfoTokenSrc = new CancellationTokenSource();
		public static async Task UpdateSymbolInfos ()
		{
			await DisposeSymbolInfoTask ();
			CancellationToken token = symbolInfoTokenSrc.Token;
			SymbolInfoTask = Task.Run (delegate {
				return GetSymbolInfos (token);
			}, token);
		}

		static async Task DisposeSymbolInfoTask ()
		{
			symbolInfoTokenSrc.Cancel ();
			if (SymbolInfoTask != null) {
				try {
					var old = await SymbolInfoTask;
					if (old != null)
						old.Dispose ();
				} catch (OperationCanceledException) {
					// Ignore
				} catch (Exception ex) {
					LoggingService.LogError ("UpdateSymbolInfos failed", ex);
				}
			}
			symbolInfoTokenSrc = new CancellationTokenSource();
			lastResult = new WorkerResult ();
			SymbolInfoTask = null;
		}

		internal class SymbolCache : IDisposable
		{
			public static readonly SymbolCache Empty = new SymbolCache ();

			List<Workspace> workspaces = new List<Workspace> ();
			ConcurrentDictionary<DocumentId, Dictionary<DeclaredSymbolInfoKind, List<DeclaredSymbolInfoWrapper>>> documentInfos = new ConcurrentDictionary<DocumentId, Dictionary<DeclaredSymbolInfoKind, List<DeclaredSymbolInfoWrapper>>> ();

			public void AddWorkspace (Workspace ws, CancellationToken token)
			{
				workspaces.Add (ws);
				ws.WorkspaceChanged += Ws_WorkspaceChanged;

				foreach (var p in ws.CurrentSolution.Projects) {
					if (p.FilePath.EndsWith ("csproj", StringComparison.Ordinal))
						SearchAsync (documentInfos, p, token);
				}
			}

			static IEnumerable<DeclaredSymbolInfoKind> AllKinds {
				get {
					// No Constructor, Indexer, Module
					yield return DeclaredSymbolInfoKind.Class;
					yield return DeclaredSymbolInfoKind.Constant;
					yield return DeclaredSymbolInfoKind.Delegate;
					yield return DeclaredSymbolInfoKind.Enum;
					yield return DeclaredSymbolInfoKind.EnumMember;
					yield return DeclaredSymbolInfoKind.ExtensionMethod;
					yield return DeclaredSymbolInfoKind.Event;
					yield return DeclaredSymbolInfoKind.Field;
					yield return DeclaredSymbolInfoKind.Interface;
					yield return DeclaredSymbolInfoKind.Method;
					yield return DeclaredSymbolInfoKind.Property;
					yield return DeclaredSymbolInfoKind.Struct;
				}
			}

			static IEnumerable<DeclaredSymbolInfoKind> TagToKinds (string tag)
			{
				if (tag == null) {
					foreach (var kind in AllKinds) {
						yield return kind;
					}
					yield break;
				}

				switch (tag) {
				case "type":
				case "t":
					yield return DeclaredSymbolInfoKind.Class;
					yield return DeclaredSymbolInfoKind.Struct;
					yield return DeclaredSymbolInfoKind.Interface;
					yield return DeclaredSymbolInfoKind.Enum;
					yield return DeclaredSymbolInfoKind.Delegate;
					break;
				case "member":
				case "m":
					yield return DeclaredSymbolInfoKind.Method;
					yield return DeclaredSymbolInfoKind.Property;
					yield return DeclaredSymbolInfoKind.Field;
					yield return DeclaredSymbolInfoKind.Event;
					break;
				case "class":
					yield return DeclaredSymbolInfoKind.Class;
					break;
				case "struct":
					yield return DeclaredSymbolInfoKind.Struct;
					break;
				case "interface":
					yield return DeclaredSymbolInfoKind.Interface;
					break;
				case "enum":
					yield return DeclaredSymbolInfoKind.Enum;
					break;
				case "delegate":
					yield return DeclaredSymbolInfoKind.Delegate;
					break;
				case "method":
					yield return DeclaredSymbolInfoKind.Method;
					break;
				case "property":
					yield return DeclaredSymbolInfoKind.Property;
					break;
				case "field":
					yield return DeclaredSymbolInfoKind.Field;
					break;
				case "event":
					yield return DeclaredSymbolInfoKind.Event;
					break;
				}
			}

			public IEnumerable<DeclaredSymbolInfoWrapper> GetAllTypes(string tag, CancellationToken token)
			{
				var kinds = TagToKinds (tag).ToArray ();

				foreach (var infosByKind in documentInfos.Values) {
					if (token.IsCancellationRequested)
						yield break;

					foreach (var kind in kinds)
						foreach (var info in infosByKind [kind]) {
							if (token.IsCancellationRequested)
								yield break;
							yield return info;
						}
				}
			}

			static async void SearchAsync (ConcurrentDictionary<Microsoft.CodeAnalysis.DocumentId, Dictionary<DeclaredSymbolInfoKind, List<DeclaredSymbolInfoWrapper>>> result, Microsoft.CodeAnalysis.Project project, CancellationToken cancellationToken)
			{
				if (project == null)
					throw new ArgumentNullException (nameof (project));
				try {
					foreach (var document in project.Documents) {
						cancellationToken.ThrowIfCancellationRequested ();
						await UpdateDocument (result, document, cancellationToken);
					}
				} catch (AggregateException ae) {
					ae.Flatten ().Handle (ex => ex is OperationCanceledException);
				} catch (OperationCanceledException) {
				}
			}

			static async Task UpdateDocument (ConcurrentDictionary<DocumentId, Dictionary<DeclaredSymbolInfoKind, List<DeclaredSymbolInfoWrapper>>> result, Microsoft.CodeAnalysis.Document document, CancellationToken cancellationToken)
			{
				var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService> ();
				var declaredSymbolInfoService = document.GetLanguageService<IDeclaredSymbolInfoFactoryService> ();
				var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
				var infos = new Dictionary<DeclaredSymbolInfoKind, List<DeclaredSymbolInfoWrapper>> ();
				var stringTable = Roslyn.Utilities.StringTable.GetInstance (); // TODO: fix this
				foreach (var kind in AllKinds) {
					infos [kind] = new List<DeclaredSymbolInfoWrapper> ();
				}
				foreach (var current in root.DescendantNodesAndSelf (n => !(n is BlockSyntax))) {
					cancellationToken.ThrowIfCancellationRequested ();
					var kind = current.Kind ();
					if (kind == SyntaxKind.ConstructorDeclaration ||
						kind == SyntaxKind.IndexerDeclaration)
						continue;
					if (declaredSymbolInfoService.TryGetDeclaredSymbolInfo (stringTable, current, out DeclaredSymbolInfo info)) {
						var declaredSymbolInfo = new DeclaredSymbolInfoWrapper (current, document.Id, info);
						infos[info.Kind].Add (declaredSymbolInfo);
					}
				}
				RemoveDocument (result, document.Id);
				result.TryAdd (document.Id, infos);
			}

			static void RemoveDocument (ConcurrentDictionary<DocumentId, Dictionary<DeclaredSymbolInfoKind, List<DeclaredSymbolInfoWrapper>>> result, DocumentId documentId)
			{
				result.TryRemove (documentId, out Dictionary<DeclaredSymbolInfoKind, List<DeclaredSymbolInfoWrapper>> val);
			}

			public void Dispose ()
			{
				if (workspaces == null)
					return;
				foreach (var ws in workspaces)
					ws.WorkspaceChanged -= Ws_WorkspaceChanged;
				lock (documentChangedCts) {
					foreach (var cts in documentChangedCts.Values)
						cts.Cancel ();
					documentChangedCts.Clear ();
				}
				workspaces = null;
				documentInfos = null;
			}
			Dictionary<DocumentId, CancellationTokenSource> documentChangedCts = new Dictionary<DocumentId, CancellationTokenSource> ();
			async void Ws_WorkspaceChanged (object sender, WorkspaceChangeEventArgs e)
			{
				var ws = (Microsoft.CodeAnalysis.Workspace)sender;
				var currentSolution = ws.CurrentSolution;
				if (currentSolution == null)
					return;
				try {
					switch (e.Kind) {
					case WorkspaceChangeKind.ProjectAdded:
						var project1 = currentSolution.GetProject (e.ProjectId);
						if (project1 != null)
							SearchAsync (documentInfos, project1, default (CancellationToken));
						break;
					case WorkspaceChangeKind.ProjectRemoved:
						var project = currentSolution.GetProject (e.ProjectId);
						if (project != null) {
							foreach (var docId in project.DocumentIds)
								RemoveDocument (documentInfos, docId);
						}
						break;
					case WorkspaceChangeKind.DocumentAdded:
						var document = currentSolution.GetDocument (e.DocumentId);
						if (document != null)
							await UpdateDocument (documentInfos, document, default (CancellationToken));
						break;
					case WorkspaceChangeKind.DocumentRemoved:
						RemoveDocument (documentInfos, e.DocumentId);
						break;
					case WorkspaceChangeKind.DocumentChanged:
						var doc = currentSolution.GetDocument (e.DocumentId);
						if (doc != null) {
							CancellationTokenSource tcs;
							lock(documentChangedCts) {
								CancellationTokenSource oldTcs;
								if (documentChangedCts.TryGetValue (e.DocumentId, out oldTcs)) {
									oldTcs.Cancel ();
								}
								tcs = new CancellationTokenSource ();
								documentChangedCts [e.DocumentId] = tcs;
							}
							try {
								//Delaying parsing of new content for 1 second shouldn't be noticable by user
								//since he would have to edit file and instantlly go to search for newly written member...
								await Task.Delay (1000, tcs.Token).ConfigureAwait (false);
								await Task.Run (delegate {
									return UpdateDocument (documentInfos, doc, tcs.Token);
								}, tcs.Token).ConfigureAwait (false);
							} finally {
								lock (documentChangedCts) {
									//cts might be replaced by newer call cts
									CancellationTokenSource existingCts;
									if (documentChangedCts.TryGetValue (e.DocumentId, out existingCts) && tcs == existingCts)
										documentChangedCts.Remove (e.DocumentId);
								}
							}
						}
						break;
					}
				} catch (AggregateException ae) {
					ae.Flatten ().Handle (ex => ex is OperationCanceledException);
				} catch (OperationCanceledException) {
				} catch (Exception ex) {
					LoggingService.LogError ("Error while updating navigation symbol cache.", ex);
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
				ae.Flatten ().Handle (ex => ex is OperationCanceledException);
				return SymbolCache.Empty;
			} catch (OperationCanceledException) {
				return SymbolCache.Empty;
			} finally {
				getTypesTimer.EndTiming ();
			}
		}


		static WorkerResult lastResult;
		static readonly string[] typeTags = new [] { "type", "t", "class", "struct", "interface", "enum", "delegate" };
		static readonly string[] memberTags = new [] { "member", "m", "method", "property", "field", "event" };
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
					return;
				try {
					if (SymbolInfoTask == null)
						SymbolInfoTask = Task.FromResult (default(SymbolCache)).ContinueWith(t => GetSymbolInfos (token));
					var cache = await SymbolInfoTask.ConfigureAwait (false);
					if (token.IsCancellationRequested)
						return;
					
					string toMatch = searchPattern.Pattern;
					var newResult = new WorkerResult ();
					newResult.pattern = searchPattern.Pattern;
					newResult.Tag = searchPattern.Tag;
					newResult.matcher = StringMatcher.GetMatcher (toMatch, false);
					newResult.FullSearch = toMatch.IndexOf ('.') > 0;

					var oldLastResult = lastResult;
					if (newResult.FullSearch && oldLastResult != null && !oldLastResult.FullSearch)
						oldLastResult = new WorkerResult ();

					if (token.IsCancellationRequested)
						return;
					
					var allTypes = cache.GetAllTypes (searchPattern.Tag, token);
					
//					var now = DateTime.Now;
					AllResults (searchResultCallback, oldLastResult, newResult, allTypes, token);
					//newResult.results.SortUpToN (new DataItemComparer (token), resultsCount);
					lastResult = newResult;
					//					Console.WriteLine ((now - DateTime.Now).TotalMilliseconds);
				} catch {
					token.ThrowIfCancellationRequested ();
					throw;
				}
			}, token);
		}

		static bool IsSameFilterStart (WorkerResult oldLastResult, WorkerResult newResult)
		{
			return oldLastResult.pattern != null && newResult.pattern.StartsWith (oldLastResult.pattern, StringComparison.Ordinal) && oldLastResult.filteredSymbols != null;
		}

		void AllResults (ISearchResultCallback searchResultCallback, WorkerResult lastResult, WorkerResult newResult, IEnumerable<DeclaredSymbolInfoWrapper> completeTypeList, CancellationToken token)
		{
			// Search Types
			newResult.filteredSymbols = new List<DeclaredSymbolInfoWrapper> ();
			bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern, StringComparison.Ordinal) && lastResult.filteredSymbols != null;
			var allTypes = startsWithLastFilter ? lastResult.filteredSymbols : completeTypeList;
			foreach (var type in allTypes) {
				if (token.IsCancellationRequested) {
					newResult.filteredSymbols = null;
					return;
				}
				SearchResult curResult = newResult.CheckType (type);
				if (curResult != null) {
					newResult.filteredSymbols.Add (type);
					searchResultCallback.ReportResult (curResult);
				}
			}
		}

		class WorkerResult
		{
			public string Tag {
				get;
				set;
			}

			public List<DeclaredSymbolInfoWrapper> filteredSymbols;

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

			public bool FullSearch;
			public StringMatcher matcher;

			public WorkerResult ()
			{
			}

			internal SearchResult CheckType (DeclaredSymbolInfoWrapper symbol)
			{
				int rank;
				var name = symbol.SymbolInfo.Name;
				if (MatchName(name, out rank)) {
//					if (type.ContainerDisplayName != null)
//						rank--;
					return new DeclaredSymbolInfoResult (pattern, name, rank, symbol, false);
				}
				if (!FullSearch)
					return null;
				name = symbol.SymbolInfo.FullyQualifiedContainerName;
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
