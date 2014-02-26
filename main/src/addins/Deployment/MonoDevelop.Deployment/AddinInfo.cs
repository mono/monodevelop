
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Deployment", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Deployment")]

[assembly:AddinName ("Deployment Services Core")]
[assembly:AddinDescription ("Provides basic deployment services")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
