using System;
using Octokit;
using Gtk;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Issues
{
	public class IssueNode
	{
		public Octokit.Issue Issue;

		[Description("Title")]
		public String Title { get { return this.Issue.Title; } }
		[Description("Description")]
		public String Body { get { return this.Issue.Body; } }
		[Description("Assigned To")]
		public String Assigee { get { return this.Issue.Assignee != null ? this.Issue.Assignee.Login : "Unassigned"; } }
		[Description("Last Updated At")]
		public String UpdatedAt { get { return this.Issue.UpdatedAt.ToString (); } }
		[Description("State")]
		public String State { get { return this.Issue.State.ToString (); } }
		[Description("Labels")]
		public String Labels
		{
			get {
				var query = from label in this.Issue.Labels select label.Name;

				return String.Join (", ", query);
			}
		}

		public IssueNode (Octokit.Issue Issue)
		{
			this.Issue = Issue;
		}
	}
}

