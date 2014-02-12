
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Debugger.Soft", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Debugging")]

[assembly:AddinName ("Mono Soft Debugger Support")]
[assembly:AddinDescription ("Mono Soft Debugger Support")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
