using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using GitHub.Issues.Views;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;

namespace GitHub.Issues
{
	public enum Commands
	{
		GetAllIssues
	}

	class GetAllIssuesHandler : CommandHandler
	{
		protected override void Run ()
		{
			IssuesManager manager = new IssuesManager ();
			IReadOnlyList<Octokit.Issue> issues = manager.GetAllIssues ();

			IdeApp.Workbench.OpenDocument (new IssuesView ("Issues View", issues), true);
		}
	}
}

