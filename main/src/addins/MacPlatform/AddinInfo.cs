
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("MacPlatform", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Flags = AddinFlags.Hidden,
        Category = "Platform Support")]

[assembly:AddinName ("MonoDevelop Mac Platform Support")]
[assembly:AddinDescription ("Mac Platform Support for MonoDevelop")]

[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
