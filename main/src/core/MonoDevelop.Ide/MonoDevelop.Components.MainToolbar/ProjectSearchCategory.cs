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
using MonoDevelop.Ide.NavigateToDialog;
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

		/*	IEnumerable<ProjectFile> files {
			get {
				HashSet<ProjectFile> list = new HashSet<ProjectFile> ();
				foreach (Document doc in IdeApp.Workbench.Documents) {
					// We only want to check it here if it's not part
					// of the open combine.  Otherwise, it will get
					// checked down below.
					if (doc.Project == null && doc.IsFile)
						yield return new ProjectFile (doc.Name);
				}
				
				var projects = IdeApp.Workspace.GetAllProjects ();

				foreach (Project p in projects) {
					foreach (ProjectFile file in p.Files) {
						if (file.Subtype != Subtype.Directory)
							yield return file;
					}
				}
			}
		}
		*/
		static TimerCounter getMembersTimer = InstrumentationService.CreateTimerCounter ("Time to get all members", "NavigateToDialog");

		IEnumerable<IMember> members {
			get {
				getMembersTimer.BeginTiming ();
				try {
					lock (members) {
						foreach (var type in types) {
							if (type.Kind == TypeKind.Delegate)
								continue;
							foreach (var m in type.Members) {
								yield return m;
							}
						}
					}
				} finally {
					getMembersTimer.EndTiming ();
				}
				
			}
		}

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

		public override Task<ISearchDataSource> GetResults (string searchPattern, CancellationToken token)
		{
			return Task.Factory.StartNew (delegate {
				try {
					WorkerResult newResult = new WorkerResult (widget);
					newResult.pattern = searchPattern;
					newResult.IncludeFiles = true;
					newResult.IncludeTypes = true;
					newResult.IncludeMembers = widget.SearchForMembers;
					var firstType = types.FirstOrDefault ();
					newResult.ambience = firstType != null ? AmbienceService.GetAmbienceForFile (firstType.Region.FileName) : AmbienceService.DefaultAmbience;
					
					string toMatch = searchPattern;
					int i = toMatch.IndexOf (':');
					if (i != -1) {
						toMatch = toMatch.Substring (0, i);
						newResult.isGotoFilePattern = true;
					}
					newResult.matcher = StringMatcher.GetMatcher (toMatch, true);
					newResult.FullSearch = searchPattern.IndexOf ('.') > 0;
					var oldLastResult = lastResult;
					if (newResult.FullSearch && oldLastResult != null && !oldLastResult.FullSearch)
						oldLastResult = new WorkerResult (widget);
					var now = DateTime.Now;
					foreach (SearchResult result in AllResults (oldLastResult, newResult, token)) {
						newResult.results.AddResult (result);
					}
					if (token.IsCancellationRequested) {
						return null;
					}
					now = DateTime.Now;
					newResult.results.Sort (new DataItemComparer ());
					lastResult = newResult;
					return (ISearchDataSource)newResult.results;
				} catch (Exception e) {
					LoggingService.LogError ("Error while retrieving search results.", e);
					return null;
				}
			}, token);
		}

		IEnumerable<SearchResult> AllResults (WorkerResult lastResult, WorkerResult newResult, CancellationToken token)
		{
//			// Search files
//			if (newResult.IncludeFiles) {
//				newResult.filteredFiles = new List<ProjectFile> ();
//				bool startsWithLastFilter = lastResult != null && lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern) && lastResult.filteredFiles != null;
//				IEnumerable<ProjectFile> allFiles = startsWithLastFilter ? lastResult.filteredFiles : files;
//				foreach (ProjectFile file in allFiles) {
//					SearchResult curResult = newResult.CheckFile (file);
//					if (curResult != null) {
//						newResult.filteredFiles.Add (file);
//						yield return curResult;
//					}
//				}
//			}
			if (newResult.isGotoFilePattern)
				yield break;
			
			// Search Types
			if (newResult.IncludeTypes) {
				newResult.filteredTypes = new List<ITypeDefinition> ();
				bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern) && lastResult.filteredTypes != null;
				var allTypes = startsWithLastFilter ? lastResult.filteredTypes : types;
				foreach (var type in allTypes) {
					if (token.IsCancellationRequested)
						yield break;
					SearchResult curResult = newResult.CheckType (type);
					if (curResult != null) {
						newResult.filteredTypes.Add (type);
						yield return curResult;
					}
				}
			}
			
			// Search members
			if (newResult.IncludeMembers) {
				newResult.filteredMembers = new List<IMember> ();
				bool startsWithLastFilter = lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern) && lastResult.filteredMembers != null;
				var allMembers = startsWithLastFilter ? lastResult.filteredMembers : members;
				foreach (var member in allMembers) {
					if (token.IsCancellationRequested)
						yield break;
					SearchResult curResult = newResult.CheckMember (member);
					if (curResult != null) {
						newResult.filteredMembers.Add (member);
						yield return curResult;
					}
				}
			}
		}
		
		class WorkerResult
		{
			public List<ProjectFile> filteredFiles = null;
			public List<ITypeDefinition> filteredTypes = null;
			public List<IMember> filteredMembers = null;
			public string pattern = null;
			public bool isGotoFilePattern;
			public ResultsDataSource results;
			public bool FullSearch;
			public bool IncludeFiles, IncludeTypes, IncludeMembers;
			public Ambience ambience;
			public StringMatcher matcher = null;
			
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
			
			internal SearchResult CheckMember (IMember member)
			{
				int rank;
				bool useDeclaringTypeName = member is IMethod && (((IMethod)member).IsConstructor || ((IMethod)member).IsDestructor);
				string memberName = useDeclaringTypeName ? member.DeclaringType.Name : member.Name;
				if (MatchName (memberName, out rank))
					return new MemberSearchResult (pattern, memberName, rank, member, false) { Ambience = ambience };
/*				if (!FullSearch)
					return null;
				memberName = useDeclaringTypeName ? member.DeclaringType.FullName : member.FullName;
				if (MatchName (memberName, out rank))
					return new MemberSearchResult (pattern, memberName, rank, member, true) { Ambience = ambience };*/
				return null;
			}
			
			Dictionary<string, MatchResult> savedMatches = new Dictionary<string, MatchResult> ();

			bool MatchName (string name, out int matchRank)
			{
				if (name == null) {
					matchRank = -1;
					return false;
				}
				MatchResult savedMatch;
				if (!savedMatches.TryGetValue (name, out savedMatch)) {
					bool doesMatch = matcher.CalcMatchRank (name, out matchRank);
					savedMatches [name] = savedMatch = new MatchResult (doesMatch, matchRank);
				}
				
				matchRank = savedMatch.Rank;
				return savedMatch.Match;
			}
		}

		class DataItemComparer : IComparer<SearchResult>
		{
			public int Compare (SearchResult o1, SearchResult o2)
			{
				var r = o2.Rank.CompareTo (o1.Rank);
				if (r == 0)
					r = o1.SearchResultType.CompareTo (o2.SearchResultType);
				if (r == 0)
					return String.CompareOrdinal (o1.MatchedString, o2.MatchedString);
				return r;
			}
		}

		struct MatchResult
		{
			public bool Match;
			public int Rank;
			
			public MatchResult (bool match, int rank)
			{
				this.Match = match;
				this.Rank = rank;
			}
		}
		
	}
	
}
