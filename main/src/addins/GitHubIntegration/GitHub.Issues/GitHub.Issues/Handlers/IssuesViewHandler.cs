using System;
using GitHub.Issues.Views;

namespace GitHub.Issues
{
	public interface IIssuesViewHandler : IGitHubIssuesViewHandler<IIssuesView>
	{
	}

	public class IssuesViewHandler
	{
		public bool canHandle()
		{
			return true;
		}

		public IIssuesView CreateView(String name)
		{
			return new IssuesView (name);
		}
	}
}

