
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("MonoDeveloperExtensions", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Project Import and Export")]

[assembly:AddinName ("IDE Extensions for developers of the Mono framework")]
[assembly:AddinDescription ("Provides some IDE extensions useful for developing and building the Mono class libraries")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]

[assembly:AddinModule ("MonoDeveloperExtensions_nunit.dll")]