
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("VersionControl", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Version Control")]

[assembly:AddinName ("Version Control Support")]
[assembly:AddinDescription ("A MonoDevelop addin for using version control systems like Subversion")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]
