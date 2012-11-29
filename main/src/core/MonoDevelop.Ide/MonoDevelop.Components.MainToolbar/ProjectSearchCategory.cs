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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core.Text;
using Gtk;
using System.Linq;

namespace MonoDevelop.Components.MainToolbar
{
	class ProjectSearchCategory : SearchCategory
	{
		SearchPopupWindow widget;

		public ProjectSearchCategory (SearchPopupWindow widget) : base (GettextCatalog.GetString("Solution"))
		{
			this.widget = widget;
			this.lastResult = new WorkerResult (widget);
		}

		static TimerCounter getMembersTimer = InstrumentationService.CreateTimerCounter ("Time to get all members", "NavigateToDialog");


		static TimerCounter getTypesTimer = InstrumentationService.CreateTimerCounter ("Time to get all types", "NavigateToDialog");

		IEnumerable<ITypeDefinition> types {
			get {
				getTypesTimer.BeginTiming ();
				try {
					foreach (Document doc in IdeApp.Workbench.Documents) {
						// We only want to check it here if it's not part
						// of the open combine. Otherwise, it will get
						// checked down below.
						if (doc.Project == null && doc.IsFile) {
							var info = doc.ParsedDocument;
							if (info != null) {
								var ctx = doc.Compilation;
								foreach (var type in ctx.MainAssembly.GetAllTypeDefinitions ()) {
									yield return type;
								}
							}
						}
					}
					
					var projects = IdeApp.Workspace.GetAllProjects ();
					
					foreach (Project p in projects) {
						var pctx = TypeSystemService.GetCompilation (p);
						foreach (var type in pctx.MainAssembly.GetAllTypeDefinitions ())
							yield return type;
					}
				} finally {
					getTypesTimer.EndTiming ();
				}
			}
		}

		WorkerResult lastResult;
		string[] typeTags = new [] { "type", "c", "s", "i", "e", "d"};
		string[] memberTags = new [] { "member", "m", "p", "f", "evt"};

		public override Task<ISearchDataSource> GetResults (SearchPopupSearchPattern searchPattern, int resultsCount, CancellationToken token)
		{
			return Task.Factory.StartNew (delegate {
				if (searchPattern.Tag != null && !(typeTags.Contains (searchPattern.Tag) || memberTags.Contains (searchPattern.Tag)) || searchPattern.HasLineNumber)
					return null;
				try {
					var newResult = new WorkerResult (widget);
					newResult.pattern = searchPattern.Pattern;
					newResult.IncludeFiles = true;
					newResult.Tag = searchPattern.Tag;
					newResult.IncludeTypes = searchPattern.Tag == null || typeTags.Contains (searchPattern.Tag) ;
					newResult.IncludeMembers = searchPattern.Tag == null || memberTags.Contains (searchPattern.Tag);
					var firstType = types.FirstOrDefault ();
					newResult.ambience = firstType != null ? AmbienceService.GetAmbienceForFile (firstType.Region.FileName) : AmbienceService.DefaultAmbience;
					
					string toMatch = searchPattern.Pattern;
					newResult.matcher = StringMatcher.GetMatcher (toMatch, false);
					newResult.FullSearch = toMatch.IndexOf ('.') > 0;
					var oldLastResult = lastResult;
					if (newResult.FullSearch && oldLastResult != null && !oldLastResult.FullSearch)
						oldLastResult = new WorkerResult (widget);
//					var now = DateTime.Now;

					AllResults (oldLastResult, newResult, token);
					newResult.results.SortUpToN (new DataItemComparer (token), resultsCount);
					lastResult = newResult;
//					Console.WriteLine ((now - DateTime.Now).TotalMilliseconds);
					return (ISearchDataSource)newResult.results;
				} catch {
					token.ThrowIfCancellationRequested ();
					throw;
				}
			}, token);
		}

		void AllResults (WorkerResult lastResult, WorkerResult newResult, CancellationToken token)
		{
			if (newResult.isGotoFilePattern)
				return;
			uint x = 0;
			// Search Types
			if (newResult.IncludeTypes && (newResult.Tag == null || typeTags.Any (t => t == newResult.Tag))) {
				newResult.filteredTypes = new List<ITypeDefinition> ();
				bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern, StringComparison.Ordinal) && lastResult.filteredTypes != null;
				var allTypes = startsWithLastFilter ? lastResult.filteredTypes : types;
				foreach (var type in allTypes) {
					if (unchecked(x++) % 100 == 0 && token.IsCancellationRequested)
						return;

					if (newResult.Tag != null) {
						if (newResult.Tag == "c" && type.Kind != TypeKind.Class)
							continue;
						if (newResult.Tag == "s" && type.Kind != TypeKind.Struct)
							continue;
						if (newResult.Tag == "i" && type.Kind != TypeKind.Interface)
							continue;
						if (newResult.Tag == "e" && type.Kind != TypeKind.Enum)
							continue;
						if (newResult.Tag == "d" && type.Kind != TypeKind.Delegate)
							continue;
					}
					SearchResult curResult = newResult.CheckType (type);
					if (curResult != null) {
						newResult.filteredTypes.Add (type);
						newResult.results.AddResult (curResult);
					}
				}
			}
			
