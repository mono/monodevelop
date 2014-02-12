
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Deployment.Linux", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Deployment")]

[assembly:AddinName ("Deployment Services for Linux")]
[assembly:AddinDescription ("Provides basic deployment services for Linux")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Deployment", MonoDevelop.BuildInfo.Version)]
