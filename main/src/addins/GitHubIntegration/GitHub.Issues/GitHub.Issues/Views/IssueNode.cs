using System;
using Octokit;
using Gtk;

namespace GitHub.Issues
{
	public class IssueNode
	{
		private Octokit.Issue issue;

		public IssueNode (Octokit.Issue issue)
		{
			this.issue = issue;
		}
	}
}

