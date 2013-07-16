
using System;
using Mono.Addins;

[AddinRoot ("Ide", 
            Namespace = "MonoDevelop", 
            Version = MonoDevelop.BuildInfo.Version,
            CompatVersion = MonoDevelop.BuildInfo.CompatVersion,
            Category = "MonoDevelop Core")]

[AddinName ("MonoDevelop Ide")]
[AddinDescription ("The MonoDevelop IDE application")]

[AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
