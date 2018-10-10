namespace MonoDevelop.AspNetCore.Commands
{
	using MonoDevelop.Core.Serialization;

	[DataItem]
	public sealed class ProjectPublishProfile
	{
		public const string ProjectPublishProfileKey = "PublishProfiles";

		[ItemProperty]
		public string Name { get; set; }

		[ItemProperty]
		public string FileName { get; set; }

		public ProjectPublishProfile () {}
		 
		public ProjectPublishProfile (string name, string fileName)
		{
			this.FileName = fileName;
			this.Name = name;
		}
	}
}
