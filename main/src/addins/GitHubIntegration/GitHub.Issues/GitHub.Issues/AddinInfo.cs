using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("GitHub.Issues", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "GitHub Integration")]

[assembly:AddinName ("GitHub Issues Integration")]
[assembly:AddinDescription ("A MonoDevelop addin for Issue Management Integration on GitHub")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("GitHub.Auth", MonoDevelop.BuildInfo.Version)]