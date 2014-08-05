using System;
using GitHub.Issues.Views;
using System.Collections.Generic;

namespace GitHub.Issues
{
	public interface IIssuesViewHandler : IGitHubIssuesViewHandler<IIssuesView>
	{
	}

	public class IssuesViewHandler
	{
		public bool canHandle ()
		{
			return true;
		}

		public IIssuesView CreateView (String name, IReadOnlyList<Octokit.Issue> issues)
		{
			return new IssuesView (name, issues);
		}
	}
}

