using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("TextEditor.Cocoa", 
        Namespace = "MonoDevelop", 
        Version = MonoDevelop.BuildInfo.Version,
        Category = "MonoDevelop Core")]

[assembly:AddinName ("MonoDevelop Text Editor")]
[assembly:AddinDescription ("Integrates the Visual Studio text editor")]
[assembly: AddinFlags (AddinFlags.Hidden)]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
