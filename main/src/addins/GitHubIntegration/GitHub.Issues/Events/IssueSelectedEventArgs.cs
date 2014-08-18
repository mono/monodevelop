using System;

namespace GitHub.Issues
{
	/// <summary>
	/// Represents the event arguments for selecting an issue from the tree view/data grid
	/// </summary>
	public class IssueSelectedEventArgs : EventArgs
	{
		/// <summary>
		/// The selected issue.
		/// </summary>
		public Octokit.Issue SelectedIssue;

		/// <summary>
		/// The old selected issue.
		/// </summary>
		public Octokit.Issue OldSelectedIssue;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.IssueSelectedEventArgs"/> class.
		/// </summary>
		public IssueSelectedEventArgs ()
		{

		}
	}
}

