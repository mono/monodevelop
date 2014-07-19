using System;
using GitHub.Auth;
using Octokit;
using System.Collections.Generic;
using Gtk;
using System.Threading.Tasks;
using MonoDevelop.Ide;

namespace GitHub.Issues
{
	/// <summary>
	/// Issues manager.
	/// </summary>
	public class IssuesManager
	{
		/// <summary>
		/// The git hub client.
		/// </summary>
		private GitHubClient gitHubClient = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.IssuesManager"/> class.
		/// </summary>
		public IssuesManager ()
		{
			this.gitHubClient = GitHubService.Client;
		}

		/// <summary>
		/// Gets all issues for the current repository
		/// </summary>
		/// <returns>The all issues.</returns>
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
				// Need to compare manually otherwise it escapes into another catch which catches "Exception" :(
				if (e.InnerException != null && e.InnerException is Octokit.ApiException) {
					MessageService.ShowError (e.InnerException.Message);
				} 
				else {
					// Done to preserve the stack trace
					throw new Exception ("", e);
				}
			}

			return null;
		}

		/// <summary>
		/// Gets all labels for the current repository
		/// </summary>
		/// <returns>The all labels.</returns>
		public IReadOnlyList<Octokit.Label> GetAllLabels()
		{
			try {
				GitHubUtilities utilities = new GitHubUtilities();
				Octokit.Repository currentRepository = utilities.ORepository;

				Task<IReadOnlyList<Octokit.Label>> task = gitHubClient.Issue.Labels.GetForRepository(currentRepository.Owner.Login, currentRepository.Name);
				IReadOnlyList<Octokit.Label> labels = task.Result;
				return labels;
			}
			catch (Exception e) {
				// Need to compare manually otherwise it escapes into another catch which catches "Exception" :(
				if (e.InnerException != null && e.InnerException is Octokit.ApiException) {
					MessageService.ShowError (e.InnerException.Message);
				} 
				else {
					// Done to preserve the stack trace
					throw new Exception ("", e);
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the comments for issue.
		/// </summary>
		/// <returns>The comments for issue.</returns>
		/// <param name="issue">Issue.</param>
		public IReadOnlyList<Octokit.IssueComment> GetCommentsForIssue(Octokit.Issue issue)
		{
			try {
				int numberOfComments = issue.Comments;

				GitHubUtilities utilities = new GitHubUtilities();
				Octokit.Repository currentRepository = utilities.ORepository;

				Task<IReadOnlyList<Octokit.IssueComment>> comments = gitHubClient.Issue.Comment.GetForIssue(issue.User.Login, currentRepository.Name, issue.Number);

				return comments.Result;
			}
			catch (Exception e) {
				// Need to compare manually otherwise it escapes into another catch which catches "Exception" :(
				if (e.InnerException != null && e.InnerException is Octokit.ApiException) {
					MessageService.ShowError (e.InnerException.Message);
				} 
				else {
					// Done to preserve the stack trace
					throw new Exception ("", e);
				}
			}

			return null;
		}
	}
}