
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Debugger.Soft.AspNet", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Debugging")]

[assembly:AddinName ("Mono Soft Debugger Support for ASP.NET")]
[assembly:AddinDescription ("Mono Soft Debugger Support for ASP.NET")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("AspNet", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger.Soft", MonoDevelop.BuildInfo.Version)]
