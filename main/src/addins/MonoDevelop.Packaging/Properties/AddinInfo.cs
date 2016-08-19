
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Packaging", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "IDE extensions")]

[assembly:AddinName ("NuGet Packaging")]
[assembly:AddinDescription ("Provides support for creating NuGet packages.")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("PackageManagement", MonoDevelop.BuildInfo.Version)]
