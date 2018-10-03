using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore.Commands
{
	sealed class PublishCommandItem
	{
		public PublishCommandItem (DotNetProject project, ProjectPublishProfile profile)
		{
			IsReentrant |= profile != null;
			this.Project = project;
			this.Profile = profile;
		}

		public bool IsReentrant { get; private set; }

		public DotNetProject Project { get; private set; }

		public ProjectPublishProfile Profile { get; set; }
	}
}
