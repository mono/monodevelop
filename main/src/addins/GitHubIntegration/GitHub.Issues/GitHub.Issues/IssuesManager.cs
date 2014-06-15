using System;
using GitHub.Auth;
using Octokit;
using System.Collections.Generic;
using Gtk;
using System.Threading.Tasks;

namespace GitHub.Issues
{
	public class IssuesManager
	{
		private GitHubClient gitHubClient = null;

		public IssuesManager ()
		{
			this.gitHubClient = GitHubService.Client;
		}

		public IReadOnlyList<Octokit.Issue> GetAllIssues()
		{
			Task<IReadOnlyList<Octokit.Issue>> task = gitHubClient.Issue.GetForRepository ("Kalnor", "testRepo");
			return task.Result;
		}
	}
}