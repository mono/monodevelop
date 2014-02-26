
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("VersionControl.Subversion", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Version Control")]

[assembly:AddinName ("Subversion core engine")]
[assembly:AddinDescription ("Subversion core engine")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl", MonoDevelop.BuildInfo.Version)]
