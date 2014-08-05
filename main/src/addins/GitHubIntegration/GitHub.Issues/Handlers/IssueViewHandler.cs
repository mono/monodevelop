using System;
using GitHub.Issues.Views;
using System.Collections.Generic;

namespace GitHub.Issues
{
	public interface IIssueViewHandler : IGitHubIssueViewHandler<IIssueView>
	{
	}

	public class IssueViewHandler
	{
		public bool canHandle ()
		{
			return true;
		}

		public IIssueView CreateView (String name, Octokit.Issue issue)
		{
			return new IssueView (name, issue);
		}
	}
}

