//
// FileSearchCategory.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 mkrueger
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
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Components.MainToolbar
{
	class FileSearchCategory : SearchCategory
	{
		public FileSearchCategory () : base (GettextCatalog.GetString ("Files"))
		{
		}

		static FileSearchCategory ()
		{
			IdeApp.Workspace.SolutionLoaded += delegate { allFilesCache = null; };
			IdeApp.Workspace.SolutionUnloaded += delegate { allFilesCache = null; };
			IdeApp.Workspace.ItemAddedToSolution += delegate { allFilesCache = null; };
			IdeApp.Workspace.ItemRemovedFromSolution += delegate { allFilesCache = null; };
			IdeApp.Workspace.FileAddedToProject += delegate { allFilesCache = null; };
			IdeApp.Workspace.FileRemovedFromProject += delegate { allFilesCache = null; };
			IdeApp.Workspace.FileRenamedInProject += delegate { allFilesCache = null; };
			IdeApp.Workbench.DocumentOpened += delegate { allFilesCache = null; };
			IdeApp.Workbench.DocumentClosed += delegate { allFilesCache = null; };
		}

		List<Tuple<string, string, ProjectFile>> GenerateAllFiles ()
		{
			//Slowest thing here is GetRelProjectPath, hence Tuple<,,> needs to be cached
			var list = new List<Tuple<string, string, ProjectFile>> ();
			foreach (var doc in IdeApp.Workbench.Documents) {
				// We only want to check it here if it's not part
				// of the open combine.  Otherwise, it will get
				// checked down below.
				if (doc.Project == null && doc.IsFile) {
					var pf = new ProjectFile (doc.Name);
					list.Add (new Tuple<string, string, ProjectFile> (System.IO.Path.GetFileName (pf.FilePath), FileSearchResult.GetRelProjectPath (pf), pf));
				}
			}

			var projects = IdeApp.Workspace.GetAllProjects ();

			foreach (var p in projects) {
				foreach (ProjectFile pf in p.Files) {
					if (pf.Subtype != Subtype.Directory && (pf.Flags & ProjectItemFlags.Hidden) != ProjectItemFlags.Hidden) {
						list.Add (new Tuple<string, string, ProjectFile> (System.IO.Path.GetFileName (pf.FilePath), FileSearchResult.GetRelProjectPath (pf), pf));
					}
				}
			}
			return list;
		}

		string [] validTags = new [] { "file", "f" };

		public override string [] Tags {
			get {
				return validTags;
			}
		}

		public override bool IsValidTag (string tag)
		{
			return validTags.Any (t => t == tag);
		}

		static List<Tuple<string, string, ProjectFile>> allFilesCache;

		public override Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern pattern, CancellationToken token)
		{
			return Task.Run (delegate {
				var files = allFilesCache = allFilesCache ?? GenerateAllFiles ();
				var matcher = StringMatcher.GetMatcher (pattern.Pattern, false);
				var savedMatches = new Dictionary<string, MatchResult> ();
				foreach (var file in files) {
					if (token.IsCancellationRequested)
						break;
					int rank1;
					int rank2;
					var match1 = MatchName (savedMatches, matcher, file.Item1, out rank1);
					var match2 = MatchName (savedMatches, matcher, file.Item2, out rank2);
					if (match1 && match2) {
						if (rank1 > rank2 || (rank1 == rank2 && String.CompareOrdinal (file.Item1, file.Item2) > 0)) {
							searchResultCallback.ReportResult (new FileSearchResult (pattern.Pattern, file.Item1, rank1, file.Item3));
						} else {
							searchResultCallback.ReportResult (new FileSearchResult (pattern.Pattern, file.Item2, rank2, file.Item3));
						}
					} else if (match1) {
						searchResultCallback.ReportResult (new FileSearchResult (pattern.Pattern, file.Item1, rank1, file.Item3));
					} else if (match2) {
						searchResultCallback.ReportResult (new FileSearchResult (pattern.Pattern, file.Item2, rank2, file.Item3));
					}
				}
			}, token);
		}

		static bool MatchName (Dictionary<string, MatchResult> savedMatches, StringMatcher matcher, string name, out int matchRank)
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
}

