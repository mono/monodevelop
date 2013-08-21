
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("HexEditor", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "IDE extensions")]

[assembly:AddinName ("MonoDevelop Hex Editor")]
[assembly:AddinDescription ("Provides a hex editor for MonoDevelop")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("AssemblyBrowser", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
