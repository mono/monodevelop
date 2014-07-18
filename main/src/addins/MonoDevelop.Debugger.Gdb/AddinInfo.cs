
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Debugger.Gdb", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Debugging")]

[assembly:AddinName ("GDB Debugger Support")]
[assembly:AddinDescription ("Support for debugging native applications using the GDB debugger")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
