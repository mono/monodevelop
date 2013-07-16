
using System;
using Mono.Addins;
using Mono.Addins.Description;

[Addin ("SourceEditor2", 
        Namespace = "MonoDevelop", 
        Version = MonoDevelop.BuildInfo.Version,
        Category = "MonoDevelop Core",
        Flags = AddinFlags.Hidden)]

[AddinName ("MonoDevelop Source Editor")]
[AddinDescription ("Provides a text editor for the MonoDevelop based on Mono.TextEditor")]

[AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
