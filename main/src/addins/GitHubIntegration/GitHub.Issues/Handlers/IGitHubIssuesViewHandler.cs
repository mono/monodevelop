using System;
using MonoDevelop.Ide.Gui;

namespace GitHub.Issues
{
	public interface IGitHubIssuesViewHandler<T>
		where T : IAttachableViewContent
	{
		bool CanHandle ();

		T CreateView (String name);
	}
}

