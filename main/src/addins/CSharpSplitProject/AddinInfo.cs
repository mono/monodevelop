using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("C# Split Project",
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "IDE Extensions")]

[assembly:AddinName ("C# Split Project")]
[assembly:AddinDescription ("C# Split Project")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Refactoring", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("CSharpBinding", MonoDevelop.BuildInfo.Version)]
