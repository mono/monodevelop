
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("VersionControl.Git", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Version Control")]

[assembly:AddinName ("Git support")]
[assembly:AddinDescription ("Git support for the Version Control Add-in")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl", MonoDevelop.BuildInfo.Version)]
