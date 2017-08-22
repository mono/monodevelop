
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("UnitTesting.NUnit", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Testing")]

[assembly:AddinName ("NUnit support")]
[assembly:AddinDescription ("Integrates NUnit into the MonoDevelop IDE")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("UnitTesting", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("PackageManagement", MonoDevelop.BuildInfo.Version)]
