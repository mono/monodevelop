namespace Microsoft.WebTools.Scaffolding.Core.Config
{
	class PackageDescription
	{
		public string PackageId { get; set; }
		public string MinVersion { get; set; }
		public string MaxVersion { get; set; }
		public bool IsOptionalEfPackage { get; set; } = false;
		public bool IsOptionalIdentityPackage { get; set; } = false;
	}
}
