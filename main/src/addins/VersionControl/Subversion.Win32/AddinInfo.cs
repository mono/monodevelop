
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("VersionControl.Subversion.Windows", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Version Control")]

[assembly:AddinName ("Subversion support")]
[assembly:AddinDescription ("Subversion support for Windows")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl.Subversion", MonoDevelop.BuildInfo.Version)]
