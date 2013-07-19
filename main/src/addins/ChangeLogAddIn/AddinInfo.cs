
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("ChangeLogAddIn", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Flags = AddinFlags.Hidden,
        Category = "Version Control")]

[assembly:AddinName ("ChangeLog Add-in")]
[assembly:AddinDescription ("Add-in for working with ChangeLog files")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VersionControl", MonoDevelop.BuildInfo.Version)]
