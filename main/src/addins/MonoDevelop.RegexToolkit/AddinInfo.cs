
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("RegexToolkit", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "IDE extensions")]

[assembly:AddinName ("Regex Toolkit")]
[assembly:AddinDescription ("Provides a testing workbench for regular expressions")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
