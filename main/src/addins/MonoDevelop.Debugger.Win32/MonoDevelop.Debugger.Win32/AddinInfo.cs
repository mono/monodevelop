
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Debugger.Win32", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Debugging")]

[assembly:AddinName ("Microsoft .NET support for Mono.Debugging")]
[assembly:AddinDescription ("Managed Debugging Engine support for MS.NET")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("AspNet", MonoDevelop.BuildInfo.Version)]
