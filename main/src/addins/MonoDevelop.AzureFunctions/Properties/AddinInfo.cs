using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin ("AzureFunctions",
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Ide")]

[assembly: AddinName ("Azure Functions")]
[assembly: AddinDescription ("Azure Functions")]

[assembly: AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("DotNetCore", MonoDevelop.BuildInfo.Version)]
