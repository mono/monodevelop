using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin ("ConnectedServices",
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Ide")]

[assembly: AddinName ("ConnectedServices")]
[assembly: AddinDescription ("ConnectedServices")]
//[assembly: AddinFlags (AddinFlags.Hidden)]

[assembly: AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
