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
		/// Current repository that we are working in
		/// </summary>
		/// <value>The current repository.</value>
		private Octokit.Repository currentRepository 
		{
			get {
				if (this.repository == null) {
					// Find the current repository and its information
					GitHubUtilities utilities = new GitHubUtilities ();
					this.repository = utilities.ORepository;
				}

				return this.repository;
			}
		}

		/// <summary>
		/// The repository we are working with - read from the other property since it's a smart getter
		/// </summary>
		private Octokit.Repository repository;

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
				Task<IReadOnlyList<Octokit.Issue>> task = gitHubClient.Issue.GetForRepository (currentRepository.Owner.Login, currentRepository.Name);
				IReadOnlyList<Octokit.Issue> issues = task.Result;
				return issues;
			}
			catch (Exception e) {
				HandleException (e);
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
				Task<IReadOnlyList<Octokit.Label>> task = gitHubClient.Issue.Labels.GetForRepository(currentRepository.Owner.Login, currentRepository.Name);
				IReadOnlyList<Octokit.Label> labels = task.Result;
				return labels;
			}
			catch (Exception e) {
				HandleException (e);
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

				Task<IReadOnlyList<Octokit.IssueComment>> comments = gitHubClient.Issue.Comment.GetForIssue(currentRepository.Owner.Login, currentRepository.Name, issue.Number);

				return comments.Result;
			}
			catch (Exception e) {
				HandleException (e);
			}

			return null;
		}

		/// <summary>
		/// Adds the comment to a given issue.
		/// </summary>
		/// <returns>Comment that got created.</returns>
		/// <param name="issue">Issue to add the comment to.</param>
		/// <param name="comment">Comment to add.</param>
		public Octokit.IssueComment AddComment(Octokit.Issue issue, String comment)
		{
			try
			{
				return gitHubClient.Issue.Comment.Create (currentRepository.Owner.Login, currentRepository.Name, issue.Number, comment).Result;
			}
			catch (Exception e) {
				HandleException (e);
			}

			return null;
		}

		/// <summary>
		/// Deletes a comment from the issue
		/// </summary>
		/// <param name="comment">Comment to delete.</param>
		public void DeleteComment(Octokit.IssueComment comment)
		{
			try
			{
				gitHubClient.Issue.Comment.Delete(currentRepository.Owner.Login, currentRepository.Name, comment.Id);
			}
			catch (Exception e) {
				HandleException (e);
			}
		}

		/// <summary>
		/// Updates the issue.
		/// </summary>
		/// <param name="issue">Issue we are updating.</param>
		/// <param name="title">New title of the issue.</param>
		/// <param name="body">New body of the issue.</param>
		/// <param name="labels">All labels assigned to the issue.</param>
		/// <param name="state">New state of the issue.</param>
		/// <param name="milestone">New milestone of the issue</param> 
		public void UpdateIssue(Octokit.Issue issue, String title, String body, String assignee, String[] labels, Octokit.ItemState state, Octokit.Milestone milestone)
		{
			try
			{
				// Update issue details
				gitHubClient.Issue.Update (currentRepository.Owner.Login, currentRepository.Name, issue.Number, new IssueUpdate () {
					Title = title,
					Body = body,
					State = state,
					Milestone = milestone.Number,
					Assignee = assignee
				});
						
				// Update labels for issue
				gitHubClient.Issue.Labels.ReplaceAllForIssue(currentRepository.Owner.Login, currentRepository.Name, issue.Number, labels).Wait();
			}
			catch (Exception e) {
				HandleException (e);
			}
		}

		/// <summary>
		/// Creates a new issue
		/// </summary>
		/// <returns>The created issue.</returns>
		/// <param name="title">Title.</param>
		/// <param name="body">Body.</param>
		/// <param name="assignee">Assignee.</param>
		/// <param name="labels">Labels.</param>
		/// <param name="milestone">Milestone.</param>
		public Octokit.Issue CreateIssue (String title, String body, String assignee, String[] labels, Octokit.Milestone milestone)
		{
			Octokit.Issue newIssue = null;

			try
			{
				newIssue = gitHubClient.Issue.Create (currentRepository.Owner.Login, currentRepository.Name, new NewIssue (title) {
					Body = body,
					Assignee = assignee,
					Milestone = milestone != null ? (int?)milestone.Number : null
				}).Result;

				gitHubClient.Issue.Labels.ReplaceAllForIssue (currentRepository.Owner.Login, currentRepository.Name, newIssue.Number, labels).Wait();
			}
			catch (Exception e) {
				HandleException (e);
			}

			return newIssue;
		}

		/// <summary>
		/// Gets the selected milestone for a specified issue.
		/// </summary>
		/// <returns>The selected milestone.</returns>
		/// <param name="issue">Issue.</param>
		public Octokit.Milestone GetSelectedMilestone(Octokit.Issue issue)
		{
			try
			{
				return gitHubClient.Issue.Milestone.Get(currentRepository.Owner.Login, currentRepository.Name, issue.Number).Result;
			}
			catch (Exception e) {
				HandleException (e);
			}

			return null;
		}

		/// <summary>
		/// Gets all milestones for the current repository
		/// </summary>
		/// <returns>All milestones for the current repository.</returns>
		public IReadOnlyList<Octokit.Milestone> GetAllMilestones()
		{
			try
			{
				return gitHubClient.Issue.Milestone.GetForRepository(currentRepository.Owner.Login, currentRepository.Name).Result;
			}
			catch (Exception e) {
				HandleException (e);
			}

			return null;
		}

		/// <summary>
		/// Gets all assignees for the current repository.
		/// </summary>
		/// <returns>All possible assignees.</returns>
		public IReadOnlyList<Octokit.User> GetAllAssignees()
		{
			try
			{
				return gitHubClient.Issue.Assignee.GetForRepository(currentRepository.Owner.Login, currentRepository.Name).Result;
			}
			catch (Exception e) {
				HandleException (e);
			}

			return null;
		}

		/// <summary>
		/// Handles exceptions for all GitHub client calls
		/// </summary>
		/// <param name="e">Exception</param>
		static void HandleException (Exception e)
		{
			// Need to compare manually otherwise it escapes into another catch which catches "Exception" :(
			if (e.InnerException != null && e.InnerException is Octokit.ApiException) {
				MessageService.ShowError (e.InnerException.Message);
			}
			else {
				// Done to preserve the stack trace
				throw new Exception ("", e);
			}
		}
	}
}