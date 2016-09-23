using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin ("PerformanceDiagnostics",
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Diagnostics")]

[assembly: AddinName ("Performance Diagnostics of IDE")]
[assembly: AddinDescription ("Set of tools which help tracking IDE performance bottlenecks.")]
[assembly: AddinFlags (AddinFlags.Hidden)]

[assembly: AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]