
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Prj2Make", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "Project Import and Export")]

[assembly:AddinName ("Visual Studio .NET Project Support")]
[assembly:AddinDescription ("Importer for VS2003 projects")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("CSharpBinding", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("VBBinding", MonoDevelop.BuildInfo.Version)]
