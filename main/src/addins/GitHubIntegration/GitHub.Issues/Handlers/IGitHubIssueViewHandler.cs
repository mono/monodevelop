using System;
using MonoDevelop.Ide.Gui;

namespace GitHub.Issues
{
	/// <summary>
	/// Interface for the Issue View Handler
	/// </summary>
	public interface IGitHubIssueViewHandler<T>
		where T : IAttachableViewContent
	{
		bool CanHandle ();

		T CreateView (String name);
	}
}

