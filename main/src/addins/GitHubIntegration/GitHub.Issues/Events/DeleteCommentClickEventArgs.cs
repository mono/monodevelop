using System;

namespace GitHub.Issues
{
	/// <summary>
	/// Represents the event arguments for deletion of a comment
	/// </summary>
	public class DeleteCommentClickEventArgs : EventArgs
	{
		/// <summary>
		/// The comment to delete.
		/// </summary>
		public Octokit.IssueComment CommentToDelete;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.DeleteCommentClickEventArgs"/> class.
		/// </summary>
		public DeleteCommentClickEventArgs ()
		{

		}
	}
}

