using System;

namespace GitHub.Issues
{
	public class DeleteCommentClickEventArgs : EventArgs
	{
		public Octokit.IssueComment CommentToDelete;

		public DeleteCommentClickEventArgs ()
		{

		}
	}
}

