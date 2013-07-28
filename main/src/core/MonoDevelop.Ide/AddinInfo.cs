
using System;
using Mono.Addins;

[assembly:AddinRoot ("Ide", 
            Namespace = "MonoDevelop", 
            Version = MonoDevelop.BuildInfo.Version,
            CompatVersion = MonoDevelop.BuildInfo.CompatVersion,
            Category = "MonoDevelop Core")]

[assembly:AddinName ("MonoDevelop Ide")]
[assembly:AddinDescription ("The MonoDevelop IDE application")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
