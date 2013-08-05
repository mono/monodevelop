
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("DesignerSupport", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Flags = AddinFlags.Hidden,
        Category = "MonoDevelop Core")]

[assembly:AddinName ("Visual Designer Support")]
[assembly:AddinDescription ("Supporting services and pads for visual design tools")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
