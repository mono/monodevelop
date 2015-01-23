
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("XUnit", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Testing")]

[assembly:AddinName ("xUnit.NET support")]
[assembly:AddinDescription ("Integrates xUnit.NET into the MonoDevelop IDE")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("NUnit", MonoDevelop.BuildInfo.Version)]
