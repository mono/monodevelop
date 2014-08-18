using System;
using GitHub.Issues.Views;
using System.Collections.Generic;

namespace GitHub.Issues
{
	/// <summary>
	/// Interface for issues view handler
	/// </summary>
	public interface IIssuesViewHandler : IGitHubIssuesViewHandler<IIssuesView>
	{
	}

	/// <summary>
	/// Handles creation of the Issues View which displays all issues
	/// </summary>
	public class IssuesViewHandler
	{
		/// <summary>
		/// Can it run?
		/// </summary>
		/// <returns><c>true</c>, if handle was caned, <c>false</c> otherwise.</returns>
		public bool canHandle ()
		{
			return true;
		}

		/// <summary>
		/// Creates the view.
		/// </summary>
		/// <returns>The view.</returns>
		/// <param name="name">Name.</param>
		/// <param name="issues">Issues.</param>
		public IIssuesView CreateView (String name, IReadOnlyList<Octokit.Issue> issues)
		{
			return new IssuesView (name, issues);
		}
	}
}

