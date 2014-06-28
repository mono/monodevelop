using System;
using GitHub.Auth;
using Octokit;
using System.Collections.Generic;
using Gtk;
using System.Threading.Tasks;
using MonoDevelop.Ide;

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
			try {
				// Find the current repository and its information
				GitHubUtilities utilities = new GitHubUtilities ();
				Octokit.Repository currentRepository = utilities.ORepository;

				Task<IReadOnlyList<Octokit.Issue>> task = gitHubClient.Issue.GetForRepository (currentRepository.Owner.Login, currentRepository.Name);
				IReadOnlyList<Octokit.Issue> issues = task.Result;
				return issues;
			}
			catch (Exception e) {
				if (e.InnerException != null) {
					MessageService.ShowError (e.InnerException.Message);
				} 
				else {
					MessageService.ShowError (e.Message);
				}
			}

			return null;
		}
	}
}