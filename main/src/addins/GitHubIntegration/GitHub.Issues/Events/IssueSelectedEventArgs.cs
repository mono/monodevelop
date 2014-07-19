using System;

namespace GitHub.Issues
{
	public class IssueSelectedEventArgs : EventArgs
	{
		public Octokit.Issue SelectedIssue;

		public Octokit.Issue OldSelectedIssue;

		public IssueSelectedEventArgs ()
		{

		}
	}
}

