
using System;
using Mono.Addins;

[assembly:AddinRoot ("Core", 
        Namespace = "MonoDevelop", 
        Version = MonoDevelop.BuildInfo.Version,
        CompatVersion = MonoDevelop.BuildInfo.CompatVersion,
        Category = "MonoDevelop Core")]

[assembly:AddinName ("MonoDevelop Runtime")]
[assembly:AddinDescription ("Provides the core services of the MonoDevelop platform")]
