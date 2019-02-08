
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("ExtensionTool", 
    Namespace = "MonoDevelop",
    Version = MonoDevelop.BuildInfo.Version,
	Category = "MonoDevelop Core")]

[assembly:AddinName ("Extension Developer Tools")]
[assembly:AddinDescription ("Tools used to analyze the extension model")]
[assembly:AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
