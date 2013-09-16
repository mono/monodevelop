
using System;
using Mono.Addins;

[assembly:Addin ("DocFood", 
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "IDE extensions")]

[assembly:AddinName ("DocFood")]
[assembly:AddinDescription ("DocFood is an automated comment generator")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Refactoring", MonoDevelop.BuildInfo.Version)]
