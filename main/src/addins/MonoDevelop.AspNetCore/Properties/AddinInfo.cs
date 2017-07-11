
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("AspNetCore", 
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "Web Development")]

[assembly:AddinName ("ASP.NET Core Support")]
[assembly:AddinDescription ("Adds support for building and running ASP.NET Core projects")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DotNetCore", MonoDevelop.BuildInfo.Version)]
