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
		ViewIssueHandler,
		LabelsHandler
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

	class ViewIssueHandler : CommandHandler
	{
		public ViewIssueHandler (Octokit.Issue issue) : base ()
		{
			this.issue = issue;
			this.Run ();
		}

		private Octokit.Issue issue;

		protected override void Run ()
		{
			IdeApp.Workbench.OpenDocument (new IssueView (this.issue != null ? this.issue.Title : StringResources.NewIssueTitle, this.issue), true);
		}
	}

	class LabelsHandler : CommandHandler
	{
		public LabelsHandler ()
		{
			this.Run ();
		}

		protected override void Run ()
		{
			IdeApp.Workbench.OpenDocument (new LabelsView (StringResources.ManageLabels), true);
		}
	}
}

