
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("TestRunner", 
    Namespace = "MonoDevelop",
    Version = MonoDevelop.BuildInfo.Version,
	Category = "MonoDevelop Core")]

[assembly:AddinName ("Test Runner")]
[assembly:AddinDescription ("Test runner for the MonoDevelop unit tests")]
[assembly: AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
