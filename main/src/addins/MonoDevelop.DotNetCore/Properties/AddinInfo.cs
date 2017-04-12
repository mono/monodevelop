
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("DotNetCore", 
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "Language bindings")]

[assembly:AddinName (".NET Core Support")]
[assembly:AddinDescription ("Adds support for building and running .NET Core projects")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger.VsCodeDebugProtocol", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("PackageManagement", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("UnitTesting", MonoDevelop.BuildInfo.Version)]
