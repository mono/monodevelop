
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Autotools", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "Project Import and Export")]

[assembly:AddinName ("Makefile generation")]
[assembly:AddinDescription ("Allows generating simple makefiles and Autotools based makefiles for projects")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Deployment", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
