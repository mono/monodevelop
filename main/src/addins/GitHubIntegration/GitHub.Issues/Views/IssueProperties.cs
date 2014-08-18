using System;
using System.ComponentModel;

namespace GitHub.Issues
{
	public enum IssueProperties
	{
		[Description("Title")]
		Title,
		[Description("Body")]
		Body,
		[Description("Assignee.Login")]
		AssignedTo,
		[Description("UpdatedAt")]
		LastUpdated,
		[Description("State")]
		State
	}
}

