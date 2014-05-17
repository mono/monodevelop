using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("GitHub.Auth", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Version Control")]

[assembly:AddinName ("GitHub Authentication support")]
[assembly:AddinDescription ("A MonoDevelop addin for authenticating with GitHub")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
