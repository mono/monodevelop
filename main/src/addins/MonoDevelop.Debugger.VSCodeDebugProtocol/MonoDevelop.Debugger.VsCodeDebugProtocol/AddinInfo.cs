
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Debugger.VsCodeDebugProtocol", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Debugging")]

[assembly:AddinName ("VsCode Debug Protocol support for MonoDevelop")]
[assembly:AddinDescription ("Support for Debugging over VsCode debug protocol")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly: AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
