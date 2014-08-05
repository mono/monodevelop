using System;
using MonoDevelop.Ide.Gui;

namespace GitHub.Issues
{
	public interface IGitHubIssueViewHandler<T>
		where T : IAttachableViewContent
	{
		bool CanHandle ();

		T CreateView (String name);
	}
}

