
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("SourceEditor2", 
        Namespace = "MonoDevelop", 
        Version = MonoDevelop.BuildInfo.Version,
        Category = "MonoDevelop Core",
        Flags = AddinFlags.Hidden)]

[assembly:AddinName ("MonoDevelop Source Editor")]
[assembly:AddinDescription ("Provides a text editor for the MonoDevelop based on Mono.TextEditor")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
