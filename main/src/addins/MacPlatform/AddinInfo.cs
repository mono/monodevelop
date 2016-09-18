
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("MacPlatform", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "Platform Support")]

[assembly:AddinName ("MonoDevelop Mac Platform Support")]
[assembly:AddinDescription ("Mac Platform Support for MonoDevelop")]
[assembly: AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
