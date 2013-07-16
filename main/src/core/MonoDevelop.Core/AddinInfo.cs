
using System;
using Mono.Addins;

[AddinRoot ("Core", 
        Namespace = "MonoDevelop", 
        Version = MonoDevelop.BuildInfo.Version,
        CompatVersion = MonoDevelop.BuildInfo.CompatVersion,
        Category = "MonoDevelop Core")]

[AddinName ("MonoDevelop Runtime")]
[AddinDescription ("Provides the core services of the MonoDevelop platform")]
