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
		Widget widget;
		public FileSearchCategory (Widget widget) : base (GettextCatalog.GetString("Files"))
		{
			this.widget = widget;
		}

		IEnumerable<ProjectFile> AllFiles {
			get {
				foreach (var doc in IdeApp.Workbench.Documents) {
					// We only want to check it here if it's not part
					// of the open combine.  Otherwise, it will get
					// checked down below.
					if (doc.Project == null && doc.IsFile)
						yield return new ProjectFile (doc.Name);
				}
				
				var projects = IdeApp.Workspace.GetAllProjects ();

				foreach (var p in projects) {
					foreach (ProjectFile file in p.Files) {
						if (file.Subtype != Subtype.Directory && (file.Flags & ProjectItemFlags.Hidden) != ProjectItemFlags.Hidden)
							yield return file;
					}
				}
			}
		}

		string[] validTags = new [] { "file"};

		public override string [] Tags {
			get {
				return validTags;
			}
		}

		public override bool IsValidTag (string tag)
		{
			return validTags.Any (t => t == tag);
		}

		public override Task GetResults (ISearchResultCallback searchResultCallback, SearchPopupSearchPattern pattern, CancellationToken token)
		{
			var files = AllFiles.ToList ();
			return Task.Run (delegate {
				var matcher = StringMatcher.GetMatcher (pattern.Pattern, false);
				savedMatches = new Dictionary<string, MatchResult> ();
				foreach (ProjectFile file in files) {
					if (token.IsCancellationRequested)
						break;
					int rank;
					string matchString = System.IO.Path.GetFileName (file.FilePath);
					if (MatchName (matcher, matchString, out rank))
						searchResultCallback.ReportResult (new FileSearchResult (pattern.Pattern, matchString, rank, file, true));
					matchString = FileSearchResult.GetRelProjectPath (file);
					if (MatchName (matcher, matchString, out rank)) 
						searchResultCallback.ReportResult (new FileSearchResult (pattern.Pattern, matchString, rank, file, true));
					
				}
				savedMatches = null;
			});
		}

		bool MatchName (StringMatcher matcher, string name, out int matchRank)
		{
			if (name == null) {
				matchRank = -1;
				return false;
			}
			MatchResult savedMatch;
			if (!savedMatches.TryGetValue (name, out savedMatch)) {
				bool doesMatch = matcher.CalcMatchRank (name, out matchRank);
				savedMatches[name] = savedMatch = new MatchResult (doesMatch, matchRank);
			}

			matchRank = savedMatch.Rank;
			return savedMatch.Match;
		}

		Dictionary<string, MatchResult> savedMatches = new Dictionary<string, MatchResult> ();
	}
}

