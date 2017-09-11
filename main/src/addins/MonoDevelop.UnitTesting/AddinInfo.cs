
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("UnitTesting", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Testing")]

[assembly:AddinName ("Unit Testing core support")]
[assembly:AddinDescription ("Unit Testing core support")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("PackageManagement", MonoDevelop.BuildInfo.Version)]
