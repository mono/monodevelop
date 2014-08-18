using System;
using Octokit;
using Gtk;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Issues
{
	/// <summary>
	/// Issue node - wrapper class for the Octokit version of the issue
	/// Allows to access more complex details in a simple manner (see Assigee)
	/// </summary>
	public class IssueNode
	{
		/// <summary>
		/// The issue.
		/// </summary>
		public Octokit.Issue Issue;

		/// <summary>
		/// Gets the title.
		/// </summary>
		/// <value>The title.</value>
		[Description ("Title")]
		public String Title { get { return this.Issue.Title; } }

		/// <summary>
		/// Gets the body.
		/// </summary>
		/// <value>The body.</value>
		[Description ("Description")]
		public String Body { get { return this.Issue.Body; } }

		/// <summary>
		/// Gets the assigee.
		/// </summary>
		/// <value>The assigee.</value>
		[Description ("Assigned To")]
		public String Assigee { get { return this.Issue.Assignee != null ? this.Issue.Assignee.Login : "Unassigned"; } }

		/// <summary>
		/// Gets the updated at.
		/// </summary>
		/// <value>The updated at.</value>
		[Description ("Last Updated At")]
		public String UpdatedAt { get { return this.Issue.UpdatedAt.ToString (); } }

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <value>The state.</value>
		[Description ("State")]
		public String State { get { return this.Issue.State.ToString (); } }

		/// <summary>
		/// Gets the labels.
		/// </summary>
		/// <value>The labels.</value>
		[Description ("Labels")]
		public String Labels {
			get {
				var query = from label in this.Issue.Labels
				            select label.Name;

				return String.Join (", ", query);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.IssueNode"/> class.
		/// </summary>
		/// <param name="Issue">Issue.</param>
		public IssueNode (Octokit.Issue Issue)
		{
			this.Issue = Issue;
		}
	}
}

