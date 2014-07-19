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
		GetAllIssues,
		ViewIssueHandler
	}

	class GetAllIssuesHandler : CommandHandler
	{
		protected override void Run ()
		{
			IssuesManager manager = new IssuesManager ();
			IReadOnlyList<Octokit.Issue> issues = manager.GetAllIssues ();

			if (issues != null)
				IdeApp.Workbench.OpenDocument (new IssuesView ("Issues View", issues), true);
		}
	}

	class ViewIssueHandler : CommandHandler
	{
		public ViewIssueHandler(Octokit.Issue issue) : base ()
		{
			this.issue = issue;
			this.Run ();
		}

		private Octokit.Issue issue;

		protected override void Run ()
		{
			if (this.issue != null)
				IdeApp.Workbench.OpenDocument(new IssueView(this.issue.Title, this.issue), true);
		}
	}
}

