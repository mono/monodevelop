
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("Web", 
                 Namespace = "MonoDevelop",
                 Version = MonoDevelop.BuildInfo.Version,
                 Category = "Web Development")]

[assembly:AddinName ("Web Project Support")]
[assembly:AddinDescription ("Support for ASP.NET projects, including editing, compiling and previewing")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("XmlEditor", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]
