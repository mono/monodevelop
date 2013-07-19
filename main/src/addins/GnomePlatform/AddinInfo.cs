
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("GnomePlatform", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Flags = AddinFlags.Hidden,
        Category = "MonoDevelop Core")]

[assembly:AddinName ("GNOME Platform Support")]
[assembly:AddinDescription ("GNOME Platform Support for MonoDevelop")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
