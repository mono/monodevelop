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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core.Text;
using Gtk;
using System.Linq;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Components.MainToolbar
{
	class CommandSearchCategory : SearchCategory
	{
		Widget widget;
		public CommandSearchCategory (Widget widget) : base (GettextCatalog.GetString("Commands"))
		{
			this.widget = widget;
			this.lastResult = new WorkerResult (widget);
		}

		WorkerResult lastResult;
		string[] validTags = new [] { "cmd", "command" };

		public override Task<ISearchDataSource> GetResults (SearchPopupSearchPattern searchPattern, int resultsCount, CancellationToken token)
		{
			// NOTE: This is run on the UI thread as checking whether or not a command is enabled is not thread-safe
			return Task.Factory.StartNew (delegate {
				try {
					if (searchPattern.Tag != null && !validTags.Contains (searchPattern.Tag) || searchPattern.HasLineNumber)
						return null;
					WorkerResult newResult = new WorkerResult (widget);
					newResult.pattern = searchPattern.Pattern;

					newResult.matcher = StringMatcher.GetMatcher (searchPattern.Pattern, false);
					newResult.FullSearch = true;


					AllResults (lastResult, newResult, token);
					newResult.results.SortUpToN (new DataItemComparer (token), resultsCount);
					lastResult = newResult;
					return (ISearchDataSource)newResult.results;
				} catch {
					token.ThrowIfCancellationRequested ();
					throw;
				}
			}, token, TaskCreationOptions.None, Xwt.Application.UITaskScheduler);
		}

		void AllResults (WorkerResult lastResult, WorkerResult newResult, CancellationToken token)
		{
			newResult.filteredCommands = new List<Command> ();
			bool startsWithLastFilter = lastResult != null && lastResult.pattern != null && newResult.pattern.StartsWith (lastResult.pattern) && lastResult.filteredCommands != null;
			IEnumerable<Command> allCommands = startsWithLastFilter ? lastResult.filteredCommands : IdeApp.CommandService.GetCommands ();
			foreach (Command cmd in allCommands) {
				token.ThrowIfCancellationRequested ();
				SearchResult curResult = newResult.CheckCommand (cmd);
				if (curResult != null) {
					newResult.filteredCommands.Add (cmd);
					newResult.results.AddResult (curResult);
				}
			}
		}
		
		class WorkerResult 
		{
			public List<Command> filteredCommands = null;
			public string pattern = null;
			public ResultsDataSource results;
			public bool FullSearch;
			public StringMatcher matcher = null;
			
			public WorkerResult (Widget widget)
			{
				results = new ResultsDataSource (widget);
			}
			
			internal SearchResult CheckCommand (Command c)
			{
				ActionCommand cmd = c as ActionCommand;
				if (cmd == null || cmd.CommandArray)
					return null;

				int rank;
				string matchString = cmd.Text.Replace ("_", "");
				if (MatchName (matchString, out rank)) {
					var ci = IdeApp.CommandService.GetCommandInfo (cmd.Id);
					if (ci.Enabled && ci.Visible)
						return new CommandResult (cmd, pattern, matchString, rank);
				}
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
					savedMatches[name] = savedMatch = new MatchResult (doesMatch, matchRank);
				}
				
				matchRank = savedMatch.Rank;
				return savedMatch.Match;
			}
		}
	}
}

