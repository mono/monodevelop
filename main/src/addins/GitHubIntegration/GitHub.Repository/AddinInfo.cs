using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("GitHub.Repository", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "GitHub Integration")]

[assembly:AddinName ("GitHub Repository integration support")]
[assembly:AddinDescription ("A MonoDevelop addin for authenticating with GitHub")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("GitHub.Auth", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl.Git", MonoDevelop.BuildInfo.Version)]