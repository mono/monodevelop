using System;
using MonoDevelop.Ide.Gui;

namespace GitHub.Issues
{
	/// <summary>
	/// Interface for the Issues View handler
	/// </summary>
	public interface IGitHubIssuesViewHandler<T>
		where T : IAttachableViewContent
	{
		bool CanHandle ();

		T CreateView (String name);
	}
}

