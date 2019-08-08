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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace MonoDevelop.Components.MainToolbar
{
	class CommandSearchCategory : SearchCategory
	{
		static readonly Mono.Addins.RuntimeAddin currentRuntimeAddin;
		static readonly string hiddenCategory = GettextCatalog.GetString ("Hidden");

		static IEnumerable<Command> GetAllCommands ()
		{
			Runtime.AssertMainThread ();

			foreach (var cmd in IdeApp.CommandService.GetCommands ()) {
				if ((cmd as ActionCommand)?.CommandArray != true && cmd.Category != hiddenCategory) 
					yield return cmd;
			}
		}


		static CommandSearchCategory ()
		{
			// The AddinManager is not thread safe, so we need to make sure we're
			// calling from the main thread.
			Runtime.AssertMainThread ();
			currentRuntimeAddin = Mono.Addins.AddinManager.CurrentAddin;
		}

		public CommandSearchCategory () : base (GettextCatalog.GetString("Commands"))
		{
		}

		readonly string[] validTags = { "cmd", "command", "c" };

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
			if (pattern.HasLineNumber)
				return Task.CompletedTask;
			var route = new CommandTargetRoute (IdeApp.CommandService.LastCommandTarget);
			var matcher = StringMatcher.GetMatcher (pattern.Pattern, false);
			foreach (var cmd in GetAllCommands ()) {
				if (token.IsCancellationRequested)
					break;
				var matchString = cmd.DisplayName;

				if (matcher.CalcMatchRank (matchString, out var rank)) {
					if ((cmd as ActionCommand)?.RuntimeAddin == currentRuntimeAddin)
						rank += 1; // we prefer commands comming from the addin
					searchResultCallback.ReportResult (new CommandResult (cmd, null, route, pattern.Pattern, matchString, rank));
				}
			}
			return Task.CompletedTask;
		}
	}
}