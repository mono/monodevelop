
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Debugger", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "Debugging")]

[assembly:AddinName ("Debugger support for MonoDevelop")]
[assembly:AddinDescription ("Support for Debugging projects")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
