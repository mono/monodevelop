
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Core.Tests.Addin",
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "IDE extensions")]

[assembly:AddinName ("MonoDevelop.Core.Tests addin")]
[assembly:AddinDescription ("Test addin for MonoDevelop.Core.Tests")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
