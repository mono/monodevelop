using System;

namespace GitHub.Issues
{
	/// <summary>
	/// Represents the event arguments for saving an issue
	/// </summary>
	public class IssueSavedEventArgs : EventArgs
	{
		/// <summary>
		/// The issue.
		/// </summary>
		public Octokit.Issue issue;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.IssueSavedEventArgs"/> class.
		/// </summary>
		public IssueSavedEventArgs ()
		{

		}
	}
}