
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("AspNet", 
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "Web Development")]

[assembly:AddinName ("ASP.NET Project Support")]
[assembly:AddinDescription ("Support for ASP.NET projects, including editing, compiling, previewing and deploying to remote servers")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Deployment", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("XmlEditor", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]
