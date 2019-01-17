﻿
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("PackageManagement", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
	Category = "IDE extensions")]

[assembly:AddinName ("NuGet Package Management")]
[assembly:AddinDescription ("Provides support for adding and maintaining NuGet packages in your project.")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Refactoring", MonoDevelop.BuildInfo.Version)]
