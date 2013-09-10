
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("AspNet.Mvc", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Flags = AddinFlags.Hidden,
        Category = "Web Development")]

[assembly:AddinName ("ASP.NET MVC Support")]
[assembly:AddinDescription ("Support for ASP.NET MVC projects")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("AspNet", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("XmlEditor", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("SourceEditor2", MonoDevelop.BuildInfo.Version)]
