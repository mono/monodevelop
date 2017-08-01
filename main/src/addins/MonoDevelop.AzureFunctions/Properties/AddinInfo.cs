using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin ("AzureFunctions",
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Ide")]

+[assembly: AddinName ("Azure Functions development (Preview)")]
 +[assembly: AddinDescription ("Preview support for developing micro-services with Azure Functions.")]

[assembly: AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("DotNetCore", MonoDevelop.BuildInfo.Version)]
