
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("AssemblyBrowser", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "IDE extensions")]

[assembly:AddinName ("MonoDevelop Assembly Browser")]
[assembly:AddinDescription ("Provides an assembly browser for MonoDevelop")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Refactoring", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("CSharpBinding", MonoDevelop.BuildInfo.Version)]