			// Search members
			if (newResult.IncludeMembers && (newResult.Tag == null || memberTags.Any (t => t == newResult.Tag))) {
				newResult.filteredMembers = new List<Tuple<ITypeDefinition, IUnresolvedMember>> ();
				bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern, StringComparison.Ordinal) && lastResult.filteredMembers != null;
				if (startsWithLastFilter) {
					foreach (var t in lastResult.filteredMembers) {
						if (unchecked(x++) % 100 == 0 && token.IsCancellationRequested)
							return;
						var member = t.Item2;
						if (newResult.Tag != null) {
							if (newResult.Tag == "m" && member.EntityType != EntityType.Method)
								continue;
							if (newResult.Tag == "p" && member.EntityType != EntityType.Property)
								continue;
							if (newResult.Tag == "f" && member.EntityType != EntityType.Field)
								continue;
							if (newResult.Tag == "evt" && member.EntityType != EntityType.Event)
								continue;
						}
						SearchResult curResult = newResult.CheckMember (t.Item1, member);
						if (curResult != null) {
							newResult.filteredMembers.Add (t);
							newResult.results.AddResult (curResult);
						}
					}
				} else {
					Func<IUnresolvedMember, bool> mPred = member => {
						if (newResult.Tag != null) {
							if (newResult.Tag == "m" && member.EntityType != EntityType.Method)
								return false;
							if (newResult.Tag == "p" && member.EntityType != EntityType.Property)
								return false;
							if (newResult.Tag == "f" && member.EntityType != EntityType.Field)
								return false;
							if (newResult.Tag == "evt" && member.EntityType != EntityType.Event)
								return false;
						}
						return newResult.IsMatchingMember (member);
					};

					getMembersTimer.BeginTiming ();
					try {
						foreach (var type in types) {
							if (type.Kind == TypeKind.Delegate)
								continue;
							foreach (var p in type.Parts) {
								foreach (var member in p.Members.Where (mPred)) {
									if (unchecked(x++) % 100 == 0 && token.IsCancellationRequested)
										return;
									SearchResult curResult = newResult.CheckMember (type, member);
									if (curResult != null) {
										newResult.filteredMembers.Add (Tuple.Create (type, member));
										newResult.results.AddResult (curResult);
									}
								}
							}
						}
					} finally {
						getMembersTimer.EndTiming ();
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

			public List<ProjectFile> filteredFiles;
			public List<ITypeDefinition> filteredTypes;
			public List<Tuple<ITypeDefinition, IUnresolvedMember>> filteredMembers;
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
						firstChar = pattern2[0];
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
			public Ambience ambience;
			public StringMatcher matcher;
			
			public WorkerResult (Widget widget)
			{
				results = new ResultsDataSource (widget);
			}
			
			internal SearchResult CheckFile (ProjectFile file)
			{
				int rank;
				string matchString = System.IO.Path.GetFileName (file.FilePath);
				if (MatchName (matchString, out rank)) 
					return new FileSearchResult (pattern, matchString, rank, file, true);
				
				if (!FullSearch)
					return null;
				matchString = FileSearchResult.GetRelProjectPath (file);
				if (MatchName (matchString, out rank)) 
					return new FileSearchResult (pattern, matchString, rank, file, false);
				
				return null;
			}
			
			internal SearchResult CheckType (ITypeDefinition type)
			{
				int rank;
				if (MatchName (TypeSearchResult.GetPlainText (type, false), out rank))
					return new TypeSearchResult (pattern, TypeSearchResult.GetPlainText (type, false), rank, type, false) { Ambience = ambience };
				if (!FullSearch)
					return null;
				if (MatchName (TypeSearchResult.GetPlainText (type, true), out rank))
					return new TypeSearchResult (pattern, TypeSearchResult.GetPlainText (type, true), rank, type, true) { Ambience = ambience };
				return null;
			}
			
			internal SearchResult CheckMember (ITypeDefinition declaringType, IUnresolvedMember member)
			{
				int rank;
				bool useDeclaringTypeName = member is IUnresolvedMethod && (((IUnresolvedMethod)member).IsConstructor || ((IUnresolvedMethod)member).IsDestructor);
				string memberName = useDeclaringTypeName ? member.DeclaringTypeDefinition.Name : member.Name;
				if (MatchName (memberName, out rank))
					return new MemberSearchResult (pattern, memberName, rank, declaringType, member, false) { Ambience = ambience };
				return null;
			}

			internal bool IsMatchingMember (IUnresolvedMember member)
			{
				int rank;
				bool useDeclaringTypeName = member is IUnresolvedMethod && (((IUnresolvedMethod)member).IsConstructor || ((IUnresolvedMethod)member).IsDestructor);
				string memberName = useDeclaringTypeName ? member.DeclaringTypeDefinition.Name : member.Name;
				return MatchName (memberName, out rank);
			}

			Dictionary<string, MatchResult> savedMatches = new Dictionary<string, MatchResult> (StringComparer.Ordinal);

			bool MatchName (string name, out int matchRank)
			{
				if (name == null) {
					matchRank = -1;
					return false;
				}

				bool doesMatch;
				if (firstChars != null) {
					int idx = name.IndexOfAny (firstChars);
					doesMatch = idx >= 0;
					if (doesMatch) {
						matchRank = int.MaxValue - (name.Length - 1) * 10 - idx;
						if (name[idx] != firstChar)
							matchRank /= 2;
						return true;
					} else {
						matchRank = -1;
					}
					return false;
				}
				MatchResult savedMatch;
				if (!savedMatches.TryGetValue (name, out savedMatch)) {
					doesMatch = matcher.CalcMatchRank (name, out matchRank);
					savedMatches [name] = savedMatch = new MatchResult (doesMatch, matchRank);
				}
				
				matchRank = savedMatch.Rank;
				return savedMatch.Match;
			}
		}
	}
}
